using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Application.Common.Financial;

/// <summary>Per-loan facts the calculator needs beyond the ledger rows themselves — the current aggregate Balance (for the ledger-drift check) and write-off state (Gross Receivables excludes a written-off loan from its write-off date on).</summary>
/// <param name="WriteOffDate">Business-local date of the loan's WrittenOff audit row, already converted from its UTC timestamp by the caller. Null on a written-off loan means no audit row was found.</param>
public sealed record CapitalGrowthLoanInfo(LoanId LoanId, string LoanNumber, decimal CurrentBalance, bool IsWrittenOff, DateOnly? WriteOffDate);

public enum CapitalGrowthMonthStatus
{
    /// <summary>A fully elapsed month entirely inside recorded history.</summary>
    Complete,
    /// <summary>The current month — ending figures are as of today, not a month-end estimate.</summary>
    PartialMonth,
    /// <summary>Recorded history begins inside this month, so its beginning position is "nothing recorded yet", not a verified opening balance.</summary>
    PartialHistory,
    /// <summary>The whole month precedes the first ledger entry — amounts are unavailable, never zero.</summary>
    NoHistory,
}

public sealed record CapitalGrowthMonth(
    int Year,
    int Month,
    DateOnly Start,
    DateOnly EndCutoff,
    bool IsPartialMonth,
    CapitalGrowthMonthStatus Status,
    decimal? BeginningCash,
    decimal? EndingCash,
    decimal? BeginningGrossReceivables,
    decimal? EndingGrossReceivables,
    decimal? BeginningPosition,
    decimal? EndingPosition,
    decimal? OwnerWithdrawals,
    decimal? OwnerDeposits,
    decimal? PositionChange,
    decimal? AdjustedGrowth,
    decimal? GrowthPercent)
{
    public string MonthKey => $"{Year:D4}-{Month:D2}";
}

public sealed record CapitalGrowthSummary(
    decimal? TotalGrowth,
    int MonthsInTotal,
    decimal? AverageCompletedMonthGrowth,
    int CompletedMonthCount,
    decimal? AverageGrowthIncludingCurrentMonth,
    CapitalGrowthMonth? BestCompletedMonth,
    decimal? PreviousPeriodGrowth,
    decimal? ChangeVsPreviousPeriodPercent);

public sealed record CapitalGrowthWarning(string Code, string Message, IReadOnlyList<string> LoanNumbers);

public sealed record CapitalGrowthResult(
    DateOnly AsOfDate,
    DateOnly? HistoryStartDate,
    IReadOnlyList<CapitalGrowthMonth> Months,
    CapitalGrowthSummary Summary,
    IReadOnlyList<CapitalGrowthWarning> Warnings);

/// <summary>
/// Monthly Capital Growth (Monthly_Capital_Growth requirements §3–§5, §8):
/// reconstructs Total Business Position (Cash on Hand + Gross Receivables) at
/// every business-local month boundary and derives Adjusted Capital Growth =
/// (Ending − Beginning) + Owner Withdrawals − Owner Deposits. Pure function,
/// no I/O — same contract as FinancialCalculations: the query handler loads
/// the data, this does the math.
///
/// Each ledger feeds exactly one metric — cash_ledger → Cash on Hand,
/// loan_ledger → Gross Receivables — so a payment (mirrored in both) moves
/// cash up and receivables down once each, never twice. Per-loan receivable
/// at a cutoff is max(0, Σ Debit − Credit through the cutoff), matching
/// Loan.Balance's own overpayment clamp (Money.Subtract), so "today" here
/// reconciles with the dashboard's Gross Receivables KPI.
///
/// Ledger dates are business-local DateOnly values already, so a cutoff is
/// "every entry with TransactionDate &lt;= cutoff" with no timezone math —
/// equivalent to the half-open [monthStart, nextMonthStart) interval. Because
/// edits/deletes revise ledger rows in place, every historical month is a
/// restatement from today's records, not an audit-exact replay.
/// </summary>
public static class CapitalGrowthCalculator
{
    public static readonly IReadOnlySet<int> AllowedMonthRanges = new HashSet<int> { 6, 12, 24 };

    public static CapitalGrowthResult Compute(
        IReadOnlyCollection<CashLedgerEntry> cashEntries,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        IReadOnlyCollection<CapitalGrowthLoanInfo> loans,
        DateOnly today,
        int months)
    {
        if (months <= 0)
            throw new ArgumentOutOfRangeException(nameof(months), "Month count must be positive.");

        var warnings = new List<CapitalGrowthWarning>();
        var writeOffDates = ResolveWriteOffDates(loans, loanLedgerEntries, today, warnings);

        // Computed over twice the displayed range so the "vs previous period"
        // comparison comes out of the same single sweep.
        var allMonths = BuildMonths(cashEntries, loanLedgerEntries, writeOffDates, today, months * 2);
        var displayed = allMonths.Skip(months).ToList();
        var previous = allMonths.Take(months).ToList();

        AddLedgerDriftWarning(loans, loanLedgerEntries, warnings);
        AddFutureDatedWarning(cashEntries, loanLedgerEntries, today, warnings);
        AddNegativeCashWarning(displayed, warnings);
        AddUnknownCashTypeWarning(cashEntries, warnings);
        AddInvariantWarnings(displayed, warnings);

        var historyStart = EarliestDate(cashEntries, loanLedgerEntries);
        return new CapitalGrowthResult(today, historyStart, displayed, Summarize(displayed, previous), warnings);
    }

    /// <summary>
    /// Reconstructed (Cash on Hand, Gross Receivables) at the end of one
    /// business date, using the exact same per-ledger rules as the monthly
    /// series — for reconciling against a Financial Snapshot captured that day.
    /// </summary>
    public static (decimal Cash, decimal GrossReceivables) ComputePositionAt(
        IReadOnlyCollection<CashLedgerEntry> cashEntries,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        IReadOnlyCollection<CapitalGrowthLoanInfo> loans,
        DateOnly cutoff,
        DateOnly today)
    {
        var writeOffDates = ResolveWriteOffDates(loans, loanLedgerEntries, today, new List<CapitalGrowthWarning>());
        var cash = cashEntries.Where(e => e.TransactionDate <= cutoff).Sum(e => e.SignedAmount);
        var gross = loanLedgerEntries
            .Where(e => e.TransactionDate <= cutoff)
            .GroupBy(e => e.LoanId)
            .Where(g => !(writeOffDates.TryGetValue(g.Key, out var writtenOff) && writtenOff <= cutoff))
            .Sum(g => Math.Max(g.Sum(e => e.SignedAmount), 0m));
        return (cash, gross);
    }

    private static List<CapitalGrowthMonth> BuildMonths(
        IReadOnlyCollection<CashLedgerEntry> cashEntries,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        IReadOnlyDictionary<LoanId, DateOnly> writeOffDates,
        DateOnly today,
        int monthCount)
    {
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var firstMonthStart = currentMonthStart.AddMonths(-(monthCount - 1));
        var historyStart = EarliestDate(cashEntries, loanLedgerEntries);

        // Entries dated after today are excluded from every cutoff — the
        // series never reaches past "as of now". See AddFutureDatedWarning.
        var cash = cashEntries.Where(e => e.TransactionDate <= today).OrderBy(e => e.TransactionDate).ToList();
        var loanRows = loanLedgerEntries.Where(e => e.TransactionDate <= today).OrderBy(e => e.TransactionDate).ToList();

        var cashIndex = 0;
        var loanIndex = 0;
        var cashBalance = 0m;
        var loanBalances = new Dictionary<LoanId, decimal>();

        // Advances both ledgers through `cutoff` (inclusive), returning the
        // owner deposits/withdrawals crossed on the way.
        (decimal Withdrawals, decimal Deposits) AdvanceTo(DateOnly cutoff)
        {
            decimal withdrawals = 0m, deposits = 0m;
            for (; cashIndex < cash.Count && cash[cashIndex].TransactionDate <= cutoff; cashIndex++)
            {
                var entry = cash[cashIndex];
                cashBalance += entry.SignedAmount;
                if (entry.TransactionType == CashTransactionType.OwnerWithdrawal) withdrawals += entry.Amount.Amount;
                else if (entry.TransactionType == CashTransactionType.OwnerDeposit) deposits += entry.Amount.Amount;
            }
            for (; loanIndex < loanRows.Count && loanRows[loanIndex].TransactionDate <= cutoff; loanIndex++)
            {
                var row = loanRows[loanIndex];
                loanBalances[row.LoanId] = loanBalances.GetValueOrDefault(row.LoanId) + row.SignedAmount;
            }
            return (withdrawals, deposits);
        }

        decimal GrossAt(DateOnly cutoff) => loanBalances
            .Where(kv => !(writeOffDates.TryGetValue(kv.Key, out var writtenOff) && writtenOff <= cutoff))
            .Sum(kv => Math.Max(kv.Value, 0m));

        var openingCutoff = firstMonthStart.AddDays(-1);
        AdvanceTo(openingCutoff);
        var beginningCash = cashBalance;
        var beginningGross = GrossAt(openingCutoff);

        var result = new List<CapitalGrowthMonth>(monthCount);
        for (var i = 0; i < monthCount; i++)
        {
            var start = firstMonthStart.AddMonths(i);
            var isCurrent = start == currentMonthStart;
            var endCutoff = isCurrent ? today : start.AddMonths(1).AddDays(-1);

            var (withdrawals, deposits) = AdvanceTo(endCutoff);
            var endingCash = cashBalance;
            var endingGross = GrossAt(endCutoff);

            var status = historyStart is not { } hs || hs > endCutoff ? CapitalGrowthMonthStatus.NoHistory
                : hs >= start ? CapitalGrowthMonthStatus.PartialHistory
                : isCurrent ? CapitalGrowthMonthStatus.PartialMonth
                : CapitalGrowthMonthStatus.Complete;

            if (status == CapitalGrowthMonthStatus.NoHistory)
            {
                result.Add(new CapitalGrowthMonth(start.Year, start.Month, start, endCutoff, isCurrent, status,
                    null, null, null, null, null, null, null, null, null, null, null));
            }
            else
            {
                var beginningPosition = beginningCash + beginningGross;
                var endingPosition = endingCash + endingGross;
                var change = endingPosition - beginningPosition;
                var growth = change + withdrawals - deposits;
                decimal? growthPercent = beginningPosition > 0 ? Math.Round(growth / beginningPosition * 100, 2, MidpointRounding.AwayFromZero) : null;

                result.Add(new CapitalGrowthMonth(start.Year, start.Month, start, endCutoff, isCurrent, status,
                    beginningCash, endingCash, beginningGross, endingGross, beginningPosition, endingPosition,
                    withdrawals, deposits, change, growth, growthPercent));
            }

            beginningCash = endingCash;
            beginningGross = endingGross;
        }

        return result;
    }

    /// <summary>
    /// Totals and averages count only months with trustworthy beginning
    /// balances (Complete, plus the current PartialMonth where noted) —
    /// PartialHistory months still render as bars but are kept out of the
    /// summary, since their "beginning position" is just the absence of
    /// earlier records, not a verified opening balance.
    /// </summary>
    private static CapitalGrowthSummary Summarize(List<CapitalGrowthMonth> displayed, List<CapitalGrowthMonth> previous)
    {
        var completed = displayed.Where(m => m.Status == CapitalGrowthMonthStatus.Complete).ToList();
        var withCurrent = displayed.Where(m => m.Status is CapitalGrowthMonthStatus.Complete or CapitalGrowthMonthStatus.PartialMonth).ToList();

        decimal? total = withCurrent.Count > 0 ? withCurrent.Sum(m => m.AdjustedGrowth!.Value) : null;
        decimal? avgCompleted = completed.Count > 0 ? Math.Round(completed.Average(m => m.AdjustedGrowth!.Value), 2, MidpointRounding.AwayFromZero) : null;
        decimal? avgWithCurrent = withCurrent.Count > 0 ? Math.Round(withCurrent.Average(m => m.AdjustedGrowth!.Value), 2, MidpointRounding.AwayFromZero) : null;

        // Earliest month wins a tie — deterministic, and the first month to reach a figure is the one that "set" it.
        var best = completed.Count > 0 ? completed.Aggregate((a, b) => b.AdjustedGrowth > a.AdjustedGrowth ? b : a) : null;

        // D3: a prior-period comparison only when every prior month is a
        // completed, fully-recorded month — otherwise the comparison base is
        // partly unknown and the percentage would be misleading.
        decimal? previousTotal = previous.Count > 0 && previous.All(m => m.Status == CapitalGrowthMonthStatus.Complete)
            ? previous.Sum(m => m.AdjustedGrowth!.Value)
            : null;
        decimal? changeVsPrevious = total is { } t && previousTotal is { } p && p > 0
            ? Math.Round((t - p) / p * 100, 1, MidpointRounding.AwayFromZero)
            : null;

        return new CapitalGrowthSummary(total, withCurrent.Count, avgCompleted, completed.Count, avgWithCurrent, best, previousTotal, changeVsPrevious);
    }

    /// <summary>
    /// A written-off loan leaves Gross Receivables from its write-off date
    /// on. The date comes from the WrittenOff audit row; when that row is
    /// missing, fall back to the loan's last ledger date (the latest moment
    /// it can be shown to still be carried) and flag it.
    /// </summary>
    private static Dictionary<LoanId, DateOnly> ResolveWriteOffDates(
        IReadOnlyCollection<CapitalGrowthLoanInfo> loans,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        DateOnly today,
        List<CapitalGrowthWarning> warnings)
    {
        var dates = new Dictionary<LoanId, DateOnly>();
        var missing = new List<string>();
        var lastLedgerDate = loanLedgerEntries
            .GroupBy(e => e.LoanId)
            .ToDictionary(g => g.Key, g => g.Max(e => e.TransactionDate));

        foreach (var loan in loans.Where(l => l.IsWrittenOff))
        {
            if (loan.WriteOffDate is { } date)
            {
                dates[loan.LoanId] = date;
            }
            else
            {
                dates[loan.LoanId] = lastLedgerDate.TryGetValue(loan.LoanId, out var last) ? last : today;
                missing.Add(loan.LoanNumber);
            }
        }

        if (missing.Count > 0)
            warnings.Add(new CapitalGrowthWarning("writeOffDateUnknown",
                "Some written-off loans have no write-off audit record; they are treated as written off from their last ledger date.",
                missing));

        return dates;
    }

    /// <summary>Requirements §8 — today's ledger-derived balance should equal the Loan aggregate's own Balance. A mismatch means history for that loan can't be trusted.</summary>
    private static void AddLedgerDriftWarning(
        IReadOnlyCollection<CapitalGrowthLoanInfo> loans,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        List<CapitalGrowthWarning> warnings)
    {
        var ledgerTotals = loanLedgerEntries
            .GroupBy(e => e.LoanId)
            .ToDictionary(g => g.Key, g => Math.Max(g.Sum(e => e.SignedAmount), 0m));

        var drifted = loans
            .Where(l => ledgerTotals.GetValueOrDefault(l.LoanId) != l.CurrentBalance)
            .Select(l => l.LoanNumber)
            .ToList();

        if (drifted.Count > 0)
            warnings.Add(new CapitalGrowthWarning("ledgerBalanceMismatch",
                "Some loans' ledger history doesn't add up to their current balance; their historical receivables may be inaccurate.",
                drifted));
    }

    private static void AddFutureDatedWarning(
        IReadOnlyCollection<CashLedgerEntry> cashEntries,
        IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries,
        DateOnly today,
        List<CapitalGrowthWarning> warnings)
    {
        var futureCash = cashEntries.Count(e => e.TransactionDate > today);
        var futureLoan = loanLedgerEntries.Count(e => e.TransactionDate > today);
        if (futureCash + futureLoan > 0)
            warnings.Add(new CapitalGrowthWarning("futureDatedEntries",
                $"{futureCash + futureLoan} ledger entr{(futureCash + futureLoan == 1 ? "y is" : "ies are")} dated after today. The dashboard KPIs include them; month-to-date figures here do not.",
                Array.Empty<string>()));
    }

    private static void AddNegativeCashWarning(List<CapitalGrowthMonth> months, List<CapitalGrowthWarning> warnings)
    {
        var negative = months.Where(m => m.EndingCash < 0).Select(m => m.MonthKey).ToList();
        if (negative.Count > 0)
            warnings.Add(new CapitalGrowthWarning("negativeCash",
                $"Cash on hand was negative at the end of {string.Join(", ", negative)} — likely a missing or misdated cash entry.",
                Array.Empty<string>()));
    }

    private static void AddUnknownCashTypeWarning(IReadOnlyCollection<CashLedgerEntry> cashEntries, List<CapitalGrowthWarning> warnings)
    {
        var unknown = cashEntries.Count(e => !Enum.IsDefined(e.TransactionType));
        if (unknown > 0)
            warnings.Add(new CapitalGrowthWarning("unknownCashType",
                $"{unknown} cash ledger entr{(unknown == 1 ? "y has" : "ies have")} an unrecognized transaction type.",
                Array.Empty<string>()));
    }

    /// <summary>Requirements §8's three identities, checked per month. They hold by construction today; this guards against a future change breaking them silently.</summary>
    private static void AddInvariantWarnings(List<CapitalGrowthMonth> months, List<CapitalGrowthWarning> warnings)
    {
        var broken = months
            .Where(m => m.Status != CapitalGrowthMonthStatus.NoHistory)
            .Where(m =>
                m.EndingPosition - m.BeginningPosition != m.PositionChange ||
                m.PositionChange + m.OwnerWithdrawals - m.OwnerDeposits != m.AdjustedGrowth ||
                m.EndingPosition != m.EndingCash + m.EndingGrossReceivables)
            .Select(m => m.MonthKey)
            .ToList();

        if (broken.Count > 0)
            warnings.Add(new CapitalGrowthWarning("reconciliationFailed",
                $"Internal reconciliation failed for {string.Join(", ", broken)}.",
                Array.Empty<string>()));
    }

    private static DateOnly? EarliestDate(IReadOnlyCollection<CashLedgerEntry> cashEntries, IReadOnlyCollection<LoanLedgerEntry> loanLedgerEntries)
    {
        DateOnly? earliest = null;
        if (cashEntries.Count > 0) earliest = cashEntries.Min(e => e.TransactionDate);
        if (loanLedgerEntries.Count > 0)
        {
            var loanMin = loanLedgerEntries.Min(e => e.TransactionDate);
            if (earliest is null || loanMin < earliest) earliest = loanMin;
        }
        return earliest;
    }
}

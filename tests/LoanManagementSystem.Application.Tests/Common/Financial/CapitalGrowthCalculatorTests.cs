using System.Diagnostics;
using LoanManagementSystem.Application.Common.Financial;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Common.Financial;

/// <summary>Monthly Capital Growth requirements §9 scenarios, against hand-computed expectations.</summary>
public class CapitalGrowthCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);

    private readonly List<CashLedgerEntry> _cash = new();
    private readonly List<LoanLedgerEntry> _loanRows = new();
    private readonly List<CapitalGrowthLoanInfo> _loans = new();

    // ---- fixture helpers -------------------------------------------------

    private void Deposit(decimal amount, DateOnly date) => _cash.Add(CashLedgerEntry.Record(CashTransactionType.OwnerDeposit, Money.Of(amount), "", date));
    private void Withdraw(decimal amount, DateOnly date) => _cash.Add(CashLedgerEntry.Record(CashTransactionType.OwnerWithdrawal, Money.Of(amount), "", date));
    private void Expense(decimal amount, DateOnly date) => _cash.Add(CashLedgerEntry.Record(CashTransactionType.Expense, Money.Of(amount), "", date));

    /// <summary>Mirrors LoanCreatedEventHandler: a cash `loan_release` out plus LoanReleased/InterestAdded loan_ledger debits.</summary>
    private LoanId Release(decimal principal, decimal interest, DateOnly date, string number = "LM-001", bool isWrittenOff = false, DateOnly? writeOffDate = null)
    {
        var id = LoanId.New();
        _cash.Add(CashLedgerEntry.Record(CashTransactionType.LoanRelease, Money.Of(principal), "", date, referenceId: number));
        _loanRows.Add(LoanLedgerEntry.Record(id, LoanLedgerTransactionType.LoanReleased, Money.Of(principal), Money.Zero, Money.Zero, "", date));
        _loanRows.Add(LoanLedgerEntry.Record(id, LoanLedgerTransactionType.InterestAdded, Money.Of(interest), Money.Zero, Money.Zero, "", date));
        _loans.Add(new CapitalGrowthLoanInfo(id, number, principal + interest, isWrittenOff, writeOffDate));
        return id;
    }

    /// <summary>Mirrors PaymentRecordedEventHandler: a cash `payment_received` in plus a loan_ledger Payment credit.</summary>
    private void Pay(LoanId loanId, decimal amount, DateOnly date)
    {
        _cash.Add(CashLedgerEntry.Record(CashTransactionType.PaymentReceived, Money.Of(amount), "", date));
        _loanRows.Add(LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(amount), Money.Zero, "", date));
        AdjustBalance(loanId, -amount);
    }

    /// <summary>Extension charge: loan_ledger debit only — no cash moves.</summary>
    private void Extend(LoanId loanId, decimal charge, DateOnly date)
    {
        _loanRows.Add(LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.Extension, Money.Of(charge), Money.Zero, Money.Zero, "", date));
        AdjustBalance(loanId, charge);
    }

    private void AdjustBalance(LoanId loanId, decimal delta)
    {
        var i = _loans.FindIndex(l => l.LoanId == loanId);
        _loans[i] = _loans[i] with { CurrentBalance = Math.Max(_loans[i].CurrentBalance + delta, 0) };
    }

    private CapitalGrowthResult Compute(int months = 12, DateOnly? today = null) =>
        CapitalGrowthCalculator.Compute(_cash, _loanRows, _loans, today ?? Today, months);

    private static CapitalGrowthMonth MonthOf(CapitalGrowthResult r, int year, int month) =>
        r.Months.Single(m => m.Year == year && m.Month == month);

    // ---- §9.1 + §3.2 worked example -------------------------------------

    [Fact]
    public void SimpleMonth_ReproducesTheRequirementsWorkedExample()
    {
        Deposit(1_441_481m, new DateOnly(2026, 8, 1));

        var loan = Release(100_000m, 3_000m, new DateOnly(2026, 9, 3)); // +3,000 position
        Pay(loan, 42_731m, new DateOnly(2026, 9, 10));                   // 0 position
        Extend(loan, 42_731m, new DateOnly(2026, 9, 20));                // +42,731 position
        Withdraw(30_000m, new DateOnly(2026, 9, 25));                    // -30,000 position

        var sep = MonthOf(Compute(), 2026, 9);

        Assert.Equal(CapitalGrowthMonthStatus.Complete, sep.Status);
        Assert.Equal(1_441_481m, sep.BeginningPosition);
        Assert.Equal(1_457_212m, sep.EndingPosition);
        Assert.Equal(15_731m, sep.PositionChange);
        Assert.Equal(30_000m, sep.OwnerWithdrawals);
        Assert.Equal(0m, sep.OwnerDeposits);
        Assert.Equal(45_731m, sep.AdjustedGrowth);
        Assert.Equal(3.17m, sep.GrowthPercent);
        Assert.Equal(sep.EndingPosition, sep.EndingCash + sep.EndingGrossReceivables);
    }

    // ---- §9.2 / §9.3 / §9.4 owner movements -----------------------------

    [Fact]
    public void OwnerMovements_MultipleOfEach_AreNettedExactlyOnce()
    {
        Deposit(10_000m, new DateOnly(2026, 7, 1));
        Deposit(1_000m, new DateOnly(2026, 8, 2));
        Deposit(500m, new DateOnly(2026, 8, 9));
        Withdraw(200m, new DateOnly(2026, 8, 12));
        Withdraw(300m, new DateOnly(2026, 8, 30));

        var aug = MonthOf(Compute(), 2026, 8);

        Assert.Equal(1_500m, aug.OwnerDeposits);
        Assert.Equal(500m, aug.OwnerWithdrawals);
        Assert.Equal(1_000m, aug.PositionChange);
        Assert.Equal(0m, aug.AdjustedGrowth); // owner money in/out isn't growth
    }

    [Fact]
    public void PositionDeclines_ButGrowthIsPositive_WhenOwnerWithdrewMoreThanTheDecline()
    {
        Deposit(50_000m, new DateOnly(2026, 7, 1));
        Release(10_000m, 600m, new DateOnly(2026, 8, 5)); // +600
        Withdraw(2_000m, new DateOnly(2026, 8, 20));      // -2,000

        var aug = MonthOf(Compute(), 2026, 8);

        Assert.Equal(-1_400m, aug.PositionChange);
        Assert.Equal(600m, aug.AdjustedGrowth);
    }

    [Fact]
    public void DepositOnlyMonth_HasZeroAdjustedGrowth()
    {
        Deposit(50_000m, new DateOnly(2026, 7, 1));
        Deposit(25_000m, new DateOnly(2026, 8, 14));

        var aug = MonthOf(Compute(), 2026, 8);

        Assert.Equal(25_000m, aug.PositionChange);
        Assert.Equal(0m, aug.AdjustedGrowth);
    }

    // ---- §9.5 growth % availability -------------------------------------

    [Fact]
    public void GrowthPercent_IsNull_WhenBeginningPositionIsZeroOrNegative()
    {
        // History starts in June with a zero-sum pair → July begins at exactly 0.
        Deposit(1_000m, new DateOnly(2026, 6, 1));
        Withdraw(1_000m, new DateOnly(2026, 6, 2));
        Deposit(5_000m, new DateOnly(2026, 7, 3));
        // August: an expense larger than cash → September begins negative.
        Expense(9_000m, new DateOnly(2026, 8, 3));
        Deposit(10_000m, new DateOnly(2026, 9, 3));

        var result = Compute();

        Assert.Equal(0m, MonthOf(result, 2026, 7).BeginningPosition);
        Assert.Null(MonthOf(result, 2026, 7).GrowthPercent);
        Assert.Equal(-4_000m, MonthOf(result, 2026, 9).BeginningPosition);
        Assert.Null(MonthOf(result, 2026, 9).GrowthPercent);
        Assert.Contains(result.Warnings, w => w.Code == "negativeCash");
    }

    // ---- §9.6 write-offs, overpayment, backdating ------------------------

    [Fact]
    public void WrittenOffLoan_StaysInReceivablesUntilItsWriteOffDate()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        Release(10_000m, 300m, new DateOnly(2026, 7, 1), isWrittenOff: true, writeOffDate: new DateOnly(2026, 9, 10));

        var result = Compute();

        Assert.Equal(10_300m, MonthOf(result, 2026, 8).EndingGrossReceivables);
        Assert.Equal(0m, MonthOf(result, 2026, 9).EndingGrossReceivables);
        Assert.Equal(-10_300m, MonthOf(result, 2026, 9).AdjustedGrowth);
        Assert.DoesNotContain(result.Warnings, w => w.Code == "writeOffDateUnknown");
    }

    [Fact]
    public void WrittenOffLoan_WithoutAuditDate_FallsBackToLastLedgerDate_AndWarns()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        var loan = Release(10_000m, 300m, new DateOnly(2026, 7, 1), number: "LM-009", isWrittenOff: true);
        Pay(loan, 300m, new DateOnly(2026, 8, 20));

        var result = Compute();

        Assert.Equal(10_300m, MonthOf(result, 2026, 7).EndingGrossReceivables);
        Assert.Equal(0m, MonthOf(result, 2026, 8).EndingGrossReceivables);
        var warning = Assert.Single(result.Warnings, w => w.Code == "writeOffDateUnknown");
        Assert.Equal(new[] { "LM-009" }, warning.LoanNumbers);
    }

    [Fact]
    public void Overpayment_ClampsTheLoanAtZero_LikeLoanBalanceDoes()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        var loan = Release(1_000m, 30m, new DateOnly(2026, 7, 1));
        Pay(loan, 1_100m, new DateOnly(2026, 8, 1));

        var aug = MonthOf(Compute(), 2026, 8);

        Assert.Equal(0m, aug.EndingGrossReceivables);
        Assert.Equal(50_100m, aug.EndingCash);
    }

    [Fact]
    public void BackdatedEntry_CountsInItsEffectiveMonth_RegardlessOfRecordingOrder()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        var loan = Release(1_000m, 30m, new DateOnly(2026, 7, 1));
        Pay(loan, 500m, new DateOnly(2026, 9, 5));
        Pay(loan, 200m, new DateOnly(2026, 8, 5)); // recorded later, dated earlier

        var result = Compute();

        Assert.Equal(830m, MonthOf(result, 2026, 8).EndingGrossReceivables);
        Assert.Equal(330m, MonthOf(result, 2026, 9).EndingGrossReceivables);
    }

    // ---- §9.7 mirrored entries ------------------------------------------

    [Fact]
    public void Payment_MirroredInBothLedgers_DoesNotChangePosition()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        var loan = Release(1_000m, 30m, new DateOnly(2026, 7, 1));
        Pay(loan, 500m, new DateOnly(2026, 8, 10));

        var aug = MonthOf(Compute(), 2026, 8);

        Assert.Equal(0m, aug.PositionChange);
        Assert.Equal(500m, aug.EndingCash - aug.BeginningCash);
        Assert.Equal(-500m, aug.EndingGrossReceivables - aug.BeginningGrossReceivables);
    }

    // ---- §9.8 empty and pre-history months ------------------------------

    [Fact]
    public void MonthsBeforeHistory_AreUnavailable_NotZero_AndNoActivityMonthsAreRealZeros()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 10));

        var result = Compute();

        var may = MonthOf(result, 2026, 5);
        Assert.Equal(CapitalGrowthMonthStatus.NoHistory, may.Status);
        Assert.Null(may.AdjustedGrowth);
        Assert.Null(may.EndingPosition);

        Assert.Equal(CapitalGrowthMonthStatus.PartialHistory, MonthOf(result, 2026, 6).Status);

        var jul = MonthOf(result, 2026, 7);
        Assert.Equal(CapitalGrowthMonthStatus.Complete, jul.Status);
        Assert.Equal(0m, jul.AdjustedGrowth);
        Assert.Equal(new DateOnly(2026, 6, 10), result.HistoryStartDate);
    }

    [Fact]
    public void NoLedgerData_EveryMonthIsNoHistory_AndSummaryIsEmpty()
    {
        var result = Compute();

        Assert.All(result.Months, m => Assert.Equal(CapitalGrowthMonthStatus.NoHistory, m.Status));
        Assert.Null(result.HistoryStartDate);
        Assert.Null(result.Summary.TotalGrowth);
        Assert.Null(result.Summary.BestCompletedMonth);
    }

    // ---- §9.9 calendar edges --------------------------------------------

    [Fact]
    public void YearRollover_ProducesChronologicalDecemberThenJanuary()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        Expense(100m, new DateOnly(2026, 12, 31));
        Expense(200m, new DateOnly(2027, 1, 1));

        var result = Compute(today: new DateOnly(2027, 1, 10));

        Assert.Equal("2026-12", result.Months[^2].MonthKey);
        Assert.Equal("2027-01", result.Months[^1].MonthKey);
        Assert.Equal(-100m, result.Months[^2].AdjustedGrowth);
        Assert.Equal(-200m, result.Months[^1].AdjustedGrowth);
    }

    [Fact]
    public void LeapYear_FebruaryCutoffIncludesThe29th()
    {
        Deposit(50_000m, new DateOnly(2027, 6, 1));
        Expense(100m, new DateOnly(2028, 2, 29));

        var feb = MonthOf(Compute(today: new DateOnly(2028, 3, 5)), 2028, 2);

        Assert.Equal(new DateOnly(2028, 2, 29), feb.EndCutoff);
        Assert.Equal(-100m, feb.AdjustedGrowth);
    }

    [Fact]
    public void CurrentMonth_IsPartial_EndsToday_AndExcludesFutureDatedEntries()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        Expense(100m, new DateOnly(2026, 10, 3));
        Expense(999m, new DateOnly(2026, 10, 20)); // after today

        var result = Compute();
        var oct = result.Months[^1];

        Assert.True(oct.IsPartialMonth);
        Assert.Equal(CapitalGrowthMonthStatus.PartialMonth, oct.Status);
        Assert.Equal(Today, oct.EndCutoff);
        Assert.Equal(-100m, oct.AdjustedGrowth);
        Assert.Contains(result.Warnings, w => w.Code == "futureDatedEntries");
    }

    // ---- §9.10 range size, data quality, performance --------------------

    [Fact]
    public void TwentyFourMonthRange_ReturnsTwentyFourChronologicalMonthsEndingThisMonth()
    {
        var result = Compute(months: 24);

        Assert.Equal(24, result.Months.Count);
        Assert.Equal("2024-11", result.Months[0].MonthKey);
        Assert.Equal("2026-10", result.Months[^1].MonthKey);
    }

    [Fact]
    public void LedgerThatDoesNotMatchLoanBalance_IsFlagged()
    {
        Deposit(50_000m, new DateOnly(2026, 6, 1));
        var loan = Release(1_000m, 30m, new DateOnly(2026, 7, 1), number: "LM-004");
        AdjustBalance(loan, -200m); // aggregate says 830, ledger says 1,030

        var warning = Assert.Single(Compute().Warnings, w => w.Code == "ledgerBalanceMismatch");
        Assert.Equal(new[] { "LM-004" }, warning.LoanNumbers);
    }

    [Fact]
    public void ProductionScaleLedger_ComputesWellUnderBudget()
    {
        var random = new Random(42);
        Deposit(10_000_000m, new DateOnly(2024, 1, 1));
        for (var i = 0; i < 2_500; i++)
        {
            var date = new DateOnly(2024, 1, 2).AddDays(random.Next(0, 1000));
            var loan = Release(1_000m, 30m, date, number: $"LM-{i:D4}");
            Pay(loan, 300m, date.AddDays(10));
            Pay(loan, 300m, date.AddDays(20));
        }

        var stopwatch = Stopwatch.StartNew();
        var result = Compute(months: 24);
        stopwatch.Stop();

        Assert.Equal(24, result.Months.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 1_000, $"Took {stopwatch.ElapsedMilliseconds} ms for {_cash.Count + _loanRows.Count} rows.");
    }

    // ---- summary cards ---------------------------------------------------

    [Fact]
    public void Summary_SeparatesCompletedMonthsFromTheCurrentPartialMonth()
    {
        Deposit(50_000m, new DateOnly(2024, 1, 1)); // prior 12-month window fully recorded
        Release(10_000m, 1_000m, new DateOnly(2026, 8, 5));  // Aug +1,000
        Release(10_000m, 3_000m, new DateOnly(2026, 9, 5));  // Sep +3,000
        Release(10_000m, 9_000m, new DateOnly(2026, 10, 5)); // Oct (MTD) +9,000

        var summary = Compute().Summary;

        Assert.Equal(13_000m, summary.TotalGrowth);
        Assert.Equal(12, summary.MonthsInTotal);
        Assert.Equal(11, summary.CompletedMonthCount);
        Assert.Equal(Math.Round(4_000m / 11, 2), summary.AverageCompletedMonthGrowth);
        Assert.Equal(Math.Round(13_000m / 12, 2), summary.AverageGrowthIncludingCurrentMonth);
        Assert.Equal("2026-09", summary.BestCompletedMonth!.MonthKey); // October's bigger figure is MTD, excluded
        Assert.Equal(0m, summary.PreviousPeriodGrowth);  // prior 12 months complete but flat
        Assert.Null(summary.ChangeVsPreviousPeriodPercent); // no % change off a zero base
    }

    [Fact]
    public void Summary_OmitsPreviousPeriodComparison_WhenPriorMonthsLackHistory()
    {
        Deposit(50_000m, new DateOnly(2026, 3, 1));

        var summary = Compute().Summary;

        Assert.Null(summary.PreviousPeriodGrowth);
        Assert.Null(summary.ChangeVsPreviousPeriodPercent);
    }

    // ---- §8 reconciliation with the existing KPI formula -----------------

    [Fact]
    public void CurrentMonthEndingPosition_EqualsTheDashboardKpiFormula()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1_000m), InterestRate.Default, new DateOnly(2026, 9, 1));
        loan.RecordPayment(Money.Of(300m), PaymentMethod.Cash, "", new DateOnly(2026, 9, 15));

        Deposit(5_000m, new DateOnly(2026, 8, 1));
        _cash.Add(CashLedgerEntry.Record(CashTransactionType.LoanRelease, Money.Of(1_000m), "", new DateOnly(2026, 9, 1)));
        _cash.Add(CashLedgerEntry.Record(CashTransactionType.PaymentReceived, Money.Of(300m), "", new DateOnly(2026, 9, 15)));
        _loanRows.Add(LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1_000m), Money.Zero, Money.Zero, "", new DateOnly(2026, 9, 1)));
        _loanRows.Add(LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, loan.TotalInterest, Money.Zero, Money.Zero, "", new DateOnly(2026, 9, 1)));
        _loanRows.Add(LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(300m), Money.Zero, "", new DateOnly(2026, 9, 15)));
        _loans.Add(new CapitalGrowthLoanInfo(loan.Id, "LM-001", loan.Balance.Amount, false, null));

        var result = Compute();
        var kpiPosition = FinancialCalculations.ComputeReceivables(new List<Loan> { loan }).Gross + FinancialCalculations.ComputeCashOnHand(_cash);

        Assert.Equal(kpiPosition, result.Months[^1].EndingPosition);
        Assert.Empty(result.Warnings);
    }
}

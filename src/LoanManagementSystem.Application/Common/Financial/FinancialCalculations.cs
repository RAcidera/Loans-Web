using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Application.Common.Financial;

/// <summary>
/// Shared pure-function financial formulas, extracted out of
/// GetDashboardSummaryQueryHandler and GetCashSummaryQueryHandler so the
/// Diary module's Financial Snapshot (requirements §9: "reuse the same
/// calculation services... do not duplicate financial formulas") calls the
/// exact same math instead of re-deriving it. No I/O here — callers fetch
/// the loans/entries/payments themselves (each already has its own
/// eager-loading needs) and pass them in.
/// </summary>
public static class FinancialCalculations
{
    /// <summary>Gross/Collectible/Bad Loan Receivables (requirements §9) — identical to GetDashboardSummaryQueryHandler's former private ComputeReceivables.</summary>
    public static (decimal Gross, decimal Collectible, decimal BadLoan) ComputeReceivables(IEnumerable<Loan> loans)
    {
        var loanList = loans as ICollection<Loan> ?? loans.ToList();
        var gross = loanList.Where(l => l.Status != LoanStatus.WrittenOff).Sum(l => l.Balance.Amount);
        var badLoan = loanList.Where(l => l.Classification == LoanClassification.BadLoan).Sum(l => l.Balance.Amount);
        return (gross, gross - badLoan, badLoan);
    }

    /// <summary>Cash on Hand (requirements §9: SUM(Cash In) - SUM(Cash Out)) — identical to GetCashSummaryQueryHandler's former inline sum, with an optional as-of cutoff so a snapshot captured for a specific moment only counts entries up to that date.</summary>
    public static decimal ComputeCashOnHand(IEnumerable<CashLedgerEntry> entries, DateOnly? asOfDate = null) =>
        asOfDate is { } cutoff
            ? entries.Where(e => e.TransactionDate <= cutoff).Sum(e => e.SignedAmount)
            : entries.Sum(e => e.SignedAmount);

    /// <summary>Collections Today / Collections Month to Date (requirements §9) — sum of payments received in [from, to].</summary>
    public static decimal ComputeCollections(IEnumerable<Payment> payments, DateOnly from, DateOnly to) =>
        payments.Where(p => p.PaymentDate >= from && p.PaymentDate <= to).Sum(p => p.AmountPaid.Amount);

    /// <summary>Loan Releases Today / Loan Releases Month to Date (requirements §9) — sum of principal for loans originated in [from, to].</summary>
    public static decimal ComputeLoanReleases(IEnumerable<Loan> loans, DateOnly from, DateOnly to) =>
        loans.Where(l => l.StartDate >= from && l.StartDate <= to).Sum(l => l.PrincipalAmount.Amount);
}

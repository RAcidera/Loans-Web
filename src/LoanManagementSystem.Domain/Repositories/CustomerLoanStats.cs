namespace LoanManagementSystem.Domain.Repositories;

/// <summary>
/// Per-customer loan figures shown on the Customers list grid.
/// <see cref="OverdueLoanCount"/> uses the same rule as the Loans list's
/// Overdue filter (DueDate before the business-local "today", and the loan
/// neither Paid nor WrittenOff) — see ILoanRepository.GetPageAsync — so the
/// two screens always agree. <see cref="LastPaymentDate"/> is the most
/// recent PaymentDate across all of the customer's loans (null = never paid).
/// </summary>
public readonly record struct CustomerLoanStats(
    int LoanCount,
    decimal OutstandingBalance,
    int OverdueLoanCount,
    DateOnly? LastPaymentDate);

namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Spec's "Dashboard Receivable Calculations" + "Dashboard Summary Cards" —
/// Gross Receivables excludes Written Off loans; Bad Loan Receivables sums
/// balance where Classification = BadLoan; Collectible Receivables is
/// Gross minus Bad Loan.
/// </summary>
public sealed record DashboardReceivablesDto(
    decimal GrossReceivables,
    decimal CollectibleReceivables,
    decimal BadLoanReceivables,
    int ActiveLoansCount,
    int OverdueLoansCount,
    int WrittenOffLoansCount,
    int LoansDueThisWeekCount
);

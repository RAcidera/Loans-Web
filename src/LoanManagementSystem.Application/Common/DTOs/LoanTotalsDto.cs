namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Spec's "Loan Grid Footer Totals" plus the Loans list's KPI strip — both summed/counted across the whole filtered result set, not just the visible page.</summary>
public sealed record LoanTotalsDto(
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalExtensionCharges,
    decimal TotalPayments,
    decimal TotalOutstandingBalance,
    int TotalLoansCount,
    int ActiveLoansCount,
    int OverdueLoansCount,
    int PaidLoansCount
);

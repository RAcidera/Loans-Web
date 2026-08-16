namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>The Customers list's KPI strip — counted/summed across the whole filtered result set, not just the visible page.</summary>
public sealed record CustomerTotalsDto(
    int TotalCustomersCount,
    int ActiveCustomersCount,
    int InactiveCustomersCount,
    int TotalLoansCount,
    decimal TotalOutstandingBalance
);

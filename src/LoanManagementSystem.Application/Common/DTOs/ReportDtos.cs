namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>SRS 3.5 "total interest earned" — a gap the backend didn't cover until now.</summary>
public sealed record InterestSummaryDto(decimal TotalInterestEarned);

/// <summary>SRS 3.5 "summaries per customer" — one row per customer.</summary>
public sealed record CustomerSummaryDto(
    string CustomerId,
    string CustomerName,
    decimal TotalBorrowed,
    decimal TotalPaid,
    int LoansCount
);

/// <summary>SRS 3.5 "summaries per period" — everything scoped to [StartDate, EndDate].</summary>
public sealed record PeriodSummaryDto(
    string StartDate,
    string EndDate,
    int LoansOriginated,
    decimal PaymentsCollected,
    int ExtensionsGranted,
    decimal InterestEarned
);

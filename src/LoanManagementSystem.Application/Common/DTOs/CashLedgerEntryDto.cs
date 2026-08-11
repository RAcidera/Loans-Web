namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record CashLedgerEntryDto(
    string LedgerId,
    string TransactionDate,
    string TransactionType,
    string? ReferenceId,
    decimal Amount,
    string Remarks,
    string CreatedAt
);

/// <summary>Matches Angular's CashSummary — Formulas 1-5 from the SRS, precomputed server-side.</summary>
public sealed record CashSummaryDto(
    decimal TotalCashIn,
    decimal TotalCashOut,
    decimal CashOnHand,
    decimal RevolvingFunds,
    decimal OutstandingPrincipal,
    List<decimal> SevenDayTrend
);

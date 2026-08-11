namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record LoanLedgerEntryDto(
    string LedgerId,
    string LoanId,
    string TransactionDate,
    string TransactionType,
    string? ReferenceId,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance,
    string Remarks,
    string CreatedAt
);

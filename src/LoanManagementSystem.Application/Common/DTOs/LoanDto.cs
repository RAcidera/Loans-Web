namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Field names match src/app/domain/entities/loan.entity.ts on the Angular side.</summary>
public sealed record LoanDto(
    string LoanId,
    string LoanNumber,
    string CustomerId,
    string CustomerName,
    decimal PrincipalAmount,
    decimal InterestRate,
    string StartDate,
    string DueDate,
    decimal TotalInterest,
    decimal TotalExtensionCharges,
    decimal TotalAmountDue,
    decimal TotalPaid,
    decimal Balance,
    string Status,
    string Classification,
    string Remarks,
    string CreatedAt
);

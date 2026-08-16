namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record LoanExtensionDto(
    string ExtensionId,
    string LoanId,
    string ExtensionDate,
    int ExtensionDays,
    decimal AdditionalChargesAmount,
    string Remarks
);

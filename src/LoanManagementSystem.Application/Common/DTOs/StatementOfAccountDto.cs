namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Everything the SOA PDF needs, assembled once by GenerateLoanSoaQuery so
/// IStatementOfAccountPdfGenerator stays a pure rendering concern with no
/// repository access of its own — mirrors the DocumentFileDto precedent of
/// carrying content alongside metadata for a download endpoint.
/// </summary>
public sealed record StatementOfAccountDto(
    string CustomerName,
    string CustomerAddress,
    string CustomerContactNumber,
    string LoanNumber,
    string LoanDate,
    string DueDate,
    decimal PrincipalAmount,
    decimal InterestRate,
    decimal InterestAmount,
    List<SoaExtensionRowDto> Extensions,
    List<SoaPaymentRowDto> Payments,
    decimal TotalExtensionCharges,
    decimal TotalAmountDue,
    decimal TotalPaid,
    decimal OutstandingBalance,
    string Status,
    string Classification
);

public sealed record SoaExtensionRowDto(string ExtensionDate, decimal AdditionalCharges, decimal AdditionalInterest, string NewDueDate);

public sealed record SoaPaymentRowDto(string PaymentDate, decimal PaymentAmount, decimal RunningBalance);

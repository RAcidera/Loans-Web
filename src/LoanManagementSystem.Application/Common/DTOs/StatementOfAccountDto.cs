namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// Everything the SOA PDF needs, assembled once by GenerateLoanSoaQuery so
/// IStatementOfAccountPdfGenerator stays a pure rendering concern with no
/// repository access of its own — mirrors the DocumentFileDto precedent of
/// carrying content alongside metadata for a download endpoint.
/// </summary>
public sealed record StatementOfAccountDto(
    string CustomerName,
    string CustomerCode,
    string CustomerAddress,
    string CustomerContactNumber,
    string LoanNumber,
    string StatementDate,
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

public sealed record SoaExtensionRowDto(string ExtensionDate, decimal AdditionalCharges, string NewDueDate);

/// <summary>PaymentMethod is the wire-format string (see MappingExtensions.ToWireString) — the PDF generator owns turning it into a display label, same split as everywhere else in this codebase.</summary>
public sealed record SoaPaymentRowDto(
    string PaymentDate, decimal PaymentAmount, string PaymentMethod, string? ReferenceNumber, decimal RunningBalance, string? Remarks
);

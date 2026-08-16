namespace LoanManagementSystem.Application.Common.Xlsx;

/// <summary>Rendering-only concern, same split as ILoansXlsxExportGenerator.</summary>
public interface ICustomersXlsxExportGenerator
{
    byte[] Generate(IReadOnlyList<CustomerExportRowDto> rows);
}

/// <summary>One row of the Customers list export — mirrors the Customers list table's own columns, in the same left-to-right order.</summary>
public sealed record CustomerExportRowDto(
    string CustomerCode,
    string FullName,
    string ContactNumber,
    string BorrowerType,
    string Status,
    int LoanCount,
    decimal OutstandingBalance,
    string DateAdded
);

namespace LoanManagementSystem.Application.Common.Xlsx;

/// <summary>Rendering-only concern, same split as IStatementOfAccountPdfGenerator — the query handler assembles the rows, this just turns them into bytes.</summary>
public interface ILoansXlsxExportGenerator
{
    byte[] Generate(IReadOnlyList<LoanExportRowDto> rows);
}

/// <summary>One row of the Loans list export — mirrors the Loans list table's own columns, in the same left-to-right order.</summary>
public sealed record LoanExportRowDto(
    string LoanNumber,
    string CustomerName,
    string LoanDate,
    string DueDate,
    decimal PrincipalAmount,
    decimal TotalInterest,
    decimal TotalExtensionCharges,
    decimal TotalPaid,
    decimal Balance,
    string Status,
    string Classification
);

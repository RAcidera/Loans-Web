namespace LoanManagementSystem.Application.Common.Xlsx;

/// <summary>Rendering-only concern, same split as ILoansXlsxExportGenerator — the query handler assembles the rows, this just turns them into bytes.</summary>
public interface IPaymentsXlsxExportGenerator
{
    byte[] Generate(IReadOnlyList<PaymentExportRowDto> rows);
}

/// <summary>One row of the Payments list export — mirrors the Payments list table's own columns, in the same left-to-right order.</summary>
public sealed record PaymentExportRowDto(
    string LoanNumber,
    string CustomerName,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string? ReferenceNumber,
    string Notes
);

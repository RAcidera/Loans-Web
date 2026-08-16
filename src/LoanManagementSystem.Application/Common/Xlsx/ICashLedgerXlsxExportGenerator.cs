namespace LoanManagementSystem.Application.Common.Xlsx;

/// <summary>Rendering-only concern, same split as ILoansXlsxExportGenerator — the query handler assembles the rows, this just turns them into bytes.</summary>
public interface ICashLedgerXlsxExportGenerator
{
    byte[] Generate(IReadOnlyList<CashLedgerExportRowDto> rows);
}

/// <summary>One row of the Cash Transactions export — mirrors the grid's own columns, in the same left-to-right order.</summary>
public sealed record CashLedgerExportRowDto(
    string TransactionDate,
    string Transaction,
    string? Reference,
    decimal? CashIn,
    decimal? CashOut,
    decimal RunningBalance,
    string Remarks
);

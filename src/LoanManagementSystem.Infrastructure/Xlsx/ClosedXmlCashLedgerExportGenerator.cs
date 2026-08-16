using ClosedXML.Excel;
using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Infrastructure.Xlsx;

/// <summary>Renders the Cash Transactions export as a single-sheet .xlsx workbook — the Cash Transactions grid's "Export" button.</summary>
public class ClosedXmlCashLedgerExportGenerator : ICashLedgerXlsxExportGenerator
{
    private static readonly string[] Headers = { "Date", "Transaction", "Reference", "Cash In", "Cash Out", "Running Balance", "Remarks" };

    public byte[] Generate(IReadOnlyList<CashLedgerExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Cash Transactions");

        for (var col = 0; col < Headers.Length; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = Headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C195F");
        }

        var rowIndex = 2;
        foreach (var r in rows)
        {
            sheet.Cell(rowIndex, 1).Value = DateOnly.Parse(r.TransactionDate).ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowIndex, 2).Value = r.Transaction;
            sheet.Cell(rowIndex, 3).Value = r.Reference ?? string.Empty;
            if (r.CashIn is not null) sheet.Cell(rowIndex, 4).Value = r.CashIn.Value;
            if (r.CashOut is not null) sheet.Cell(rowIndex, 5).Value = r.CashOut.Value;
            sheet.Cell(rowIndex, 6).Value = r.RunningBalance;
            sheet.Cell(rowIndex, 7).Value = r.Remarks;
            rowIndex++;
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        sheet.Range(2, 1, lastRow, 1).Style.DateFormat.Format = "MM/dd/yyyy";
        sheet.Range(2, 4, lastRow, 6).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

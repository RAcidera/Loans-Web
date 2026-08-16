using ClosedXML.Excel;
using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Infrastructure.Xlsx;

/// <summary>Renders the Loans list export as a single-sheet .xlsx workbook — the Loans list "Export" button.</summary>
public class ClosedXmlLoansExportGenerator : ILoansXlsxExportGenerator
{
    private static readonly string[] Headers =
    {
        "Loan #", "Customer", "Loan Date", "Due Date", "Principal", "Interest",
        "Ext. Charges", "Payments", "Balance", "Status", "Classification",
    };

    public byte[] Generate(IReadOnlyList<LoanExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Loans");

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
            sheet.Cell(rowIndex, 1).Value = r.LoanNumber;
            sheet.Cell(rowIndex, 2).Value = r.CustomerName;
            sheet.Cell(rowIndex, 3).Value = DateOnly.Parse(r.LoanDate).ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowIndex, 4).Value = DateOnly.Parse(r.DueDate).ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowIndex, 5).Value = r.PrincipalAmount;
            sheet.Cell(rowIndex, 6).Value = r.TotalInterest;
            sheet.Cell(rowIndex, 7).Value = r.TotalExtensionCharges;
            sheet.Cell(rowIndex, 8).Value = r.TotalPaid;
            sheet.Cell(rowIndex, 9).Value = r.Balance;
            sheet.Cell(rowIndex, 10).Value = r.Status;
            sheet.Cell(rowIndex, 11).Value = r.Classification;
            rowIndex++;
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        sheet.Range(2, 3, lastRow, 4).Style.DateFormat.Format = "MM/dd/yyyy";
        sheet.Range(2, 5, lastRow, 9).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

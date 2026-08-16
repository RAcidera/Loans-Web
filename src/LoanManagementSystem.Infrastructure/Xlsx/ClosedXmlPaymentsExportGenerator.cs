using ClosedXML.Excel;
using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Infrastructure.Xlsx;

/// <summary>Renders the Payments list export as a single-sheet .xlsx workbook — the Payments list "Export" button.</summary>
public class ClosedXmlPaymentsExportGenerator : IPaymentsXlsxExportGenerator
{
    private static readonly string[] Headers =
    {
        "Loan #", "Customer", "Payment Date", "Amount Paid", "Method", "Reference", "Notes",
    };

    public byte[] Generate(IReadOnlyList<PaymentExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Payments");

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
            sheet.Cell(rowIndex, 3).Value = DateOnly.Parse(r.PaymentDate).ToDateTime(TimeOnly.MinValue);
            sheet.Cell(rowIndex, 4).Value = r.AmountPaid;
            sheet.Cell(rowIndex, 5).Value = r.PaymentMethod;
            sheet.Cell(rowIndex, 6).Value = r.ReferenceNumber ?? "";
            sheet.Cell(rowIndex, 7).Value = r.Notes;
            rowIndex++;
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        sheet.Range(2, 3, lastRow, 3).Style.DateFormat.Format = "MM/dd/yyyy";
        sheet.Range(2, 4, lastRow, 4).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

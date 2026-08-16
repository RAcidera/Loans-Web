using ClosedXML.Excel;
using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Infrastructure.Xlsx;

/// <summary>Renders the Customers list export as a single-sheet .xlsx workbook — the Customers list "Export" button.</summary>
public class ClosedXmlCustomersExportGenerator : ICustomersXlsxExportGenerator
{
    private static readonly string[] Headers =
    {
        "Code", "Name", "Contact", "Borrower Type", "Status", "Total Loans", "Outstanding Balance", "Date Added",
    };

    public byte[] Generate(IReadOnlyList<CustomerExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Customers");

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
            sheet.Cell(rowIndex, 1).Value = r.CustomerCode;
            sheet.Cell(rowIndex, 2).Value = r.FullName;
            sheet.Cell(rowIndex, 3).Value = r.ContactNumber;
            sheet.Cell(rowIndex, 4).Value = r.BorrowerType;
            sheet.Cell(rowIndex, 5).Value = r.Status;
            sheet.Cell(rowIndex, 6).Value = r.LoanCount;
            sheet.Cell(rowIndex, 7).Value = r.OutstandingBalance;
            sheet.Cell(rowIndex, 8).Value = DateOnly.Parse(r.DateAdded).ToDateTime(TimeOnly.MinValue);
            rowIndex++;
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        sheet.Range(2, 7, lastRow, 7).Style.NumberFormat.Format = "#,##0.00";
        sheet.Range(2, 8, lastRow, 8).Style.DateFormat.Format = "MM/dd/yyyy";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

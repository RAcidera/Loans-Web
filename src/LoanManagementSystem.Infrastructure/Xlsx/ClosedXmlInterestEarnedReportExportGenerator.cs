using ClosedXML.Excel;
using LoanManagementSystem.Application.Common.Xlsx;

namespace LoanManagementSystem.Infrastructure.Xlsx;

/// <summary>Renders the Interest Earned Report export as a single-sheet .xlsx workbook — spec §27, numeric cells for money rather than formatted strings.</summary>
public class ClosedXmlInterestEarnedReportExportGenerator : IInterestEarnedReportXlsxExportGenerator
{
    private static readonly string[] Headers =
    {
        "Loan #", "Customer", "Loan Date", "Due Date", "Principal", "Contract Interest", "Extension Interest",
        "Earned Before Period", "Earned This Period", "Total Earned", "Adjustment", "Final Earned", "Status", "Classification",
    };

    public byte[] Generate(IReadOnlyList<InterestEarnedExportRowDto> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Interest Earned");

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
            sheet.Cell(rowIndex, 5).Value = r.Principal;
            sheet.Cell(rowIndex, 6).Value = r.ContractInterest;
            sheet.Cell(rowIndex, 7).Value = r.ExtensionInterest;
            sheet.Cell(rowIndex, 8).Value = r.EarnedBeforePeriod;
            sheet.Cell(rowIndex, 9).Value = r.EarnedThisPeriod;
            sheet.Cell(rowIndex, 10).Value = r.TotalEarned;
            sheet.Cell(rowIndex, 11).Value = r.Adjustment;
            sheet.Cell(rowIndex, 12).Value = r.FinalEarned;
            sheet.Cell(rowIndex, 13).Value = r.Status;
            sheet.Cell(rowIndex, 14).Value = r.Classification;
            rowIndex++;
        }

        var lastRow = Math.Max(1, rowIndex - 1);
        sheet.Range(2, 3, lastRow, 4).Style.DateFormat.Format = "MM/dd/yyyy";
        sheet.Range(2, 5, lastRow, 12).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}

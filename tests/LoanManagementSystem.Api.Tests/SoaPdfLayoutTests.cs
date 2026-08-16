using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Infrastructure.Pdf;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Guards the SOA PDF's explicit acceptance criterion: 60+ payment rows
/// must render on a single A4 page, not spill onto a second one. Counts
/// "/Type /Page" object markers in the raw PDF bytes (excluding the
/// "/Type /Pages" parent node) — QuestPDF writes the page tree as plain,
/// uncompressed indirect objects even when content streams are compressed,
/// so this is a reliable proxy for page count without a PDF-parsing
/// dependency.
/// </summary>
public class SoaPdfLayoutTests
{
    static SoaPdfLayoutTests()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    [Fact]
    public void Generate_With60Payments_FitsOnOnePage()
    {
        var payments = Enumerable.Range(1, 60)
            .Select(i => new SoaPaymentRowDto(
                new DateOnly(2026, 8, 1).AddDays(i).ToString("yyyy-MM-dd"),
                150m, "cash", null, 10000m - i * 150m, i == 60 ? "Final payment" : null))
            .ToList();

        var statement = new StatementOfAccountDto(
            CustomerName: "Ana Villanueva", CustomerCode: "CUS00001", CustomerAddress: "45 Rizal Ave.",
            CustomerContactNumber: "+63 919 333 5566", LoanNumber: "LOA00025", StatementDate: "2026-10-13",
            LoanDate: "2026-08-09", DueDate: "2026-10-08", PrincipalAmount: 10000m, InterestRate: 0.03m,
            InterestAmount: 300m, Extensions: new List<SoaExtensionRowDto>(), Payments: payments,
            TotalExtensionCharges: 0m, TotalAmountDue: 10300m, TotalPaid: 9000m, OutstandingBalance: 1300m,
            Status: "Active", Classification: "Normal");

        var bytes = new QuestPdfStatementOfAccountGenerator().Generate(statement);
        Assert.Equal(1, CountPdfPages(bytes));
    }

    [Fact]
    public void Generate_With60PaymentsAndExtensions_StillFitsOnOnePage()
    {
        var payments = Enumerable.Range(1, 60)
            .Select(i => new SoaPaymentRowDto(
                new DateOnly(2026, 8, 1).AddDays(i).ToString("yyyy-MM-dd"),
                150m, "cash", "REF-1001", 10000m - i * 150m, "Weekly installment"))
            .ToList();
        var extensions = Enumerable.Range(1, 3)
            .Select(i => new SoaExtensionRowDto(new DateOnly(2026, 8, 1).AddDays(i * 5).ToString("yyyy-MM-dd"), 50m, "2026-10-08"))
            .ToList();

        var statement = new StatementOfAccountDto(
            CustomerName: "Ana Villanueva", CustomerCode: "CUS00001", CustomerAddress: "45 Rizal Ave.",
            CustomerContactNumber: "+63 919 333 5566", LoanNumber: "LOA00025", StatementDate: "2026-10-13",
            LoanDate: "2026-08-09", DueDate: "2026-10-08", PrincipalAmount: 10000m, InterestRate: 0.03m,
            InterestAmount: 300m, Extensions: extensions, Payments: payments,
            TotalExtensionCharges: 150m, TotalAmountDue: 10450m, TotalPaid: 9000m, OutstandingBalance: 1450m,
            Status: "Extended", Classification: "WatchList");

        var bytes = new QuestPdfStatementOfAccountGenerator().Generate(statement);
        Assert.Equal(1, CountPdfPages(bytes));
    }

    private static int CountPdfPages(byte[] pdfBytes)
    {
        var pdfText = System.Text.Encoding.Latin1.GetString(pdfBytes);
        return System.Text.RegularExpressions.Regex.Matches(pdfText, @"/Type\s*/Page(?!s)").Count;
    }
}

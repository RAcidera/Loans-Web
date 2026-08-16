using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoanManagementSystem.Infrastructure.Pdf;

/// <summary>Renders the Interest Earned Report (spec §26): header with period/filters/generated timestamp, the six summary cards, the detailed grid, and totals — reusing the same style helpers as QuestPdfStatementOfAccountGenerator.</summary>
public class QuestPdfInterestEarnedReportGenerator : IInterestEarnedReportPdfGenerator
{
    private const string Primary = "#5b21e8";
    private const string HeaderBg = "#302369";
    private const string Text = "#172033";
    private const string Muted = "#68748a";
    private const string Border = "#dfe3ea";
    private const string ZebraBg = "#fafbfc";
    private const string FooterBg = "#f0f2f6";
    private const string Green = "#15966b";
    private const string Red = "#d94a50";

    public byte[] Generate(InterestEarnedReportPdfDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(7).FontColor(Text));

                page.Header().Column(col =>
                {
                    col.Item().BorderBottom(1.5f).BorderColor(Primary).PaddingBottom(4).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Loan Management").FontSize(14).Bold();
                            c.Item().Text("Interest Earned Report").FontSize(7.5f).FontColor(Muted);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("INTEREST EARNED REPORT").FontSize(12).Bold();
                            c.Item().AlignRight().Text($"Period: {FormatDate(report.FromDate)} – {FormatDate(report.ToDate)}").FontSize(7.5f).FontColor(Muted);
                            c.Item().AlignRight().Text($"Generated: {report.GeneratedAt}").FontSize(6.5f).FontColor(Muted);
                        });
                    });

                    col.Item().PaddingTop(3).Text($"Filters: {report.FiltersSummary}").FontSize(6.5f).FontColor(Muted);

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.Spacing(4);
                        Metric(row, "Total Earned Interest", Php(report.Summary.TotalEarnedInterest), highlight: true);
                        Metric(row, "Original Interest", Php(report.Summary.OriginalInterestEarned));
                        Metric(row, "Extension Interest", Php(report.Summary.ExtensionInterestEarned));
                        Metric(row, "Interest Adjustments", Php(report.Summary.InterestAdjustments), color: report.Summary.InterestAdjustments < 0 ? Red : Text);
                        Metric(row, "Interest Collected", report.Summary.InterestCollected is { } c ? Php(c) : "N/A");
                        Metric(row, "Interest Receivable", report.Summary.InterestReceivable is { } r ? Php(r) : "N/A");
                    });
                });

                page.Content().PaddingTop(4).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Detailed Loan Interest").FontSize(8).Bold();
                        row.AutoItem().Text($"{report.Rows.Count} loan{(report.Rows.Count == 1 ? "" : "s")}").FontSize(6.3f).FontColor(Muted);
                    });

                    if (report.Rows.Count == 0)
                    {
                        col.Item().PaddingTop(4).Text("No loans match the selected filters.").FontColor(Muted);
                    }
                    else
                    {
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(9);   // Loan #
                                columns.RelativeColumn(14);  // Customer
                                columns.RelativeColumn(8);   // Loan Date
                                columns.RelativeColumn(8);   // Due Date
                                columns.RelativeColumn(9);   // Principal
                                columns.RelativeColumn(9);   // Contract Interest
                                columns.RelativeColumn(9);   // Extension Interest
                                columns.RelativeColumn(9);   // Earned Before
                                columns.RelativeColumn(9);   // Earned This Period
                                columns.RelativeColumn(8);   // Total Earned
                                columns.RelativeColumn(8);   // Adjustment
                                columns.RelativeColumn(9);   // Final Earned
                                columns.RelativeColumn(7);   // Status
                                columns.RelativeColumn(8);   // Classification
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(c => Th(c)).Text("Loan #");
                                header.Cell().Element(c => Th(c)).Text("Customer");
                                header.Cell().Element(c => Th(c)).Text("Loan Date");
                                header.Cell().Element(c => Th(c)).Text("Due Date");
                                header.Cell().Element(c => Th(c, right: true)).Text("Principal");
                                header.Cell().Element(c => Th(c, right: true)).Text("Contract Int.");
                                header.Cell().Element(c => Th(c, right: true)).Text("Ext. Int.");
                                header.Cell().Element(c => Th(c, right: true)).Text("Earned Before");
                                header.Cell().Element(c => Th(c, right: true)).Text("Earned This Pd.");
                                header.Cell().Element(c => Th(c, right: true)).Text("Total Earned");
                                header.Cell().Element(c => Th(c, right: true)).Text("Adjustment");
                                header.Cell().Element(c => Th(c, right: true)).Text("Final Earned");
                                header.Cell().Element(c => Th(c)).Text("Status");
                                header.Cell().Element(c => Th(c)).Text("Classification");
                            });

                            var rowIndex = 0;
                            foreach (var r in report.Rows)
                            {
                                var isEven = rowIndex++ % 2 == 1;
                                table.Cell().Element(c => Td(c, isEven)).Text(r.LoanNumber);
                                table.Cell().Element(c => Td(c, isEven)).Text(r.CustomerName);
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(r.LoanDate));
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(r.DueDate));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.Principal));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.ContractInterest));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.ExtensionInterest));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.EarnedBeforePeriod));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.EarnedThisPeriod));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.TotalEarned));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.Adjustment));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(r.FinalEarned));
                                table.Cell().Element(c => Td(c, isEven)).Text(r.Status);
                                table.Cell().Element(c => Td(c, isEven)).Text(r.Classification);
                            }

                            table.Cell().ColumnSpan(4).Element(c => Tf(c)).Text("TOTALS");
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.Principal));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.ContractInterest));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.ExtensionInterest));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.EarnedBeforePeriod));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.EarnedThisPeriod));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.TotalEarned));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.Adjustment));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(report.Totals.FinalEarned));
                            table.Cell().ColumnSpan(2).Element(c => Tf(c)).Text("");
                        });
                    }
                });

                page.Footer().BorderTop(1).BorderColor(Border).PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Generated by Loan Management System").FontSize(5.8f).FontColor(Muted);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(5.8f).FontColor(Muted));
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void Metric(RowDescriptor row, string label, string value, bool highlight = false, string? color = null)
    {
        row.RelativeItem().Border(1).BorderColor(Border).Padding(4).Column(mc =>
        {
            mc.Item().Text(label.ToUpperInvariant()).FontSize(5.6f).FontColor(Muted);
            mc.Item().PaddingTop(1).Text(value).FontSize(8.5f).Bold().FontColor(color ?? (highlight ? Primary : Text));
        });
    }

    private static IContainer Th(IContainer c, bool right = false)
    {
        var styled = c.Background(HeaderBg).PaddingVertical(2f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6f).FontColor(Colors.White).Bold());
        return right ? styled.AlignRight() : styled;
    }

    private static IContainer Td(IContainer c, bool isEven, bool right = false)
    {
        var styled = c.BorderBottom(0.5f).BorderColor(Border).Background(isEven ? ZebraBg : Colors.White)
            .PaddingVertical(0.5f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6f).LineHeight(1f));
        return right ? styled.AlignRight() : styled;
    }

    private static IContainer Tf(IContainer c, bool right = false)
    {
        var styled = c.Background(FooterBg).BorderTop(1).BorderColor("#7b8492").PaddingVertical(1.2f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6.2f).Bold());
        return right ? styled.AlignRight() : styled;
    }

    private static string FormatDate(string iso) =>
        DateOnly.TryParse(iso, out var d) ? d.ToString("MMM dd, yyyy") : iso;

    private static string Php(decimal amount) => $"₱{amount:N2}";
}

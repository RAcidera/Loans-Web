using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoanManagementSystem.Infrastructure.Pdf;

/// <summary>
/// Renders the SOA as a single compact A4 page — layout follows the
/// "loan-statement-of-account-portrait.html" reference (header, Customer +
/// Loan Details cards, a 6-metric summary row, Payment History table, an
/// optional Extension History table, and a notes/signature block), sized
/// so the Payment History table alone can hold 60+ rows without spilling
/// onto a second page. QuestPDF.Settings.License is set once at startup —
/// see Program.cs.
/// </summary>
public class QuestPdfStatementOfAccountGenerator : IStatementOfAccountPdfGenerator
{
    private const string Primary = "#5b21e8";
    private const string HeaderBg = "#302369";
    private const string Text = "#172033";
    private const string Muted = "#68748a";
    private const string Border = "#dfe3ea";
    private const string ZebraBg = "#fafbfc";
    private const string FooterBg = "#f0f2f6";

    public byte[] Generate(StatementOfAccountDto s)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(7).FontColor(Text));

                page.Header().Column(col =>
                {
                    col.Item().BorderBottom(1.5f).BorderColor(Primary).PaddingBottom(4).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Loan Management").FontSize(14).Bold();
                            c.Item().Text("Borrower Statement of Account").FontSize(7.5f).FontColor(Muted);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("STATEMENT OF ACCOUNT").FontSize(12).Bold();
                            c.Item().AlignRight().Text(t =>
                            {
                                t.Span(s.LoanNumber).FontColor(Primary).Bold().FontSize(7.5f);
                                t.Span($"   |   Statement Date: {FormatDate(s.StatementDate)}").FontColor(Muted).FontSize(7.5f);
                            });
                        });
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem(1.2f).Element(Card).Column(cc =>
                        {
                            cc.Spacing(1f);
                            cc.Item().Text("CUSTOMER").FontSize(6.3f).Bold().FontColor(Muted);
                            KeyValue(cc, "Name", s.CustomerName);
                            KeyValue(cc, "Customer Code", s.CustomerCode);
                            KeyValue(cc, "Contact", s.CustomerContactNumber);
                            KeyValue(cc, "Address", s.CustomerAddress);
                        });
                        row.RelativeItem().Element(Card).Column(cc =>
                        {
                            cc.Spacing(1f);
                            cc.Item().Text("LOAN DETAILS").FontSize(6.3f).Bold().FontColor(Muted);
                            KeyValue(cc, "Loan Date", FormatDate(s.LoanDate));
                            KeyValue(cc, "Due Date", FormatDate(s.DueDate));
                            KeyValue(cc, "Interest Rate", $"{s.InterestRate:P2}");
                            KeyValue(cc, "Status", $"{StatusLabel(s.Status)} / {ClassificationLabel(s.Classification)}");
                        });
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.Spacing(4);
                        Metric(row, "Principal", Php(s.PrincipalAmount));
                        Metric(row, "Interest", Php(s.InterestAmount));
                        Metric(row, "Extensions", Php(s.TotalExtensionCharges));
                        Metric(row, "Total Due", Php(s.TotalAmountDue));
                        Metric(row, "Payments", Php(s.TotalPaid));
                        Metric(row, "Outstanding", Php(s.OutstandingBalance), highlight: true);
                    });
                });

                page.Content().PaddingTop(4).Column(col =>
                {
                    if (s.Extensions.Count > 0)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Extension History").FontSize(8).Bold();
                            row.AutoItem().Text($"{s.Extensions.Count} extension record{(s.Extensions.Count == 1 ? "" : "s")}").FontSize(6.3f).FontColor(Muted);
                        });

                        col.Item().PaddingBottom(3).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(20);   // Extension Date
                                columns.RelativeColumn(18);   // Additional Charges
                                columns.RelativeColumn(6);    // spacer — extra breathing room before New Due Date
                                columns.RelativeColumn(18);   // New Due Date
                                columns.RelativeColumn(38);   // Remarks
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(c => Th(c)).Text("Extension Date");
                                header.Cell().Element(c => Th(c, right: true)).Text("Additional Charges");
                                header.Cell().Element(c => Th(c)).Text("");
                                header.Cell().Element(c => Th(c)).Text("New Due Date");
                                header.Cell().Element(c => Th(c)).Text("Remarks");
                            });

                            var extIndex = 0;
                            foreach (var e in s.Extensions)
                            {
                                var isEven = extIndex++ % 2 == 1;
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(e.ExtensionDate));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(e.AdditionalCharges));
                                table.Cell().Element(c => Td(c, isEven)).Text("");
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(e.NewDueDate));
                                table.Cell().Element(c => Td(c, isEven)).Text(string.IsNullOrWhiteSpace(e.Remarks) ? "—" : e.Remarks);
                            }
                        });
                    }

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Payment History").FontSize(8).Bold();
                        row.AutoItem().Text($"{s.Payments.Count} payment record{(s.Payments.Count == 1 ? "" : "s")}").FontSize(6.3f).FontColor(Muted);
                    });

                    if (s.Payments.Count == 0)
                    {
                        col.Item().PaddingTop(4).Text("No payments recorded yet.").FontColor(Muted);
                    }
                    else
                    {
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(15);
                                columns.RelativeColumn(13);
                                columns.RelativeColumn(12);
                                columns.RelativeColumn(13);
                                columns.RelativeColumn(16);
                                columns.RelativeColumn(27);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(c => Th(c)).Text("#");
                                header.Cell().Element(c => Th(c)).Text("Payment Date");
                                header.Cell().Element(c => Th(c, right: true)).Text("Amount");
                                header.Cell().Element(c => Th(c)).Text("Method");
                                header.Cell().Element(c => Th(c)).Text("Reference");
                                header.Cell().Element(c => Th(c, right: true)).Text("Running Balance");
                                header.Cell().Element(c => Th(c)).Text("Remarks");
                            });

                            var rowIndex = 0;
                            foreach (var p in s.Payments)
                            {
                                rowIndex++;
                                var isEven = rowIndex % 2 == 0;
                                table.Cell().Element(c => Td(c, isEven)).Text(rowIndex.ToString());
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(p.PaymentDate));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(p.PaymentAmount));
                                table.Cell().Element(c => Td(c, isEven)).Text(PaymentMethodLabel(p.PaymentMethod));
                                table.Cell().Element(c => Td(c, isEven)).Text(p.ReferenceNumber ?? "—");
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(p.RunningBalance));
                                table.Cell().Element(c => Td(c, isEven)).Text(p.Remarks ?? "");
                            }

                            table.Cell().ColumnSpan(2).Element(c => Tf(c)).Text("TOTAL PAYMENTS");
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(s.TotalPaid));
                            table.Cell().ColumnSpan(2).Element(c => Tf(c)).Text("");
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(s.OutstandingBalance));
                            table.Cell().Element(c => Tf(c)).Text("");
                        });
                    }

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem(1.55f).Border(1).BorderColor(Border).Padding(4).Text(t =>
                        {
                            t.Span("Notes: ").Bold().FontSize(6.3f);
                            t.Span("This statement summarizes the loan and recorded payment activity as of the statement date.").FontColor(Muted).FontSize(6.3f);
                        });
                        row.RelativeItem(0.65f).Column(sc =>
                        {
                            sc.Item().PaddingTop(10).PaddingHorizontal(10).BorderTop(1).BorderColor("#7b8492");
                            sc.Item().AlignCenter().PaddingTop(1).Text("Authorized Signature").FontSize(6.3f).FontColor(Muted);
                        });
                    });
                });

                page.Footer().BorderTop(1).BorderColor(Border).PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Generated by Loan Management System").FontSize(5.8f).FontColor(Muted);
                    row.RelativeItem().AlignRight().Text($"{s.LoanNumber} • Statement of Account").FontSize(5.8f).FontColor(Muted);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void KeyValue(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(62).Text(label).FontSize(7).FontColor(Muted);
            row.RelativeItem().Text(value).FontSize(7).Bold();
        });
    }

    private static void Metric(RowDescriptor row, string label, string value, bool highlight = false)
    {
        row.RelativeItem().Border(1).BorderColor(Border).Padding(4).Column(mc =>
        {
            mc.Item().Text(label.ToUpperInvariant()).FontSize(5.6f).FontColor(Muted);
            mc.Item().PaddingTop(1).Text(value).FontSize(8.5f).Bold().FontColor(highlight ? Primary : Text);
        });
    }

    private static IContainer Card(IContainer c) => c.Border(1).BorderColor(Border).Padding(5);

    private static IContainer Th(IContainer c, bool right = false)
    {
        var styled = c.Background(HeaderBg).PaddingVertical(2f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6.1f).FontColor(Colors.White).Bold());
        return right ? styled.AlignRight() : styled;
    }

    private static IContainer Td(IContainer c, bool isEven, bool right = false)
    {
        var styled = c.BorderBottom(0.5f).BorderColor(Border).Background(isEven ? ZebraBg : Colors.White)
            .PaddingVertical(0.5f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6.1f).LineHeight(1f));
        return right ? styled.AlignRight() : styled;
    }

    private static IContainer Tf(IContainer c, bool right = false)
    {
        var styled = c.Background(FooterBg).BorderTop(1).BorderColor("#7b8492").PaddingVertical(1.2f).PaddingHorizontal(3)
            .DefaultTextStyle(x => x.FontSize(6.3f).Bold());
        return right ? styled.AlignRight() : styled;
    }

    private static string FormatDate(string iso) =>
        DateOnly.TryParse(iso, out var d) ? d.ToString("MMM dd, yyyy") : iso;

    private static string PaymentMethodLabel(string wire) => wire switch
    {
        "cash" => "Cash",
        "gcash" => "GCash",
        "bank_transfer" => "Bank Transfer",
        "other" => "Other",
        _ => wire,
    };

    private static string StatusLabel(string raw) => raw switch
    {
        "WrittenOff" => "Written Off",
        _ => raw,
    };

    private static string ClassificationLabel(string raw) => raw switch
    {
        "WatchList" => "Watch List",
        "BadLoan" => "Bad Loan",
        _ => raw,
    };

    private static string Php(decimal amount) => $"₱{amount:N2}";
}

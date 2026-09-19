using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoanManagementSystem.Infrastructure.Pdf;

/// <summary>
/// Renders SOA V2 — a single chronological "Account Activity" table (Date,
/// Transaction, Details/Remarks, Debit, Credit, Balance) built from
/// loan_ledger, instead of V1's separate Extension History / Payment
/// History sections (see assets/Statement of Account V2.md). Shares V1's
/// visual language (colors, Card/KeyValue/Metric/Th/Td/Tf helpers) so both
/// statements read as the same family of document, but unlike V1 (sized to
/// always fit one page) this one supports unbounded rows: QuestPDF repeats
/// table.Header() on every page automatically, and the footer prints
/// "page / totalPages" (same pattern as QuestPdfInterestEarnedReportGenerator).
/// </summary>
public class QuestPdfStatementOfAccountV2Generator : IStatementOfAccountV2PdfGenerator
{
    private const string Primary = "#5b21e8";
    private const string HeaderBg = "#302369";
    private const string Text = "#172033";
    private const string Muted = "#68748a";
    private const string Border = "#dfe3ea";
    private const string ZebraBg = "#fafbfc";
    private const string FooterBg = "#f0f2f6";

    public byte[] Generate(StatementOfAccountV2Dto s)
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
                        Metric(row, "Payments", Php(s.TotalPaid));
                        Metric(row, "Outstanding", Php(s.OutstandingBalance), highlight: true);
                    });
                });

                page.Content().PaddingTop(4).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Account Activity").FontSize(8).Bold();
                        row.AutoItem().Text($"{s.Activity.Count} transaction{(s.Activity.Count == 1 ? "" : "s")}").FontSize(6.3f).FontColor(Muted);
                    });

                    if (s.Activity.Count == 0)
                    {
                        col.Item().PaddingTop(4).Text("No account activity recorded yet.").FontColor(Muted);
                    }
                    else
                    {
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(12);   // Date
                                columns.RelativeColumn(18);   // Transaction
                                columns.RelativeColumn(30);   // Details / Remarks
                                columns.RelativeColumn(13);   // Debit
                                columns.RelativeColumn(13);   // Credit
                                columns.RelativeColumn(14);   // Balance
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(c => Th(c)).Text("Date");
                                header.Cell().Element(c => Th(c)).Text("Transaction");
                                header.Cell().Element(c => Th(c)).Text("Details / Remarks");
                                header.Cell().Element(c => Th(c, right: true)).Text("Debit");
                                header.Cell().Element(c => Th(c, right: true)).Text("Credit");
                                header.Cell().Element(c => Th(c, right: true)).Text("Balance");
                            });

                            var rowIndex = 0;
                            foreach (var a in s.Activity)
                            {
                                var isEven = rowIndex++ % 2 == 1;
                                table.Cell().Element(c => Td(c, isEven)).Text(FormatDate(a.TransactionDate));
                                table.Cell().Element(c => Td(c, isEven)).Text(TransactionTypeLabel(a.TransactionType));
                                table.Cell().Element(c => Td(c, isEven)).Text(string.IsNullOrWhiteSpace(a.Remarks) ? "—" : a.Remarks);
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(AmountOrDash(a.Debit));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(AmountOrDash(a.Credit));
                                table.Cell().Element(c => Td(c, isEven, right: true)).Text(Php(a.RunningBalance));
                            }

                            table.Cell().ColumnSpan(3).Element(c => Tf(c)).Text("TOTAL");
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(s.TotalDebit));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(s.TotalCredit));
                            table.Cell().Element(c => Tf(c, right: true)).Text(Php(s.TotalDebit - s.TotalCredit));
                        });
                    }

                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.Spacing(8);
                        row.RelativeItem(1.55f).Border(1).BorderColor(Border).Padding(4).Text(t =>
                        {
                            t.Span("Notes: ").Bold().FontSize(6.3f);
                            t.Span("This statement presents every ledger transaction affecting this loan's balance, in chronological order, as of the statement date.").FontColor(Muted).FontSize(6.3f);
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
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(5.8f).FontColor(Muted));
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
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
            .DefaultTextStyle(x => x.FontSize(6.1f).LineHeight(1.15f));
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

    /// <summary>
    /// Centralized transaction-type → display-label mapping (spec §12) —
    /// the only place in the V2 PDF that knows about wire-string transaction
    /// types, so no ugly internal codes ever leak into the rendered table.
    /// Falls back to a generic snake_case → Title Case conversion for any
    /// ledger transaction type this mapping doesn't yet know about, rather
    /// than hard-coding only the four types the system currently writes.
    /// </summary>
    private static string TransactionTypeLabel(string wire) => wire switch
    {
        "loan_released" => "Loan Released",
        "interest_added" => "Interest Added",
        "payment" => "Payment",
        "extension" => "Extension Charge",
        _ => TitleCase(wire),
    };

    private static string TitleCase(string wire) =>
        string.Join(' ', wire.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

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

    /// <summary>Spec §11: zero/null Debit or Credit renders as "—", never ₱0.00.</summary>
    private static string AmountOrDash(decimal amount) => amount == 0 ? "—" : Php(amount);

    private static string Php(decimal amount) => $"₱{amount:N2}";
}

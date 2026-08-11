using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoanManagementSystem.Infrastructure.Pdf;

/// <summary>
/// Renders the SOA spec's five sections (Customer Information, Loan
/// Information, Extension History, Payment History, Summary) as a single
/// A4 page. QuestPDF.Settings.License is set once at startup — see Program.cs.
/// </summary>
public class QuestPdfStatementOfAccountGenerator : IStatementOfAccountPdfGenerator
{
    public byte[] Generate(StatementOfAccountDto s)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Statement of Account").FontSize(18).Bold();
                    col.Item().Text($"Loan {s.LoanNumber}").FontSize(11).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Column(section =>
                    {
                        section.Item().Text("Customer Information").FontSize(13).Bold();
                        section.Item().PaddingTop(4).Text($"Name: {s.CustomerName}");
                        section.Item().Text($"Address: {s.CustomerAddress}");
                        section.Item().Text($"Contact Number: {s.CustomerContactNumber}");
                    });

                    col.Item().Column(section =>
                    {
                        section.Item().Text("Loan Information").FontSize(13).Bold();
                        section.Item().PaddingTop(4).Text($"Loan Number: {s.LoanNumber}");
                        section.Item().Text($"Loan Date: {s.LoanDate}");
                        section.Item().Text($"Due Date: {s.DueDate}");
                        section.Item().Text($"Principal Amount: {Php(s.PrincipalAmount)}");
                        section.Item().Text($"Interest Rate: {s.InterestRate:P2}");
                        section.Item().Text($"Interest Amount: {Php(s.InterestAmount)}");
                    });

                    col.Item().Column(section =>
                    {
                        section.Item().Text("Extension History").FontSize(13).Bold();

                        if (s.Extensions.Count == 0)
                        {
                            section.Item().PaddingTop(4).Text("No extensions.").FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            section.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Extension Date").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Additional Charges").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Additional Interest").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("New Due Date").Bold();
                                });

                                foreach (var e in s.Extensions)
                                {
                                    table.Cell().Element(BodyCellStyle).Text(e.ExtensionDate);
                                    table.Cell().Element(BodyCellStyle).Text(Php(e.AdditionalCharges));
                                    table.Cell().Element(BodyCellStyle).Text(Php(e.AdditionalInterest));
                                    table.Cell().Element(BodyCellStyle).Text(e.NewDueDate);
                                }
                            });
                        }
                    });

                    col.Item().Column(section =>
                    {
                        section.Item().Text("Payment History").FontSize(13).Bold();

                        if (s.Payments.Count == 0)
                        {
                            section.Item().PaddingTop(4).Text("No payments.").FontColor(Colors.Grey.Darken1);
                        }
                        else
                        {
                            section.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Payment Date").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Payment Amount").Bold();
                                    header.Cell().Element(HeaderCellStyle).Text("Running Balance").Bold();
                                });

                                foreach (var p in s.Payments)
                                {
                                    table.Cell().Element(BodyCellStyle).Text(p.PaymentDate);
                                    table.Cell().Element(BodyCellStyle).Text(Php(p.PaymentAmount));
                                    table.Cell().Element(BodyCellStyle).Text(Php(p.RunningBalance));
                                }
                            });
                        }
                    });

                    col.Item().Column(section =>
                    {
                        section.Item().Text("Summary").FontSize(13).Bold();
                        section.Item().PaddingTop(4).Text($"Principal Amount: {Php(s.PrincipalAmount)}");
                        section.Item().Text($"Interest Amount: {Php(s.InterestAmount)}");
                        section.Item().Text($"Extension Charges: {Php(s.TotalExtensionCharges)}");
                        section.Item().Text($"Total Amount Due: {Php(s.TotalAmountDue)}");
                        section.Item().Text($"Total Payments: {Php(s.TotalPaid)}");
                        section.Item().Text($"Outstanding Balance: {Php(s.OutstandingBalance)}").Bold();
                        section.Item().Text($"Loan Status: {s.Status}");
                        section.Item().Text($"Loan Classification: {s.Classification}");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated ").FontColor(Colors.Grey.Darken1);
                    x.Span(DateTime.UtcNow.ToString("yyyy-MM-dd")).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCellStyle(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(4).PaddingRight(6);

    private static IContainer BodyCellStyle(IContainer c) =>
        c.PaddingVertical(3).PaddingRight(6);

    private static string Php(decimal amount) => $"₱{amount:N2}";
}

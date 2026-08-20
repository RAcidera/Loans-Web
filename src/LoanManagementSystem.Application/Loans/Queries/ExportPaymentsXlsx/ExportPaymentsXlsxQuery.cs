using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Xlsx;
using LoanManagementSystem.Application.Loans.Queries.GetPaymentsTotals;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.ExportPaymentsXlsx;

/// <summary>
/// Same filter set as GetPaymentsTotalsQuery, minus paging — the Payments
/// list's "Export" button exports the whole filtered result set, not just
/// the visible page, same scope as the footer total.
/// </summary>
public sealed record ExportPaymentsXlsxQuery(
    string? LoanSearch, string? CustomerSearch, DateOnly? DateFrom, DateOnly? DateTo,
    DateOnly? CreatedAtFrom = null, DateOnly? CreatedAtTo = null)
    : IRequest<DocumentFileDto>;

public sealed class ExportPaymentsXlsxQueryHandler : IRequestHandler<ExportPaymentsXlsxQuery, DocumentFileDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentsXlsxExportGenerator _xlsxGenerator;
    private readonly IAppDateTimeService _appDateTime;

    public ExportPaymentsXlsxQueryHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository, IPaymentsXlsxExportGenerator xlsxGenerator,
        IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _xlsxGenerator = xlsxGenerator;
        _appDateTime = appDateTime;
    }

    public async Task<DocumentFileDto> Handle(ExportPaymentsXlsxQuery request, CancellationToken ct)
    {
        var effectiveCustomerIds = await GetPaymentsTotalsQueryHandler.ResolveCustomerIdsAsync(_customerRepository, request.CustomerSearch, ct);

        var rows = await _loanRepository.GetPaymentsFilteredAsync(
            request.LoanSearch, effectiveCustomerIds, request.DateFrom, request.DateTo, ct,
            request.CreatedAtFrom, request.CreatedAtTo);

        var exportRows = rows
            .OrderByDescending(r => r.Payment.PaymentDate)
            .Select(r => new PaymentExportRowDto(
                LoanNumber: MappingExtensions.FormatLoanNumber(r.LoanNumber),
                CustomerName: r.CustomerName,
                PaymentDate: r.Payment.PaymentDate.ToString("yyyy-MM-dd"),
                AmountPaid: r.Payment.AmountPaid.Amount,
                PaymentMethod: MethodLabel(r.Payment.PaymentMethod.ToString()),
                ReferenceNumber: r.Payment.ReferenceNumber,
                Notes: r.Payment.Notes))
            .ToList();

        var bytes = _xlsxGenerator.Generate(exportRows);
        var today = _appDateTime.Today;
        return new DocumentFileDto($"payments_export_{today:yyyy-MM-dd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }

    private static string MethodLabel(string raw) => raw switch
    {
        "GCash" => "GCash",
        "BankTransfer" => "Bank transfer",
        _ => raw,
    };
}

using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Xlsx;
using LoanManagementSystem.Application.Customers.Queries.GetCustomersPage;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.ExportCustomersXlsx;

/// <summary>
/// Same search/status filters as GetCustomersTotalsQuery, minus paging — the
/// Customers list's "Export" button exports the whole filtered result set,
/// not just the visible page, same scope as the KPI strip.
/// </summary>
public sealed record ExportCustomersXlsxQuery(string? Search = null, string? Status = null) : IRequest<DocumentFileDto>;

public sealed class ExportCustomersXlsxQueryHandler : IRequestHandler<ExportCustomersXlsxQuery, DocumentFileDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomersXlsxExportGenerator _xlsxGenerator;

    public ExportCustomersXlsxQueryHandler(
        ICustomerRepository customerRepository, ILoanRepository loanRepository, ICustomersXlsxExportGenerator xlsxGenerator)
    {
        _customerRepository = customerRepository;
        _loanRepository = loanRepository;
        _xlsxGenerator = xlsxGenerator;
    }

    public async Task<DocumentFileDto> Handle(ExportCustomersXlsxQuery request, CancellationToken ct)
    {
        var status = GetCustomersPageQueryHandler.ParseStatus(request.Status);
        var customers = await _customerRepository.GetFilteredAsync(request.Search, status, ct);

        var loanStats = await _loanRepository.GetLoanCountsAndBalanceByCustomerAsync(
            customers.Select(c => c.Id).ToList(), ct);

        var rows = customers
            .OrderBy(c => c.CustomerNumber)
            .Select(c =>
            {
                var (loanCount, balance) = loanStats.GetValueOrDefault(c.Id);
                return new CustomerExportRowDto(
                    CustomerCode: MappingExtensions.FormatCustomerCode(c.CustomerNumber),
                    FullName: c.FullName,
                    ContactNumber: c.ContactNumber,
                    BorrowerType: c.BorrowerType,
                    Status: c.Status.ToString(),
                    LoanCount: loanCount,
                    OutstandingBalance: balance,
                    DateAdded: c.CreatedAtUtc.ToString("yyyy-MM-dd"));
            })
            .ToList();

        var bytes = _xlsxGenerator.Generate(rows);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DocumentFileDto($"customers_export_{today:yyyy-MM-dd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }
}

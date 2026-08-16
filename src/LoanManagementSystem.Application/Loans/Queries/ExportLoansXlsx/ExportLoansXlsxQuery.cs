using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Xlsx;
using LoanManagementSystem.Application.Loans.Queries.GetLoansPage;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.ExportLoansXlsx;

/// <summary>
/// Same filter set as GetLoansTotalsQuery (search + spec's "Loan Search and
/// Filtering" set), minus paging — the Loans list's "Export" button exports
/// the whole filtered result set, not just the visible page, same scope as
/// the footer totals.
/// </summary>
public sealed record ExportLoansXlsxQuery(
    string? Search = null, string? Status = null, string? Classification = null,
    DateOnly? LoanDateFrom = null, DateOnly? LoanDateTo = null,
    DateOnly? DueDateFrom = null, DateOnly? DueDateTo = null,
    bool BadLoansOnly = false, bool OverdueOnly = false)
    : IRequest<DocumentFileDto>;

public sealed class ExportLoansXlsxQueryHandler : IRequestHandler<ExportLoansXlsxQuery, DocumentFileDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoansXlsxExportGenerator _xlsxGenerator;

    public ExportLoansXlsxQueryHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository, ILoansXlsxExportGenerator xlsxGenerator)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _xlsxGenerator = xlsxGenerator;
    }

    public async Task<DocumentFileDto> Handle(ExportLoansXlsxQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var matchingCustomerIds = string.IsNullOrWhiteSpace(request.Search)
            ? new List<Domain.Customers.CustomerId>()
            : await _customerRepository.SearchIdsByNameAsync(request.Search, ct);

        // Reuses GetLoansPageQuery's own string-to-enum filter parsing, same
        // as GetLoansTotalsQuery, so the list/totals/export never read
        // different filters for the same request.
        var filters = GetLoansPageQueryHandler.BuildFilters(new GetLoansPageQuery(
            0, 1, request.Search, null, null, request.Status, request.Classification,
            request.LoanDateFrom, request.LoanDateTo, request.DueDateFrom, request.DueDateTo,
            request.BadLoansOnly, request.OverdueOnly));

        var loans = await _loanRepository.GetFilteredAsync(request.Search, matchingCustomerIds, filters, today, ct);
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);

        var customerIds = loans.Select(l => l.CustomerId).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        var rows = loans
            .OrderBy(l => l.LoanNumber)
            .Select(l => new LoanExportRowDto(
                LoanNumber: MappingExtensions.FormatLoanNumber(l.LoanNumber),
                CustomerName: customerNames.TryGetValue(l.CustomerId, out var name) ? name : "Unknown",
                LoanDate: l.StartDate.ToString("yyyy-MM-dd"),
                DueDate: l.DueDate.ToString("yyyy-MM-dd"),
                PrincipalAmount: l.PrincipalAmount.Amount,
                TotalInterest: l.TotalInterest.Amount,
                TotalExtensionCharges: l.TotalExtensionCharges.Amount,
                TotalPaid: l.TotalPaid.Amount,
                Balance: l.Balance.Amount,
                Status: StatusLabel(l.Status.ToString()),
                Classification: ClassificationLabel(l.Classification.ToString())))
            .ToList();

        var bytes = _xlsxGenerator.Generate(rows);
        return new DocumentFileDto($"loans_export_{today:yyyy-MM-dd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }

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
}

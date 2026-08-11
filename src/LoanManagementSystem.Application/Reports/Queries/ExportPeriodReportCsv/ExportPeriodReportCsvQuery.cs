using System.Text;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Reports.Queries.ExportPeriodReportCsv;

public sealed record ExportPeriodReportCsvQuery(DateOnly StartDate, DateOnly EndDate) : IRequest<string>;

/// <summary>
/// CSV export for the Reports page's period view (SRS 3.5). Hand-written
/// with System.Text.StringBuilder rather than a CSV library — one row
/// shape, no new dependency, per the plan's "CSV first" call.
/// </summary>
public sealed class ExportPeriodReportCsvQueryHandler : IRequestHandler<ExportPeriodReportCsvQuery, string>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public ExportPeriodReportCsvQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<string> Handle(ExportPeriodReportCsvQuery request, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllAsync(ct);
        var customers = await _customerRepository.GetAllAsync(ct);
        var customerNames = customers.ToDictionary(c => c.Id, c => c.FullName);

        var loansInRange = loans
            .Where(l => l.StartDate >= request.StartDate && l.StartDate <= request.EndDate)
            .OrderBy(l => l.StartDate);

        var csv = new StringBuilder();
        csv.AppendLine("Loan Number,Customer,Principal,Start Date,Due Date,Status,Total Interest,Total Paid,Balance");

        foreach (var loan in loansInRange)
        {
            var customerName = customerNames.GetValueOrDefault(loan.CustomerId, "Unknown");
            csv.AppendLine(string.Join(',', new[]
            {
                MappingExtensions.FormatLoanNumber(loan.LoanNumber),
                Escape(customerName),
                loan.PrincipalAmount.Amount.ToString(),
                loan.StartDate.ToString("yyyy-MM-dd"),
                loan.DueDate.ToString("yyyy-MM-dd"),
                loan.Status.ToString(),
                loan.TotalInterest.Amount.ToString(),
                loan.TotalPaid.Amount.ToString(),
                loan.Balance.Amount.ToString(),
            }));
        }

        return csv.ToString();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}

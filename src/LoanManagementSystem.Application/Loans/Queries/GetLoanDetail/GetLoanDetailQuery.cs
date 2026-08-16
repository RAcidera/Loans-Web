using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GetLoanDetail;

public sealed record GetLoanDetailQuery(string LoanId) : IRequest<LoanDetailDto>;

/// <summary>Composes one loan + its extensions + its payments — the same shape Angular's GetLoanDetailUseCase builds client-side via forkJoin.</summary>
public sealed class GetLoanDetailQueryHandler : IRequestHandler<GetLoanDetailQuery, LoanDetailDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;

    public GetLoanDetailQueryHandler(ILoanRepository loanRepository, ICustomerRepository customerRepository)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
    }

    public async Task<LoanDetailDto> Handle(GetLoanDetailQuery request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct);
        if (loan is null)
            return new LoanDetailDto(null, new List<LoanExtensionDto>(), new List<PaymentDto>());

        loan.RefreshOverdueStatus(DateOnly.FromDateTime(DateTime.UtcNow));

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct);
        var loanDto = loan.ToDto(customer?.FullName ?? "Unknown", customer?.ContactNumber);

        var extensions = loan.Extensions.OrderBy(e => e.ExtensionDate).ThenBy(e => e.CreatedAtUtc).Select(e => e.ToDto()).ToList();
        var payments = loan.Payments.OrderBy(p => p.PaymentDate).Select(p => p.ToDto()).ToList();

        return new LoanDetailDto(loanDto, extensions, payments);
    }
}

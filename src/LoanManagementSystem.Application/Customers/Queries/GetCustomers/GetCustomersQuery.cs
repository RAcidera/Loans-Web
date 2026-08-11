using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery : IRequest<List<CustomerDto>>;

public sealed class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomersQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<List<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var customers = await _customerRepository.GetAllAsync(ct);
        return customers.Select(c => c.ToDto()).ToList();
    }
}

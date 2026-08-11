using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(
    string FullName,
    string Address,
    string ContactNumber,
    string BorrowerType,
    string? NicknameAlias = null,
    string? Notes = null
) : IRequest<CustomerDto>;

public sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var customer = Customer.Create(request.FullName, request.Address, request.ContactNumber, request.BorrowerType, request.NicknameAlias ?? "", request.Notes ?? "");

        _customerRepository.Add(customer);
        await _unitOfWork.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}

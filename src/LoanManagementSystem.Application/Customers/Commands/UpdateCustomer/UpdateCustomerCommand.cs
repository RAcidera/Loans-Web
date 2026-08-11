using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Customers.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    string CustomerId,
    string FullName,
    string Address,
    string ContactNumber,
    string BorrowerType,
    string? NicknameAlias = null,
    string? Notes = null
) : IRequest<CustomerDto?>;

public sealed class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto?> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await _customerRepository.GetByIdAsync(CustomerId.Parse(request.CustomerId), ct);
        if (customer is null)
            return null;

        customer.UpdateProfile(request.FullName, request.Address, request.ContactNumber, request.BorrowerType, request.NicknameAlias ?? "", request.Notes ?? "");
        await _unitOfWork.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}

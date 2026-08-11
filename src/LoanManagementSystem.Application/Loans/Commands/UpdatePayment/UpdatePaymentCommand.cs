using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.UpdatePayment;

public sealed record UpdatePaymentCommand(
    string LoanId,
    string PaymentId,
    decimal AmountPaid,
    string PaymentMethod,
    string? Notes,
    string? ReferenceNumber,
    string? PaymentDate
) : IRequest<PaymentDto>;

public sealed class UpdatePaymentCommandHandler : IRequestHandler<UpdatePaymentCommand, PaymentDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePaymentCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentDto> Handle(UpdatePaymentCommand request, CancellationToken ct)
    {
        var loan = await _loanRepository.GetByIdAsync(LoanId.Parse(request.LoanId), ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var paymentId = PaymentId.Parse(request.PaymentId);
        var existing = loan.Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        var payment = loan.EditPayment(
            paymentId,
            Money.Of(request.AmountPaid),
            MappingExtensions.ParsePaymentMethod(request.PaymentMethod),
            request.Notes ?? string.Empty,
            request.ReferenceNumber,
            request.PaymentDate is not null ? DateOnly.Parse(request.PaymentDate) : existing.PaymentDate);

        await _unitOfWork.SaveChangesAsync(ct);

        return payment.ToDto();
    }
}

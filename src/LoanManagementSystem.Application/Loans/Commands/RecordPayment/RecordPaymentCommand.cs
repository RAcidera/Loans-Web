using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Commands.RecordPayment;

public sealed record RecordPaymentCommand(
    string LoanId,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber = null
) : IRequest<PaymentDto>;

public sealed class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordPaymentCommandHandler(ILoanRepository loanRepository, IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentDto> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var payment = loan.RecordPayment(
            Money.Of(request.AmountPaid),
            MappingExtensions.ParsePaymentMethod(request.PaymentMethod),
            request.Notes,
            DateOnly.FromDateTime(DateTime.UtcNow),
            request.ReferenceNumber);

        // No explicit repository.Add() needed — `loan` is already tracked by
        // EF Core from GetByIdAsync, and Payment is a child entity reached
        // via the Loan aggregate's navigation, so SaveChanges persists both
        // the updated Loan and the new Payment together.
        await _unitOfWork.SaveChangesAsync(ct); // also flushes PaymentRecordedDomainEvent → payment_received ledger entry

        return payment.ToDto();
    }
}

using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;
using DomainCashLedgerEntry = LoanManagementSystem.Domain.CashLedger.CashLedgerEntry;

namespace LoanManagementSystem.Application.CashLedger.Commands.AddCashTransaction;

/// <summary>
/// For the manual entries an admin types in from the Cash & Funds page
/// (owner deposit / withdrawal / expense). loan_release and
/// payment_received are system-generated as side effects of
/// CreateLoanCommand and RecordPaymentCommand via domain events, and
/// deliberately not exposed here — allowing them through this endpoint
/// would let someone double-count a loan disbursement or a payment that
/// already has its own ledger entry.
/// </summary>
public sealed record AddCashTransactionCommand(
    string TransactionType,
    decimal Amount,
    string Remarks
) : IRequest<CashLedgerEntryDto>;

public sealed class AddCashTransactionCommandHandler : IRequestHandler<AddCashTransactionCommand, CashLedgerEntryDto>
{
    private static readonly HashSet<string> ManualTypes = new() { "owner_deposit", "owner_withdrawal", "expense" };

    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddCashTransactionCommandHandler(ICashLedgerRepository cashLedgerRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CashLedgerEntryDto> Handle(AddCashTransactionCommand request, CancellationToken ct)
    {
        if (!ManualTypes.Contains(request.TransactionType))
            throw new Domain.Common.DomainException(
                $"'{request.TransactionType}' cannot be entered manually — loan_release and payment_received are created automatically.");

        var type = MappingExtensions.ParseCashTransactionType(request.TransactionType);
        var entry = DomainCashLedgerEntry.Record(type, Money.Of(request.Amount), request.Remarks, DateOnly.FromDateTime(DateTime.UtcNow));

        _cashLedgerRepository.Add(entry);
        await _unitOfWork.SaveChangesAsync(ct);

        return entry.ToDto();
    }
}

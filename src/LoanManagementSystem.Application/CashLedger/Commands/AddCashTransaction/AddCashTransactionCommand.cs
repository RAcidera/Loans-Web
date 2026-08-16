using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;
using DomainCashLedgerEntry = LoanManagementSystem.Domain.CashLedger.CashLedgerEntry;

namespace LoanManagementSystem.Application.CashLedger.Commands.AddCashTransaction;

/// <summary>
/// For the manual entries an admin types in from the Cash Transactions page
/// (owner deposit / withdrawal / expense / adjustment). loan_release and
/// payment_received are system-generated as side effects of
/// CreateLoanCommand and RecordPaymentCommand via domain events, and
/// deliberately not exposed here — allowing them through this endpoint
/// would let someone double-count a loan disbursement or a payment that
/// already has its own ledger entry.
/// </summary>
public sealed record AddCashTransactionCommand(
    string TransactionType,
    decimal Amount,
    string Remarks,
    /// <summary>"yyyy-MM-dd"; defaults to today (server time) when omitted.</summary>
    string? TransactionDate = null,
    /// <summary>Required only when TransactionType is "adjustment" — every other manual type has a fixed direction.</summary>
    bool? IsCashIn = null
) : IRequest<CashLedgerEntryDto>;

public sealed class AddCashTransactionCommandHandler : IRequestHandler<AddCashTransactionCommand, CashLedgerEntryDto>
{
    private static readonly HashSet<string> ManualTypes = new() { "owner_deposit", "owner_withdrawal", "expense", "adjustment" };

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
        var transactionDate = request.TransactionDate is not null
            ? DateOnly.Parse(request.TransactionDate)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var entry = DomainCashLedgerEntry.Record(type, Money.Of(request.Amount), request.Remarks, transactionDate, isCashIn: request.IsCashIn);

        _cashLedgerRepository.Add(entry);
        await _unitOfWork.SaveChangesAsync(ct);

        return entry.ToDto();
    }
}

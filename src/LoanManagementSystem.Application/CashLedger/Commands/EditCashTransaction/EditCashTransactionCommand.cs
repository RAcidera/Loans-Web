using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using MediatR;
using DomainCashLedgerEntry = LoanManagementSystem.Domain.CashLedger.CashLedgerEntry;

namespace LoanManagementSystem.Application.CashLedger.Commands.EditCashTransaction;

/// <summary>The Cash Transactions grid's row-menu "Edit" action — manually-entered rows only, see CashLedgerEntry.EditManual.</summary>
public sealed record EditCashTransactionCommand(
    string LedgerId,
    string TransactionType,
    decimal Amount,
    string Remarks,
    string TransactionDate,
    bool? IsCashIn = null
) : IRequest<CashLedgerEntryDto>;

public sealed class EditCashTransactionCommandHandler : IRequestHandler<EditCashTransactionCommand, CashLedgerEntryDto>
{
    private static readonly HashSet<string> ManualTypes = new() { "owner_deposit", "owner_withdrawal", "expense", "adjustment" };

    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditCashTransactionCommandHandler(ICashLedgerRepository cashLedgerRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CashLedgerEntryDto> Handle(EditCashTransactionCommand request, CancellationToken ct)
    {
        if (!ManualTypes.Contains(request.TransactionType))
            throw new Domain.Common.DomainException($"'{request.TransactionType}' is not a valid manual transaction type.");

        var entry = await _cashLedgerRepository.GetByIdAsync(CashLedgerEntryId.Parse(request.LedgerId), ct)
            ?? throw new NotFoundException(nameof(DomainCashLedgerEntry), request.LedgerId);

        var type = MappingExtensions.ParseCashTransactionType(request.TransactionType);
        entry.EditManual(type, Money.Of(request.Amount), request.Remarks, DateOnly.Parse(request.TransactionDate), request.IsCashIn);

        await _unitOfWork.SaveChangesAsync(ct);
        return entry.ToDto();
    }
}

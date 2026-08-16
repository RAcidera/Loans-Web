using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using MediatR;
using DomainCashLedgerEntry = LoanManagementSystem.Domain.CashLedger.CashLedgerEntry;

namespace LoanManagementSystem.Application.CashLedger.Commands.DeleteCashTransaction;

/// <summary>The Cash Transactions grid's row-menu "Delete" action — manually-entered rows only; automatic rows are rejected here too, not just hidden in the UI (see CashLedgerEntry.IsAutomatic).</summary>
public sealed record DeleteCashTransactionCommand(string LedgerId) : IRequest;

public sealed class DeleteCashTransactionCommandHandler : IRequestHandler<DeleteCashTransactionCommand>
{
    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCashTransactionCommandHandler(ICashLedgerRepository cashLedgerRepository, IUnitOfWork unitOfWork)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteCashTransactionCommand request, CancellationToken ct)
    {
        var entry = await _cashLedgerRepository.GetByIdAsync(CashLedgerEntryId.Parse(request.LedgerId), ct)
            ?? throw new NotFoundException(nameof(DomainCashLedgerEntry), request.LedgerId);

        if (entry.IsAutomatic)
            throw new Domain.Common.DomainException("Automatically generated transactions cannot be deleted from the Cash Ledger.");

        _cashLedgerRepository.Remove(entry);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}

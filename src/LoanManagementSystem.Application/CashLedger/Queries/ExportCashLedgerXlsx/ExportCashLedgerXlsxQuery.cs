using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Xlsx;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.CashLedger.Queries.ExportCashLedgerXlsx;

/// <summary>Same filters as GetCashLedgerPageQuery, no paging — the Cash Transactions grid's "Export" button exports the whole filtered result set, newest first, same order as the grid.</summary>
public sealed record ExportCashLedgerXlsxQuery(string? Search, string? TransactionType, string? DateFrom, string? DateTo) : IRequest<DocumentFileDto>;

public sealed class ExportCashLedgerXlsxQueryHandler : IRequestHandler<ExportCashLedgerXlsxQuery, DocumentFileDto>
{
    private static readonly Dictionary<CashTransactionType, string> TypeLabel = new()
    {
        [CashTransactionType.LoanRelease] = "Loan Release",
        [CashTransactionType.PaymentReceived] = "Payment Received",
        [CashTransactionType.OwnerDeposit] = "Cash Deposit",
        [CashTransactionType.OwnerWithdrawal] = "Cash Withdrawal",
        [CashTransactionType.Expense] = "Expense",
        [CashTransactionType.Adjustment] = "Adjustment",
    };

    private readonly ICashLedgerRepository _cashLedgerRepository;
    private readonly ICashLedgerXlsxExportGenerator _xlsxGenerator;
    private readonly IAppDateTimeService _appDateTime;

    public ExportCashLedgerXlsxQueryHandler(ICashLedgerRepository cashLedgerRepository, ICashLedgerXlsxExportGenerator xlsxGenerator, IAppDateTimeService appDateTime)
    {
        _cashLedgerRepository = cashLedgerRepository;
        _xlsxGenerator = xlsxGenerator;
        _appDateTime = appDateTime;
    }

    public async Task<DocumentFileDto> Handle(ExportCashLedgerXlsxQuery request, CancellationToken ct)
    {
        var all = await _cashLedgerRepository.GetAllAsync(ct);

        // Same "compute running balance over the full unfiltered ledger
        // first" rule as GetCashLedgerPageQuery — see that handler's comment.
        var runningBalance = new Dictionary<CashLedgerEntryId, decimal>();
        var balance = 0m;
        foreach (var entry in all.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc))
        {
            balance += entry.SignedAmount;
            runningBalance[entry.Id] = balance;
        }

        var filtered = CashLedgerFilterHelper.Apply(all, request.Search, request.TransactionType, request.DateFrom, request.DateTo)
            .OrderByDescending(e => e.TransactionDate)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToList();

        var rows = filtered
            .Select(e => new CashLedgerExportRowDto(
                TransactionDate: e.TransactionDate.ToString("yyyy-MM-dd"),
                Transaction: TypeLabel[e.TransactionType],
                Reference: e.ReferenceId,
                CashIn: e.IsCashIn ? e.Amount.Amount : null,
                CashOut: e.IsCashIn ? null : e.Amount.Amount,
                RunningBalance: runningBalance[e.Id],
                Remarks: e.Remarks))
            .ToList();

        var bytes = _xlsxGenerator.Generate(rows);
        var today = _appDateTime.Today;
        return new DocumentFileDto($"cash_transactions_export_{today:yyyy-MM-dd}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", bytes);
    }
}

using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanManagementSystem.Application.Loans.Queries.GenerateLoanSoaV2;

public sealed record GenerateLoanSoaV2Query(string LoanId) : IRequest<DocumentFileDto>;

/// <summary>
/// SOA V2 — assembles ONE chronological "Account Activity" ledger straight
/// from loan_ledger (every LoanReleased/InterestAdded/Payment/Extension row
/// in TransactionDate, then CreatedAtUtc order, per
/// assets/Statement of Account V2.md), instead of V1's GenerateLoanSoaQuery
/// approach of building separate Extension History / Payment History
/// tables from loan.Extensions/loan.Payments. Does not modify or replace
/// GenerateLoanSoaQuery; the two coexist behind separate endpoints.
/// </summary>
public sealed class GenerateLoanSoaV2QueryHandler : IRequestHandler<GenerateLoanSoaV2Query, DocumentFileDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IStatementOfAccountV2PdfGenerator _pdfGenerator;
    private readonly IAppDateTimeService _appDateTime;
    private readonly ILogger<GenerateLoanSoaV2QueryHandler> _logger;

    public GenerateLoanSoaV2QueryHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository,
        ILoanLedgerRepository loanLedgerRepository, IStatementOfAccountV2PdfGenerator pdfGenerator,
        IAppDateTimeService appDateTime, ILogger<GenerateLoanSoaV2QueryHandler> logger)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _pdfGenerator = pdfGenerator;
        _appDateTime = appDateTime;
        _logger = logger;
    }

    public async Task<DocumentFileDto> Handle(GenerateLoanSoaV2Query request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct)
            ?? throw new NotFoundException("Customer", loan.CustomerId.ToString());

        var ledger = await _loanLedgerRepository.GetByLoanIdAsync(loanId, ct);

        // Deterministic chronological order: TransactionDate first, then
        // CreatedAtUtc as the secondary sort so same-day entries (e.g. the
        // LoanReleased/InterestAdded rows recorded together at origination)
        // never depend on undefined database ordering.
        var ordered = ledger.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc).ToList();

        var runningBalance = 0m;
        var totalDebit = 0m;
        var totalCredit = 0m;
        var activity = new List<SoaLedgerRowDto>(ordered.Count);
        foreach (var entry in ordered)
        {
            runningBalance += entry.Debit.Amount;
            runningBalance -= entry.Credit.Amount;
            totalDebit += entry.Debit.Amount;
            totalCredit += entry.Credit.Amount;

            activity.Add(new SoaLedgerRowDto(
                entry.TransactionDate.ToString("yyyy-MM-dd"),
                entry.TransactionType.ToWireString(),
                entry.Remarks,
                entry.Debit.Amount,
                entry.Credit.Amount,
                runningBalance));
        }

        // Reconciliation check (spec §9): the ledger-derived balance must
        // agree with the loan aggregate's own Balance. Never silently force
        // one to match the other — this is a read-only reporting feature,
        // so a mismatch is only logged (with the loan id and both amounts)
        // for investigation, not corrected here.
        var ledgerBalance = totalDebit - totalCredit;
        if (Math.Abs(ledgerBalance - loan.Balance.Amount) > 0.01m)
        {
            _logger.LogWarning(
                "SOA V2 reconciliation mismatch for Loan {LoanId}: ledger balance {LedgerBalance:N2} does not match loan outstanding balance {OutstandingBalance:N2} (discrepancy {Discrepancy:N2}).",
                request.LoanId, ledgerBalance, loan.Balance.Amount, ledgerBalance - loan.Balance.Amount);
        }

        var statement = new StatementOfAccountV2Dto(
            CustomerName: customer.FullName,
            CustomerCode: MappingExtensions.FormatCustomerCode(customer.CustomerNumber),
            CustomerAddress: customer.Address,
            CustomerContactNumber: customer.ContactNumber,
            LoanNumber: MappingExtensions.FormatLoanNumber(loan.LoanNumber),
            StatementDate: _appDateTime.Today.ToString("yyyy-MM-dd"),
            LoanDate: loan.StartDate.ToString("yyyy-MM-dd"),
            DueDate: loan.DueDate.ToString("yyyy-MM-dd"),
            PrincipalAmount: loan.PrincipalAmount.Amount,
            InterestRate: loan.InterestRate.Value,
            InterestAmount: loan.TotalInterest.Amount,
            Activity: activity,
            TotalDebit: totalDebit,
            TotalCredit: totalCredit,
            TotalExtensionCharges: loan.TotalExtensionCharges.Amount,
            TotalPaid: loan.TotalPaid.Amount,
            OutstandingBalance: loan.Balance.Amount,
            Status: loan.Status.ToString(),
            Classification: loan.Classification.ToString());

        var bytes = _pdfGenerator.Generate(statement);
        var fileName = $"SOA-V2-{statement.LoanNumber}.pdf";
        return new DocumentFileDto(fileName, "application/pdf", bytes);
    }
}

using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Loans.Queries.GenerateLoanSoa;

public sealed record GenerateLoanSoaQuery(string LoanId) : IRequest<DocumentFileDto>;

/// <summary>
/// Assembles Customer Info, Loan Info, Extension History, Payment History
/// (sourced from the Phase 6 ledger for authoritative running balances —
/// not re-derived from Payments/Extensions ad hoc, though the balance
/// itself is recomputed chronologically over that ledger rather than
/// trusting each row's stamped snapshot; see the comment in Handle), and
/// Summary into a PDF via IStatementOfAccountPdfGenerator.
/// </summary>
public sealed class GenerateLoanSoaQueryHandler : IRequestHandler<GenerateLoanSoaQuery, DocumentFileDto>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILoanLedgerRepository _loanLedgerRepository;
    private readonly IStatementOfAccountPdfGenerator _pdfGenerator;
    private readonly IAppDateTimeService _appDateTime;

    public GenerateLoanSoaQueryHandler(
        ILoanRepository loanRepository, ICustomerRepository customerRepository,
        ILoanLedgerRepository loanLedgerRepository, IStatementOfAccountPdfGenerator pdfGenerator,
        IAppDateTimeService appDateTime)
    {
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _loanLedgerRepository = loanLedgerRepository;
        _pdfGenerator = pdfGenerator;
        _appDateTime = appDateTime;
    }

    public async Task<DocumentFileDto> Handle(GenerateLoanSoaQuery request, CancellationToken ct)
    {
        var loanId = LoanId.Parse(request.LoanId);
        var loan = await _loanRepository.GetByIdAsync(loanId, ct)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var customer = await _customerRepository.GetByIdAsync(loan.CustomerId, ct)
            ?? throw new NotFoundException("Customer", loan.CustomerId.ToString());

        var ledger = await _loanLedgerRepository.GetByLoanIdAsync(loanId, ct);

        // Each entry's stamped RunningBalance reflects the loan's Balance at
        // RECORDING time (see LoanLedgerEntry's doc comment) — correct only
        // when entries are recorded in date order. An antedated payment (or
        // one edited to an earlier date after later payments already exist)
        // breaks that assumption, so the balance shown against each payment
        // row here is recomputed chronologically from SignedAmount over the
        // FULL ledger, sorted by TransactionDate, instead of trusted as-is.
        var runningBalance = 0m;
        var paymentBalanceByPaymentId = new Dictionary<string, decimal>();
        foreach (var entry in ledger.OrderBy(e => e.TransactionDate).ThenBy(e => e.CreatedAtUtc))
        {
            runningBalance += entry.SignedAmount;
            if (entry.TransactionType == LoanLedgerTransactionType.Payment && entry.ReferenceId is not null)
                paymentBalanceByPaymentId[entry.ReferenceId] = runningBalance;
        }

        var extensions = loan.Extensions
            .OrderBy(e => e.ExtensionDate)
            .ThenBy(e => e.CreatedAtUtc)
            .Select(e => new SoaExtensionRowDto(
                e.ExtensionDate.ToString("yyyy-MM-dd"),
                e.AdditionalChargesAmount.Amount,
                loan.DueDate.ToString("yyyy-MM-dd"),
                e.Remarks))
            .ToList();

        var payments = loan.Payments
            .OrderBy(p => p.PaymentDate)
            .Select(p => new SoaPaymentRowDto(
                p.PaymentDate.ToString("yyyy-MM-dd"),
                p.AmountPaid.Amount,
                p.PaymentMethod.ToWireString(),
                p.ReferenceNumber,
                paymentBalanceByPaymentId.GetValueOrDefault(p.Id.ToString(), loan.Balance.Amount),
                p.Notes))
            .ToList();

        var statement = new StatementOfAccountDto(
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
            Extensions: extensions,
            Payments: payments,
            TotalExtensionCharges: loan.TotalExtensionCharges.Amount,
            TotalAmountDue: loan.TotalAmountDue.Amount,
            TotalPaid: loan.TotalPaid.Amount,
            OutstandingBalance: loan.Balance.Amount,
            Status: loan.Status.ToString(),
            Classification: loan.Classification.ToString());

        var bytes = _pdfGenerator.Generate(statement);
        var fileName = $"SOA-{statement.LoanNumber}.pdf";
        return new DocumentFileDto(fileName, "application/pdf", bytes);
    }
}

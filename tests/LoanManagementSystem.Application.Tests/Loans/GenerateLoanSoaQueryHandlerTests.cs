using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Application.Loans.Queries.GenerateLoanSoa;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GenerateLoanSoaQueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<IStatementOfAccountPdfGenerator> _pdfGenerator = new();
    private readonly Mock<IAppDateTimeService> _appDateTime = new();
    private readonly GenerateLoanSoaQueryHandler _handler;

    public GenerateLoanSoaQueryHandlerTests()
    {
        _appDateTime.Setup(s => s.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        _handler = new GenerateLoanSoaQueryHandler(_loanRepository.Object, _customerRepository.Object, _loanLedgerRepository.Object, _pdfGenerator.Object, _appDateTime.Object);
    }

    [Fact]
    public async Task Handle_AssemblesStatementFromLoanCustomerAndLedger_PassesToGenerator()
    {
        var customer = Customer.Create("Ana Villanueva", "45 Rizal Ave.", "+63 919 333 5566", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1), 60);
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 2, 1));
        loan.Extend(15, Money.Of(50), "grace period", new DateOnly(2026, 3, 1));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ledgerReleaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 1, 1));
        var ledgerInterestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030),
            "Interest added", new DateOnly(2026, 1, 1));
        var ledgerPaymentEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(400), Money.Of(630),
            "Payment received", new DateOnly(2026, 2, 1), referenceId: payment.Id.ToString());
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { ledgerReleaseEntry, ledgerInterestEntry, ledgerPaymentEntry });

        StatementOfAccountDto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountDto>()))
            .Callback<StatementOfAccountDto>(dto => captured = dto)
            .Returns(new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(new GenerateLoanSoaQuery(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Ana Villanueva", captured!.CustomerName);
        Assert.Equal(1000m, captured.PrincipalAmount);
        Assert.Single(captured.Payments);
        Assert.Equal(630m, captured.Payments[0].RunningBalance);
        Assert.Single(captured.Extensions);
        Assert.Equal(loan.Balance.Amount, captured.OutstandingBalance);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
    }

    /// <summary>
    /// Regression test for an antedated/backfilled payment: Payment A (300)
    /// is recorded first against 2026-02-20, then Payment B (200) is
    /// recorded second but dated earlier, 2026-02-10 (a "previously missed"
    /// payment entered after the fact). Each ledger row's stamped
    /// RunningBalance reflects the loan's Balance at RECORDING time (A: 730,
    /// B: 530) — correct insertion order, wrong date order. The SOA sorts
    /// payments by PaymentDate, so trusting those stamped values would show
    /// B (earlier date) at 530 and A (later date) at 730 — balance going UP
    /// as later payments are listed, which is impossible. The fix
    /// recomputes chronologically from the full ledger instead, so B (the
    /// earlier payment) must show 830 and A (the later payment) 530.
    /// </summary>
    [Fact]
    public async Task Handle_PaymentRecordedOutOfDateOrder_RunningBalanceReflectsChronologicalOrderNotRecordingOrder()
    {
        var customer = Customer.Create("Ben Cruz", "12 Mabini St.", "+63 918 222 4444", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1), 60);
        var paymentA = loan.RecordPayment(Money.Of(300), PaymentMethod.Cash, "", new DateOnly(2026, 2, 20));
        var paymentB = loan.RecordPayment(Money.Of(200), PaymentMethod.Cash, "missed payment", new DateOnly(2026, 2, 10));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ledgerReleaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 1, 1));
        var ledgerInterestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030),
            "Interest added", new DateOnly(2026, 1, 1));
        var ledgerPaymentAEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(300), Money.Of(730),
            "Payment received", new DateOnly(2026, 2, 20), referenceId: paymentA.Id.ToString());
        var ledgerPaymentBEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(200), Money.Of(530),
            "Payment received", new DateOnly(2026, 2, 10), referenceId: paymentB.Id.ToString());
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { ledgerReleaseEntry, ledgerInterestEntry, ledgerPaymentAEntry, ledgerPaymentBEntry });

        StatementOfAccountDto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountDto>()))
            .Callback<StatementOfAccountDto>(dto => captured = dto)
            .Returns(new byte[] { 1, 2, 3 });

        await _handler.Handle(new GenerateLoanSoaQuery(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Payments.Count);
        Assert.Equal("2026-02-10", captured.Payments[0].PaymentDate);
        Assert.Equal(830m, captured.Payments[0].RunningBalance);
        Assert.Equal("2026-02-20", captured.Payments[1].PaymentDate);
        Assert.Equal(530m, captured.Payments[1].RunningBalance);
    }

    /// <summary>
    /// Regression test for editing a loan's disbursement date: the loan was
    /// entered with the wrong StartDate (2026-03-01), then corrected to the
    /// true date (2026-01-01) via EditLoan — which LoanOriginationEditedEventHandler
    /// now mirrors onto this loan's own LoanReleased/InterestAdded ledger
    /// rows (see that handler), not just the corrected loan itself. This
    /// test builds the ledger as it looks AFTER that handler ran (both rows
    /// dated 2026-01-01) with a payment recorded 2026-01-15 — chronologically
    /// after the corrected start date. Before the fix, those rows would
    /// still carry the stale 2026-03-01 date, so this payment would sort
    /// BEFORE the loan's own origination rows and show a nonsensical
    /// negative running balance (0 - 250 = -250) instead of the true 780
    /// (1000 principal + 30 interest - 250 paid).
    /// </summary>
    [Fact]
    public async Task Handle_LoanStartDateCorrectedAfterOriginPayments_RunningBalanceStaysCorrect()
    {
        var customer = Customer.Create("Cora Santos", "3 Rizal St.", "+63 917 111 2233", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0.03m), new DateOnly(2026, 3, 1), 60);
        loan.EditLoan(new DateOnly(2026, 1, 1), null, null, null, null, "admin");
        var payment = loan.RecordPayment(Money.Of(250), PaymentMethod.Cash, "", new DateOnly(2026, 1, 15));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ledgerReleaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 1, 1));
        var ledgerInterestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030),
            "Interest added", new DateOnly(2026, 1, 1));
        var ledgerPaymentEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(250), Money.Of(780),
            "Payment received", new DateOnly(2026, 1, 15), referenceId: payment.Id.ToString());
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { ledgerReleaseEntry, ledgerInterestEntry, ledgerPaymentEntry });

        StatementOfAccountDto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountDto>()))
            .Callback<StatementOfAccountDto>(dto => captured = dto)
            .Returns(new byte[] { 1, 2, 3 });

        await _handler.Handle(new GenerateLoanSoaQuery(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Single(captured!.Payments);
        Assert.Equal(780m, captured.Payments[0].RunningBalance);
    }

    /// <summary>
    /// Regression test for editing an extension's fee: the extension was
    /// entered with a wrong charge (20), then corrected to 35 via
    /// EditExtension — which LoanExtensionEditedEventHandler now mirrors
    /// onto the extension's own loan_ledger row (see that handler). This
    /// test builds the ledger as it looks AFTER that handler ran (Extension
    /// row's Debit corrected to 35) with a payment recorded after the
    /// extension. Before the fix, that row would still carry the stale 20
    /// charge, so the payment's running balance would read 720 instead of
    /// the true 735 (1000 principal + 35 corrected charge - 300 paid).
    /// </summary>
    [Fact]
    public async Task Handle_ExtensionChargeCorrectedAfterPayment_RunningBalanceStaysCorrect()
    {
        var customer = Customer.Create("Fina Torres", "8 Aguinaldo St.", "+63 917 888 3344", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0), new DateOnly(2026, 1, 1), 60);
        var extension = loan.Extend(20, Money.Of(20), "grace period", new DateOnly(2026, 1, 20));
        loan.EditExtension(extension.Id, 20, Money.Of(35), "corrected fee", new DateOnly(2026, 1, 20));
        var payment = loan.RecordPayment(Money.Of(300), PaymentMethod.Cash, "", new DateOnly(2026, 1, 25));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ledgerReleaseEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 1, 1));
        var ledgerInterestEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Zero, Money.Zero, Money.Of(1000),
            "Interest added", new DateOnly(2026, 1, 1));
        var ledgerExtensionEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Extension, Money.Of(35), Money.Zero, Money.Of(1035),
            "Extension +20 days", new DateOnly(2026, 1, 20), referenceId: extension.Id.ToString());
        var ledgerPaymentEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(300), Money.Of(735),
            "Payment received", new DateOnly(2026, 1, 25), referenceId: payment.Id.ToString());
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { ledgerReleaseEntry, ledgerInterestEntry, ledgerExtensionEntry, ledgerPaymentEntry });

        StatementOfAccountDto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountDto>()))
            .Callback<StatementOfAccountDto>(dto => captured = dto)
            .Returns(new byte[] { 1, 2, 3 });

        await _handler.Handle(new GenerateLoanSoaQuery(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Single(captured!.Payments);
        Assert.Equal(735m, captured.Payments[0].RunningBalance);
        Assert.Equal(loan.Balance.Amount, captured.OutstandingBalance);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GenerateLoanSoaQuery(LoanId.New().ToString()), CancellationToken.None));
    }
}

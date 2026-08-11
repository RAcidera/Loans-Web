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
    private readonly GenerateLoanSoaQueryHandler _handler;

    public GenerateLoanSoaQueryHandlerTests()
    {
        _handler = new GenerateLoanSoaQueryHandler(_loanRepository.Object, _customerRepository.Object, _loanLedgerRepository.Object, _pdfGenerator.Object);
    }

    [Fact]
    public async Task Handle_AssemblesStatementFromLoanCustomerAndLedger_PassesToGenerator()
    {
        var customer = Customer.Create("Ana Villanueva", "45 Rizal Ave.", "+63 919 333 5566", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(1000), InterestRate.Of(0.03m), new DateOnly(2026, 1, 1), 60);
        var payment = loan.RecordPayment(Money.Of(400), PaymentMethod.Cash, "", new DateOnly(2026, 2, 1));
        loan.Extend(15, Money.Of(20), Money.Of(50), "grace period", new DateOnly(2026, 3, 1));

        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var ledgerPaymentEntry = LoanLedgerEntry.Record(
            loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(400), Money.Of(630),
            "Payment received", new DateOnly(2026, 2, 1), referenceId: payment.Id.ToString());
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { ledgerPaymentEntry });

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

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GenerateLoanSoaQuery(LoanId.New().ToString()), CancellationToken.None));
    }
}

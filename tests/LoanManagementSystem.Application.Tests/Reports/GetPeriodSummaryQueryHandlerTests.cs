using LoanManagementSystem.Application.Reports.Queries.GetPeriodSummary;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Reports;

public class GetPeriodSummaryQueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly GetPeriodSummaryQueryHandler _handler;

    public GetPeriodSummaryQueryHandlerTests()
    {
        _handler = new GetPeriodSummaryQueryHandler(_loanRepository.Object);
    }

    [Fact]
    public async Task Handle_AggregatesOnlyWhatFallsInsideTheRange()
    {
        // Originates inside the range (June), and its payment also falls inside the range.
        var loanA = Loan.Originate(CustomerId.New(), Money.Of(1000), InterestRate.Default, new DateOnly(2026, 6, 15));
        loanA.RecordPayment(Money.Of(300), PaymentMethod.Cash, "", new DateOnly(2026, 6, 20));

        // Originates before the range, but has a payment and an extension that fall inside it —
        // those should still count even though the loan itself doesn't count as "originated" in range.
        var loanB = Loan.Originate(CustomerId.New(), Money.Of(2000), InterestRate.Default, new DateOnly(2026, 1, 1));
        loanB.RecordPayment(Money.Of(500), PaymentMethod.Cash, "", new DateOnly(2026, 6, 10));
        loanB.Extend(15, Money.Of(50), Money.Zero, "late", new DateOnly(2026, 6, 25));

        // Entirely outside the range — nothing about it should affect any total.
        var loanC = Loan.Originate(CustomerId.New(), Money.Of(5000), InterestRate.Default, new DateOnly(2026, 7, 1));
        loanC.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 7, 5));

        _loanRepository.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { loanA, loanB, loanC });

        var query = new GetPeriodSummaryQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(1, result.LoansOriginated); // only loanA
        Assert.Equal(800m, result.PaymentsCollected); // loanA's 300 + loanB's 500; loanC's 1000 excluded
        Assert.Equal(1, result.ExtensionsGranted); // only loanB's
        Assert.Equal(loanA.TotalInterest.Amount, result.InterestEarned); // only loanA originated in range
    }

    [Fact]
    public async Task Handle_NoLoansInRange_ReturnsZeroedSummary()
    {
        _loanRepository.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan>());

        var result = await _handler.Handle(new GetPeriodSummaryQuery(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)), CancellationToken.None);

        Assert.Equal(0, result.LoansOriginated);
        Assert.Equal(0m, result.PaymentsCollected);
        Assert.Equal(0, result.ExtensionsGranted);
        Assert.Equal(0m, result.InterestEarned);
    }
}

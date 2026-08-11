using LoanManagementSystem.Application.Loans.Queries.GetLoanLedger;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GetLoanLedgerQueryHandlerTests
{
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly GetLoanLedgerQueryHandler _handler;

    public GetLoanLedgerQueryHandlerTests()
    {
        _handler = new GetLoanLedgerQueryHandler(_loanLedgerRepository.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntries_OrderedByTransactionDate()
    {
        var loanId = LoanId.New();
        var later = LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(500), Money.Of(9500), "Payment received", new DateOnly(2026, 2, 1));
        var earlier = LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.LoanReleased, Money.Of(10000), Money.Zero, Money.Of(10000), "Loan released", new DateOnly(2026, 1, 1));

        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { later, earlier });

        var result = await _handler.Handle(new GetLoanLedgerQuery(loanId.ToString()), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("2026-01-01", result[0].TransactionDate);
        Assert.Equal("2026-02-01", result[1].TransactionDate);
        Assert.Equal("loan_released", result[0].TransactionType);
        Assert.Equal("payment", result[1].TransactionType);
    }
}

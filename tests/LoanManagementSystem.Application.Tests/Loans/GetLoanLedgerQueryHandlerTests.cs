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

    /// <summary>
    /// Regression test for an antedated/backfilled payment: the 300 payment
    /// (dated 2026-02-20) is recorded first, so it's stamped with the
    /// balance-at-recording-time of 700; the 200 payment (dated 2026-02-10,
    /// a "previously missed" payment entered afterward) is recorded second
    /// and stamped 500. Sorted by TransactionDate for display, those stamped
    /// values would show balance going 500 then 700 — up, not down — which
    /// is impossible. The handler must recompute chronologically from
    /// SignedAmount instead, giving 800 for the earlier (02-10) payment and
    /// 500 for the later (02-20) one.
    /// </summary>
    [Fact]
    public async Task Handle_PaymentRecordedOutOfDateOrder_RunningBalanceReflectsChronologicalOrderNotRecordingOrder()
    {
        var loanId = LoanId.New();
        var release = LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", new DateOnly(2026, 1, 1));
        var paymentLaterDate = LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(300), Money.Of(700), "Payment received", new DateOnly(2026, 2, 20));
        var paymentEarlierDateRecordedSecond = LoanLedgerEntry.Record(loanId, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(200), Money.Of(500), "Payment received", new DateOnly(2026, 2, 10));

        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanLedgerEntry> { release, paymentLaterDate, paymentEarlierDateRecordedSecond });

        var result = await _handler.Handle(new GetLoanLedgerQuery(loanId.ToString()), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal("2026-02-10", result[1].TransactionDate);
        Assert.Equal(800m, result[1].RunningBalance);
        Assert.Equal("2026-02-20", result[2].TransactionDate);
        Assert.Equal(500m, result[2].RunningBalance);
    }
}

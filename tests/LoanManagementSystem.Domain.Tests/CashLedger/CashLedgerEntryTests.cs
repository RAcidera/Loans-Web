using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.CashLedger;

public class CashLedgerEntryTests
{
    [Theory]
    [InlineData(CashTransactionType.PaymentReceived, true)]
    [InlineData(CashTransactionType.OwnerDeposit, true)]
    [InlineData(CashTransactionType.LoanRelease, false)]
    [InlineData(CashTransactionType.OwnerWithdrawal, false)]
    [InlineData(CashTransactionType.Expense, false)]
    public void IsCashIn_MatchesSrsTransactionTypeEffectsTable(CashTransactionType type, bool expectedIsCashIn)
    {
        var entry = CashLedgerEntry.Record(type, Money.Of(100), "test", DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(expectedIsCashIn, entry.IsCashIn);
    }

    [Fact]
    public void SignedAmount_CashIn_IsPositive()
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.PaymentReceived, Money.Of(500), "", DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(500m, entry.SignedAmount);
    }

    [Fact]
    public void SignedAmount_CashOut_IsNegative()
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.Expense, Money.Of(300), "", DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(-300m, entry.SignedAmount);
    }

    [Fact]
    public void Record_ZeroOrNegativeAmount_Throws()
    {
        Assert.Throws<DomainException>(() =>
            CashLedgerEntry.Record(CashTransactionType.OwnerDeposit, Money.Of(0), "", DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}

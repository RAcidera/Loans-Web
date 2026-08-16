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

    [Fact]
    public void Record_Adjustment_WithoutIsCashIn_Throws()
    {
        Assert.Throws<DomainException>(() =>
            CashLedgerEntry.Record(CashTransactionType.Adjustment, Money.Of(100), "correction", DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Record_Adjustment_UsesCallerSuppliedDirection(bool isCashIn)
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.Adjustment, Money.Of(100), "correction", DateOnly.FromDateTime(DateTime.UtcNow), isCashIn: isCashIn);
        Assert.Equal(isCashIn, entry.IsCashIn);
    }

    [Theory]
    [InlineData(CashTransactionType.LoanRelease, true)]
    [InlineData(CashTransactionType.PaymentReceived, true)]
    [InlineData(CashTransactionType.OwnerDeposit, false)]
    [InlineData(CashTransactionType.OwnerWithdrawal, false)]
    [InlineData(CashTransactionType.Expense, false)]
    [InlineData(CashTransactionType.Adjustment, false)]
    public void IsAutomatic_OnlyTrueForLoanReleaseAndPaymentReceived(CashTransactionType type, bool expectedIsAutomatic)
    {
        var entry = CashLedgerEntry.Record(type, Money.Of(100), "", DateOnly.FromDateTime(DateTime.UtcNow), isCashIn: type == CashTransactionType.Adjustment ? true : null);
        Assert.Equal(expectedIsAutomatic, entry.IsAutomatic);
    }

    [Fact]
    public void EditManual_OnAutomaticEntry_Throws()
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.PaymentReceived, Money.Of(100), "", DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Throws<DomainException>(() =>
            entry.EditManual(CashTransactionType.Expense, Money.Of(50), "changed", DateOnly.FromDateTime(DateTime.UtcNow), null));
    }

    [Fact]
    public void EditManual_ToAutomaticType_Throws()
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.Expense, Money.Of(100), "", DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.Throws<DomainException>(() =>
            entry.EditManual(CashTransactionType.PaymentReceived, Money.Of(50), "changed", DateOnly.FromDateTime(DateTime.UtcNow), null));
    }

    [Fact]
    public void EditManual_OnManualEntry_UpdatesFieldsAndDirection()
    {
        var entry = CashLedgerEntry.Record(CashTransactionType.Expense, Money.Of(100), "old remarks", new DateOnly(2026, 1, 1));

        entry.EditManual(CashTransactionType.Adjustment, Money.Of(75), "new remarks", new DateOnly(2026, 2, 2), isCashIn: true);

        Assert.Equal(CashTransactionType.Adjustment, entry.TransactionType);
        Assert.Equal(75m, entry.Amount.Amount);
        Assert.Equal("new remarks", entry.Remarks);
        Assert.Equal(new DateOnly(2026, 2, 2), entry.TransactionDate);
        Assert.True(entry.IsCashIn);
    }
}

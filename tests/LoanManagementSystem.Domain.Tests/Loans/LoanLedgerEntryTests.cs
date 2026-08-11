using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.Loans;

public class LoanLedgerEntryTests
{
    private static readonly LoanId SomeLoan = LoanId.New();

    [Fact]
    public void Record_SetsFieldsFromArguments()
    {
        var date = new DateOnly(2026, 8, 1);
        var entry = LoanLedgerEntry.Record(
            SomeLoan, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(500), Money.Of(9500),
            "Payment received", date, referenceId: "some-payment-id");

        Assert.Equal(SomeLoan, entry.LoanId);
        Assert.Equal(LoanLedgerTransactionType.Payment, entry.TransactionType);
        Assert.Equal(0m, entry.Debit.Amount);
        Assert.Equal(500m, entry.Credit.Amount);
        Assert.Equal(9500m, entry.RunningBalance.Amount);
        Assert.Equal(date, entry.TransactionDate);
        Assert.Equal("some-payment-id", entry.ReferenceId);
    }

    [Fact]
    public void Record_WithNoReferenceId_DefaultsToNull()
    {
        var entry = LoanLedgerEntry.Record(
            SomeLoan, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan release", new DateOnly(2026, 8, 1));

        Assert.Null(entry.ReferenceId);
    }
}

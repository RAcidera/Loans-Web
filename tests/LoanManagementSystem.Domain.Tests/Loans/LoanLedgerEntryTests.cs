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

    [Fact]
    public void SignedAmount_IsDebitMinusCredit()
    {
        var debitEntry = LoanLedgerEntry.Record(SomeLoan, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan release", new DateOnly(2026, 8, 1));
        var creditEntry = LoanLedgerEntry.Record(SomeLoan, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(300), Money.Of(700), "Payment received", new DateOnly(2026, 8, 2));

        Assert.Equal(1000m, debitEntry.SignedAmount);
        Assert.Equal(-300m, creditEntry.SignedAmount);
    }

    [Fact]
    public void ReviseDebit_UpdatesDebitAndTransactionDate_LeavesEverythingElse()
    {
        var entry = LoanLedgerEntry.Record(
            SomeLoan, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000),
            "Loan released", new DateOnly(2026, 3, 1));

        entry.ReviseDebit(Money.Of(1500), new DateOnly(2026, 1, 1));

        Assert.Equal(1500m, entry.Debit.Amount);
        Assert.Equal(new DateOnly(2026, 1, 1), entry.TransactionDate);
        Assert.Equal(0m, entry.Credit.Amount);
        Assert.Equal(LoanLedgerTransactionType.LoanReleased, entry.TransactionType);
    }

    [Fact]
    public void ReviseForPaymentEdit_UpdatesCreditBalanceDateAndRemarks()
    {
        var entry = LoanLedgerEntry.Record(
            SomeLoan, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(500), Money.Of(9500),
            "Payment received", new DateOnly(2026, 2, 1), referenceId: "some-payment-id");

        entry.ReviseForPaymentEdit(Money.Of(600), Money.Of(9400), new DateOnly(2026, 2, 2), "Interest only");

        Assert.Equal(600m, entry.Credit.Amount);
        Assert.Equal(9400m, entry.RunningBalance.Amount);
        Assert.Equal(new DateOnly(2026, 2, 2), entry.TransactionDate);
        Assert.Equal("Interest only", entry.Remarks);
    }

    [Fact]
    public void ReviseExtension_UpdatesDebitDateAndRemarks_LeavesEverythingElse()
    {
        var entry = LoanLedgerEntry.Record(
            SomeLoan, LoanLedgerTransactionType.Extension, Money.Of(20), Money.Zero, Money.Of(1020),
            "Extension +10 days", new DateOnly(2026, 1, 20), referenceId: "some-extension-id");

        entry.ReviseExtension(Money.Of(35), new DateOnly(2026, 1, 18), "Additional interest Aug 2-Sep 2");

        Assert.Equal(35m, entry.Debit.Amount);
        Assert.Equal(new DateOnly(2026, 1, 18), entry.TransactionDate);
        Assert.Equal("Additional interest Aug 2-Sep 2", entry.Remarks);
        Assert.Equal(0m, entry.Credit.Amount);
        Assert.Equal(LoanLedgerTransactionType.Extension, entry.TransactionType);
    }
}

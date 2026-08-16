namespace LoanManagementSystem.Domain.CashLedger;

/// <summary>
/// Matches the SRS's Cash_Ledger.transaction_type column, extended with
/// Adjustment for manual corrections that don't fit the other categories.
/// For every type except Adjustment, direction (cash in vs. cash out) is
/// fixed by type: loan_release/owner_withdrawal/expense are cash out;
/// payment_received/owner_deposit are cash in. Adjustment is the one type
/// where a caller must say which way it goes (see CashLedgerEntry.Record) —
/// a correction can increase or decrease cash on hand.
/// </summary>
public enum CashTransactionType
{
    LoanRelease,
    PaymentReceived,
    OwnerDeposit,
    OwnerWithdrawal,
    Expense,
    Adjustment,
}

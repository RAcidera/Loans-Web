namespace LoanManagementSystem.Domain.CashLedger;

/// <summary>
/// Matches the SRS's Cash_Ledger.transaction_type column exactly, and the
/// "Transaction Type Effects" table: loan_release/owner_withdrawal/expense
/// are cash out; payment_received/owner_deposit are cash in.
/// </summary>
public enum CashTransactionType
{
    LoanRelease,
    PaymentReceived,
    OwnerDeposit,
    OwnerWithdrawal,
    Expense,
}

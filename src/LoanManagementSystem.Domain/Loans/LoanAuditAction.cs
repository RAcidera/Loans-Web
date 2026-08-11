namespace LoanManagementSystem.Domain.Loans;

/// <summary>The non-financial loan changes the Audit Log tab tracks — see LoanLedgerTransactionType for the financial-movement counterpart.</summary>
public enum LoanAuditAction
{
    Edited,
    ClassificationChanged,
    WrittenOff,
}

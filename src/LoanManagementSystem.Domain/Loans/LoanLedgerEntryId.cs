namespace LoanManagementSystem.Domain.Loans;

public readonly record struct LoanLedgerEntryId(Guid Value)
{
    public static LoanLedgerEntryId New() => new(Guid.NewGuid());
    public static LoanLedgerEntryId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

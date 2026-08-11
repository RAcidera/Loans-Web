namespace LoanManagementSystem.Domain.CashLedger;

public readonly record struct CashLedgerEntryId(Guid Value)
{
    public static CashLedgerEntryId New() => new(Guid.NewGuid());
    public static CashLedgerEntryId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

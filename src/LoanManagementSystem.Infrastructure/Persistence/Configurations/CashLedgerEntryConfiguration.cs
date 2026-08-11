using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class CashLedgerEntryConfiguration : IEntityTypeConfiguration<CashLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CashLedgerEntry> builder)
    {
        builder.ToTable("cash_ledger");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new CashLedgerEntryId(value))
            .HasColumnName("ledger_id")
            .ValueGeneratedNever();

        builder.Property(e => e.TransactionDate).HasColumnName("transaction_date").HasColumnType("date");

        // Stored using the SRS's own transaction_type values
        // (loan_release, payment_received, owner_deposit, owner_withdrawal,
        // expense) rather than C# enum member names.
        builder.Property(e => e.TransactionType)
            .HasConversion(t => t.ToWireString(), s => MappingExtensions.ParseCashTransactionType(s))
            .HasColumnName("transaction_type")
            .HasMaxLength(30);

        builder.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(36); // nullable loan_id, per SRS

        builder.Property(e => e.Amount)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("amount")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.Remarks).HasColumnName("remarks").HasMaxLength(500);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at");
    }
}

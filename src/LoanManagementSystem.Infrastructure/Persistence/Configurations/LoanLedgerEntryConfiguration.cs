using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class LoanLedgerEntryConfiguration : IEntityTypeConfiguration<LoanLedgerEntry>
{
    public void Configure(EntityTypeBuilder<LoanLedgerEntry> builder)
    {
        builder.ToTable("loan_ledger");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LoanLedgerEntryId(value))
            .HasColumnName("ledger_id")
            .ValueGeneratedNever();

        builder.Property(e => e.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(e => e.TransactionDate).HasColumnName("transaction_date").HasColumnType("date");

        builder.Property(e => e.TransactionType)
            .HasConversion(t => t.ToWireString(), s => MappingExtensions.ParseLoanLedgerTransactionType(s))
            .HasColumnName("transaction_type")
            .HasMaxLength(30);

        builder.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(36);

        builder.Property(e => e.Debit)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("debit")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.Credit)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("credit")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.RunningBalance)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("running_balance")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.Remarks).HasColumnName("remarks").HasMaxLength(500);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at");

        builder.HasIndex(e => e.LoanId);
    }
}

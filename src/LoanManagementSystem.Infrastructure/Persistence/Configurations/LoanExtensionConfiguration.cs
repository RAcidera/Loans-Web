using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class LoanExtensionConfiguration : IEntityTypeConfiguration<LoanExtension>
{
    public void Configure(EntityTypeBuilder<LoanExtension> builder)
    {
        builder.ToTable("loan_extensions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LoanExtensionId(value))
            .HasColumnName("extension_id")
            .ValueGeneratedNever();

        builder.Property(e => e.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(e => e.ExtensionDate).HasColumnName("extension_date").HasColumnType("date");
        builder.Property(e => e.ExtensionDays).HasColumnName("extension_days");

        builder.Property(e => e.AdditionalInterestAmount)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("additional_interest_amount")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.AdditionalChargesAmount)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("additional_charges_amount")
            .HasColumnType("decimal(12,2)");

        builder.Property(e => e.Remarks).HasColumnName("remarks").HasMaxLength(500);
    }
}

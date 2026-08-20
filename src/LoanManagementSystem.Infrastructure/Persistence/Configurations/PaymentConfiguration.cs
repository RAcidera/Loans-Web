using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PaymentId(value))
            .HasColumnName("payment_id")
            .ValueGeneratedNever();

        builder.Property(p => p.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(p => p.PaymentDate).HasColumnName("payment_date").HasColumnType("date");

        builder.Property(p => p.AmountPaid)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("amount_paid")
            .HasColumnType("decimal(12,2)");

        // Stored as the SRS's own wire values ("cash", "gcash", "bank_transfer")
        // rather than the C# enum member names, so the raw table matches the
        // SRS's payment_method column exactly if anyone inspects it directly.
        builder.Property(p => p.PaymentMethod)
            .HasConversion(m => m.ToWireString(), s => MappingExtensions.ParsePaymentMethod(s))
            .HasColumnName("payment_method")
            .HasMaxLength(20);

        builder.Property(p => p.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(p => p.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(100);

        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at").HasColumnType("datetime2");
    }
}

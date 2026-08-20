using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class PromiseToPayConfiguration : IEntityTypeConfiguration<PromiseToPay>
{
    public void Configure(EntityTypeBuilder<PromiseToPay> builder)
    {
        builder.ToTable("promises_to_pay");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PromiseToPayId(value))
            .HasColumnName("promise_id")
            .ValueGeneratedNever();

        builder.Property(p => p.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("customer_id");

        builder.Property(p => p.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(p => p.PromiseDate).HasColumnName("promise_date").HasColumnType("date");

        builder.Property(p => p.Amount)
            .HasConversion(new ValueConverter<Money, decimal>(m => m.Amount, v => Money.Of(v)))
            .HasColumnName("amount")
            .HasColumnType("decimal(12,2)");

        builder.Property(p => p.Notes).HasColumnName("notes").HasMaxLength(500);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at").HasColumnType("datetime2");
        builder.Property(p => p.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
        builder.Property(p => p.ModifiedAtUtc).HasColumnName("modified_at").HasColumnType("datetime2");

        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.LoanId);
        builder.HasIndex(p => p.PromiseDate);
    }
}

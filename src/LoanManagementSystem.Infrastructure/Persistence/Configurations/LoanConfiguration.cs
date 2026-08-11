using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("loans");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id")
            .ValueGeneratedNever();

        builder.Property(l => l.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("customer_id");

        // Not the primary key (that's still the GUID above) — a separate
        // DB-generated sequential number purely for human-friendly display
        // ("LM-001"). The identity + unique-index configuration is
        // database-generated (see AppDbContext.OnModelCreating) — Sqlite,
        // used only by the API integration tests, has no equivalent for a
        // column that isn't the table's own rowid.
        builder.Property(l => l.LoanNumber).HasColumnName("loan_number");

        builder.Property(l => l.PrincipalAmount)
            .HasConversion(MoneyConverter())
            .HasColumnName("principal_amount")
            .HasColumnType("decimal(12,2)");

        builder.Property(l => l.InterestRate)
            .HasConversion(
                rate => rate.Value,
                value => InterestRate.Of(value))
            .HasColumnName("interest_rate")
            .HasColumnType("decimal(5,4)");

        builder.Property(l => l.StartDate).HasColumnName("start_date").HasColumnType("date");
        builder.Property(l => l.DueDate).HasColumnName("due_date").HasColumnType("date");

        builder.Property(l => l.TotalInterest).HasConversion(MoneyConverter()).HasColumnName("total_interest").HasColumnType("decimal(12,2)");
        builder.Property(l => l.TotalExtensionCharges).HasConversion(MoneyConverter()).HasColumnName("total_extension_charges").HasColumnType("decimal(12,2)");
        builder.Property(l => l.TotalAmountDue).HasConversion(MoneyConverter()).HasColumnName("total_amount_due").HasColumnType("decimal(12,2)");
        builder.Property(l => l.TotalPaid).HasConversion(MoneyConverter()).HasColumnName("total_paid").HasColumnType("decimal(12,2)");
        builder.Property(l => l.Balance).HasConversion(MoneyConverter()).HasColumnName("balance").HasColumnType("decimal(12,2)");

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        // User-managed, independent of Status — see LoanClassification.
        builder.Property(l => l.Classification)
            .HasConversion<string>()
            .HasColumnName("classification")
            .HasMaxLength(20);

        builder.Property(l => l.Remarks).HasColumnName("remarks").HasMaxLength(1000);

        builder.Property(l => l.CreatedAtUtc).HasColumnName("created_at");

        // --- Extensions (child entities, one Loan -> many LoanExtension) ---
        builder.HasMany(l => l.Extensions)
            .WithOne()
            .HasForeignKey(e => e.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Loan exposes `Extensions` as a computed IReadOnlyCollection with no
        // public setter, backed by the private field `_extensions`. This
        // tells EF to materialize/persist through that field directly
        // rather than expecting a settable property.
        builder.Navigation(l => l.Extensions).UsePropertyAccessMode(PropertyAccessMode.Field);

        // --- Payments (child entities, one Loan -> many Payment) ---
        builder.HasMany(l => l.Payments)
            .WithOne()
            .HasForeignKey(p => p.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        // --- Documents (child entities, one Loan -> many LoanDocument) ---
        builder.HasMany(l => l.Documents)
            .WithOne()
            .HasForeignKey(d => d.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static ValueConverter<Money, decimal> MoneyConverter() =>
        new(money => money.Amount, value => Money.Of(value));
}

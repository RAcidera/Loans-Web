using LoanManagementSystem.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("customer_id")
            .ValueGeneratedNever();

        // Not the primary key (that's still the GUID above) — a separate
        // DB-generated sequential number purely for human-friendly display
        // ("CUS00001"), same pattern as Loan.LoanNumber. Identity + unique
        // index configuration is in AppDbContext.OnModelCreating.
        builder.Property(c => c.CustomerNumber).HasColumnName("customer_number");

        builder.Property(c => c.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Address).HasColumnName("address").HasMaxLength(300);
        builder.Property(c => c.ContactNumber).HasColumnName("contact_number").HasMaxLength(30);
        builder.Property(c => c.BorrowerType).HasColumnName("borrower_type").HasMaxLength(100);
        builder.Property(c => c.NicknameAlias).HasColumnName("nickname_alias").HasMaxLength(100);
        builder.Property(c => c.Notes).HasColumnName("notes").HasMaxLength(1000);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20);

        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at");

        // --- Documents (child entities, one Customer -> many CustomerDocument) ---
        builder.HasMany(c => c.Documents)
            .WithOne()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Same field-backed-collection reasoning as Loan's Extensions/Payments navigations.
        builder.Navigation(c => c.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

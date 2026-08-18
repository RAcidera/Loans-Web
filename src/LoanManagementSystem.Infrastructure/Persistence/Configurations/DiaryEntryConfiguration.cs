using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class DiaryEntryConfiguration : IEntityTypeConfiguration<DiaryEntry>
{
    public void Configure(EntityTypeBuilder<DiaryEntry> builder)
    {
        builder.ToTable("diary_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new DiaryEntryId(value))
            .HasColumnName("diary_entry_id")
            .ValueGeneratedNever();

        builder.Property(e => e.EntryDate).HasColumnName("entry_date").HasColumnType("date");
        builder.Property(e => e.EntryTime).HasColumnName("entry_time").HasColumnType("time");

        builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

        builder.Property(e => e.CategoryId)
            .HasConversion(id => id.Value, value => new DiaryCategoryId(value))
            .HasColumnName("category_id");

        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(e => e.Tags).HasColumnName("tags").HasMaxLength(500).IsRequired();

        builder.Property(e => e.CustomerId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new CustomerId(value.Value) : (CustomerId?)null)
            .HasColumnName("customer_id");

        builder.Property(e => e.LoanId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new LoanId(value.Value) : (LoanId?)null)
            .HasColumnName("loan_id");

        builder.Property(e => e.ReminderDate).HasColumnName("reminder_date").HasColumnType("date");
        builder.Property(e => e.ReminderTime).HasColumnName("reminder_time").HasColumnType("time");

        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(e => e.ModifiedBy).HasColumnName("modified_by").HasMaxLength(100);
        builder.Property(e => e.ModifiedAtUtc).HasColumnName("modified_at");

        // --- Snapshot (child entity, one DiaryEntry -> zero-or-one DiaryFinancialSnapshot) ---
        builder.HasOne(e => e.Snapshot)
            .WithOne()
            .HasForeignKey<DiaryFinancialSnapshot>(s => s.DiaryEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // DiaryEntry exposes `Snapshot` as a property with a private setter,
        // populated only through AttachSnapshot() — same field-backed-navigation
        // reasoning as Loan's Extensions/Payments (see LoanConfiguration).
        builder.Navigation(e => e.Snapshot).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.EntryDate);
        builder.HasIndex(e => e.CustomerId);
        builder.HasIndex(e => e.LoanId);
    }
}

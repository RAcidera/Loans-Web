using LoanManagementSystem.Domain.Diary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class DiaryFinancialSnapshotConfiguration : IEntityTypeConfiguration<DiaryFinancialSnapshot>
{
    public void Configure(EntityTypeBuilder<DiaryFinancialSnapshot> builder)
    {
        builder.ToTable("diary_financial_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(id => id.Value, value => new DiaryFinancialSnapshotId(value))
            .HasColumnName("snapshot_id")
            .ValueGeneratedNever();

        builder.Property(s => s.DiaryEntryId)
            .HasConversion(id => id.Value, value => new DiaryEntryId(value))
            .HasColumnName("diary_entry_id");

        builder.Property(s => s.GrossReceivables).HasColumnName("gross_receivables").HasColumnType("decimal(12,2)");
        builder.Property(s => s.CollectibleReceivables).HasColumnName("collectible_receivables").HasColumnType("decimal(12,2)");
        builder.Property(s => s.BadLoanReceivables).HasColumnName("bad_loan_receivables").HasColumnType("decimal(12,2)");
        builder.Property(s => s.CashOnHand).HasColumnName("cash_on_hand").HasColumnType("decimal(12,2)");
        builder.Property(s => s.ActiveLoanCount).HasColumnName("active_loan_count");
        builder.Property(s => s.OverdueLoanCount).HasColumnName("overdue_loan_count");
        builder.Property(s => s.BadLoanCount).HasColumnName("bad_loan_count");
        builder.Property(s => s.CollectionsToday).HasColumnName("collections_today").HasColumnType("decimal(12,2)");
        builder.Property(s => s.CollectionsMonthToDate).HasColumnName("collections_month_to_date").HasColumnType("decimal(12,2)");
        builder.Property(s => s.LoanReleasesToday).HasColumnName("loan_releases_today").HasColumnType("decimal(12,2)");
        builder.Property(s => s.LoanReleasesMonthToDate).HasColumnName("loan_releases_month_to_date").HasColumnType("decimal(12,2)");
        builder.Property(s => s.SnapshotDateTimeUtc).HasColumnName("snapshot_datetime");

        builder.HasIndex(s => s.DiaryEntryId).IsUnique();
    }
}

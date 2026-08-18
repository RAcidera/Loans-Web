using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Diary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class DiaryAuditLogEntryConfiguration : IEntityTypeConfiguration<DiaryAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<DiaryAuditLogEntry> builder)
    {
        builder.ToTable("diary_audit_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new DiaryAuditLogEntryId(value))
            .HasColumnName("audit_log_id")
            .ValueGeneratedNever();

        builder.Property(e => e.DiaryEntryId)
            .HasConversion(id => id.Value, value => new DiaryEntryId(value))
            .HasColumnName("diary_entry_id");

        builder.Property(e => e.Action)
            .HasConversion(a => a.ToWireString(), s => MappingExtensions.ParseDiaryAuditAction(s))
            .HasColumnName("action")
            .HasMaxLength(30);

        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.PerformedBy).HasColumnName("performed_by").HasMaxLength(100);
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at");

        builder.HasIndex(e => e.DiaryEntryId);
    }
}

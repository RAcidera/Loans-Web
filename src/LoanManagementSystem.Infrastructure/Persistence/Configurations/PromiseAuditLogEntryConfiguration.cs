using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Promises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class PromiseAuditLogEntryConfiguration : IEntityTypeConfiguration<PromiseAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<PromiseAuditLogEntry> builder)
    {
        builder.ToTable("promise_audit_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new PromiseAuditLogEntryId(value))
            .HasColumnName("audit_log_id")
            .ValueGeneratedNever();

        builder.Property(e => e.PromiseId)
            .HasConversion(id => id.Value, value => new PromiseToPayId(value))
            .HasColumnName("promise_id");

        builder.Property(e => e.Action)
            .HasConversion(a => a.ToWireString(), s => MappingExtensions.ParsePromiseAuditAction(s))
            .HasColumnName("action")
            .HasMaxLength(30);

        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.PerformedBy).HasColumnName("performed_by").HasMaxLength(100);
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at").HasColumnType("datetime2");

        builder.HasIndex(e => e.PromiseId);
    }
}

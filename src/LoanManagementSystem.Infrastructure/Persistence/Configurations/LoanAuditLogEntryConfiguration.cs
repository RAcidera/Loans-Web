using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class LoanAuditLogEntryConfiguration : IEntityTypeConfiguration<LoanAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<LoanAuditLogEntry> builder)
    {
        builder.ToTable("loan_audit_log");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new LoanAuditLogEntryId(value))
            .HasColumnName("audit_log_id")
            .ValueGeneratedNever();

        builder.Property(e => e.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(e => e.Action)
            .HasConversion(a => a.ToWireString(), s => MappingExtensions.ParseLoanAuditAction(s))
            .HasColumnName("action")
            .HasMaxLength(30);

        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(e => e.PerformedBy).HasColumnName("performed_by").HasMaxLength(100);
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at").HasColumnType("datetime2");

        builder.HasIndex(e => e.LoanId);
    }
}

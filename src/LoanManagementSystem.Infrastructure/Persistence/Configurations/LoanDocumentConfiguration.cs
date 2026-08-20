using LoanManagementSystem.Domain.Loans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class LoanDocumentConfiguration : IEntityTypeConfiguration<LoanDocument>
{
    public void Configure(EntityTypeBuilder<LoanDocument> builder)
    {
        builder.ToTable("loan_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new LoanDocumentId(value))
            .HasColumnName("document_id")
            .ValueGeneratedNever();

        builder.Property(d => d.LoanId)
            .HasConversion(id => id.Value, value => new LoanId(value))
            .HasColumnName("loan_id");

        builder.Property(d => d.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(d => d.FileSizeBytes).HasColumnName("file_size_bytes");

        // Same "no server file system" rule as CustomerDocumentConfiguration,
        // and the same reason for not spelling out HasColumnType("varbinary(max)") —
        // see that file's comment on this property.
        builder.Property(d => d.Content).HasColumnName("content").IsRequired();

        builder.Property(d => d.UploadedAtUtc).HasColumnName("uploaded_at").HasColumnType("datetime2");
        builder.Property(d => d.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(100);
    }
}

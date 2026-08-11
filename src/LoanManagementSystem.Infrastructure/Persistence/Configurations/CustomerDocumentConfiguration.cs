using LoanManagementSystem.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoanManagementSystem.Infrastructure.Persistence.Configurations;

public class CustomerDocumentConfiguration : IEntityTypeConfiguration<CustomerDocument>
{
    public void Configure(EntityTypeBuilder<CustomerDocument> builder)
    {
        builder.ToTable("customer_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(id => id.Value, value => new CustomerDocumentId(value))
            .HasColumnName("document_id")
            .ValueGeneratedNever();

        builder.Property(d => d.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("customer_id");

        builder.Property(d => d.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(260).IsRequired();
        builder.Property(d => d.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        builder.Property(d => d.FileSizeBytes).HasColumnName("file_size_bytes");

        // Spec 3.1: "Store documents directly in MS SQL Database using
        // VARBINARY(MAX). Do not store files in the server file system."
        // No explicit HasColumnType: EF Core's SQL Server provider already
        // maps an unsized byte[] to varbinary(max) by default — spelling it
        // out as a literal "varbinary(max)" string breaks the API test
        // suite's Sqlite provider, whose CREATE TABLE grammar rejects the
        // non-numeric "max" token in a type's parentheses (Sqlite's own
        // default mapping for byte[], BLOB, needs no size at all).
        builder.Property(d => d.Content).HasColumnName("content").IsRequired();

        builder.Property(d => d.UploadedAtUtc).HasColumnName("uploaded_at");
        builder.Property(d => d.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(100);
    }
}

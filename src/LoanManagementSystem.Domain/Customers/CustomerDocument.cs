using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Customers;

/// <summary>
/// Child entity of the Customer aggregate — spec 3.1's "Customer Documents
/// Management" (Valid ID, business permit, agreement documents, photos,
/// other supporting documents). Content is stored directly in SQL Server
/// (VARBINARY(MAX), see CustomerDocumentConfiguration) per the spec's
/// explicit "do not store files in the server file system." Like
/// Payment/LoanExtension, only ever created through Customer.UploadDocument(),
/// which is what keeps every document tied to its owning customer.
/// </summary>
public class CustomerDocument : Entity<CustomerDocumentId>
{
    public CustomerId CustomerId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public byte[] Content { get; private set; } = Array.Empty<byte>();
    public DateTime UploadedAtUtc { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;

    private CustomerDocument() { } // EF Core

    internal CustomerDocument(CustomerId customerId, string originalFileName, string contentType, byte[] content, string uploadedBy)
        : base(CustomerDocumentId.New())
    {
        DocumentValidation.Validate(originalFileName, contentType, content.LongLength);

        CustomerId = customerId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = content.LongLength;
        Content = content;
        UploadedAtUtc = DateTime.UtcNow;
        UploadedBy = uploadedBy;
    }
}

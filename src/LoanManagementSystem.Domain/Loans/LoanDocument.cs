using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Loans;

/// <summary>
/// Child entity of the Loan aggregate — spec's Loan Details "Documents"
/// tab. Content is stored directly in SQL Server (VARBINARY(MAX), see
/// LoanDocumentConfiguration), same "no server file system" rule and same
/// DocumentValidation as CustomerDocument. Like Payment/LoanExtension,
/// only ever created through Loan.UploadDocument().
/// </summary>
public class LoanDocument : Entity<LoanDocumentId>
{
    public LoanId LoanId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public byte[] Content { get; private set; } = Array.Empty<byte>();
    public DateTime UploadedAtUtc { get; private set; }
    public string UploadedBy { get; private set; } = string.Empty;

    private LoanDocument() { } // EF Core

    internal LoanDocument(LoanId loanId, string originalFileName, string contentType, byte[] content, string uploadedBy)
        : base(LoanDocumentId.New())
    {
        DocumentValidation.Validate(originalFileName, contentType, content.LongLength);

        LoanId = loanId;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        FileSizeBytes = content.LongLength;
        Content = content;
        UploadedAtUtc = DateTime.UtcNow;
        UploadedBy = uploadedBy;
    }
}

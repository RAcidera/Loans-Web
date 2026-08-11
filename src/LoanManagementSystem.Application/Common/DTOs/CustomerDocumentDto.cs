namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Metadata only — never carries the file's byte content (see DocumentFileDto for that).</summary>
public sealed record CustomerDocumentDto(
    string DocumentId,
    string CustomerId,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    string UploadedAt,
    string UploadedBy
);

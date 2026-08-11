namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>The downloadable form of a CustomerDocument/LoanDocument — includes the raw bytes, unlike the metadata-only list DTOs.</summary>
public sealed record DocumentFileDto(
    string OriginalFileName,
    string ContentType,
    byte[] Content
);

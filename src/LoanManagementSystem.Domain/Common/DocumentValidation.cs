namespace LoanManagementSystem.Domain.Common;

/// <summary>
/// Shared by CustomerDocument and LoanDocument — spec: "Supported file
/// types: JPG, PNG, PDF" for customer documents, applied identically to
/// loan documents so the two entities never validate differently. Kept as
/// a single static helper (not a DI service) since it's pure validation
/// logic with no dependencies, called directly from each entity's
/// constructor — same "validation lives in the domain constructor" pattern
/// as Payment/LoanExtension's amount checks.
/// </summary>
public static class DocumentValidation
{
    public static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "application/pdf" };

    /// <summary>10 MB — the spec doesn't state a limit; this is a reasonable ceiling for ID scans/agreement PDFs, not a business rule from the SRS.</summary>
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public static void Validate(string originalFileName, string contentType, long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new DomainException("A document must have a file name.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new DomainException($"Unsupported file type '{contentType}'. Allowed types: JPG, PNG, PDF.");

        if (fileSizeBytes <= 0)
            throw new DomainException("A document must not be empty.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new DomainException($"Document exceeds the maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}

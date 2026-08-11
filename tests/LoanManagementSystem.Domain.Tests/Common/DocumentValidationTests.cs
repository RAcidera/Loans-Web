using LoanManagementSystem.Domain.Common;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.Common;

public class DocumentValidationTests
{
    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    public void Validate_AllowedContentType_DoesNotThrow(string contentType)
    {
        var exception = Record.Exception(() => DocumentValidation.Validate("id.jpg", contentType, 1024));
        Assert.Null(exception);
    }

    [Fact]
    public void Validate_DisallowedContentType_Throws()
    {
        Assert.Throws<DomainException>(() => DocumentValidation.Validate("doc.docx", "application/msword", 1024));
    }

    [Fact]
    public void Validate_ZeroOrNegativeSize_Throws()
    {
        Assert.Throws<DomainException>(() => DocumentValidation.Validate("id.jpg", "image/jpeg", 0));
    }

    [Fact]
    public void Validate_ExceedsMaxSize_Throws()
    {
        Assert.Throws<DomainException>(() => DocumentValidation.Validate("id.jpg", "image/jpeg", DocumentValidation.MaxFileSizeBytes + 1));
    }

    [Fact]
    public void Validate_BlankFileName_Throws()
    {
        Assert.Throws<DomainException>(() => DocumentValidation.Validate("   ", "image/jpeg", 1024));
    }
}

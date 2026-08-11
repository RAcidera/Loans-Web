using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_SetsProfileFields_DefaultsNicknameAndNotesEmpty()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        Assert.Equal("Maria Santos", customer.FullName);
        Assert.Equal(CustomerStatus.Active, customer.Status);
        Assert.Equal(string.Empty, customer.NicknameAlias);
        Assert.Equal(string.Empty, customer.Notes);
    }

    [Fact]
    public void Create_WithNicknameAndNotes_TrimsAndStoresThem()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor", "  Ate Maria  ", "  Pays early.  ");

        Assert.Equal("Ate Maria", customer.NicknameAlias);
        Assert.Equal("Pays early.", customer.Notes);
    }

    [Fact]
    public void Create_BlankFullName_Throws()
    {
        Assert.Throws<DomainException>(() => Customer.Create("   ", "addr", "contact", "type"));
    }

    [Fact]
    public void UpdateProfile_ChangesFieldsIncludingNicknameAndNotes()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        customer.UpdateProfile("Maria S. Reyes", "New address", "+63 917 000 0000", "Vegetable vendor", "Inday", "Moved recently");

        Assert.Equal("Maria S. Reyes", customer.FullName);
        Assert.Equal("New address", customer.Address);
        Assert.Equal("Inday", customer.NicknameAlias);
        Assert.Equal("Moved recently", customer.Notes);
    }

    [Fact]
    public void UpdateProfile_OmittingNicknameAndNotes_ClearsThemToEmpty()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor", "Ate Maria", "Some notes");

        customer.UpdateProfile("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        Assert.Equal(string.Empty, customer.NicknameAlias);
        Assert.Equal(string.Empty, customer.Notes);
    }

    [Fact]
    public void UpdateProfile_BlankFullName_Throws()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        Assert.Throws<DomainException>(() => customer.UpdateProfile("", "addr", "contact", "type"));
    }

    [Fact]
    public void Deactivate_ThenReactivate_TogglesStatus()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        customer.Deactivate();
        Assert.Equal(CustomerStatus.Inactive, customer.Status);

        customer.Reactivate();
        Assert.Equal(CustomerStatus.Active, customer.Status);
    }

    // --- Documents (spec 3.1 "Customer Documents Management") ---

    [Fact]
    public void UploadDocument_AddsToDocuments()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        var document = customer.UploadDocument("valid-id.jpg", "image/jpeg", new byte[] { 1, 2, 3 }, "admin");

        Assert.Single(customer.Documents);
        Assert.Equal("valid-id.jpg", document.OriginalFileName);
        Assert.Equal(3, document.FileSizeBytes);
        Assert.Equal("admin", document.UploadedBy);
    }

    [Fact]
    public void UploadDocument_DisallowedContentType_Throws()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        Assert.Throws<DomainException>(() => customer.UploadDocument("resume.docx", "application/msword", new byte[] { 1 }, "admin"));
    }

    [Fact]
    public void DeleteDocument_RemovesIt()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");
        var document = customer.UploadDocument("valid-id.jpg", "image/jpeg", new byte[] { 1, 2, 3 }, "admin");

        customer.DeleteDocument(document.Id);

        Assert.Empty(customer.Documents);
    }

    [Fact]
    public void DeleteDocument_UnknownId_Throws()
    {
        var customer = Customer.Create("Maria Santos", "Blk 4 Lot 2", "+63 917 555 0142", "Fish vendor");

        Assert.Throws<DomainException>(() => customer.DeleteDocument(CustomerDocumentId.New()));
    }
}

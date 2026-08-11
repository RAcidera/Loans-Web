using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Customers;

/// <summary>
/// Customer aggregate root. Small on purpose — this SRS's Customers table
/// (3.1) is just a profile; the interesting business rules live on Loan,
/// not here.
/// </summary>
public class Customer : AggregateRoot<CustomerId>
{
    private readonly List<CustomerDocument> _documents = new();

    public string FullName { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string ContactNumber { get; private set; } = null!;
    public string BorrowerType { get; private set; } = null!;

    /// <summary>Spec's "Nickname / Alias" — optional, no uniqueness constraint.</summary>
    public string NicknameAlias { get; private set; } = string.Empty;

    /// <summary>Spec's free-text Notes field.</summary>
    public string Notes { get; private set; } = string.Empty;

    public CustomerStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// A stable, human-friendly sequential number (displayed as "CUS00001",
    /// "CUS00002", ...) — same pattern as Loan.LoanNumber. DB-generated on
    /// insert, so it's 0 on a freshly created customer until SaveChanges
    /// populates it.
    /// </summary>
    public int CustomerNumber { get; private set; }

    public IReadOnlyCollection<CustomerDocument> Documents => _documents.AsReadOnly();

    private Customer() { } // EF Core

    private Customer(CustomerId id, string fullName, string address, string contactNumber, string borrowerType, string nicknameAlias, string notes)
        : base(id)
    {
        FullName = fullName;
        Address = address;
        ContactNumber = contactNumber;
        BorrowerType = borrowerType;
        NicknameAlias = nicknameAlias;
        Notes = notes;
        Status = CustomerStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Customer Create(string fullName, string address, string contactNumber, string borrowerType, string nicknameAlias = "", string notes = "")
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("A customer must have a name.");

        return new Customer(CustomerId.New(), fullName.Trim(), address.Trim(), contactNumber.Trim(), borrowerType.Trim(), nicknameAlias.Trim(), notes.Trim());
    }

    public void UpdateProfile(string fullName, string address, string contactNumber, string borrowerType, string nicknameAlias = "", string notes = "")
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("A customer must have a name.");

        FullName = fullName.Trim();
        Address = address.Trim();
        ContactNumber = contactNumber.Trim();
        BorrowerType = borrowerType.Trim();
        NicknameAlias = nicknameAlias.Trim();
        Notes = notes.Trim();
    }

    public void Deactivate() => Status = CustomerStatus.Inactive;

    public void Reactivate() => Status = CustomerStatus.Active;

    /// <summary>Spec 3.1 "Customer Documents Management" — allows multiple file uploads (JPG/PNG/PDF).</summary>
    public CustomerDocument UploadDocument(string originalFileName, string contentType, byte[] content, string uploadedBy)
    {
        var document = new CustomerDocument(Id, originalFileName, contentType, content, uploadedBy);
        _documents.Add(document);
        return document;
    }

    public void DeleteDocument(CustomerDocumentId documentId)
    {
        var document = _documents.FirstOrDefault(d => d.Id == documentId)
            ?? throw new DomainException("Document not found on this customer.");
        _documents.Remove(document);
    }
}

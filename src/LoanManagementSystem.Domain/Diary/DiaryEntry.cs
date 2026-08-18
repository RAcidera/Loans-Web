using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary.Events;
using LoanManagementSystem.Domain.Loans;

namespace LoanManagementSystem.Domain.Diary;

/// <summary>
/// Diary/Journal aggregate root (requirements §4). Owns an optional
/// DiaryFinancialSnapshot child, captured once at creation and never
/// mutated again — see DiaryFinancialSnapshot's own doc comment for why
/// that immutability is enforced at the type level, not just by convention.
/// </summary>
public class DiaryEntry : AggregateRoot<DiaryEntryId>
{
    public DateOnly EntryDate { get; private set; }
    public TimeOnly EntryTime { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DiaryCategoryId CategoryId { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    /// <summary>Comma-separated (requirements §9's optional Tags field) — stored as plain text like Notes rather than a child collection; Angular splits/joins on "," for its chip input.</summary>
    public string Tags { get; private set; } = string.Empty;
    public CustomerId? CustomerId { get; private set; }
    public LoanId? LoanId { get; private set; }
    public DateOnly? ReminderDate { get; private set; }
    public TimeOnly? ReminderTime { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string ModifiedBy { get; private set; } = string.Empty;
    public DateTime ModifiedAtUtc { get; private set; }

    public DiaryFinancialSnapshot? Snapshot { get; private set; }

    /// <summary>Requirements §4's suggested display field — derived from EntryDate/EntryTime, not stored separately.</summary>
    public DateTime EntryDateTime => EntryDate.ToDateTime(EntryTime);

    private DiaryEntry() { } // EF Core

    private DiaryEntry(
        DiaryEntryId id, DateOnly entryDate, TimeOnly entryTime, string title, DiaryCategoryId categoryId, string notes, string tags,
        CustomerId? customerId, LoanId? loanId, DateOnly? reminderDate, TimeOnly? reminderTime, string createdBy)
        : base(id)
    {
        EntryDate = entryDate;
        EntryTime = entryTime;
        Title = title;
        CategoryId = categoryId;
        Notes = notes;
        Tags = tags;
        CustomerId = customerId;
        LoanId = loanId;
        ReminderDate = reminderDate;
        ReminderTime = reminderTime;
        CreatedBy = createdBy;
        CreatedAtUtc = DateTime.UtcNow;
        ModifiedBy = createdBy;
        ModifiedAtUtc = CreatedAtUtc;
    }

    /// <summary>
    /// Creates a diary entry (requirements §6). Financial snapshot capture
    /// is a separate step (AttachSnapshot), called by the command handler
    /// only when the "Capture current financial snapshot" checkbox was set —
    /// this factory never touches financial data itself, keeping the
    /// aggregate's own construction free of any repository dependency.
    /// </summary>
    public static DiaryEntry Create(
        DateOnly entryDate, TimeOnly entryTime, string title, DiaryCategoryId categoryId, string notes, string? tags,
        CustomerId? customerId, LoanId? loanId, DateOnly? reminderDate, TimeOnly? reminderTime, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A diary entry must have a title.");

        var entry = new DiaryEntry(
            DiaryEntryId.New(), entryDate, entryTime, title.Trim(), categoryId, notes?.Trim() ?? string.Empty, NormalizeTags(tags),
            customerId, loanId, reminderDate, reminderTime, createdBy);

        entry.RaiseDomainEvent(new DiaryEntryCreatedDomainEvent(entry.Id, createdBy));
        return entry;
    }

    /// <summary>Trims each tag, drops empty entries, and rejoins with ",": the canonical form Tags is always stored in, so equality/search comparisons never need to re-normalize.</summary>
    private static string NormalizeTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? string.Empty
            : string.Join(",", tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    /// <summary>
    /// Captures a financial snapshot at creation time from server-computed
    /// figures (requirements §7 — never accepts Angular-calculated totals;
    /// every value here comes from IFinancialSnapshotService in the
    /// Application layer, already resolved before this is called). Builds
    /// the child DiaryFinancialSnapshot itself, the same way Loan.RecordPayment()
    /// constructs its child Payment, so that type's constructor can stay
    /// internal rather than exposed for Application to call directly. Can
    /// only be called once — a diary entry either has a snapshot from the
    /// moment it's created, or it never gets one (requirements §10: any
    /// future regeneration would need to be an explicit, separately
    /// audited action, not a silent re-capture).
    /// </summary>
    public DiaryFinancialSnapshot CaptureSnapshot(
        decimal grossReceivables, decimal collectibleReceivables, decimal badLoanReceivables, decimal cashOnHand,
        int activeLoanCount, int overdueLoanCount, int badLoanCount,
        decimal collectionsToday, decimal collectionsMonthToDate, decimal loanReleasesToday, decimal loanReleasesMonthToDate)
    {
        if (Snapshot is not null)
            throw new DomainException("This diary entry already has a financial snapshot.");

        Snapshot = new DiaryFinancialSnapshot(
            Id, grossReceivables, collectibleReceivables, badLoanReceivables, cashOnHand,
            activeLoanCount, overdueLoanCount, badLoanCount,
            collectionsToday, collectionsMonthToDate, loanReleasesToday, loanReleasesMonthToDate);

        RaiseDomainEvent(new FinancialSnapshotCapturedDomainEvent(Id, ModifiedBy));
        return Snapshot;
    }

    /// <summary>
    /// Edits title/category/notes/date/time/links/reminder (requirements
    /// §13's editable fields) — never the financial snapshot, which has no
    /// setter for this method to reach even if it tried (requirements §10).
    /// Raises the base "updated" event plus one extra event per field group
    /// that actually changed (reminder/customer/loan), mirroring
    /// Loan.EditLoan's conditional multi-event pattern, so the audit trail
    /// (requirements §24) can distinguish "notes edited" from "reminder
    /// changed" without inspecting old vs. new values after the fact.
    /// </summary>
    public void Update(
        DateOnly entryDate, TimeOnly entryTime, string title, DiaryCategoryId categoryId, string notes, string? tags,
        CustomerId? customerId, LoanId? loanId, DateOnly? reminderDate, TimeOnly? reminderTime, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A diary entry must have a title.");

        var reminderChanged = ReminderDate != reminderDate || ReminderTime != reminderTime;
        var customerChanged = CustomerId != customerId;
        var loanChanged = LoanId != loanId;

        EntryDate = entryDate;
        EntryTime = entryTime;
        Title = title.Trim();
        CategoryId = categoryId;
        Notes = notes?.Trim() ?? string.Empty;
        Tags = NormalizeTags(tags);
        CustomerId = customerId;
        LoanId = loanId;
        ReminderDate = reminderDate;
        ReminderTime = reminderTime;
        ModifiedBy = modifiedBy;
        ModifiedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new DiaryEntryUpdatedDomainEvent(Id, modifiedBy));
        if (reminderChanged) RaiseDomainEvent(new DiaryReminderChangedDomainEvent(Id, modifiedBy));
        if (customerChanged) RaiseDomainEvent(new DiaryLinkedCustomerChangedDomainEvent(Id, modifiedBy));
        if (loanChanged) RaiseDomainEvent(new DiaryLinkedLoanChangedDomainEvent(Id, modifiedBy));
    }

    /// <summary>
    /// Raises the deletion audit event before the command handler removes
    /// this aggregate from the repository — see DiaryEntryDeletedDomainEvent's
    /// doc comment for why the event still reaches AppDbContext's
    /// domain-event collection despite the entity being deleted.
    /// </summary>
    public void MarkForDeletion(string deletedBy) => RaiseDomainEvent(new DiaryEntryDeletedDomainEvent(Id, deletedBy));
}

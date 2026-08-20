using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;

namespace LoanManagementSystem.Application.Common.Mappings;

/// <summary>
/// Deliberately hand-written rather than AutoMapper: there are only a
/// handful of shapes, each mapping is one-directional (domain -> DTO), and
/// explicit code here is easier to keep in sync with the Angular entities
/// it mirrors than a configuration-driven mapper would be.
/// </summary>
public static class MappingExtensions
{
    /// <summary>The "LOA00001" display format for Loan.LoanNumber — the single place that convention is spelled out.</summary>
    public static string FormatLoanNumber(int loanNumber) => $"LOA{loanNumber:D5}";

    /// <summary>The "CUS00001" display format for Customer.CustomerNumber — same convention as FormatLoanNumber.</summary>
    public static string FormatCustomerCode(int customerNumber) => $"CUS{customerNumber:D5}";

    public static CustomerDto ToDto(this Customer customer) => new(
        CustomerId: customer.Id.ToString(),
        CustomerCode: FormatCustomerCode(customer.CustomerNumber),
        FullName: customer.FullName,
        Address: customer.Address,
        ContactNumber: customer.ContactNumber,
        BorrowerType: customer.BorrowerType,
        NicknameAlias: customer.NicknameAlias,
        Notes: customer.Notes,
        Status: customer.Status.ToString().ToLowerInvariant(),
        CreatedAt: customer.CreatedAtUtc.ToString("O")
    );

    public static CustomerDocumentDto ToDto(this CustomerDocument document) => new(
        DocumentId: document.Id.ToString(),
        CustomerId: document.CustomerId.ToString(),
        OriginalFileName: document.OriginalFileName,
        ContentType: document.ContentType,
        FileSizeBytes: document.FileSizeBytes,
        UploadedAt: document.UploadedAtUtc.ToString("O"),
        UploadedBy: document.UploadedBy
    );

    public static LoanDocumentDto ToDto(this LoanDocument document) => new(
        DocumentId: document.Id.ToString(),
        LoanId: document.LoanId.ToString(),
        OriginalFileName: document.OriginalFileName,
        ContentType: document.ContentType,
        FileSizeBytes: document.FileSizeBytes,
        UploadedAt: document.UploadedAtUtc.ToString("O"),
        UploadedBy: document.UploadedBy
    );

    public static CustomerListItemDto ToListItemDto(this Customer customer, int loanCount, decimal outstandingBalance = 0m) => new(
        CustomerId: customer.Id.ToString(),
        CustomerCode: FormatCustomerCode(customer.CustomerNumber),
        FullName: customer.FullName,
        Address: customer.Address,
        ContactNumber: customer.ContactNumber,
        BorrowerType: customer.BorrowerType,
        NicknameAlias: customer.NicknameAlias,
        Notes: customer.Notes,
        Status: customer.Status.ToString().ToLowerInvariant(),
        CreatedAt: customer.CreatedAtUtc.ToString("O"),
        LoanCount: loanCount,
        OutstandingBalance: outstandingBalance
    );

    public static UserDto ToDto(this User user) => new(
        UserId: user.Id.ToString(),
        Username: user.Username,
        Role: user.Role.ToString().ToLowerInvariant(),
        Status: user.Status.ToString().ToLowerInvariant(),
        CreatedAt: user.CreatedAtUtc.ToString("O")
    );

    public static LoanDto ToDto(this Loan loan, string customerName, string? customerContactNumber = null, int? daysUntilDue = null) => new(
        LoanId: loan.Id.ToString(),
        LoanNumber: FormatLoanNumber(loan.LoanNumber),
        CustomerId: loan.CustomerId.ToString(),
        CustomerName: customerName,
        PrincipalAmount: loan.PrincipalAmount.Amount,
        InterestRate: loan.InterestRate.Value,
        StartDate: loan.StartDate.ToString("yyyy-MM-dd"),
        DueDate: loan.DueDate.ToString("yyyy-MM-dd"),
        PaymentTermsMonths: loan.PaymentTermsMonths,
        TotalInterest: loan.TotalInterest.Amount,
        TotalExtensionCharges: loan.TotalExtensionCharges.Amount,
        TotalAmountDue: loan.TotalAmountDue.Amount,
        TotalPaid: loan.TotalPaid.Amount,
        Balance: loan.Balance.Amount,
        DailyPayment: CalculateDailyPayment(loan),
        Status: loan.Status.ToString().ToLowerInvariant(),
        Classification: loan.Classification.ToString().ToLowerInvariant(),
        Remarks: loan.Remarks,
        CreatedAt: loan.CreatedAtUtc.ToString("O"),
        CustomerContactNumber: customerContactNumber,
        DaysUntilDue: daysUntilDue
    );

    /// <summary>(Principal + Interest) / (Payment terms in months * 30) — see LoanDto.DailyPayment.</summary>
    private static decimal CalculateDailyPayment(Loan loan)
    {
        var days = Math.Max(1, loan.PaymentTermsMonths * 30);
        return Math.Round((loan.PrincipalAmount.Amount + loan.TotalInterest.Amount) / days, 2);
    }

    public static LoanExtensionDto ToDto(this LoanExtension extension) => new(
        ExtensionId: extension.Id.ToString(),
        LoanId: extension.LoanId.ToString(),
        ExtensionDate: extension.ExtensionDate.ToString("yyyy-MM-dd"),
        ExtensionDays: extension.ExtensionDays,
        AdditionalChargesAmount: extension.AdditionalChargesAmount.Amount,
        Remarks: extension.Remarks
    );

    public static PaymentDto ToDto(this Payment payment) => new(
        PaymentId: payment.Id.ToString(),
        LoanId: payment.LoanId.ToString(),
        PaymentDate: payment.PaymentDate.ToString("yyyy-MM-dd"),
        AmountPaid: payment.AmountPaid.Amount,
        PaymentMethod: ToWireString(payment.PaymentMethod),
        Notes: payment.Notes,
        ReferenceNumber: payment.ReferenceNumber,
        CreatedAt: payment.CreatedAtUtc.ToString("O")
    );

    public static PaymentWithCustomerDto ToDtoWithCustomer(this Payment payment, string customerId, string customerName, int loanNumber) => new(
        PaymentId: payment.Id.ToString(),
        LoanId: payment.LoanId.ToString(),
        LoanNumber: FormatLoanNumber(loanNumber),
        PaymentDate: payment.PaymentDate.ToString("yyyy-MM-dd"),
        AmountPaid: payment.AmountPaid.Amount,
        PaymentMethod: ToWireString(payment.PaymentMethod),
        Notes: payment.Notes,
        ReferenceNumber: payment.ReferenceNumber,
        CustomerId: customerId,
        CustomerName: customerName,
        CreatedAt: payment.CreatedAtUtc.ToString("O")
    );

    public static PaymentWithLoanDto ToDtoWithLoan(this Payment payment, int loanNumber) => new(
        PaymentId: payment.Id.ToString(),
        LoanId: payment.LoanId.ToString(),
        LoanNumber: FormatLoanNumber(loanNumber),
        PaymentDate: payment.PaymentDate.ToString("yyyy-MM-dd"),
        AmountPaid: payment.AmountPaid.Amount,
        PaymentMethod: ToWireString(payment.PaymentMethod),
        Notes: payment.Notes,
        ReferenceNumber: payment.ReferenceNumber,
        CreatedAt: payment.CreatedAtUtc.ToString("O")
    );

    public static LoanLedgerEntryDto ToDto(this LoanLedgerEntry entry) => new(
        LedgerId: entry.Id.ToString(),
        LoanId: entry.LoanId.ToString(),
        TransactionDate: entry.TransactionDate.ToString("yyyy-MM-dd"),
        TransactionType: entry.TransactionType.ToWireString(),
        ReferenceId: entry.ReferenceId,
        Debit: entry.Debit.Amount,
        Credit: entry.Credit.Amount,
        RunningBalance: entry.RunningBalance.Amount,
        Remarks: entry.Remarks,
        CreatedAt: entry.CreatedAtUtc.ToString("O")
    );

    public static LoanAuditLogEntryDto ToDto(this LoanAuditLogEntry entry) => new(
        AuditLogId: entry.Id.ToString(),
        LoanId: entry.LoanId.ToString(),
        Action: entry.Action.ToWireString(),
        Description: entry.Description,
        PerformedBy: entry.PerformedBy,
        OccurredAt: entry.OccurredAtUtc.ToString("O")
    );

    /// <summary>snake_case on the wire, matching this codebase's other transaction-type conventions (see CashTransactionType.ToWireString).</summary>
    public static string ToWireString(this LoanLedgerTransactionType type) => type switch
    {
        LoanLedgerTransactionType.LoanReleased => "loan_released",
        LoanLedgerTransactionType.InterestAdded => "interest_added",
        LoanLedgerTransactionType.Payment => "payment",
        LoanLedgerTransactionType.Extension => "extension",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static LoanLedgerTransactionType ParseLoanLedgerTransactionType(string value) => value switch
    {
        "loan_released" => LoanLedgerTransactionType.LoanReleased,
        "interest_added" => LoanLedgerTransactionType.InterestAdded,
        "payment" => LoanLedgerTransactionType.Payment,
        "extension" => LoanLedgerTransactionType.Extension,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown loan ledger transaction type."),
    };

    public static string ToWireString(this LoanAuditAction action) => action switch
    {
        LoanAuditAction.Edited => "edited",
        LoanAuditAction.ClassificationChanged => "classification_changed",
        LoanAuditAction.WrittenOff => "written_off",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static LoanAuditAction ParseLoanAuditAction(string value) => value switch
    {
        "edited" => LoanAuditAction.Edited,
        "classification_changed" => LoanAuditAction.ClassificationChanged,
        "written_off" => LoanAuditAction.WrittenOff,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown loan audit action."),
    };

    /// <param name="runningBalance">Cash on Hand immediately after this entry, computed by the caller over the FULL chronological ledger (not just a filtered/paged subset) — null when the caller hasn't computed one (e.g. the Add/Edit command's own response, where the UI reloads the list anyway).</param>
    public static CashLedgerEntryDto ToDto(this CashLedgerEntry entry, decimal? runningBalance = null) => new(
        LedgerId: entry.Id.ToString(),
        TransactionDate: entry.TransactionDate.ToString("yyyy-MM-dd"),
        TransactionType: ToWireString(entry.TransactionType),
        ReferenceId: entry.ReferenceId,
        Amount: entry.Amount.Amount,
        IsCashIn: entry.IsCashIn,
        IsAutomatic: entry.IsAutomatic,
        RunningBalance: runningBalance,
        Remarks: entry.Remarks,
        CreatedAt: entry.CreatedAtUtc.ToString("O")
    );

    /// <summary>snake_case on the wire, to match the SRS's own transaction_type values and Angular's CashTransactionType union.</summary>
    public static string ToWireString(this CashTransactionType type) => type switch
    {
        CashTransactionType.LoanRelease => "loan_release",
        CashTransactionType.PaymentReceived => "payment_received",
        CashTransactionType.OwnerDeposit => "owner_deposit",
        CashTransactionType.OwnerWithdrawal => "owner_withdrawal",
        CashTransactionType.Expense => "expense",
        CashTransactionType.Adjustment => "adjustment",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static CashTransactionType ParseCashTransactionType(string value) => value switch
    {
        "loan_release" => CashTransactionType.LoanRelease,
        "payment_received" => CashTransactionType.PaymentReceived,
        "owner_deposit" => CashTransactionType.OwnerDeposit,
        "owner_withdrawal" => CashTransactionType.OwnerWithdrawal,
        "expense" => CashTransactionType.Expense,
        "adjustment" => CashTransactionType.Adjustment,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown cash transaction type."),
    };

    /// <summary>snake_case on the wire, to match Angular's PaymentMethod union ('gcash', 'bank_transfer', 'cash', 'other').</summary>
    public static string ToWireString(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "cash",
        PaymentMethod.GCash => "gcash",
        PaymentMethod.BankTransfer => "bank_transfer",
        PaymentMethod.Other => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    public static PaymentMethod ParsePaymentMethod(string value) => value switch
    {
        "cash" => PaymentMethod.Cash,
        "gcash" => PaymentMethod.GCash,
        "bank_transfer" => PaymentMethod.BankTransfer,
        "other" => PaymentMethod.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown payment method."),
    };

    public static DiaryCategoryDto ToDto(this DiaryCategory category) => new(
        CategoryId: category.Id.ToString(),
        Name: category.Name,
        Icon: category.Icon,
        DisplayColor: category.DisplayColor,
        IsActive: category.IsActive,
        SortOrder: category.SortOrder
    );

    public static DiaryFinancialSnapshotDto ToDto(this DiaryFinancialSnapshot snapshot) => new(
        SnapshotId: snapshot.Id.ToString(),
        DiaryEntryId: snapshot.DiaryEntryId.ToString(),
        GrossReceivables: snapshot.GrossReceivables,
        CollectibleReceivables: snapshot.CollectibleReceivables,
        BadLoanReceivables: snapshot.BadLoanReceivables,
        CashOnHand: snapshot.CashOnHand,
        ActiveLoanCount: snapshot.ActiveLoanCount,
        OverdueLoanCount: snapshot.OverdueLoanCount,
        BadLoanCount: snapshot.BadLoanCount,
        CollectionsToday: snapshot.CollectionsToday,
        CollectionsMonthToDate: snapshot.CollectionsMonthToDate,
        LoanReleasesToday: snapshot.LoanReleasesToday,
        LoanReleasesMonthToDate: snapshot.LoanReleasesMonthToDate,
        SnapshotDateTime: snapshot.SnapshotDateTimeUtc.ToString("O")
    );

    /// <param name="customerName">Resolved by the caller (DiaryEntry has no navigation to Customer — separate aggregates, same reasoning as Loan/Customer elsewhere in this codebase); null when the entry has no linked customer.</param>
    /// <param name="loanNumber">Resolved by the caller; null when the entry has no linked loan.</param>
    public static DiaryEntryDto ToDto(this DiaryEntry entry, DiaryCategory category, string? customerName, int? loanNumber) => new(
        DiaryEntryId: entry.Id.ToString(),
        EntryDate: entry.EntryDate.ToString("yyyy-MM-dd"),
        EntryTime: entry.EntryTime.ToString("HH:mm"),
        EntryDateTime: entry.EntryDateTime.ToString("O"),
        Title: entry.Title,
        CategoryId: category.Id.ToString(),
        CategoryName: category.Name,
        CategoryIcon: category.Icon,
        CategoryDisplayColor: category.DisplayColor,
        Notes: entry.Notes,
        Tags: entry.Tags,
        CustomerId: entry.CustomerId?.ToString(),
        CustomerName: customerName,
        LoanId: entry.LoanId?.ToString(),
        LoanNumber: loanNumber is { } n ? FormatLoanNumber(n) : null,
        ReminderDate: entry.ReminderDate?.ToString("yyyy-MM-dd"),
        ReminderTime: entry.ReminderTime?.ToString("HH:mm"),
        Snapshot: entry.Snapshot?.ToDto(),
        CreatedBy: entry.CreatedBy,
        CreatedAt: entry.CreatedAtUtc.ToString("O"),
        ModifiedBy: entry.ModifiedBy,
        ModifiedAt: entry.ModifiedAtUtc.ToString("O")
    );

    public static DiaryAuditLogEntryDto ToDto(this DiaryAuditLogEntry entry) => new(
        AuditLogId: entry.Id.ToString(),
        DiaryEntryId: entry.DiaryEntryId.ToString(),
        Action: entry.Action.ToWireString(),
        Description: entry.Description,
        PerformedBy: entry.PerformedBy,
        OccurredAt: entry.OccurredAtUtc.ToString("O")
    );

    public static string ToWireString(this DiaryAuditAction action) => action switch
    {
        DiaryAuditAction.Created => "created",
        DiaryAuditAction.Updated => "updated",
        DiaryAuditAction.Deleted => "deleted",
        DiaryAuditAction.SnapshotCaptured => "snapshot_captured",
        DiaryAuditAction.ReminderChanged => "reminder_changed",
        DiaryAuditAction.LinkedCustomerChanged => "linked_customer_changed",
        DiaryAuditAction.LinkedLoanChanged => "linked_loan_changed",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static DiaryAuditAction ParseDiaryAuditAction(string value) => value switch
    {
        "created" => DiaryAuditAction.Created,
        "updated" => DiaryAuditAction.Updated,
        "deleted" => DiaryAuditAction.Deleted,
        "snapshot_captured" => DiaryAuditAction.SnapshotCaptured,
        "reminder_changed" => DiaryAuditAction.ReminderChanged,
        "linked_customer_changed" => DiaryAuditAction.LinkedCustomerChanged,
        "linked_loan_changed" => DiaryAuditAction.LinkedLoanChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown diary audit action."),
    };

    /// <param name="customerName">Resolved by the caller — PromiseToPay has no navigation to Customer (separate aggregates).</param>
    /// <param name="loanNumber">Resolved by the caller — PromiseToPay has no navigation to Loan.</param>
    public static PromiseToPayDto ToDto(this PromiseToPay promise, string customerName, int loanNumber) => new(
        PromiseId: promise.Id.ToString(),
        CustomerId: promise.CustomerId.ToString(),
        CustomerName: customerName,
        LoanId: promise.LoanId.ToString(),
        LoanNumber: FormatLoanNumber(loanNumber),
        PromiseDate: promise.PromiseDate.ToString("yyyy-MM-dd"),
        Amount: promise.Amount.Amount,
        Notes: promise.Notes,
        Status: promise.Status.ToString().ToLowerInvariant(),
        CreatedBy: promise.CreatedBy,
        CreatedAt: promise.CreatedAtUtc.ToString("O"),
        ModifiedBy: promise.ModifiedBy,
        ModifiedAt: promise.ModifiedAtUtc.ToString("O")
    );

    public static PromiseAuditLogEntryDto ToDto(this PromiseAuditLogEntry entry) => new(
        AuditLogId: entry.Id.ToString(),
        PromiseId: entry.PromiseId.ToString(),
        Action: entry.Action.ToWireString(),
        Description: entry.Description,
        PerformedBy: entry.PerformedBy,
        OccurredAt: entry.OccurredAtUtc.ToString("O")
    );

    public static string ToWireString(this PromiseAuditAction action) => action switch
    {
        PromiseAuditAction.Created => "created",
        PromiseAuditAction.Updated => "updated",
        PromiseAuditAction.Kept => "kept",
        PromiseAuditAction.Missed => "missed",
        PromiseAuditAction.Rescheduled => "rescheduled",
        PromiseAuditAction.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    public static PromiseAuditAction ParsePromiseAuditAction(string value) => value switch
    {
        "created" => PromiseAuditAction.Created,
        "updated" => PromiseAuditAction.Updated,
        "kept" => PromiseAuditAction.Kept,
        "missed" => PromiseAuditAction.Missed,
        "rescheduled" => PromiseAuditAction.Rescheduled,
        "cancelled" => PromiseAuditAction.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown promise audit action."),
    };
}

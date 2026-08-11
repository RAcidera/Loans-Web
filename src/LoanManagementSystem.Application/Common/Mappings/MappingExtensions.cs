using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Loans;

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
        CreatedAt: customer.CreatedAtUtc.ToString("yyyy-MM-dd")
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

    public static CustomerListItemDto ToListItemDto(this Customer customer, int loanCount) => new(
        CustomerId: customer.Id.ToString(),
        CustomerCode: FormatCustomerCode(customer.CustomerNumber),
        FullName: customer.FullName,
        Address: customer.Address,
        ContactNumber: customer.ContactNumber,
        BorrowerType: customer.BorrowerType,
        NicknameAlias: customer.NicknameAlias,
        Notes: customer.Notes,
        Status: customer.Status.ToString().ToLowerInvariant(),
        CreatedAt: customer.CreatedAtUtc.ToString("yyyy-MM-dd"),
        LoanCount: loanCount
    );

    public static UserDto ToDto(this User user) => new(
        UserId: user.Id.ToString(),
        Username: user.Username,
        Role: user.Role.ToString().ToLowerInvariant(),
        Status: user.Status.ToString().ToLowerInvariant(),
        CreatedAt: user.CreatedAtUtc.ToString("yyyy-MM-dd")
    );

    public static LoanDto ToDto(this Loan loan, string customerName) => new(
        LoanId: loan.Id.ToString(),
        LoanNumber: FormatLoanNumber(loan.LoanNumber),
        CustomerId: loan.CustomerId.ToString(),
        CustomerName: customerName,
        PrincipalAmount: loan.PrincipalAmount.Amount,
        InterestRate: loan.InterestRate.Value,
        StartDate: loan.StartDate.ToString("yyyy-MM-dd"),
        DueDate: loan.DueDate.ToString("yyyy-MM-dd"),
        TotalInterest: loan.TotalInterest.Amount,
        TotalExtensionCharges: loan.TotalExtensionCharges.Amount,
        TotalAmountDue: loan.TotalAmountDue.Amount,
        TotalPaid: loan.TotalPaid.Amount,
        Balance: loan.Balance.Amount,
        Status: loan.Status.ToString().ToLowerInvariant(),
        Classification: loan.Classification.ToString().ToLowerInvariant(),
        Remarks: loan.Remarks,
        CreatedAt: loan.CreatedAtUtc.ToString("yyyy-MM-dd")
    );

    public static LoanExtensionDto ToDto(this LoanExtension extension) => new(
        ExtensionId: extension.Id.ToString(),
        LoanId: extension.LoanId.ToString(),
        ExtensionDate: extension.ExtensionDate.ToString("yyyy-MM-dd"),
        ExtensionDays: extension.ExtensionDays,
        AdditionalInterestAmount: extension.AdditionalInterestAmount.Amount,
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
        ReferenceNumber: payment.ReferenceNumber
    );

    public static PaymentWithCustomerDto ToDtoWithCustomer(this Payment payment, string customerName, int loanNumber) => new(
        PaymentId: payment.Id.ToString(),
        LoanId: payment.LoanId.ToString(),
        LoanNumber: FormatLoanNumber(loanNumber),
        PaymentDate: payment.PaymentDate.ToString("yyyy-MM-dd"),
        AmountPaid: payment.AmountPaid.Amount,
        PaymentMethod: ToWireString(payment.PaymentMethod),
        Notes: payment.Notes,
        ReferenceNumber: payment.ReferenceNumber,
        CustomerName: customerName
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

    public static CashLedgerEntryDto ToDto(this CashLedgerEntry entry) => new(
        LedgerId: entry.Id.ToString(),
        TransactionDate: entry.TransactionDate.ToString("yyyy-MM-dd"),
        TransactionType: ToWireString(entry.TransactionType),
        ReferenceId: entry.ReferenceId,
        Amount: entry.Amount.Amount,
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
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static CashTransactionType ParseCashTransactionType(string value) => value switch
    {
        "loan_release" => CashTransactionType.LoanRelease,
        "payment_received" => CashTransactionType.PaymentReceived,
        "owner_deposit" => CashTransactionType.OwnerDeposit,
        "owner_withdrawal" => CashTransactionType.OwnerWithdrawal,
        "expense" => CashTransactionType.Expense,
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
}

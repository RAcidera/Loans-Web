namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Field names match src/app/domain/entities/loan.entity.ts on the Angular side.</summary>
public sealed record LoanDto(
    string LoanId,
    string LoanNumber,
    string CustomerId,
    string CustomerName,
    decimal PrincipalAmount,
    decimal InterestRate,
    string StartDate,
    string DueDate,
    int PaymentTermsMonths,
    decimal TotalInterest,
    decimal TotalExtensionCharges,
    decimal TotalAmountDue,
    decimal TotalPaid,
    decimal Balance,
    /// <summary>Computed, not stored: (PrincipalAmount + TotalInterest) / (PaymentTermsMonths * 30) — the suggested default amount when recording a payment on this loan.</summary>
    decimal DailyPayment,
    string Status,
    string Classification,
    string Remarks,
    string CreatedAt,
    /// <summary>Only populated by GetLoanDetailQuery (the Loan Details page's "Contact" stat) — every other caller of Loan.ToDto() omits it.</summary>
    string? CustomerContactNumber = null,
    /// <summary>DueDate.DayNumber - business-local Today.DayNumber (negative once overdue). Only populated by GetLoanDetailQuery (the Loan Details page's "days until due" banner) — computed server-side against the Business Time Zone so it never disagrees with Loan.RefreshOverdueStatus's own decision, unlike a client-side recomputation from the browser's clock would.</summary>
    int? DaysUntilDue = null
);

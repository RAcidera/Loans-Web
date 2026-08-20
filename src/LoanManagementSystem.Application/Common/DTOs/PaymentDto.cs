namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record PaymentDto(
    string PaymentId,
    string LoanId,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber,
    string CreatedAt
);

/// <summary>Matches Angular's PaymentWithCustomer projection — used for the dashboard feed and the Payments list page (CustomerId is what makes the Payments page's Customer cell clickable to the Customer Profile).</summary>
public sealed record PaymentWithCustomerDto(
    string PaymentId,
    string LoanId,
    string LoanNumber,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber,
    string CustomerId,
    string CustomerName,
    string CreatedAt
);

/// <summary>Payments list page footer total — same filters as GetPaymentsPageQuery, no paging.</summary>
public sealed record PaymentsTotalsDto(decimal TotalAmountPaid, int Count);

/// <summary>Matches Angular's CustomerPayment projection — Customer Profile's "Payment History" tab, one customer already known so no CustomerName needed.</summary>
public sealed record PaymentWithLoanDto(
    string PaymentId,
    string LoanId,
    string LoanNumber,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber,
    string CreatedAt
);

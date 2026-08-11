namespace LoanManagementSystem.Application.Common.DTOs;

public sealed record PaymentDto(
    string PaymentId,
    string LoanId,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber
);

/// <summary>Matches Angular's PaymentWithCustomer projection used for the dashboard feed.</summary>
public sealed record PaymentWithCustomerDto(
    string PaymentId,
    string LoanId,
    string LoanNumber,
    string PaymentDate,
    decimal AmountPaid,
    string PaymentMethod,
    string Notes,
    string? ReferenceNumber,
    string CustomerName
);

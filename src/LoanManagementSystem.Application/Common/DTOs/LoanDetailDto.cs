namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>Backs the loan details dialog's GetLoanDetailUseCase equivalent on the Angular side.</summary>
public sealed record LoanDetailDto(
    LoanDto? Loan,
    List<LoanExtensionDto> Extensions,
    List<PaymentDto> Payments
);

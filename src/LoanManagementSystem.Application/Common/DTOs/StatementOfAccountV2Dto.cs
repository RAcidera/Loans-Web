namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// SOA V2's payload — one chronological "Account Activity" ledger (sourced
/// directly from loan_ledger, unlike V1's StatementOfAccountDto which
/// splits Extension History/Payment History out of loan.Extensions/
/// loan.Payments) plus the same summary/customer/loan fields V1 already
/// shows. See assets/Statement of Account V2.md.
/// </summary>
public sealed record StatementOfAccountV2Dto(
    string CustomerName,
    string CustomerCode,
    string CustomerAddress,
    string CustomerContactNumber,
    string LoanNumber,
    string StatementDate,
    string LoanDate,
    string DueDate,
    decimal PrincipalAmount,
    decimal InterestRate,
    decimal InterestAmount,
    List<SoaLedgerRowDto> Activity,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalExtensionCharges,
    decimal TotalPaid,
    decimal OutstandingBalance,
    string Status,
    string Classification
);

/// <summary>
/// One loan_ledger row as a PDF table row. TransactionType is the wire
/// string (see LoanLedgerTransactionType.ToWireString) — the PDF generator
/// owns turning it into a display label, same split as SoaPaymentRowDto's
/// PaymentMethod. RunningBalance is the sequential balance recomputed by
/// the handler (Previous + Debit - Credit), not the ledger row's own
/// stamped RunningBalance column — see GenerateLoanSoaV2Query.
/// </summary>
public sealed record SoaLedgerRowDto(
    string TransactionDate, string TransactionType, string Remarks, decimal Debit, decimal Credit, decimal RunningBalance
);

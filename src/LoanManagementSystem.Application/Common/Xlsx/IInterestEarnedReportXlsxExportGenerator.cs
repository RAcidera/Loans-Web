namespace LoanManagementSystem.Application.Common.Xlsx;

/// <summary>Rendering-only concern, same split as ILoansXlsxExportGenerator — the query handler assembles the rows, this just turns them into bytes.</summary>
public interface IInterestEarnedReportXlsxExportGenerator
{
    byte[] Generate(IReadOnlyList<InterestEarnedExportRowDto> rows);
}

/// <summary>One row of the Interest Earned Report export — spec §27's minimum column list, in that order.</summary>
public sealed record InterestEarnedExportRowDto(
    string LoanNumber,
    string CustomerName,
    string LoanDate,
    string DueDate,
    decimal Principal,
    decimal ContractInterest,
    decimal ExtensionInterest,
    decimal EarnedBeforePeriod,
    decimal EarnedThisPeriod,
    decimal TotalEarned,
    decimal Adjustment,
    decimal FinalEarned,
    string Status,
    string Classification
);

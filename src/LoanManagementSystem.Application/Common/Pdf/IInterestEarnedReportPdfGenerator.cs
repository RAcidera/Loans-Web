using LoanManagementSystem.Application.Common.DTOs;

namespace LoanManagementSystem.Application.Common.Pdf;

/// <summary>Interface lives in Application, same split as IStatementOfAccountPdfGenerator — the QuestPDF-backed implementation lives in Infrastructure.</summary>
public interface IInterestEarnedReportPdfGenerator
{
    byte[] Generate(InterestEarnedReportPdfDto report);
}

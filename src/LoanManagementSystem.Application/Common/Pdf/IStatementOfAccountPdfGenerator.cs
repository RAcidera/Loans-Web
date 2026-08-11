using LoanManagementSystem.Application.Common.DTOs;

namespace LoanManagementSystem.Application.Common.Pdf;

/// <summary>
/// Interface lives in Application (like IJwtTokenGenerator/IPasswordHasher)
/// so GenerateLoanSoaQuery doesn't depend on the concrete PDF library;
/// the QuestPDF-backed implementation lives in Infrastructure, alongside
/// this codebase's other third-party-library concerns.
/// </summary>
public interface IStatementOfAccountPdfGenerator
{
    byte[] Generate(StatementOfAccountDto statement);
}

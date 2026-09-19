using LoanManagementSystem.Application.Common.DTOs;

namespace LoanManagementSystem.Application.Common.Pdf;

/// <summary>
/// SOA V2's rendering port — sibling of IStatementOfAccountPdfGenerator,
/// kept as its own interface (rather than an overload) so V1's contract
/// and implementation are never touched while V2 is developed alongside it.
/// </summary>
public interface IStatementOfAccountV2PdfGenerator
{
    byte[] Generate(StatementOfAccountV2Dto statement);
}

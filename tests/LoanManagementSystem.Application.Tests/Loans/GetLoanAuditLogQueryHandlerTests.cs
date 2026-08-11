using LoanManagementSystem.Application.Loans.Queries.GetLoanAuditLog;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GetLoanAuditLogQueryHandlerTests
{
    private readonly Mock<ILoanAuditLogRepository> _loanAuditLogRepository = new();
    private readonly GetLoanAuditLogQueryHandler _handler;

    public GetLoanAuditLogQueryHandlerTests()
    {
        _handler = new GetLoanAuditLogQueryHandler(_loanAuditLogRepository.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEntries_NewestFirst()
    {
        var loanId = LoanId.New();
        var older = LoanAuditLogEntry.Record(loanId, LoanAuditAction.Edited, "Loan details edited.", "admin");
        var newer = LoanAuditLogEntry.Record(loanId, LoanAuditAction.WrittenOff, "Loan written off.", "admin");

        _loanAuditLogRepository.Setup(r => r.GetByLoanIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoanAuditLogEntry> { older, newer });

        var result = await _handler.Handle(new GetLoanAuditLogQuery(loanId.ToString()), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("written_off", result[0].Action);
        Assert.Equal("edited", result[1].Action);
    }
}

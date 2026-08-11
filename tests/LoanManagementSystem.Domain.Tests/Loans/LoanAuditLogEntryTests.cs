using LoanManagementSystem.Domain.Loans;
using Xunit;

namespace LoanManagementSystem.Domain.Tests.Loans;

public class LoanAuditLogEntryTests
{
    [Fact]
    public void Record_SetsFieldsFromArguments()
    {
        var loanId = LoanId.New();
        var entry = LoanAuditLogEntry.Record(loanId, LoanAuditAction.ClassificationChanged, "Classification changed from Normal to BadLoan", "admin");

        Assert.Equal(loanId, entry.LoanId);
        Assert.Equal(LoanAuditAction.ClassificationChanged, entry.Action);
        Assert.Equal("Classification changed from Normal to BadLoan", entry.Description);
        Assert.Equal("admin", entry.PerformedBy);
    }
}

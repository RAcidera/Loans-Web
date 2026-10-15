using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Loans.Queries.GetMonthlyCapitalGrowth;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

public class GetMonthlyCapitalGrowthQueryHandlerTests
{
    private const string Manila = "Asia/Manila";
    private static readonly DateOnly Today = new(2026, 10, 15);

    private readonly Mock<ICashLedgerRepository> _cash = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedger = new();
    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<ILoanAuditLogRepository> _auditLog = new();
    private readonly Mock<IDiaryRepository> _diary = new();
    private readonly Mock<IAppDateTimeService> _appDateTime = new();
    private readonly GetMonthlyCapitalGrowthQueryHandler _handler;

    public GetMonthlyCapitalGrowthQueryHandlerTests()
    {
        _appDateTime.Setup(s => s.Today).Returns(Today);
        _appDateTime.Setup(s => s.UtcNow).Returns(new DateTime(2026, 10, 15, 4, 0, 0, DateTimeKind.Utc));
        _appDateTime.Setup(s => s.BusinessTimeZoneId).Returns(Manila);
        _appDateTime.Setup(s => s.ConvertUtcToBusinessLocal(It.IsAny<DateTime>()))
            .Returns((DateTime utc) => BusinessTimeZoneCalculator.ConvertUtcToBusinessLocal(utc, Manila));
        _appDateTime.Setup(s => s.GetStartOfBusinessDayUtc(It.IsAny<DateOnly>()))
            .Returns((DateOnly d) => BusinessTimeZoneCalculator.GetStartOfBusinessDayUtc(d, Manila));
        _diary.Setup(r => r.GetSnapshotsCapturedBetweenAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DiaryFinancialSnapshot>());

        _handler = new GetMonthlyCapitalGrowthQueryHandler(_cash.Object, _loanLedger.Object, _loans.Object, _auditLog.Object, _diary.Object, _appDateTime.Object);
    }

    /// <summary>
    /// §9.9 timezone boundary: written off at 2026-08-31 16:30 UTC, which is
    /// already 2026-09-01 00:30 in Manila — so the loan is still carried at
    /// the end of August and dropped by the end of September. A UTC-date
    /// reading would have dropped it a month early.
    /// </summary>
    [Fact]
    public async Task WriteOff_JustAfterManilaMidnight_LandsInTheBusinessLocalMonth()
    {
        var loan = Loan.Originate(CustomerId.New(), Money.Of(1_000m), InterestRate.Default, new DateOnly(2026, 7, 1));
        loan.WriteOff("admin");
        var writeOff = LoanAuditLogEntry.Record(loan.Id, LoanAuditAction.WrittenOff, "Written off", "admin");
        typeof(LoanAuditLogEntry).GetProperty(nameof(LoanAuditLogEntry.OccurredAtUtc))!
            .SetValue(writeOff, new DateTime(2026, 8, 31, 16, 30, 0, DateTimeKind.Utc));

        _loans.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Loan> { loan });
        _auditLog.Setup(r => r.GetByActionAsync(LoanAuditAction.WrittenOff, It.IsAny<CancellationToken>())).ReturnsAsync(new List<LoanAuditLogEntry> { writeOff });
        _cash.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<CashLedgerEntry>
        {
            CashLedgerEntry.Record(CashTransactionType.OwnerDeposit, Money.Of(5_000m), "", new DateOnly(2026, 6, 1)),
            CashLedgerEntry.Record(CashTransactionType.LoanRelease, Money.Of(1_000m), "", new DateOnly(2026, 7, 1)),
        });
        _loanLedger.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1_000m), Money.Zero, Money.Zero, "", new DateOnly(2026, 7, 1)),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, loan.TotalInterest, Money.Zero, Money.Zero, "", new DateOnly(2026, 7, 1)),
        });

        var result = await _handler.Handle(new GetMonthlyCapitalGrowthQuery(12), CancellationToken.None);

        Assert.Equal(1_030m, result.Series.Single(m => m.MonthKey == "2026-08").EndingGrossReceivables);
        Assert.Equal(0m, result.Series.Single(m => m.MonthKey == "2026-09").EndingGrossReceivables);
        Assert.True(result.Reconciliation.Matches);
        Assert.Equal("Asia/Manila", result.TimeZone);
        Assert.DoesNotContain(result.Warnings, w => w.Code == "writeOffDateUnknown");
    }

    [Fact]
    public async Task UnsupportedRange_ThrowsDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(new GetMonthlyCapitalGrowthQuery(7), CancellationToken.None));
    }
}

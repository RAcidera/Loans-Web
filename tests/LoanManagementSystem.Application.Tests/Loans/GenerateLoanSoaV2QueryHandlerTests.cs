using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Application.Common.Pdf;
using LoanManagementSystem.Application.Loans.Queries.GenerateLoanSoaV2;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LoanManagementSystem.Application.Tests.Loans;

/// <summary>
/// Covers assets/Statement of Account V2.md §20's test matrix: the ledger
/// is the sole source for the Account Activity table, running balance is
/// recomputed sequentially (never trusted from the ledger's own stamped
/// RunningBalance), rows are ordered by TransactionDate then CreatedAtUtc,
/// and the reconciliation check only logs — it never mutates data or
/// blocks generation.
/// </summary>
public class GenerateLoanSoaV2QueryHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepository = new();
    private readonly Mock<ICustomerRepository> _customerRepository = new();
    private readonly Mock<ILoanLedgerRepository> _loanLedgerRepository = new();
    private readonly Mock<IStatementOfAccountV2PdfGenerator> _pdfGenerator = new();
    private readonly Mock<IAppDateTimeService> _appDateTime = new();
    private readonly Mock<ILogger<GenerateLoanSoaV2QueryHandler>> _logger = new();
    private readonly GenerateLoanSoaV2QueryHandler _handler;

    public GenerateLoanSoaV2QueryHandlerTests()
    {
        _appDateTime.Setup(s => s.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        _handler = new GenerateLoanSoaV2QueryHandler(
            _loanRepository.Object, _customerRepository.Object, _loanLedgerRepository.Object,
            _pdfGenerator.Object, _appDateTime.Object, _logger.Object);
    }

    private static (Customer customer, Loan loan) SeedLoan(decimal principal = 100_000m, decimal interestRate = 0.07m)
    {
        var customer = Customer.Create("Maria Reyes", "1 Rizal Ave.", "+63 917 000 1111", "Community member");
        var loan = Loan.Originate(customer.Id, Money.Of(principal), InterestRate.Of(interestRate), new DateOnly(2026, 7, 2), 60);
        return (customer, loan);
    }

    private void SetupRepositories(Customer customer, Loan loan, List<LoanLedgerEntry> ledger)
    {
        _loanRepository.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _customerRepository.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
        _loanLedgerRepository.Setup(r => r.GetByLoanIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ledger);
    }

    private StatementOfAccountV2Dto? CaptureStatement()
    {
        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto)
            .Returns(new byte[] { 1, 2, 3 });
        return captured;
    }

    // 1. Loan with no payments
    [Fact]
    public async Task Handle_LoanWithNoPayments_ActivityHasOnlyOriginationRows()
    {
        var (customer, loan) = SeedLoan(1000m, 0.03m);
        var released = LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", loan.StartDate);
        var interest = LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030), "Interest added", loan.StartDate);
        SetupRepositories(customer, loan, new List<LoanLedgerEntry> { released, interest });

        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>())).Returns(new byte[] { 1 });
        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Activity.Count);
        Assert.Equal(1030m, captured.Activity[^1].RunningBalance);
        Assert.Equal(loan.Balance.Amount, captured.OutstandingBalance);
        Assert.Equal(1030m, captured.TotalDebit - captured.TotalCredit);
    }

    // 2 & 3. One payment / many payments, plus 6. extension between payments — the
    // LOA00020-style scenario from the spec's §19 worked example.
    [Fact]
    public async Task Handle_Loa00020Scenario_MatchesSpecWorkedExample()
    {
        var (customer, loan) = SeedLoan(100_000m, 0.07m);
        var p1 = loan.RecordPayment(Money.Of(7000), PaymentMethod.Cash, "Interest only", new DateOnly(2026, 8, 4));
        var p2 = loan.RecordPayment(Money.Of(7000), PaymentMethod.Cash, "Interest only", new DateOnly(2026, 9, 2));
        var ext = loan.Extend(30, Money.Of(7000), "Additional interest Aug 2-Sep 2", new DateOnly(2026, 9, 4));
        var p3 = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 9, 13));
        var p4 = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 9, 15));
        var p5 = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 9, 17));
        var p6 = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 9, 19));
        var p7 = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 9, 21));

        var ledger = new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(100_000), Money.Zero, Money.Of(100_000), "Loan released", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(7000), Money.Zero, Money.Of(107_000), "Interest added", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(7000), Money.Of(100_000), "Interest only", p1.PaymentDate, p1.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(7000), Money.Of(93_000), "Interest only", p2.PaymentDate, p2.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Extension, Money.Of(7000), Money.Zero, Money.Of(100_000), "Additional interest Aug 2-Sep 2", ext.ExtensionDate, ext.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Of(99_000), null!, p3.PaymentDate, p3.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Of(98_000), null!, p4.PaymentDate, p4.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Of(97_000), null!, p5.PaymentDate, p5.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Of(96_000), null!, p6.PaymentDate, p6.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Of(95_000), null!, p7.PaymentDate, p7.Id.ToString()),
        };
        SetupRepositories(customer, loan, ledger);

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(10, captured!.Activity.Count);
        Assert.Equal(114_000m, captured.TotalDebit);
        Assert.Equal(19_000m, captured.TotalCredit);
        Assert.Equal(95_000m, captured.TotalDebit - captured.TotalCredit);
        Assert.Equal(95_000m, captured.Activity[^1].RunningBalance);
        Assert.Equal(loan.Balance.Amount, captured.OutstandingBalance);
        Assert.Equal(95_000m, loan.Balance.Amount);

        // Balance follows the exact worked-example trajectory.
        Assert.Equal(new[] { 100_000m, 107_000m, 100_000m, 93_000m, 100_000m, 99_000m, 98_000m, 97_000m, 96_000m, 95_000m },
            captured.Activity.Select(a => a.RunningBalance).ToArray());
    }

    // 4 & 5. One extension / multiple extensions.
    [Fact]
    public async Task Handle_LoanWithMultipleExtensions_EachAppearsAsItsOwnDebitRow()
    {
        var (customer, loan) = SeedLoan(1000m, 0m);
        var ext1 = loan.Extend(15, Money.Of(50), "first extension", new DateOnly(2026, 3, 1));
        var ext2 = loan.Extend(15, Money.Of(60), "second extension", new DateOnly(2026, 3, 16));

        var ledger = new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Zero, Money.Zero, Money.Of(1000), "Interest added", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Extension, Money.Of(50), Money.Zero, Money.Of(1050), "first extension", ext1.ExtensionDate, ext1.Id.ToString()),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Extension, Money.Of(60), Money.Zero, Money.Of(1110), "second extension", ext2.ExtensionDate, ext2.Id.ToString()),
        };
        SetupRepositories(customer, loan, ledger);

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.Activity.Count(a => a.TransactionType == "extension"));
        Assert.Equal(1110m, captured.Activity[^1].RunningBalance);
        Assert.Equal(loan.Balance.Amount, captured.OutstandingBalance);
    }

    // 7. Fully paid loan
    [Fact]
    public async Task Handle_FullyPaidLoan_FinalRunningBalanceIsZero()
    {
        var (customer, loan) = SeedLoan(1000m, 0m);
        var payment = loan.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "Full settlement", new DateOnly(2026, 8, 1));

        var ledger = new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Zero, Money.Zero, Money.Of(1000), "Interest added", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(1000), Money.Zero, "Full settlement", payment.PaymentDate, payment.Id.ToString()),
        };
        SetupRepositories(customer, loan, ledger);

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(0m, captured!.Activity[^1].RunningBalance);
        Assert.Equal(0m, loan.Balance.Amount);
    }

    // 9. Bad loan / 10. Written-off loan — classification/status flow through untouched.
    [Fact]
    public async Task Handle_WrittenOffBadLoan_StatusAndClassificationPassThrough()
    {
        var (customer, loan) = SeedLoan(1000m, 0m);
        loan.ChangeClassification(LoanClassification.BadLoan, "admin");
        loan.WriteOff("admin");

        var ledger = new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Zero, Money.Zero, Money.Of(1000), "Interest added", loan.StartDate),
        };
        SetupRepositories(customer, loan, ledger);

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("WrittenOff", captured!.Status);
        Assert.Equal("BadLoan", captured.Classification);
    }

    // 12. Multiple transactions on the same date — deterministic secondary sort by CreatedAtUtc.
    [Fact]
    public async Task Handle_MultipleEntriesOnSameDate_OrderedByCreatedAtUtcNotArbitrarily()
    {
        var (customer, loan) = SeedLoan(1000m, 0.03m);
        // LoanReleased is recorded before InterestAdded (same TransactionDate),
        // reflecting LoanCreatedEventHandler's real write order.
        var released = LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "Loan released", loan.StartDate);
        var interest = LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.InterestAdded, Money.Of(30), Money.Zero, Money.Of(1030), "Interest added", loan.StartDate);

        // Ledger returned out of CreatedAtUtc order to prove the handler re-sorts rather than trusting repository order.
        SetupRepositories(customer, loan, new List<LoanLedgerEntry> { interest, released });

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("loan_released", captured!.Activity[0].TransactionType);
        Assert.Equal("interest_added", captured.Activity[1].TransactionType);
        Assert.Equal(1000m, captured.Activity[0].RunningBalance);
        Assert.Equal(1030m, captured.Activity[1].RunningBalance);
    }

    // 13. Null/empty remarks
    [Fact]
    public async Task Handle_NullOrEmptyRemarks_PassedThroughAsIs()
    {
        var (customer, loan) = SeedLoan(1000m, 0m);
        var payment = loan.RecordPayment(Money.Of(200), PaymentMethod.Cash, "", new DateOnly(2026, 8, 1));

        var ledger = new List<LoanLedgerEntry>
        {
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(1000), Money.Zero, Money.Of(1000), "", loan.StartDate),
            LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.Payment, Money.Zero, Money.Of(200), Money.Of(800), null!, payment.PaymentDate, payment.Id.ToString()),
        };
        SetupRepositories(customer, loan, ledger);

        StatementOfAccountV2Dto? captured = null;
        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>()))
            .Callback<StatementOfAccountV2Dto>(dto => captured = dto).Returns(new byte[] { 1 });

        await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.True(string.IsNullOrEmpty(captured!.Activity[0].Remarks));
        Assert.True(string.IsNullOrEmpty(captured.Activity[1].Remarks));
    }

    // Reconciliation check: mismatch is logged, never thrown, never mutates.
    [Fact]
    public async Task Handle_LedgerBalanceDoesNotMatchLoanBalance_LogsWarningButStillGeneratesPdf()
    {
        var (customer, loan) = SeedLoan(1000m, 0m);

        // Ledger under-represents the loan's balance (e.g. an origination row missing from a pre-ledger loan).
        var released = LoanLedgerEntry.Record(loan.Id, LoanLedgerTransactionType.LoanReleased, Money.Of(900), Money.Zero, Money.Of(900), "Loan released", loan.StartDate);
        SetupRepositories(customer, loan, new List<LoanLedgerEntry> { released });

        _pdfGenerator.Setup(g => g.Generate(It.IsAny<StatementOfAccountV2Dto>())).Returns(new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(new GenerateLoanSoaV2Query(loan.Id.ToString()), CancellationToken.None);

        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
        _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // The loan aggregate itself must be untouched — reconciliation only logs.
        Assert.Equal(1000m, loan.Balance.Amount);
    }

    [Fact]
    public async Task Handle_UnknownLoanId_ThrowsNotFound()
    {
        _loanRepository.Setup(r => r.GetByIdAsync(It.IsAny<LoanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GenerateLoanSoaV2Query(LoanId.New().ToString()), CancellationToken.None));
    }
}

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// The single most important test in this suite: it proves the domain
/// event architecture described throughout the backend README actually
/// works when exercised through real HTTP requests, a real EF Core
/// SaveChangesAsync, and real MediatR publishing — not just that the
/// individual pieces compile in isolation. If PaymentRecordedEventHandler
/// were never registered, or AppDbContext's event collection logic had
/// the AggregateRoot&lt;Guid&gt; bug described in the backend README's
/// history, this test would catch it: the cash ledger balance would not
/// move after recording a payment.
/// </summary>
public class FunctionalFlowTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public FunctionalFlowTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecordingAPayment_AutomaticallyCreatesAMatchingCashLedgerEntry()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var loans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var loan = loans!.First(l => l.Status != "paid");

        var cashBefore = await (await client.GetAsync("/api/cash-funds/summary")).Content.ReadFromJsonAsync<CashSummaryDto>();

        const decimal paymentAmount = 37m; // an amount unlikely to collide with seeded data, to make the assertion below unambiguous

        var paymentResponse = await client.PostAsJsonAsync(
            $"/api/loans/{loan.LoanId}/payments",
            new { amountPaid = paymentAmount, paymentMethod = "cash", notes = "e2e test" });
        paymentResponse.EnsureSuccessStatusCode();

        var cashAfter = await (await client.GetAsync("/api/cash-funds/summary")).Content.ReadFromJsonAsync<CashSummaryDto>();
        var ledgerAfter = await (await client.GetAsync("/api/cash-funds/ledger")).Content.ReadFromJsonAsync<List<CashLedgerEntryDto>>();

        // 1. Cash on hand went up by exactly the payment amount — proves
        //    PaymentRecordedDomainEvent fired and was handled.
        Assert.Equal(cashBefore!.CashOnHand + paymentAmount, cashAfter!.CashOnHand);

        // 2. A payment_received entry referencing this exact loan exists —
        //    proves it's the *right* handler doing the *right* thing, not
        //    a coincidental balance match.
        var matchingEntry = ledgerAfter!.SingleOrDefault(e =>
            e.TransactionType == "payment_received" && e.ReferenceId == loan.LoanNumber && e.Amount == paymentAmount);
        Assert.NotNull(matchingEntry);
    }

    [Fact]
    public async Task OriginatingALoan_AutomaticallyCreatesALoanReleaseEntry()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var customers = await (await client.GetAsync("/api/customers")).Content.ReadFromJsonAsync<List<CustomerDto>>();
        var customer = customers!.First();

        var cashBefore = await (await client.GetAsync("/api/cash-funds/summary")).Content.ReadFromJsonAsync<CashSummaryDto>();

        const decimal principal = 777m;
        var createResponse = await client.PostAsJsonAsync("/api/loans", new { customerId = customer.CustomerId, principal });
        createResponse.EnsureSuccessStatusCode();
        var createdLoan = await createResponse.Content.ReadFromJsonAsync<LoanDto>();

        var cashAfter = await (await client.GetAsync("/api/cash-funds/summary")).Content.ReadFromJsonAsync<CashSummaryDto>();
        var ledgerAfter = await (await client.GetAsync("/api/cash-funds/ledger")).Content.ReadFromJsonAsync<List<CashLedgerEntryDto>>();

        // Releasing a loan is Cash OUT (SRS: loan_release -> Cash Out).
        Assert.Equal(cashBefore!.CashOnHand - principal, cashAfter!.CashOnHand);

        var matchingEntry = ledgerAfter!.SingleOrDefault(e =>
            e.TransactionType == "loan_release" && e.ReferenceId == createdLoan!.LoanNumber && e.Amount == principal);
        Assert.NotNull(matchingEntry);
    }

    [Fact]
    public async Task ExtendingALoan_DoesNotCreateAnyCashLedgerEntry()
    {
        // The negative-space assertion matters as much as the positive
        // ones: LoanExtendedDomainEvent has no handler that touches the
        // ledger (an extension adds a fee, it doesn't move cash), and this
        // proves that's actually true at runtime, not just true "because
        // no handler class exists for it" (which could regress silently
        // if someone added one incorrectly).
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var loans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var loan = loans!.First(l => l.Status != "paid");

        var ledgerCountBefore = (await (await client.GetAsync("/api/cash-funds/ledger")).Content.ReadFromJsonAsync<List<CashLedgerEntryDto>>())!.Count;

        var extendResponse = await client.PostAsJsonAsync(
            $"/api/loans/{loan.LoanId}/extensions",
            new { extensionDays = 15, additionalInterestAmount = 25, remarks = "e2e test extension" });
        extendResponse.EnsureSuccessStatusCode();

        var ledgerCountAfter = (await (await client.GetAsync("/api/cash-funds/ledger")).Content.ReadFromJsonAsync<List<CashLedgerEntryDto>>())!.Count;

        Assert.Equal(ledgerCountBefore, ledgerCountAfter);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private sealed record LoginResponseDto(string Token, DateTime ExpiresAtUtc, string Username, string Role);
    private sealed record LoanDto(string LoanId, string LoanNumber, string Status);
    private sealed record CustomerDto(string CustomerId);
    private sealed record CashSummaryDto(decimal TotalCashIn, decimal TotalCashOut, decimal CashOnHand, decimal RevolvingFunds, decimal OutstandingPrincipal, List<decimal> SevenDayTrend);
    private sealed record CashLedgerEntryDto(string LedgerId, string TransactionDate, string TransactionType, string? ReferenceId, decimal Amount, string Remarks, string CreatedAt);
}

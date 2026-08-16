using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Exercises the Loans list page's server-side filters (spec "Loan Search
/// and Filtering") and its footer-totals endpoint end-to-end, against the
/// real seeded data — not just the repository's WHERE-clause logic in
/// isolation, which unit tests can't reach since ILoanRepository is mocked
/// everywhere else in the test suite.
/// </summary>
public class LoansPageFilterTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public LoansPageFilterTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FilterByClassification_ReturnsOnlyLoansWithThatClassification()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var allLoans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var target = allLoans!.First(l => l.Status != "paid");

        var changeResponse = await client.PutAsJsonAsync($"/api/loans/{target.LoanId}/classification", new { classification = "BadLoan" });
        changeResponse.EnsureSuccessStatusCode();

        var page = await (await client.GetAsync("/api/loans/page?classification=BadLoan")).Content.ReadFromJsonAsync<PagedResultDto>();

        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, l => Assert.Equal("badloan", l.Classification));
        Assert.Contains(page.Items, l => l.LoanId == target.LoanId);
    }

    [Fact]
    public async Task FilterByBadLoansOnly_MatchesFilterByClassificationBadLoan()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var byClassification = await (await client.GetAsync("/api/loans/page?classification=BadLoan&pageSize=100")).Content.ReadFromJsonAsync<PagedResultDto>();
        var byCheckbox = await (await client.GetAsync("/api/loans/page?badLoansOnly=true&pageSize=100")).Content.ReadFromJsonAsync<PagedResultDto>();

        Assert.Equal(
            byClassification!.Items.Select(l => l.LoanId).OrderBy(x => x),
            byCheckbox!.Items.Select(l => l.LoanId).OrderBy(x => x));
    }

    [Fact]
    public async Task GetPageTotals_SumsMatchTheFilteredPage()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var page = await (await client.GetAsync("/api/loans/page?pageSize=100")).Content.ReadFromJsonAsync<PagedResultDto>();
        var totals = await (await client.GetAsync("/api/loans/page/totals")).Content.ReadFromJsonAsync<LoanTotalsDto>();

        Assert.Equal(page!.Items.Sum(l => l.PrincipalAmount), totals!.TotalPrincipal);
        Assert.Equal(page.Items.Sum(l => l.Balance), totals.TotalOutstandingBalance);
        Assert.Equal(page.TotalCount, totals.TotalLoansCount);
    }

    [Fact]
    public async Task GetPage_ExtendedLoan_ReportsExtendedNotActive()
    {
        // Regression test: GetPageAsync/GetFilteredAsync didn't Include()
        // Extensions, so Loan.RefreshOverdueStatus's _extensions.Count > 0
        // check always saw zero and silently downgraded every Extended loan
        // to Active on this endpoint only (GetLoanDetail's GetByIdAsync
        // always included Extensions, so the loan profile page never showed
        // the bug). A loan created and extended here — rather than relying
        // on DbSeeder's fixed 2026 dates, which drift into "overdue" as real
        // time passes — is extended and not yet due for a full 90 days, so
        // this assertion can't flip to overdue on its own later.
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var customers = await (await client.GetAsync("/api/customers")).Content.ReadFromJsonAsync<List<CustomerIdDto>>();
        var createResponse = await client.PostAsJsonAsync("/api/loans", new { customerId = customers!.First().CustomerId, principal = 1000m });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<LoanDto>();

        var extendResponse = await client.PostAsJsonAsync(
            $"/api/loans/{created!.LoanId}/extensions", new { extensionDays = 30, remarks = "test extension", additionalChargesAmount = 25 });
        extendResponse.EnsureSuccessStatusCode();

        var page = await (await client.GetAsync("/api/loans/page?pageSize=200")).Content.ReadFromJsonAsync<PagedResultWithStatusDto>();
        var flatList = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();

        var pageStatus = page!.Items.Single(l => l.LoanId == created.LoanId).Status;
        var listStatus = flatList!.Single(l => l.LoanId == created.LoanId).Status;

        Assert.Equal("extended", listStatus);
        Assert.Equal("extended", pageStatus);
    }

    [Fact]
    public async Task FilterByCommaSeparatedStatuses_MatchesEitherStatus()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var activeOnly = await (await client.GetAsync("/api/loans/page?status=active&pageSize=200")).Content.ReadFromJsonAsync<PagedResultWithStatusDto>();
        var paidOnly = await (await client.GetAsync("/api/loans/page?status=paid&pageSize=200")).Content.ReadFromJsonAsync<PagedResultWithStatusDto>();
        var combined = await (await client.GetAsync("/api/loans/page?status=active,paid&pageSize=200")).Content.ReadFromJsonAsync<PagedResultWithStatusDto>();

        var expectedIds = activeOnly!.Items.Concat(paidOnly!.Items).Select(l => l.LoanId).OrderBy(x => x).Distinct();
        var actualIds = combined!.Items.Select(l => l.LoanId).OrderBy(x => x).Distinct();

        Assert.Equal(expectedIds, actualIds);
        Assert.All(combined.Items, l => Assert.True(l.Status == "active" || l.Status == "paid"));
    }

    [Fact]
    public async Task SortByCustomer_ReturnsLoansOrderedByCustomerName()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var ascPage = await (await client.GetAsync("/api/loans/page?pageSize=100&sortBy=customer&sortDir=asc")).Content.ReadFromJsonAsync<PagedResultWithCustomerDto>();
        var names = ascPage!.Items.Select(l => l.CustomerName).ToList();

        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task SortByLoanDate_ReturnsLoansOrderedByStartDate()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var ascPage = await (await client.GetAsync("/api/loans/page?pageSize=100&sortBy=loanDate&sortDir=asc")).Content.ReadFromJsonAsync<PagedResultWithDatesDto>();
        var dates = ascPage!.Items.Select(l => l.StartDate).ToList();

        Assert.Equal(dates.OrderBy(d => d, StringComparer.Ordinal), dates);
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
    private sealed record LoanDto(string LoanId, string Status);
    private sealed record LoanPageItemDto(string LoanId, decimal PrincipalAmount, decimal Balance, string Classification);
    private sealed record PagedResultDto(List<LoanPageItemDto> Items, int TotalCount);
    private sealed record LoanTotalsDto(
        decimal TotalPrincipal, decimal TotalInterest, decimal TotalExtensionCharges, decimal TotalPayments,
        decimal TotalOutstandingBalance, int TotalLoansCount, int ActiveLoansCount, int OverdueLoansCount, int PaidLoansCount);
    private sealed record LoanWithCustomerDto(string LoanId, string CustomerName);
    private sealed record PagedResultWithCustomerDto(List<LoanWithCustomerDto> Items, int TotalCount);
    private sealed record LoanWithDatesDto(string LoanId, string StartDate);
    private sealed record PagedResultWithDatesDto(List<LoanWithDatesDto> Items, int TotalCount);
    private sealed record PagedResultWithStatusDto(List<LoanDto> Items, int TotalCount);
    private sealed record CustomerIdDto(string CustomerId);
}

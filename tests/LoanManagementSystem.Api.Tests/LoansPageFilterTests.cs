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
    private sealed record LoanTotalsDto(decimal TotalPrincipal, decimal TotalInterest, decimal TotalExtensionCharges, decimal TotalPayments, decimal TotalOutstandingBalance);
}

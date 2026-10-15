using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>GET /api/dashboard/monthly-capital-growth over real seeded data — shape, auth, validation, reconciliation with the live KPI, and that calling it leaves every existing dashboard figure untouched (read-only).</summary>
public class MonthlyCapitalGrowthTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public MonthlyCapitalGrowthTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsTwelveMonthsEndingWithThePartialCurrentMonth_ReconciledWithTheDashboard()
    {
        var client = await CreateAuthenticatedClientAsync("staff", "Staff@12345");

        var response = await client.GetAsync("/api/dashboard/monthly-capital-growth?months=12");
        response.EnsureSuccessStatusCode();
        var growth = await response.Content.ReadFromJsonAsync<MonthlyCapitalGrowthDto>();

        Assert.NotNull(growth);
        Assert.Equal(12, growth!.Series.Count);
        Assert.Equal("PHP", growth.Currency);
        Assert.Equal("restated", growth.HistoryBasis);
        Assert.True(growth.Series[^1].IsPartialMonth);
        Assert.Equal(growth.AsOfDate, growth.Series[^1].EndCutoffDate);
        Assert.True(growth.Reconciliation.Matches, $"Dashboard {growth.Reconciliation.DashboardPosition} vs series {growth.Reconciliation.CurrentEndingPosition}");

        var cash = await client.GetFromJsonAsync<CashSummaryDto>("/api/cash-funds/summary");
        Assert.Equal(cash!.CashOnHand + cash.GrossReceivables, growth.Series[^1].EndingPosition);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(24)]
    public async Task Get_HonorsEachAllowedRange(int months)
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var growth = await client.GetFromJsonAsync<MonthlyCapitalGrowthDto>($"/api/dashboard/monthly-capital-growth?months={months}");

        Assert.Equal(months, growth!.Series.Count);
    }

    [Fact]
    public async Task Get_WithUnsupportedRange_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync("/api/dashboard/monthly-capital-growth?months=7");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/dashboard/monthly-capital-growth");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_IsReadOnly_ExistingDashboardFiguresAreUnchanged()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var summaryBefore = await client.GetStringAsync("/api/dashboard/summary");
        var cashBefore = await client.GetStringAsync("/api/cash-funds/summary");

        (await client.GetAsync("/api/dashboard/monthly-capital-growth?months=24")).EnsureSuccessStatusCode();

        Assert.Equal(summaryBefore, await client.GetStringAsync("/api/dashboard/summary"));
        Assert.Equal(cashBefore, await client.GetStringAsync("/api/cash-funds/summary"));
    }

    /// <summary>
    /// Regression guard for the drift found in production (LOA00001/03/34/36):
    /// interest and extension-charge edits made before the ledger-sync
    /// handlers existed left stale loan_ledger rows. Through the real HTTP
    /// pipeline, an interest edit and an extension-charge edit must keep
    /// Σ(debit − credit) equal to the loan's Balance, so capital growth
    /// raises no ledgerBalanceMismatch for this loan.
    /// </summary>
    [Fact]
    public async Task EditingInterestAndExtensionCharge_KeepsLoanLedgerInSyncWithBalance()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var customers = await client.GetFromJsonAsync<List<CustomerDto>>("/api/customers");
        var created = await (await client.PostAsJsonAsync("/api/loans", new { customerId = customers!.First().CustomerId, principal = 10_000m }))
            .Content.ReadFromJsonAsync<LoanDto>();

        var extensionResponse = await client.PostAsJsonAsync($"/api/loans/{created!.LoanId}/extensions", new { extensionDays = 30, remarks = "ext", additionalChargesAmount = 900m });
        extensionResponse.EnsureSuccessStatusCode();
        var extension = await extensionResponse.Content.ReadFromJsonAsync<ExtensionDto>();

        (await client.PutAsJsonAsync($"/api/loans/{created.LoanId}", new { interestAmount = 1_234m })).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"/api/loans/{created.LoanId}/extensions/{extension!.ExtensionId}", new { extensionDays = 30, remarks = "ext", additionalChargesAmount = 400m })).EnsureSuccessStatusCode();

        var loan = await client.GetFromJsonAsync<LoanDto>($"/api/loans/{created.LoanId}");
        var ledger = await client.GetFromJsonAsync<List<LedgerRowDto>>($"/api/loans/{created.LoanId}/ledger");

        Assert.Equal(10_000m + 1_234m + 400m, loan!.Balance);
        Assert.Equal(loan.Balance, ledger!.Sum(r => r.Debit - r.Credit));

        var growth = await client.GetFromJsonAsync<MonthlyCapitalGrowthWithWarningsDto>("/api/dashboard/monthly-capital-growth?months=6");
        Assert.DoesNotContain(growth!.Warnings, w => w.Code == "ledgerBalanceMismatch" && w.LoanNumbers.Contains(loan.LoanNumber));
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
    private sealed record CashSummaryDto(decimal CashOnHand, decimal GrossReceivables);
    private sealed record MonthDto(string MonthKey, string EndCutoffDate, bool IsPartialMonth, string Status, decimal? EndingPosition);
    private sealed record ReconciliationDto(decimal DashboardPosition, decimal? CurrentEndingPosition, bool Matches);
    private sealed record CustomerDto(string CustomerId);
    private sealed record LoanDto(string LoanId, string LoanNumber, decimal Balance);
    private sealed record ExtensionDto(string ExtensionId);
    private sealed record LedgerRowDto(decimal Debit, decimal Credit);
    private sealed record WarningDto(string Code, List<string> LoanNumbers);
    private sealed record MonthlyCapitalGrowthWithWarningsDto(List<WarningDto> Warnings);
    private sealed record MonthlyCapitalGrowthDto(string AsOfDate, string Currency, string HistoryBasis, List<MonthDto> Series, ReconciliationDto Reconciliation);
}

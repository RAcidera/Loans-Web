using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>Proves GET /api/dashboard/summary assembles a coherent response over real seeded data — the Dashboard's charts/trend badges/Recent Loans widget.</summary>
public class DashboardSummaryTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public DashboardSummaryTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSummary_ReturnsShapeConsistentWithSeededData()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync("/api/dashboard/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        Assert.NotNull(summary);
        Assert.Equal(12, summary!.MonthlyCollections.Count);
        Assert.Equal(7, summary.Last7DaysCollections.Count);
        Assert.True(summary.RecentLoans.Count <= 5);
        Assert.True(summary.GrossReceivables >= summary.CollectibleReceivables);

        var breakdownTotal = summary.ReceivablesBreakdown.Current + summary.ReceivablesBreakdown.Overdue
            + summary.ReceivablesBreakdown.BadLoan + summary.ReceivablesBreakdown.Paid;
        Assert.True(breakdownTotal > 0);
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
    private sealed record ReceivablesBreakdownDto(decimal Current, decimal Overdue, decimal BadLoan, decimal Paid);
    private sealed record MonthlyCollectionDto(string Month, decimal ThisYear, decimal LastYear);
    private sealed record DailyCollectionDto(string Date, decimal Amount);
    private sealed record LoanDto(string LoanId);
    private sealed record DashboardSummaryDto(
        decimal GrossReceivables, decimal? GrossReceivablesChangePercent,
        decimal CollectibleReceivables, decimal? CollectibleReceivablesChangePercent,
        decimal BadLoanReceivables, decimal? BadLoanReceivablesChangePercent,
        int ActiveLoansCount, decimal? ActiveLoansChangePercent,
        int OverdueLoansCount, decimal? OverdueLoansChangePercent,
        int LoansDueThisWeekCount,
        List<MonthlyCollectionDto> MonthlyCollections,
        List<DailyCollectionDto> Last7DaysCollections,
        ReceivablesBreakdownDto ReceivablesBreakdown,
        List<LoanDto> RecentLoans);
}

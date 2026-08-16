using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>End-to-end coverage for the Interest Earned Report endpoints — mainly guards routing/DI wiring and serialization, since the calculation math itself is covered by InterestCalculationServiceTests.</summary>
public class InterestEarnedReportTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public InterestEarnedReportTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Page_Overview_AndBreakdown_ReflectANewlyCreatedLoan()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var createCustomerResponse = await client.PostAsJsonAsync(
            "/api/customers", new { fullName = "Interest Report Test Subject", address = "N/A", contactNumber = "+63 900 000 0003", borrowerType = "Test" });
        createCustomerResponse.EnsureSuccessStatusCode();
        var customer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createLoanResponse = await client.PostAsJsonAsync("/api/loans", new
        {
            customerId = customer!.CustomerId,
            principal = 10_000m,
            interestRate = 0.03m,
            paymentTermsMonths = 2,
            startDate = today.ToString("yyyy-MM-dd"),
        });
        createLoanResponse.EnsureSuccessStatusCode();
        var loan = await createLoanResponse.Content.ReadFromJsonAsync<LoanDto>();

        var fromDate = today.ToString("yyyy-MM-dd");
        var toDate = today.AddDays(10).ToString("yyyy-MM-dd");

        // Search by loan number is a substring match and this suite shares one
        // database across many test files, so other loans' zero-padded numbers
        // can share a prefix (e.g. "LOA00001" contains "LOA0000") — page through
        // a large size and find this test's own loan by id rather than assuming
        // the search returns exactly one row.
        var page = await (await client.GetAsync(
                $"/api/reports/interest-earned/page?fromDate={fromDate}&toDate={toDate}&search={Uri.EscapeDataString(loan!.LoanNumber)}&pageSize=200"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        var row = page!.Items.Single(r => r.LoanId == loan.LoanId);
        Assert.Equal(300m, row.ContractInterest); // 10,000 * 3%
        Assert.True(row.EarnedThisPeriod > 0);
        Assert.True(row.EarnedThisPeriod <= row.ContractInterest);

        var overview = await (await client.GetAsync(
                $"/api/reports/interest-earned/overview?fromDate={fromDate}&toDate={toDate}&search={Uri.EscapeDataString(customer.FullName)}"))
            .Content.ReadFromJsonAsync<OverviewDto>();
        Assert.True(overview!.Summary.OriginalInterestEarned > 0);
        Assert.Equal(1, overview.Totals.Count);

        var breakdown = await (await client.GetAsync(
                $"/api/reports/interest-earned/{loan.LoanId}/breakdown?fromDate={fromDate}&toDate={toDate}"))
            .Content.ReadFromJsonAsync<BreakdownDto>();
        Assert.Equal(300m, breakdown!.OriginalContractInterest);
        Assert.Single(breakdown.Periods); // no extensions yet
    }

    [Fact]
    public async Task ExportXlsx_AndExportPdf_ReturnNonEmptyFiles()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = today.AddMonths(-1).ToString("yyyy-MM-dd");
        var toDate = today.ToString("yyyy-MM-dd");

        var xlsxResponse = await client.GetAsync($"/api/reports/interest-earned/export/xlsx?fromDate={fromDate}&toDate={toDate}");
        xlsxResponse.EnsureSuccessStatusCode();
        Assert.True((await xlsxResponse.Content.ReadAsByteArrayAsync()).Length > 0);

        var pdfResponse = await client.GetAsync($"/api/reports/interest-earned/export/pdf?fromDate={fromDate}&toDate={toDate}");
        pdfResponse.EnsureSuccessStatusCode();
        Assert.True((await pdfResponse.Content.ReadAsByteArrayAsync()).Length > 0);
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
    private sealed record CustomerDto(string CustomerId, string FullName);
    private sealed record LoanDto(string LoanId, string LoanNumber, decimal Balance);
    private sealed record InterestEarnedRowDto(string LoanId, string LoanNumber, decimal ContractInterest, decimal EarnedThisPeriod);
    private sealed record PagedResultDto(List<InterestEarnedRowDto> Items, int TotalCount);
    private sealed record SummaryDto(decimal TotalEarnedInterest, decimal OriginalInterestEarned, decimal ExtensionInterestEarned, decimal InterestAdjustments, decimal? InterestCollected, decimal? InterestReceivable);
    private sealed record TotalsDto(decimal Principal, decimal ContractInterest, decimal ExtensionInterest, decimal EarnedBeforePeriod, decimal EarnedThisPeriod, decimal TotalEarned, decimal Adjustment, decimal FinalEarned, int Count);
    private sealed record OverviewDto(SummaryDto Summary, TotalsDto Totals);
    private sealed record PeriodDto(string Label, decimal ContractAmount, decimal EarnedThisPeriod);
    private sealed record BreakdownDto(string LoanId, decimal OriginalContractInterest, decimal AdjustedContractInterest, decimal InterestAdjustment, List<PeriodDto> Periods);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Covers the Cash Transactions page's server-side paging/filtering/running
/// balance, and the manual-vs-automatic edit/delete rule from the Cash
/// Ledger redesign (owner_deposit/owner_withdrawal/expense/adjustment can be
/// added, edited, and deleted; loan_release/payment_received cannot).
/// </summary>
public class CashLedgerPageTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public CashLedgerPageTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddTransaction_Adjustment_RequiresIsCashIn()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.PostAsJsonAsync("/api/cash-funds/ledger", new
        {
            transactionType = "adjustment",
            amount = 123m,
            remarks = "test adjustment, no direction",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddTransaction_Adjustment_WithIsCashIn_AppearsInPageWithRunningBalance()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var marker = $"adj-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync("/api/cash-funds/ledger", new
        {
            transactionType = "adjustment",
            amount = 250m,
            remarks = marker,
            isCashIn = true,
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<CashLedgerEntryDto>();
        Assert.True(added!.IsCashIn);
        Assert.False(added.IsAutomatic);

        var page = await (await client.GetAsync($"/api/cash-funds/ledger/page?search={Uri.EscapeDataString(marker)}"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        var row = Assert.Single(page!.Items);
        Assert.Equal(added.LedgerId, row.LedgerId);
        Assert.NotNull(row.RunningBalance);
    }

    [Fact]
    public async Task EditTransaction_OnManualEntry_UpdatesItAndFooterTotals()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var marker = $"edit-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync("/api/cash-funds/ledger", new
        {
            transactionType = "expense",
            amount = 100m,
            remarks = marker,
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<CashLedgerEntryDto>();

        var editResponse = await client.PutAsJsonAsync($"/api/cash-funds/ledger/{added!.LedgerId}", new
        {
            transactionType = "expense",
            amount = 175m,
            remarks = marker,
            transactionDate = added.TransactionDate,
        });
        editResponse.EnsureSuccessStatusCode();
        var edited = await editResponse.Content.ReadFromJsonAsync<CashLedgerEntryDto>();
        Assert.Equal(175m, edited!.Amount);

        var totals = await (await client.GetAsync($"/api/cash-funds/ledger/page/totals?search={Uri.EscapeDataString(marker)}"))
            .Content.ReadFromJsonAsync<CashLedgerTotalsDto>();
        Assert.Equal(175m, totals!.CashOut);
        Assert.Equal(1, totals.Count);
    }

    [Fact]
    public async Task DeleteTransaction_OnManualEntry_RemovesItFromThePage()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var marker = $"del-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync("/api/cash-funds/ledger", new
        {
            transactionType = "owner_withdrawal",
            amount = 50m,
            remarks = marker,
        });
        addResponse.EnsureSuccessStatusCode();
        var added = await addResponse.Content.ReadFromJsonAsync<CashLedgerEntryDto>();

        var deleteResponse = await client.DeleteAsync($"/api/cash-funds/ledger/{added!.LedgerId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var page = await (await client.GetAsync($"/api/cash-funds/ledger/page?search={Uri.EscapeDataString(marker)}"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task EditAndDelete_OnAutomaticLoanReleaseEntry_AreRejected()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var createCustomerResponse = await client.PostAsJsonAsync(
            "/api/customers", new { fullName = "Cash Ledger Automatic Test Subject", address = "N/A", contactNumber = "+63 900 000 0002", borrowerType = "Test" });
        createCustomerResponse.EnsureSuccessStatusCode();
        var customer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var createLoanResponse = await client.PostAsJsonAsync("/api/loans", new { customerId = customer!.CustomerId, principal = 1000m });
        createLoanResponse.EnsureSuccessStatusCode();

        // The loan-origination event handler runs asynchronously to the
        // create-loan response (see CLAUDE.md's domain-event trade-off
        // note), but this test process is single-instance and MediatR's
        // default publisher awaits handlers before returning, so the
        // resulting loan_release row is already visible right after.
        var page = await (await client.GetAsync("/api/cash-funds/ledger/page?type=loan_release&pageSize=1"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        var automaticEntry = Assert.Single(page!.Items);
        Assert.True(automaticEntry.IsAutomatic);

        var editResponse = await client.PutAsJsonAsync($"/api/cash-funds/ledger/{automaticEntry.LedgerId}", new
        {
            transactionType = "expense",
            amount = 1m,
            remarks = "attempted edit",
            transactionDate = automaticEntry.TransactionDate,
        });
        Assert.Equal(HttpStatusCode.BadRequest, editResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/cash-funds/ledger/{automaticEntry.LedgerId}");
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
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
    private sealed record CashLedgerEntryDto(string LedgerId, string TransactionDate, string TransactionType, string? ReferenceId, decimal Amount, bool IsCashIn, bool IsAutomatic, decimal? RunningBalance, string Remarks, string CreatedAt);
    private sealed record PagedResultDto(List<CashLedgerEntryDto> Items, int TotalCount);
    private sealed record CashLedgerTotalsDto(decimal CashIn, decimal CashOut, decimal NetChange, int Count);
}

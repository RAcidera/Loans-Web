using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Regression test for a real bug: ILoanRepository.GetLoanCountsAndBalanceByCustomerAsync
/// originally computed Count() and Sum(l => l.Balance.Amount) in the same
/// GroupBy projection, which SQL Server's EF Core provider fails to
/// translate when combined with the OPENJSON-based Contains(customerIds)
/// filter — a runtime "could not be translated" 500, not a compile-time
/// error, and invisible to every other test in this suite because
/// TestApiFactory runs against Sqlite, which doesn't hit the same
/// translation path. This test can't reproduce the SQL-Server-specific
/// failure either, but it does guard the query's actual output shape
/// (loanCount/outstandingBalance correctness) against a future regression.
/// </summary>
public class CustomersPageTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public CustomersPageTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPage_AndGetPageTotals_ReflectTheCustomersLoanCountAndOutstandingBalance()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var createCustomerResponse = await client.PostAsJsonAsync(
            "/api/customers", new { fullName = "Customers Page Test Subject", address = "N/A", contactNumber = "+63 900 000 0001", borrowerType = "Test" });
        createCustomerResponse.EnsureSuccessStatusCode();
        var customer = await createCustomerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        const decimal principal = 1234m;
        var createLoanResponse = await client.PostAsJsonAsync("/api/loans", new { customerId = customer!.CustomerId, principal });
        createLoanResponse.EnsureSuccessStatusCode();
        var loan = await createLoanResponse.Content.ReadFromJsonAsync<LoanDto>();

        var page = await (await client.GetAsync($"/api/customers/page?search={Uri.EscapeDataString(customer.FullName)}&pageSize=50"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        var row = page!.Items.Single(c => c.CustomerId == customer.CustomerId);

        Assert.Equal(1, row.LoanCount);
        Assert.Equal(loan!.Balance, row.OutstandingBalance);

        var totals = await (await client.GetAsync($"/api/customers/page/totals?search={Uri.EscapeDataString(customer.FullName)}"))
            .Content.ReadFromJsonAsync<CustomerTotalsDto>();

        Assert.Equal(1, totals!.TotalCustomersCount);
        Assert.Equal(1, totals.TotalLoansCount);
        Assert.Equal(loan.Balance, totals.TotalOutstandingBalance);
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
    private sealed record LoanDto(string LoanId, decimal Balance);
    private sealed record CustomerListItemDto(string CustomerId, int LoanCount, decimal OutstandingBalance);
    private sealed record PagedResultDto(List<CustomerListItemDto> Items, int TotalCount);
    private sealed record CustomerTotalsDto(int TotalCustomersCount, int ActiveCustomersCount, int InactiveCustomersCount, int TotalLoansCount, decimal TotalOutstandingBalance);
}

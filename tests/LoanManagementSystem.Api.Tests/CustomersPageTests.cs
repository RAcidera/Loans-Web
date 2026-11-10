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

    [Fact]
    public async Task GetPage_ReportsOverdueLoanCountAndLastPaymentDate_AndSortsByThoseLoanFigures()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var prefix = $"Figures {Guid.NewGuid():N}"[..20];
        var today = DateOnly.FromDateTime(DateTime.Today);

        // "A": one long-past-due loan (start -120d → due -60d) paid once at -110d,
        // plus one current loan. "B": no loans at all.
        var a = await CreateCustomerAsync(client, $"{prefix} A");
        var overdueLoan = await CreateLoanAsync(client, a.CustomerId, 1000m, today.AddDays(-120));
        await CreateLoanAsync(client, a.CustomerId, 500m, today.AddDays(-5));
        var paymentDate = today.AddDays(-110);
        (await client.PostAsJsonAsync($"/api/loans/{overdueLoan.LoanId}/payments",
            new { amountPaid = 100m, paymentMethod = "cash", notes = "", paymentDate = paymentDate.ToString("yyyy-MM-dd") }))
            .EnsureSuccessStatusCode();
        var b = await CreateCustomerAsync(client, $"{prefix} B");

        async Task<List<CustomerListItemDto>> PageAsync(string sortBy, string sortDir) =>
            (await (await client.GetAsync($"/api/customers/page?search={Uri.EscapeDataString(prefix)}&pageSize=50&sortBy={sortBy}&sortDir={sortDir}"))
                .Content.ReadFromJsonAsync<PagedResultDto>())!.Items;

        var rowA = (await PageAsync("fullName", "asc")).Single(r => r.CustomerId == a.CustomerId);
        Assert.Equal(2, rowA.LoanCount);
        Assert.Equal(1, rowA.OverdueLoanCount);
        Assert.Equal(paymentDate.ToString("yyyy-MM-dd"), rowA.LastPaymentDate);
        var rowB = (await PageAsync("fullName", "asc")).Single(r => r.CustomerId == b.CustomerId);
        Assert.Equal(0, rowB.OverdueLoanCount);
        Assert.Null(rowB.LastPaymentDate);

        Assert.Equal(new[] { b.CustomerId, a.CustomerId }, (await PageAsync("loanCount", "asc")).Select(r => r.CustomerId));
        Assert.Equal(new[] { a.CustomerId, b.CustomerId }, (await PageAsync("loanCount", "desc")).Select(r => r.CustomerId));
        Assert.Equal(new[] { a.CustomerId, b.CustomerId }, (await PageAsync("overdueCount", "desc")).Select(r => r.CustomerId));
        Assert.Equal(new[] { a.CustomerId, b.CustomerId }, (await PageAsync("outstandingBalance", "desc")).Select(r => r.CustomerId));
        // Never-paid customers sort as the oldest.
        Assert.Equal(new[] { b.CustomerId, a.CustomerId }, (await PageAsync("lastPaymentDate", "asc")).Select(r => r.CustomerId));

        // Paging applies AFTER the loan-figure sort, and totalCount covers the whole filtered set.
        var firstPage = await (await client.GetAsync($"/api/customers/page?search={Uri.EscapeDataString(prefix)}&pageIndex=0&pageSize=1&sortBy=loanCount&sortDir=desc"))
            .Content.ReadFromJsonAsync<PagedResultDto>();
        Assert.Equal(2, firstPage!.TotalCount);
        Assert.Equal(a.CustomerId, firstPage.Items.Single().CustomerId);
    }

    // Money columns (loans: principalAmount, interest, extensionCharges,
    // totalPaid, balance; payments: amount) are left out: this suite runs on
    // Sqlite, which cannot ORDER BY a decimal column ("SQLite does not support
    // expressions of type 'decimal' in ORDER BY") — SQL Server, the production
    // database, can. Customers' outstandingBalance IS covered: it's sorted in
    // memory (see GetCustomersPageQueryHandler), not in SQL.
    [Theory]
    [InlineData("/api/customers/page", "fullName,code,contact,borrowerType,status,createdAt,loanCount,outstandingBalance,overdueCount,lastPaymentDate")]
    [InlineData("/api/loans/page", "loanNumber,customer,loanDate,dueDate,status,classification")]
    [InlineData("/api/payments/page", "loan,customer,date,createdAt,method,reference,notes")]
    public async Task EveryGridColumnSortKey_IsAccepted_InBothDirections(string endpoint, string sortKeys)
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        foreach (var key in sortKeys.Split(','))
        foreach (var dir in new[] { "asc", "desc" })
        {
            var response = await client.GetAsync($"{endpoint}?pageIndex=0&pageSize=15&sortBy={key}&sortDir={dir}");
            Assert.True(response.IsSuccessStatusCode, $"{endpoint} sortBy={key} {dir} -> {(int)response.StatusCode}");
        }
    }

    private static async Task<CustomerDto> CreateCustomerAsync(HttpClient client, string fullName)
    {
        var response = await client.PostAsJsonAsync("/api/customers", new { fullName, address = "N/A", contactNumber = "+63 900 000 0002", borrowerType = "Test" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDto>())!;
    }

    private static async Task<LoanDto> CreateLoanAsync(HttpClient client, string customerId, decimal principal, DateOnly startDate)
    {
        var response = await client.PostAsJsonAsync("/api/loans", new { customerId, principal, startDate = startDate.ToString("yyyy-MM-dd") });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoanDto>())!;
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
    private sealed record CustomerListItemDto(string CustomerId, int LoanCount, decimal OutstandingBalance, int OverdueLoanCount = 0, string? LastPaymentDate = null);
    private sealed record PagedResultDto(List<CustomerListItemDto> Items, int TotalCount);
    private sealed record CustomerTotalsDto(int TotalCustomersCount, int ActiveCustomersCount, int InactiveCustomersCount, int TotalLoansCount, decimal TotalOutstandingBalance);
}

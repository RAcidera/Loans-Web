using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Exercises the real HTTP pipeline end-to-end: real JWT issuance, real
/// [Authorize]/[Authorize(Roles=...)] enforcement, against a real (if
/// disposable) database. This is the closest thing in this test suite to
/// actual "security testing" — it proves the auth middleware rejects what
/// it should and accepts what it should, rather than just asserting that
/// C# attributes are present on methods.
/// </summary>
public class AuthenticationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthenticationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLoans_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededAdminCredentials_Returns200AndToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@12345" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("admin", body.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_Returns401_NotSomeOtherStatus()
    {
        // Verifies the "don't leak whether the username exists" property
        // holds all the way through the HTTP layer too, not just in the
        // unit-tested handler.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "no-such-user", password = "whatever" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLoans_WithValidToken_Returns200()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync("/api/loans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExtendLoan_AsStaff_Returns403()
    {
        var staffClient = await CreateAuthenticatedClientAsync("staff", "Staff@12345");

        // Any loan id works for this assertion — [Authorize(Roles="Admin")]
        // rejects the request before the handler ever looks up the loan.
        var response = await staffClient.PostAsJsonAsync(
            "/api/loans/00000000-0000-0000-0000-000000000000/extensions",
            new { extensionDays = 30, additionalChargesAmount = 50, remarks = "test" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCashTransaction_AsStaff_Returns403()
    {
        var staffClient = await CreateAuthenticatedClientAsync("staff", "Staff@12345");

        var response = await staffClient.PostAsJsonAsync(
            "/api/cash-funds/ledger",
            new { transactionType = "owner_deposit", amount = 1000, remarks = "test" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCashTransaction_AsAdmin_Returns200()
    {
        var adminClient = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await adminClient.PostAsJsonAsync(
            "/api/cash-funds/ledger",
            new { transactionType = "owner_deposit", amount = 1000, remarks = "integration test deposit" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_AsStaff_Returns200()
    {
        // Staff SHOULD be able to record payments (collectors do this in
        // the field, per SRS 1.3) even though they can't extend loans or
        // touch the cash ledger directly — this is the "allowed" half of
        // the role check, not just the "denied" half.
        var staffClient = await CreateAuthenticatedClientAsync("staff", "Staff@12345");

        var loansResponse = await staffClient.GetAsync("/api/loans");
        var loans = await loansResponse.Content.ReadFromJsonAsync<List<LoanSummaryDto>>();
        var anyUnpaidLoan = loans!.First(l => l.Status != "paid");

        var response = await staffClient.PostAsJsonAsync(
            $"/api/loans/{anyUnpaidLoan.LoanId}/payments",
            new { amountPaid = 1, paymentMethod = "cash", notes = "integration test payment" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    private sealed record LoanSummaryDto(string LoanId, string Status);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Proves GET /api/loans/{id}/soa actually renders a real PDF via QuestPDF
/// over real HTTP — the first place this codebase generates a PDF, so
/// there's no earlier precedent to lean on for "does QuestPDF's fluent API
/// actually produce valid output when wired into a real request."
/// </summary>
public class StatementOfAccountTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public StatementOfAccountTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSoa_OnLoanWithPaymentAndExtension_ReturnsValidPdf()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        // LoanNumber is always 0 under the Sqlite test provider (see AppDbContext's
        // comment on why IDENTITY is SQL-Server-only), so loans can't be told apart
        // by their display code here — find the seeded loan with both a payment and
        // an extension (DbSeeder's "Maria again, extended once") by its detail instead.
        var loans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        string? loanIdWithBoth = null;
        foreach (var candidate in loans!)
        {
            var detail = await (await client.GetAsync($"/api/loans/{candidate.LoanId}/detail")).Content.ReadFromJsonAsync<LoanDetailDto>();
            if (detail!.Extensions.Count > 0 && detail.Payments.Count > 0)
            {
                loanIdWithBoth = candidate.LoanId;
                break;
            }
        }
        Assert.NotNull(loanIdWithBoth);

        var response = await client.GetAsync($"/api/loans/{loanIdWithBoth}/soa");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100, "Expected a non-trivial PDF byte stream.");

        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GenerateSoa_UnknownLoanId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync($"/api/loans/{Guid.NewGuid()}/soa");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    private sealed record LoanDto(string LoanId, string LoanNumber);
    private sealed record LoanDetailDto(LoanDto? Loan, List<object> Extensions, List<object> Payments);
}

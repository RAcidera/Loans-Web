using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Proves GET /api/loans/{id}/soa-v2 renders a real PDF over real HTTP,
/// exactly like StatementOfAccountTests does for the original /soa
/// endpoint — the two coexist, so this asserts V2 works without asserting
/// anything about V1 (see that file, still passing unchanged).
/// </summary>
public class StatementOfAccountV2Tests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public StatementOfAccountV2Tests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GenerateSoaV2_OnLoanWithPaymentAndExtension_ReturnsValidPdf()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

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

        var response = await client.GetAsync($"/api/loans/{loanIdWithBoth}/soa-v2");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100, "Expected a non-trivial PDF byte stream.");

        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GenerateSoaV2_UnknownLoanId_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync($"/api/loans/{Guid.NewGuid()}/soa-v2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GenerateSoaV2_DoesNotAffectOriginalSoaEndpoint()
    {
        // Regression guard for the spec's "do not modify the original SOA"
        // requirement: both endpoints must succeed independently for the
        // same loan.
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var loans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var loanId = loans!.First().LoanId;

        var v1Response = await client.GetAsync($"/api/loans/{loanId}/soa");
        var v2Response = await client.GetAsync($"/api/loans/{loanId}/soa-v2");

        v1Response.EnsureSuccessStatusCode();
        v2Response.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", v1Response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("application/pdf", v2Response.Content.Headers.ContentType?.MediaType);
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

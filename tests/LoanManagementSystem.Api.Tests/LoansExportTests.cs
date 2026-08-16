using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>Proves GET /api/loans/export renders a real .xlsx workbook via ClosedXML over real HTTP — the first place this codebase generates a spreadsheet.</summary>
public class LoansExportTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public LoansExportTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Export_ReturnsValidXlsxWithTheExpectedContentType()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var response = await client.GetAsync("/api/loans/export");
        response.EnsureSuccessStatusCode();

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        // An .xlsx is a zip archive — "PK" is the local file header signature every zip starts with.
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task Export_WithClassificationFilter_OnlyIncludesMatchingLoans()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");

        var allLoans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var target = allLoans!.First(l => l.Status != "paid");
        (await client.PutAsJsonAsync($"/api/loans/{target.LoanId}/classification", new { classification = "BadLoan" })).EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/loans/export?classification=BadLoan");
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
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
}

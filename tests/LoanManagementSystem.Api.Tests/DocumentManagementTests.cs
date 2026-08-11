using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Proves the multipart upload / metadata list / byte-for-byte download /
/// delete round trip actually works over real HTTP against a real EF Core
/// SaveChangesAsync — this is the first place this codebase does file
/// upload, so unlike most other endpoints there's no earlier precedent to
/// lean on for "does IFormFile binding, the VARBINARY(MAX) mapping, and
/// the File() download result actually work together."
/// </summary>
public class DocumentManagementTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public DocumentManagementTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerDocument_UploadListDownloadDelete_RoundTripsByteForByte()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var customers = await (await client.GetAsync("/api/customers")).Content.ReadFromJsonAsync<List<CustomerDto>>();
        var customerId = customers!.First().CustomerId;

        var originalBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03 }; // fake JPEG-ish bytes
        var uploaded = await UploadAsync(client, $"/api/customers/{customerId}/documents", "test-id.jpg", "image/jpeg", originalBytes);
        uploaded.EnsureSuccessStatusCode();
        var uploadedDoc = await uploaded.Content.ReadFromJsonAsync<CustomerDocumentDto>();

        var list = await (await client.GetAsync($"/api/customers/{customerId}/documents")).Content.ReadFromJsonAsync<List<CustomerDocumentDto>>();
        Assert.Contains(list!, d => d.DocumentId == uploadedDoc!.DocumentId && d.OriginalFileName == "test-id.jpg" && d.FileSizeBytes == originalBytes.Length);

        var downloadResponse = await client.GetAsync($"/api/customers/{customerId}/documents/{uploadedDoc!.DocumentId}");
        downloadResponse.EnsureSuccessStatusCode();
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(originalBytes, downloadedBytes);
        Assert.Equal("image/jpeg", downloadResponse.Content.Headers.ContentType?.MediaType);

        var deleteResponse = await client.DeleteAsync($"/api/customers/{customerId}/documents/{uploadedDoc.DocumentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listAfterDelete = await (await client.GetAsync($"/api/customers/{customerId}/documents")).Content.ReadFromJsonAsync<List<CustomerDocumentDto>>();
        Assert.DoesNotContain(listAfterDelete!, d => d.DocumentId == uploadedDoc.DocumentId);
    }

    [Fact]
    public async Task LoanDocument_UploadListDownloadDelete_RoundTripsByteForByte()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var loans = await (await client.GetAsync("/api/loans")).Content.ReadFromJsonAsync<List<LoanDto>>();
        var loanId = loans!.First().LoanId;

        var originalBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x01, 0x02 }; // fake PDF-ish bytes
        var uploaded = await UploadAsync(client, $"/api/loans/{loanId}/documents", "agreement.pdf", "application/pdf", originalBytes);
        uploaded.EnsureSuccessStatusCode();
        var uploadedDoc = await uploaded.Content.ReadFromJsonAsync<LoanDocumentDto>();

        var downloadResponse = await client.GetAsync($"/api/loans/{loanId}/documents/{uploadedDoc!.DocumentId}");
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(originalBytes, downloadedBytes);

        var deleteResponse = await client.DeleteAsync($"/api/loans/{loanId}/documents/{uploadedDoc.DocumentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UploadDocument_DisallowedContentType_Returns400()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@12345");
        var customers = await (await client.GetAsync("/api/customers")).Content.ReadFromJsonAsync<List<CustomerDto>>();
        var customerId = customers!.First().CustomerId;

        var response = await UploadAsync(client, $"/api/customers/{customerId}/documents", "resume.docx", "application/msword", new byte[] { 1, 2, 3 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadDocument_AsStaff_Returns403()
    {
        var client = await CreateAuthenticatedClientAsync("staff", "Staff@12345");
        var customers = await (await client.GetAsync("/api/customers")).Content.ReadFromJsonAsync<List<CustomerDto>>();
        var customerId = customers!.First().CustomerId;

        var response = await UploadAsync(client, $"/api/customers/{customerId}/documents", "test-id.jpg", "image/jpeg", new byte[] { 1, 2, 3 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string url, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        return await client.PostAsync(url, content);
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
    private sealed record CustomerDto(string CustomerId);
    private sealed record LoanDto(string LoanId);
    private sealed record CustomerDocumentDto(string DocumentId, string CustomerId, string OriginalFileName, string ContentType, long FileSizeBytes, string UploadedAt, string UploadedBy);
    private sealed record LoanDocumentDto(string DocumentId, string LoanId, string OriginalFileName, string ContentType, long FileSizeBytes, string UploadedAt, string UploadedBy);
}

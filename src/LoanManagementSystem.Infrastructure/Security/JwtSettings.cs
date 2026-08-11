namespace LoanManagementSystem.Infrastructure.Security;

/// <summary>Bound from the "Jwt" section of appsettings.json — see JwtTokenGenerator and Program.cs's AddJwtBearer setup, both of which must agree on these same values.</summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

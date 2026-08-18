using System.Text;
using System.Text.Json.Serialization;
using LoanManagementSystem.Api.Middleware;
using LoanManagementSystem.Application;
using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Infrastructure;
using LoanManagementSystem.Infrastructure.Persistence;
using LoanManagementSystem.Infrastructure.Persistence.Seed;
using LoanManagementSystem.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // camelCase to match the Angular frontend's TypeScript entities exactly
    // (LoanDto.LoanId -> "loanId" on the wire), and enums serialized as
    // their string names where any DTO field is a raw enum (none currently
    // are — all enum-like fields go through MappingExtensions' wire-string
    // conversion — but this is a safe default regardless).
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets Swagger UI send a Bearer token with each request, so protected
    // endpoints can actually be exercised from /swagger during development.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

// --- Authentication / Authorization ---
// Reads the same "Jwt" config section JwtTokenGenerator (Infrastructure)
// uses to sign tokens — both sides must agree on Secret/Issuer/Audience or
// every token this API issues will fail its own validation.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30), // default is 5 minutes; tightened since this API and its clients typically run close together
        };
    });

builder.Services.AddAuthorization();

const string AngularCorsPolicy = "AngularDev";
// Defaults to ng serve's port when "Cors:AllowedOrigins" isn't configured
// (local dev); appsettings.Production.json overrides it with the deployed
// frontend's real origin(s).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularCorsPolicy, policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Seed the database on startup (idempotent — see DbSeeder) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Schema creation must happen in every environment — BackfillLoanLedgerAsync
    // below queries real tables unconditionally, so a fresh database needs
    // its schema before that runs, not just when demo seeding is also on.
    await DbSeeder.EnsureSchemaAsync(db);

    // Demo data (Customers/Loans/CashLedger/Users) is only useful for local
    // dev/demo purposes. In Production it's actively harmful: its "already
    // seeded" check only looks at Customers, so once real Customers exist
    // and then get cleared, it re-inserts a fresh demo set and crashes the
    // process trying to re-insert the already-existing admin/staff Users.
    if (app.Environment.IsDevelopment())
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DbSeeder.SeedAsync(db, passwordHasher);
    }

    // Backfill is safe in every environment — idempotent per loan, applies
    // to real loans too (see its own doc comment).
    await DbSeeder.BackfillLoanLedgerAsync(db);

    // Diary categories are reference data needed in every environment, not
    // just dev demos — see SeedDiaryCategoriesAsync's own doc comment for
    // why this isn't gated behind IsDevelopment() like SeedAsync above.
    await DbSeeder.SeedDiaryCategoriesAsync(db);
}

// --- Middleware pipeline ---
// Order matters: exception handling wraps everything; CORS must run before
// auth (a rejected-by-CORS preflight shouldn't even reach auth); auth
// before authorization (need to know *who* before deciding *can they*).

app.UseAppExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(AngularCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed for LoanManagementSystem.Api.Tests' WebApplicationFactory<Program>.
public partial class Program { }

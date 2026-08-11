using LoanManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoanManagementSystem.Api.Tests;

/// <summary>
/// Boots the real ASP.NET Core pipeline (Program.cs, all middleware, real
/// controllers, real MediatR handlers, real domain events) against a
/// disposable Sqlite database instead of the SQL Server one Program.cs is
/// configured for by default — the actual HTTP/auth/authorization layer
/// under test is 100% real; only the database engine differs.
///
/// The connection is kept open for the lifetime of the factory: Sqlite's
/// `:memory:` database only exists as long as at least one connection to
/// it is open, so closing the connection early would silently wipe the
/// schema and data out from under the running app.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TestApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

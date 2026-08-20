using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Common;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Promises;
using LoanManagementSystem.Domain.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanManagementSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IMediator _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();
    public DbSet<LoanLedgerEntry> LoanLedgerEntries => Set<LoanLedgerEntry>();
    public DbSet<LoanAuditLogEntry> LoanAuditLogEntries => Set<LoanAuditLogEntry>();
    public DbSet<User> Users => Set<User>();
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<DiaryCategory> DiaryCategories => Set<DiaryCategory>();
    public DbSet<DiaryAuditLogEntry> DiaryAuditLogEntries => Set<DiaryAuditLogEntry>();
    public DbSet<PromiseToPay> PromisesToPay => Set<PromiseToPay>();
    public DbSet<PromiseAuditLogEntry> PromiseAuditLogEntries => Set<PromiseAuditLogEntry>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>
    /// SQL Server's datetime2 columns (every DateTime property in this
    /// model — all of them audit timestamps written as DateTime.UtcNow, see
    /// each aggregate's *AtUtc naming convention) carry no offset, so EF
    /// materializes them with Kind = Unspecified by default. That silently
    /// broke every "O"-format ISO string this API returns (PaymentDto.CreatedAt,
    /// LoanAuditLogEntryDto.OccurredAt, etc.): Unspecified serializes to
    /// "O" without a trailing "Z", and both the browser's Date constructor
    /// and Angular's DatePipe then parse that string as LOCAL time, not
    /// UTC — an 8-hour display error for a UTC+8 business timezone. Tagging
    /// every DateTime as Kind=Utc on the way out of the database (this is
    /// always correct, since nothing in this codebase ever writes a
    /// non-UTC DateTime) fixes that at the source, once, instead of
    /// per-property.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Loan.LoanNumber (the human-friendly "LM-001" display code) is a
        // SQL Server IDENTITY column + a unique index in real deployments.
        // Sqlite — used only by the API integration tests, see DbSeeder —
        // has no IDENTITY equivalent for a column that isn't the table's
        // own rowid/primary key, so under Sqlite this is left as an
        // ordinary int (always 0 there), which no test relies on.
        var isSqlite = Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (!isSqlite)
        {
            modelBuilder.Entity<Loan>(loan =>
            {
                loan.Property(l => l.LoanNumber).ValueGeneratedOnAdd().UseIdentityColumn();
                loan.HasIndex(l => l.LoanNumber).IsUnique();
            });

            // Customer.CustomerNumber (the "CUS00001" display code) — same
            // IDENTITY + unique-index treatment as Loan.LoanNumber above.
            modelBuilder.Entity<Customer>(customer =>
            {
                customer.Property(c => c.CustomerNumber).ValueGeneratedOnAdd().UseIdentityColumn();
                customer.HasIndex(c => c.CustomerNumber).IsUnique();
            });
        }
    }

    /// <summary>
    /// Collects domain events raised by tracked aggregates BEFORE calling
    /// the base SaveChanges, then publishes them AFTER it succeeds. This
    /// ordering matters: an event handler (e.g. PaymentRecordedEventHandler)
    /// must never fire for a change that didn't actually commit, and the
    /// aggregate's own events must be cleared so the same event isn't
    /// re-published if SaveChanges is called again later in the same
    /// request. Each handler calls SaveChangesAsync again for its own
    /// change (e.g. adding a CashLedgerEntry) — this makes each domain
    /// event's effect its own database round trip/transaction rather than
    /// one giant transaction, which is the simpler and more common trade-off
    /// for a system this size. A stricter approach (single transaction
    /// wrapping the whole chain) would use a database-level transaction
    /// started before the initial SaveChanges and committed after all
    /// events finish handling.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker.Entries()
            .Select(e => e.Entity)
            .OfType<IHasDomainEvents>()
            .Where(a => a.DomainEvents.Any())
            .ToList();

        var domainEvents = aggregatesWithEvents.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregatesWithEvents)
            aggregate.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);

        return result;
    }
}

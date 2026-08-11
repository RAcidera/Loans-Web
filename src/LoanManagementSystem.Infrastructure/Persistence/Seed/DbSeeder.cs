using LoanManagementSystem.Application.Common.Security;
using LoanManagementSystem.Domain.CashLedger;
using LoanManagementSystem.Domain.Customers;
using LoanManagementSystem.Domain.Identity;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace LoanManagementSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the same illustrative data the Angular MockLoanRepository /
/// MockCashLedgerRepository used, so the first run of the real backend
/// looks identical to what the frontend showed before it had one. Runs
/// once at startup (see Program.cs) only if the customers table is empty —
/// safe to leave in place after real data exists.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        // The generated migrations are produced against the relational
        // provider (SQL Server), which bakes provider-specific relational
        // annotations (e.g. `SqlServer:Identity` on Loan.LoanNumber) into
        // the migration operations. Sqlite's migrations SQL generator can't
        // translate those annotations, so replaying those migrations against
        // the API integration tests' Sqlite database throws. Sqlite is only
        // ever used there (never in real deployments), so it always builds
        // its schema straight from the current model via EnsureCreated()
        // instead of replaying the provider-flavored migrations.
        var isSqlite = db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        if (!isSqlite && db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        if (await db.Customers.AnyAsync())
            return; // already seeded

        var maria = Customer.Create("Maria Santos", "Blk 4 Lot 2, Riverside", "+63 917 555 0142", "Fish vendor", "Ate Maria", "Long-time customer, always pays early.");
        var jun = Customer.Create("Jun Dela Cruz", "Purok 3, San Isidro", "+63 918 222 0987", "Vegetable vendor", "Kuya Jun");
        var liza = Customer.Create("Liza Ramos", "12 Mabini St.", "+63 920 111 4432", "Vegetable vendor");
        var ronnie = Customer.Create("Ronnie Bautista", "Sitio Bagong Buhay", "+63 917 888 1234", "Fish vendor");
        var ana = Customer.Create("Ana Villanueva", "45 Rizal Ave.", "+63 919 333 5566", "Community member");

        db.Customers.AddRange(maria, jun, liza, ronnie, ana);

        var rate = InterestRate.Default; // 3%, per SRS default

        // L-2034 equivalent: Maria, partially paid, still active
        var loan1 = Loan.Originate(maria.Id, Money.Of(5000), rate, new DateOnly(2026, 6, 10), 60);
        loan1.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 7, 10));
        loan1.RecordPayment(Money.Of(1000), PaymentMethod.GCash, "", new DateOnly(2026, 7, 25));

        // L-2035 equivalent: Jun, fully settled
        var loan2 = Loan.Originate(jun.Id, Money.Of(3000), rate, new DateOnly(2026, 5, 1), 60);
        loan2.RecordPayment(Money.Of(1545), PaymentMethod.Cash, "", new DateOnly(2026, 7, 1));
        loan2.RecordPayment(Money.Of(1545), PaymentMethod.Cash, "Full settlement", new DateOnly(2026, 7, 14));

        // L-2036 equivalent: Liza, fresh loan, no payments yet
        var loan3 = Loan.Originate(liza.Id, Money.Of(2000), rate, new DateOnly(2026, 6, 20), 60);

        // L-2037 equivalent: Ronnie, one payment, will read as overdue once RefreshOverdueStatus runs
        var loan4 = Loan.Originate(ronnie.Id, Money.Of(4000), rate, new DateOnly(2026, 5, 20), 60);
        loan4.RecordPayment(Money.Of(1000), PaymentMethod.Cash, "", new DateOnly(2026, 6, 5));

        // L-2038 equivalent: Ana, one payment
        var loan5 = Loan.Originate(ana.Id, Money.Of(1000), rate, new DateOnly(2026, 6, 25), 60);
        loan5.RecordPayment(Money.Of(400), PaymentMethod.GCash, "", new DateOnly(2026, 7, 20));

        // L-2039 equivalent: Maria again, extended once
        var loan6 = Loan.Originate(maria.Id, Money.Of(3500), rate, new DateOnly(2026, 4, 15), 75); // due 2026-06-29 before extension
        loan6.RecordPayment(Money.Of(500), PaymentMethod.Cash, "Partial payment before extension", new DateOnly(2026, 5, 30));
        loan6.Extend(30, Money.Of(105), Money.Zero, "Customer requested extra month, business slow this week", new DateOnly(2026, 6, 29));

        db.Loans.AddRange(loan1, loan2, loan3, loan4, loan5, loan6);

        // SaveChanges here publishes each loan's LoanCreatedDomainEvent and
        // PaymentRecordedDomainEvent through the normal event-handler path,
        // so the loan_release / payment_received ledger entries above are
        // created automatically — no need to also hand-seed them.
        await db.SaveChangesAsync();

        // A couple of manual entries an owner would have entered directly,
        // which the domain events above don't cover.
        var initialCapital = CashLedgerEntry.Record(CashTransactionType.OwnerDeposit, Money.Of(20000), "Initial capital", new DateOnly(2026, 6, 1));
        var expense = CashLedgerEntry.Record(CashTransactionType.Expense, Money.Of(300), "Ledger book, transport", new DateOnly(2026, 7, 5));
        var withdrawal = CashLedgerEntry.Record(CashTransactionType.OwnerWithdrawal, Money.Of(2000), "Owner draw", new DateOnly(2026, 7, 30));

        db.CashLedgerEntries.AddRange(initialCapital, expense, withdrawal);
        await db.SaveChangesAsync();

        // Demo credentials only — change these before deploying anywhere
        // reachable. See SRS 4 ("Security: User authentication and basic
        // access control") and README.md's Setup section.
        var admin = User.Create("admin", passwordHasher.Hash("Admin@12345"), UserRole.Admin);
        var staff = User.Create("staff", passwordHasher.Hash("Staff@12345"), UserRole.Staff);
        db.Users.AddRange(admin, staff);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Phase 6's "Additional Recommendation: Loan Ledger" backfill — loans
    /// created before LoanLedgerEntry existed (or seeded above, whose
    /// SaveChangesAsync already populated the ledger via the normal
    /// event-driven path) would otherwise show an empty ledger/History.
    /// Idempotent per loan (skips any loan that already has at least one
    /// entry), so it's safe to call on every startup rather than gating it
    /// behind a one-time migration flag. Reconstructs the ledger from each
    /// loan's current Payments/Extensions collections and origination
    /// fields — a best-effort replay, not a perfect audit trail, since a
    /// loan edited after the fact (EditLoan/EditPayment/EditExtension)
    /// won't have its intermediate corrections reflected here, only its
    /// current state.
    /// </summary>
    public static async Task BackfillLoanLedgerAsync(AppDbContext db)
    {
        var loans = await db.Loans.AsNoTracking().Include(l => l.Extensions).Include(l => l.Payments).ToListAsync();
        if (loans.Count == 0) return;

        var loanIdsWithLedger = await db.LoanLedgerEntries.AsNoTracking().Select(e => e.LoanId).Distinct().ToListAsync();
        var loanIdsWithLedgerSet = loanIdsWithLedger.ToHashSet();

        foreach (var loan in loans)
        {
            if (loanIdsWithLedgerSet.Contains(loan.Id)) continue;

            var runningBalance = loan.PrincipalAmount;
            db.LoanLedgerEntries.Add(LoanLedgerEntry.Record(
                loan.Id, LoanLedgerTransactionType.LoanReleased, loan.PrincipalAmount, Money.Zero, runningBalance,
                "Loan released", loan.StartDate));

            var originationInterest = loan.TotalInterest.Subtract(loan.Extensions.Aggregate(Money.Zero, (sum, e) => sum.Add(e.AdditionalInterestAmount)));
            runningBalance = runningBalance.Add(originationInterest);
            db.LoanLedgerEntries.Add(LoanLedgerEntry.Record(
                loan.Id, LoanLedgerTransactionType.InterestAdded, originationInterest, Money.Zero, runningBalance,
                "Interest added", loan.StartDate));

            var movements = loan.Extensions
                .Select(e => (Date: e.ExtensionDate, IsPayment: false, Amount: e.AdditionalInterestAmount.Add(e.AdditionalChargesAmount), ReferenceId: e.Id.ToString(), Remarks: $"Extension +{e.ExtensionDays} days"))
                .Concat(loan.Payments.Select(p => (Date: p.PaymentDate, IsPayment: true, Amount: p.AmountPaid, ReferenceId: p.Id.ToString(), Remarks: "Payment received")))
                .OrderBy(m => m.Date);

            foreach (var movement in movements)
            {
                runningBalance = movement.IsPayment ? runningBalance.Subtract(movement.Amount) : runningBalance.Add(movement.Amount);
                db.LoanLedgerEntries.Add(LoanLedgerEntry.Record(
                    loan.Id,
                    movement.IsPayment ? LoanLedgerTransactionType.Payment : LoanLedgerTransactionType.Extension,
                    debit: movement.IsPayment ? Money.Zero : movement.Amount,
                    credit: movement.IsPayment ? movement.Amount : Money.Zero,
                    runningBalance: runningBalance,
                    remarks: movement.Remarks,
                    transactionDate: movement.Date,
                    referenceId: movement.ReferenceId));
            }
        }

        await db.SaveChangesAsync();
    }
}

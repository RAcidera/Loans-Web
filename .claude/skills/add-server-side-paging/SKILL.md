---
name: add-server-side-paging
description: Convert the admin dashboard's Angular Material data tables (Customers, Loans) from client-side paging — load the entire table over HTTP once, then slice it in-browser with MatTableDataSource/MatPaginator — to real server-side paging, where the backend returns one page at a time plus a total count. Use when the user asks to "add server-side pagination", "make the table list server-side paging", "paginate on the backend", or references the tables loading too much data at once.
---

# Add server-side paging to the table lists

## Re-verify before starting

This skill was written from a snapshot of the repo. Before trusting any fact
below, re-grep for `Skip(`, `Take(`, `Paged`, `PagedResult` across
`src/LoanManagementSystem.Application`/`.Api`/`.Infrastructure`, and re-check
`customers.component.ts`/`loans.component.ts` for `MatPaginator`/`MatSort`
wiring — someone may have already started or finished this since the skill
was written.

## Current state (as surveyed — facts, not opinions)

**Nothing on the backend paginates today.** Grepped `Page|Paged|Skip\(|Take\(|TotalCount`
across all four backend projects — the only hit is
`LoanRepository.GetRecentPaymentsAsync` (`src/LoanManagementSystem.Infrastructure/Repositories/LoanRepository.cs`),
which does `.Take(limit)` for the dashboard's "recent payments" feed. No
`Skip`, no total count, no `PagedResult<T>`, no paging query base class
anywhere. Every list query is a bare `IRequest<List<Dto>>` with **zero**
parameters:

| Query | Handler → repository call | Controller route |
|---|---|---|
| `GetCustomersQuery` (`Application/Customers/Queries/GetCustomers/GetCustomersQuery.cs`) | `ICustomerRepository.GetAllAsync(ct)` → `_db.Customers.AsNoTracking().OrderBy(FullName).ToListAsync()` | `GET /api/customers` (`CustomersController.GetAll`, no query params) |
| `GetLoansQuery` (`Application/Loans/Queries/GetLoans/GetLoansQuery.cs`) | `ILoanRepository.GetAllAsync(ct)` + `ICustomerRepository.GetAllAsync(ct)` (builds an in-memory customer dict to fill `LoanDto.CustomerName`) | `GET /api/loans` (`LoansController.GetAll`, no query params) |

Both handlers load the **entire** table into memory before mapping to DTOs.
`ICustomerRepository`/`ILoanRepository` (`Domain/Repositories/`) and their EF
implementations (`Infrastructure/Repositories/`) have no `Skip`/`Take`
overload to add paging to even if the query layer wanted it.

**On the frontend, only Customers and Loans have real paginator wiring** —
and it's 100% client-side:

- `customers.component.ts` — `ngOnInit` → `load()` does
  `forkJoin({ customers: getCustomers.execute(), loans: getLoans.execute() })`,
  fetching **both entire tables** just to compute `loanCount` per customer in
  memory (`loans.filter(l => l.customerId === customer.customerId).length`).
  `ngAfterViewInit` wires `dataSource.sort = this.sort` and
  `dataSource.paginator = this.paginator` — `MatTableDataSource` slices the
  already-fully-loaded array in the browser. `applyFilter(value)` sets
  `dataSource.filter` (also in-memory).
- `loans.component.ts` — same shape: `load()` calls `getLoans.execute()`
  once for the full list, `ngAfterViewInit` wires `dataSource.sort`/
  `dataSource.paginator` client-side, `applyFilter` sets `dataSource.filter`.
- `HttpLoanRepository.getCustomers()`/`.getLoans()` (`infrastructure/repositories/http-loan.repository.ts`)
  are plain `http.get<T[]>(url)` calls with no query params at all. The one
  method in this whole port that *does* pass query params is
  `getRecentPayments(limit)` (`{ params: { limit } }`) — that's the idiom to
  copy, not a full-array `http.get`.
- `LoanRepository` (`domain/repositories/loan.repository.ts`) is the
  abstract port; `MockLoanRepository` (`infrastructure/repositories/mock-loan.repository.ts`)
  is its in-memory test double, wired but **unregistered** in `app.config.ts`
  today (only `Http*` implementations are bound) — still needs updating to
  stay interface-conformant.

**Dashboard, Reports, Cash-Funds, and Settings (Users) are out of scope by
default** — don't touch them without asking first:

- `dashboard.component.ts`'s loans table and `reports.component.ts` both
  call the same unpaged `getLoans.execute()` — Dashboard for a 5-row KPI
  widget, Reports for **client-side date-range filtering over the full
  loan list** (`applyRange()` filters `allLoans` in memory). Changing what
  `getLoans()` returns would break both.
- `cash-funds.component.ts` and `settings.component.ts` (Users) bind
  `<table mat-table [dataSource]="rawArray">` directly — **no**
  `MatTableDataSource`, `MatPaginator`, or `MatSort` at all today. They'd
  need those primitives added from scratch, not just "converted."

## Scope for this skill: Customers and Loans list pages only

Default to converting exactly the two pages that already have full
paginator/sort/filter UI over a potentially-large table: `/customers` and
`/loans`. If the user wants Dashboard, Reports, Cash-Funds, or Settings
included too, confirm with them first — say so explicitly rather than
silently expanding scope, since those either have deliberately-small/bounded
data (Dashboard) or entirely different client-side logic built on the full
array (Reports' date-range filter) that would need its own redesign.

**Don't change the existing unpaged `getCustomers()`/`getLoans()` methods.**
They're still needed as-is elsewhere: `getCustomers()` powers the customer
`mat-select` in `add-loan-dialog`, and `getLoans()` powers Dashboard and
Reports. Add new, separate paged methods instead of overloading or breaking
these — same pattern the codebase already uses for `getRecentPayments(limit)`
existing alongside plain `getLoans()`.

## Backend changes

### 1. Shared `PagedResult<T>`

New file, `Application/Common/Models/PagedResult.cs` (new `Models` folder,
sibling to the existing `Common/DTOs`/`Common/Mappings`):

```csharp
namespace LoanManagementSystem.Application.Common.Models;

public sealed record PagedResult<T>(List<T> Items, int TotalCount);
```

`PageIndex`/`PageSize` don't need to round-trip back — the client already
knows what it asked for; `TotalCount` is the only new information it needs
(to size `MatPaginator`'s `length` input and compute page count).

### 2. Customers — new DTO, repository method, query, route

Add `LoanCount` to what the Customers list needs. Don't touch the existing
`CustomerDto` (used elsewhere as-is) — add a sibling record in
`Common/DTOs/CustomerListItemDto.cs`:

```csharp
public sealed record CustomerListItemDto(
    string CustomerId, string FullName, string Address, string ContactNumber,
    string BorrowerType, string Status, string CreatedAt, int LoanCount);
```

`ICustomerRepository` (`Domain/Repositories/ICustomerRepository.cs`) — add:

```csharp
Task<(List<Customer> Items, int TotalCount)> GetPageAsync(
    int pageIndex, int pageSize, string? search, CancellationToken ct = default);
```

`CustomerRepository` (`Infrastructure/Repositories/CustomerRepository.cs`)
implementation — filter, count, then page, in that order (count must run
against the *filtered* query, not the unfiltered table):

```csharp
public async Task<(List<Customer>, int)> GetPageAsync(int pageIndex, int pageSize, string? search, CancellationToken ct = default)
{
    var query = _db.Customers.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(c => c.FullName.Contains(search) || c.ContactNumber.Contains(search));

    var totalCount = await query.CountAsync(ct);
    var items = await query.OrderBy(c => c.FullName)
        .Skip(pageIndex * pageSize).Take(pageSize)
        .ToListAsync(ct);
    return (items, totalCount);
}
```

**Gotcha — don't reproduce `GetLoansQueryHandler`'s "load everything to
build a lookup dict" pattern for `LoanCount`.** That pattern is fine when
the handler already loads the full table (as `GetLoansQuery` does today),
but it would defeat the entire point of paging here — rendering page 1
would still require a full table scan of Loans. Instead, once you have the
page's `Items`, query loan counts scoped to just those customer IDs:

```csharp
var customerIds = items.Select(c => c.Id).ToList();
var loanCounts = await _db.Loans.AsNoTracking()
    .Where(l => customerIds.Contains(l.CustomerId))
    .GroupBy(l => l.CustomerId)
    .Select(g => new { g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
```

(This needs `AppDbContext` access to `Loans` from `CustomerRepository`,
which already happens — `AppDbContext` exposes all `DbSet`s regardless of
which repository class is using it; the Domain-level aggregate-boundary
separation [[ef_migration_enum_default_gotcha]]-style conventions in
`CLAUDE.md` are about `Loan`/`CashLedgerEntry` being separate *aggregates*
with separate *repository interfaces*, not about `AppDbContext` itself
being partitioned — a repository reading a second `DbSet` for a read-only
count is fine and already how `GetLoansQueryHandler` cross-references
customers today, just done here as a scoped query instead of a full load.)

New query, `Application/Customers/Queries/GetCustomersPage/GetCustomersPageQuery.cs`:

```csharp
public sealed record GetCustomersPageQuery(int PageIndex, int PageSize, string? Search)
    : IRequest<PagedResult<CustomerListItemDto>>;

public sealed class GetCustomersPageQueryHandler
    : IRequestHandler<GetCustomersPageQuery, PagedResult<CustomerListItemDto>>
{
    // GetPageAsync returns (customers, totalCount); combine with the
    // loanCounts dictionary above to build CustomerListItemDto per row.
}
```

New route on `CustomersController.cs`, alongside the existing `GetAll`:

```csharp
/// <summary>GET /api/customers/page?pageIndex=&pageSize=&search= — server-side paging for the Customers list table.</summary>
[HttpGet("page")]
public async Task<ActionResult<PagedResult<CustomerListItemDto>>> GetPage(
    [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
    CancellationToken ct = default) =>
    Ok(await _mediator.Send(new GetCustomersPageQuery(pageIndex, pageSize, search), ct));
```

`[HttpGet("page")]` as a literal segment doesn't collide with the existing
`[HttpGet("{id}")]` — ASP.NET Core routing prefers the more specific literal
match — but confirm with a real request once built rather than assuming.

### 3. Loans — repository method, query, route

`ILoanRepository` — add:

```csharp
Task<(List<Loan> Items, int TotalCount)> GetPageAsync(
    int pageIndex, int pageSize, string? search, string? sortBy, string? sortDir, CancellationToken ct = default);
```

**Gotcha — `Loan` and `Customer` are separate aggregates with no EF
navigation property between them** (per `CLAUDE.md`'s domain-event section —
this is deliberate, not an oversight). `GetLoansQueryHandler` today works
around this by loading *all* customers into a dictionary; for a paged query
that's not acceptable. If `search` should match on customer name (not just
loan number), resolve matching customer IDs first via
`ICustomerRepository`, then filter loans by `CustomerId` in that ID list —
two queries, but each stays within one aggregate's repository, no raw SQL
join needed:

```csharp
var matchingCustomerIds = string.IsNullOrWhiteSpace(search)
    ? null
    : (await _customerRepository.GetAllAsync(ct))  // or a new lightweight "search customer ids" method if this is too heavy
        .Where(c => c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
        .Select(c => c.Id).ToList();
```

then in `LoanRepository.GetPageAsync`, `Where(l => l.LoanNumber ... || (matchingCustomerIds != null && matchingCustomerIds.Contains(l.CustomerId)))`.
If this two-query approach feels too heavy for a search-as-you-type box,
scope v1's `search` to loan-native fields only (loan number, status) and
flag customer-name search as a follow-up — confirm with the user rather
than silently dropping the feature.

For `sortBy`, restrict to loan-native columns in v1 (`principalAmount`,
`dueDate`, `balance`, `status`, `createdAt`) — these translate to a plain
EF `OrderBy`/`OrderByDescending` with no join. Sorting by `customerName`
would need the same cross-aggregate join problem as search; treat it the
same way (out of v1 scope unless asked).

New query, `Application/Loans/Queries/GetLoansPage/GetLoansPageQuery.cs`,
mirroring `GetLoansQuery`'s existing customer-dict-building for
`CustomerName` (fine here since it's dict-building for one *page's* worth
of loans, not the whole table):

```csharp
public sealed record GetLoansPageQuery(int PageIndex, int PageSize, string? Search, string? SortBy, string? SortDir)
    : IRequest<PagedResult<LoanDto>>;
```

New route on `LoansController.cs`:

```csharp
[HttpGet("page")]
public async Task<ActionResult<PagedResult<LoanDto>>> GetPage(
    [FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10, [FromQuery] string? search = null,
    [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, CancellationToken ct = default) =>
    Ok(await _mediator.Send(new GetLoansPageQuery(pageIndex, pageSize, search, sortBy, sortDir), ct));
```

No EF migration needed for any of this — it's read-path query/repository
code only, no schema change.

## Frontend changes

### 1. Shared `PagedResult<T>` type

New file, `domain/entities/paged-result.entity.ts`:

```ts
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}
```

### 2. Port additions — new methods, existing ones untouched

`LoanRepository` (`domain/repositories/loan.repository.ts`) — add two new
abstract methods (don't touch `getCustomers()`/`getLoans()`):

```ts
abstract getCustomersPage(pageIndex: number, pageSize: number, search: string): Observable<PagedResult<Customer & { loanCount: number }>>;
abstract getLoansPage(pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc'): Observable<PagedResult<Loan>>;
```

(`customers.component.ts` already has a local `CustomerRow extends Customer { loanCount: number }`
interface for this exact shape — reuse/promote that rather than inventing a
new type name.)

`HttpLoanRepository` (`infrastructure/repositories/http-loan.repository.ts`)
— implement by passing query params, same idiom as the existing
`getRecentPayments(limit)` (`{ params: { limit } }`):

```ts
getCustomersPage(pageIndex: number, pageSize: number, search: string) {
  return this.http.get<PagedResult<Customer & { loanCount: number }>>(`${this.baseUrl}/customers/page`, {
    params: { pageIndex, pageSize, search },
  });
}

getLoansPage(pageIndex: number, pageSize: number, search: string, sortBy?: string, sortDir?: 'asc' | 'desc') {
  const params: Record<string, string | number> = { pageIndex, pageSize, search };
  if (sortBy) params['sortBy'] = sortBy;
  if (sortDir) params['sortDir'] = sortDir;
  return this.http.get<PagedResult<Loan>>(`${this.baseUrl}/loans/page`, { params });
}
```

`MockLoanRepository` (`infrastructure/repositories/mock-loan.repository.ts`)
— implement the in-memory equivalent (`.filter` for search, `.slice` for
the page window), wrapped in `of(...).pipe(delay(150))` matching every
other method in that file. It's currently unregistered in `app.config.ts`
but must still compile against the updated abstract class.

### 3. New use cases

One file per operation, per this repo's convention
(`application/use-cases/`), ~10 lines each:

- `get-customers-page.use-case.ts` — forwards to `loanRepository.getCustomersPage(...)`
- `get-loans-page.use-case.ts` — forwards to `loanRepository.getLoansPage(...)`

### 4. `customers.component.ts` — convert to server-driven paging

Remove: the `forkJoin` full-load in `load()`, the `MatSort`/`MatPaginator`
`ViewChild` + `ngAfterViewInit` wiring (`dataSource.sort = this.sort` /
`dataSource.paginator = this.paginator`), and `applyFilter`'s direct
`dataSource.filter =` assignment.

Add: `pageIndex = 0`, `pageSize = 10`, `totalCount = 0`, a `searchTerm = ''`,
and a debounced search subject:

```ts
private readonly search$ = new Subject<string>();

ngOnInit(): void {
  this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
    this.searchTerm = term;
    this.pageIndex = 0;
    this.load();
  });
  this.load();
}

private load(): void {
  this.getCustomersPage.execute(this.pageIndex, this.pageSize, this.searchTerm).subscribe((result) => {
    this.dataSource.data = result.items;
    this.totalCount = result.totalCount;
  });
}

onPage(event: PageEvent): void {
  this.pageIndex = event.pageIndex;
  this.pageSize = event.pageSize;
  this.load();
}

applyFilter(value: string): void {
  this.search$.next(value.trim().toLowerCase());
}
```

**Gotcha — don't call `load()` on every keystroke without the debounce.**
Today's `dataSource.filter =` is instant and free (in-memory); a server
round trip per keystroke would hammer the API. The `Subject` +
`debounceTime`/`distinctUntilChanged` pattern above is standard RxJS, not
over-engineering — there's no existing debounce precedent in this codebase
to copy, so this is new but idiomatic Angular.

Template (`customers.component.html`): add `[length]="totalCount"
[pageIndex]="pageIndex" [pageSize]="pageSize" (page)="onPage($event)"` to
the existing `<mat-paginator>`; `[length]` is required in server-driven
mode (`MatPaginator` needs the true total to render page count / disable
"next" correctly — it can't infer it from a page-sized array anymore).
`matSort`/`mat-sort-header` can stay on the template for arrow UI, but see
the `loans.component.ts` note below on `(matSortChange)` — same applies
here if sortable columns other than `name` are kept (loanCount sort needs
the backend gotcha above solved first, or the column dropped from sortable).

### 5. `loans.component.ts` — same conversion, plus sort wiring

Same shape as Customers: remove full-load + client paginator/sort wiring,
add `pageIndex`/`pageSize`/`totalCount`/debounced `search$`, `onPage()`.

**Gotcha — decoupling `MatSort` from `dataSource.sort` breaks automatic
sorting.** Today, `dataSource.sort = this.sort` makes `MatTableDataSource`
auto-sort in-memory whenever the user clicks a `mat-sort-header`. Once
paging moves server-side, `dataSource.sort` must **not** be set (the array
in `dataSource.data` is only the current page — sorting it client-side
would just reorder those 10 rows, not the whole table). Instead, listen to
`(matSortChange)` directly and re-fetch:

```ts
onSort(sort: Sort): void {
  this.pageIndex = 0;
  this.load(); // load() reads this.sort.active / this.sort.direction for sortBy/sortDir
}
```

Template: add `(matSortChange)="onSort($event)"` to the `<table
matSort>` element, and `[length]`/`[pageIndex]`/`[pageSize]`/`(page)` to
`<mat-paginator>`, same as Customers.

## Verification

- `dotnet build` / `dotnet test` from repo root — no migration needed, this
  is read-path code only; `Api.Tests`' in-memory Sqlite DB should exercise
  the new endpoints fine.
- `ng build` clean.
- Real browser check (this project's standing rule for UI changes) on both
  `/customers` and `/loans`:
  - Open browser dev tools' Network tab and confirm each page navigation
    fetches only `pageSize` rows, not the whole table — this is the actual
    acceptance bar for "server-side," not just that a `page` param exists.
  - Page forward/back, change page size (options `[10, 25, 50]`) — confirm
    it resets to a valid page and refetches.
  - Type in the search box — confirm requests are debounced (not one per
    keystroke) and the page resets to 0 on a new search term.
  - Click a sortable column header on Loans — confirm rows reorder across
    the *whole* dataset (check by comparing against a known later page),
    not just within the currently-loaded page.
  - Empty search result — confirm the table and paginator handle
    `totalCount: 0` without erroring.
  - Confirm `add-loan-dialog`'s customer `mat-select`, Dashboard's loans
    table, and Reports' date-range filter all still work unchanged — they
    depend on the untouched `getCustomers()`/`getLoans()` methods.

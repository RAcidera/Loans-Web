using LoanManagementSystem.Application.Common.DateTimeHandling;
using LoanManagementSystem.Application.Common.DTOs;
using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.Diary;
using LoanManagementSystem.Domain.Loans;
using LoanManagementSystem.Domain.Repositories;
using MediatR;

namespace LoanManagementSystem.Application.Calendar.Queries.GetCalendarEvents;

/// <summary><paramref name="Types"/> is a comma-separated subset of "diary,reminder,loan_due,extension_due,promise" (requirements §19's togglable event types) — omitted or empty returns every type.</summary>
public sealed record GetCalendarEventsQuery(string FromDate, string ToDate, string? Types = null) : IRequest<List<CalendarEventDto>>;

/// <summary>
/// Requirements §18/§19/§21's ICalendarService, implemented directly as a
/// query handler (not a separately injected service) — unlike
/// IFinancialSnapshotService, nothing else in the Application layer needs
/// to reuse this computation, so a plain handler with private per-source
/// helper methods follows this codebase's usual one-handler-per-query
/// pattern instead of adding an abstraction with only one caller.
/// </summary>
public sealed class GetCalendarEventsQueryHandler : IRequestHandler<GetCalendarEventsQuery, List<CalendarEventDto>>
{
    /// <summary>Requirements' fixed per-type legend: Loan Due=blue, Extension Due=orange, Follow-up=green, Promise to Pay=teal, Diary=purple (used only as DiaryFallbackColor when a category was deleted — diary events are normally colored by their own DiaryCategory.DisplayColor).</summary>
    private const string LoanDueColor = "#2563EB";
    private const string ExtensionDueColor = "#EA580C";
    private const string FollowUpColor = "#16A34A";
    private const string PromiseColor = "#0D9488";
    private const string DiaryFallbackColor = "#7C3AED";

    private readonly IDiaryRepository _diaryRepository;
    private readonly IDiaryCategoryRepository _diaryCategoryRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPromiseToPayRepository _promiseRepository;
    private readonly IAppDateTimeService _appDateTime;

    public GetCalendarEventsQueryHandler(
        IDiaryRepository diaryRepository, IDiaryCategoryRepository diaryCategoryRepository,
        ILoanRepository loanRepository, ICustomerRepository customerRepository, IPromiseToPayRepository promiseRepository,
        IAppDateTimeService appDateTime)
    {
        _diaryRepository = diaryRepository;
        _diaryCategoryRepository = diaryCategoryRepository;
        _loanRepository = loanRepository;
        _customerRepository = customerRepository;
        _promiseRepository = promiseRepository;
        _appDateTime = appDateTime;
    }

    public async Task<List<CalendarEventDto>> Handle(GetCalendarEventsQuery request, CancellationToken ct)
    {
        var from = DateOnly.Parse(request.FromDate);
        var to = DateOnly.Parse(request.ToDate);
        var types = string.IsNullOrWhiteSpace(request.Types)
            ? null
            : request.Types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        var events = new List<CalendarEventDto>();

        if (Wants(types, "diary") || Wants(types, "reminder"))
            events.AddRange(await GetDiaryEventsAsync(from, to, types, ct));

        if (Wants(types, "loan_due") || Wants(types, "extension_due"))
            events.AddRange(await GetLoanDueEventsAsync(from, to, types, ct));

        if (Wants(types, "promise"))
            events.AddRange(await GetPromiseEventsAsync(from, to, ct));

        return events.OrderBy(e => e.Date).ThenBy(e => e.Time).ToList();
    }

    private static bool Wants(HashSet<string>? types, string type) => types is null || types.Contains(type);

    private async Task<List<CalendarEventDto>> GetDiaryEventsAsync(DateOnly from, DateOnly to, HashSet<string>? types, CancellationToken ct)
    {
        var entries = await _diaryRepository.GetInRangeAsync(from, to, ct);
        if (entries.Count == 0) return new List<CalendarEventDto>();

        var categoriesById = (await _diaryCategoryRepository.GetAllAsync(ct)).ToDictionary(c => c.Id);
        var customerIds = entries.Where(e => e.CustomerId.HasValue).Select(e => e.CustomerId!.Value).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);
        var result = new List<CalendarEventDto>();

        foreach (var entry in entries)
        {
            var color = categoriesById.TryGetValue(entry.CategoryId, out var category) ? category.DisplayColor : DiaryFallbackColor;
            var customerId = entry.CustomerId?.ToString();
            var customerName = entry.CustomerId is { } cid ? customerNames.GetValueOrDefault(cid) : null;
            var loanId = entry.LoanId?.ToString();

            if (Wants(types, "diary") && entry.EntryDate >= from && entry.EntryDate <= to)
            {
                result.Add(new CalendarEventDto(
                    Id: $"diary-{entry.Id}",
                    Type: "diary",
                    Title: "Diary Entry",
                    Date: entry.EntryDate.ToString("yyyy-MM-dd"),
                    Time: entry.EntryTime.ToString("HH:mm"),
                    Color: color,
                    LinkedEntityType: "diary",
                    LinkedEntityId: entry.Id.ToString(),
                    Subtitle: entry.Title,
                    CustomerId: customerId,
                    LoanId: loanId));
            }

            if (Wants(types, "reminder") && entry.ReminderDate is { } reminderDate && reminderDate >= from && reminderDate <= to)
            {
                result.Add(new CalendarEventDto(
                    Id: $"reminder-{entry.Id}",
                    Type: "reminder",
                    Title: "Follow-up",
                    Date: reminderDate.ToString("yyyy-MM-dd"),
                    Time: entry.ReminderTime?.ToString("HH:mm"),
                    Color: FollowUpColor,
                    LinkedEntityType: "diary",
                    LinkedEntityId: entry.Id.ToString(),
                    Subtitle: customerName,
                    DetailText: entry.Title,
                    CustomerId: customerId,
                    LoanId: loanId));
            }
        }

        return result;
    }

    /// <summary>
    /// "Loan Due Dates" and "Loan Extension Due Dates" are two toggleable
    /// sources over the same underlying field (Loan.DueDate) — this domain
    /// has no separate due-date concept for extensions (Extend() pushes
    /// DueDate out on the loan itself, see LoanExtension's doc comment), so
    /// a loan currently in LoanStatus.Extended is what "has an extension
    /// due date" means; anything else still open (Active/Overdue) is a
    /// plain loan due date.
    /// </summary>
    private async Task<List<CalendarEventDto>> GetLoanDueEventsAsync(DateOnly from, DateOnly to, HashSet<string>? types, CancellationToken ct)
    {
        var loans = await _loanRepository.GetAllAsync(ct);
        var today = _appDateTime.Today;
        foreach (var loan in loans)
            loan.RefreshOverdueStatus(today);

        var relevant = loans
            .Where(l => l.Status is LoanStatus.Active or LoanStatus.Extended or LoanStatus.Overdue && l.DueDate >= from && l.DueDate <= to)
            .ToList();
        if (relevant.Count == 0) return new List<CalendarEventDto>();

        var customerIds = relevant.Select(l => l.CustomerId).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        var result = new List<CalendarEventDto>();
        foreach (var loan in relevant)
        {
            var isExtended = loan.Status == LoanStatus.Extended;
            var type = isExtended ? "extension_due" : "loan_due";
            if (!Wants(types, type)) continue;

            var customerName = customerNames.GetValueOrDefault(loan.CustomerId, "Unknown");
            var loanNumber = MappingExtensions.FormatLoanNumber(loan.LoanNumber);

            result.Add(new CalendarEventDto(
                Id: $"{type}-{loan.Id}",
                Type: type,
                Title: isExtended ? "Extension Due" : "Loan Due",
                Date: loan.DueDate.ToString("yyyy-MM-dd"),
                Time: null,
                Color: isExtended ? ExtensionDueColor : LoanDueColor,
                LinkedEntityType: "loan",
                LinkedEntityId: loan.Id.ToString(),
                Subtitle: $"{loanNumber} · {customerName}",
                Amount: loan.Balance.Amount,
                CustomerId: loan.CustomerId.ToString(),
                LoanId: loan.Id.ToString()));
        }

        return result;
    }

    /// <summary>
    /// Requirements §19/§20's Promise to Pay event source (Phase 3 of the
    /// plan — this was a stub in Phase 2). Every promise in range is shown
    /// regardless of Status, colored per PromiseColorByStatus, so a Kept/
    /// Missed promise still reads as history on the calendar rather than
    /// disappearing once resolved. LinkedEntityType/Id point at the LOAN,
    /// not the promise itself — there's no standalone promise detail route
    /// in the Angular app (promises are surfaced as a tab on Loan Details
    /// per the implementation plan's Phase 3 assumption), so clicking the
    /// event takes the user to the loan that already has a Promises tab.
    /// </summary>
    private async Task<List<CalendarEventDto>> GetPromiseEventsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var promises = await _promiseRepository.GetInRangeAsync(from, to, ct);
        if (promises.Count == 0) return new List<CalendarEventDto>();

        var customerIds = promises.Select(p => p.CustomerId).Distinct().ToList();
        var customerNames = await _customerRepository.GetNamesByIdsAsync(customerIds, ct);

        var result = new List<CalendarEventDto>();
        foreach (var promise in promises)
        {
            var customerName = customerNames.GetValueOrDefault(promise.CustomerId, "Unknown");
            result.Add(new CalendarEventDto(
                Id: $"promise-{promise.Id}",
                Type: "promise",
                Title: "Promise to Pay",
                Date: promise.PromiseDate.ToString("yyyy-MM-dd"),
                Time: null,
                Color: PromiseColor,
                LinkedEntityType: "loan",
                LinkedEntityId: promise.LoanId.ToString(),
                Subtitle: customerName,
                Amount: promise.Amount.Amount,
                CustomerId: promise.CustomerId.ToString(),
                LoanId: promise.LoanId.ToString()));
        }

        return result;
    }
}

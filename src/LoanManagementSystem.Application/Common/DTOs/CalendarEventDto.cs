namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// One shape for every Calendar event source (requirements §19: Diary
/// Entries, Loan Due Dates, Loan Extension Due Dates, Follow-up Reminders,
/// Promise to Pay), so the Angular calendar renders generically off `Type`
/// rather than branching per source. Color is always resolved server-side —
/// a diary entry's color comes from its DiaryCategory.DisplayColor (never
/// hardcoded, per requirements §5); the four fixed system event types
/// (loan_due/extension_due/reminder/promise) get a fixed color chosen once
/// here, not duplicated in the Angular app.
/// </summary>
public sealed record CalendarEventDto(
    string Id,
    string Type, // "diary" | "reminder" | "loan_due" | "extension_due" | "promise"
    string Title, // the event-type label shown as the compact card's bold heading, e.g. "Loan Due"
    string Date,
    string? Time,
    string Color,
    string? LinkedEntityType, // "diary" | "loan" | "customer" | "promise"
    string? LinkedEntityId,
    /// <summary>Card detail line 1 — e.g. "LOA00001 · Maria Santos" for a loan/extension due, or a customer name.</summary>
    string? Subtitle = null,
    /// <summary>Card detail line 2 for events with no currency Amount — e.g. a follow-up's diary title.</summary>
    string? DetailText = null,
    /// <summary>Outstanding balance (loan_due/extension_due) or promised amount (promise) — null for diary/reminder. Drives the Monthly Summary's Total Amount Due and the Advanced Filters' Amount Range.</summary>
    decimal? Amount = null,
    /// <summary>Resolved regardless of event source, so the Advanced Filters' Customer field works uniformly across all five event types.</summary>
    string? CustomerId = null,
    /// <summary>Resolved regardless of event source (null when the event has no loan link, e.g. a plain diary entry) — backs the Advanced Filters' Loan/Loan Status/Classification fields.</summary>
    string? LoanId = null
);

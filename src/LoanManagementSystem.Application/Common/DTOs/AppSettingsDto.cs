namespace LoanManagementSystem.Application.Common.DTOs;

/// <summary>
/// The General Settings area's current values. CurrentBusinessDate
/// (yyyy-MM-dd, computed server-side from IAppDateTimeService.Today) lets
/// the Angular app source "today" for calendar/diary "today" grouping from
/// the business timezone instead of the browser's own clock/timezone.
/// </summary>
public sealed record AppSettingsDto(
    string BusinessTimeZoneId,
    string CurrentBusinessDate
);

using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Settings;

/// <summary>
/// A single General Setting, stored as a Key/Value pair rather than a
/// bespoke entity per setting — "Business Time Zone" is the first of what
/// the product brief calls a "General Settings" area, so a small key-value
/// table avoids a migration for every future setting while staying just as
/// simple for this one. Keys are well-known constants (see
/// <see cref="Keys"/>), not user-defined.
/// </summary>
public class AppSetting : AggregateRoot<AppSettingId>
{
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    /// <summary>Well-known setting keys — the only ones this aggregate is ever created with.</summary>
    public static class Keys
    {
        public const string BusinessTimeZone = "BusinessTimeZone";
    }

    private AppSetting() { } // EF Core

    private AppSetting(AppSettingId id, string key, string value) : base(id)
    {
        Key = key;
        Value = value;
    }

    public static AppSetting Create(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("A setting must have a key.");
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("A setting must have a value.");

        return new AppSetting(AppSettingId.New(), key.Trim(), value.Trim());
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("A setting must have a value.");
        Value = value.Trim();
    }
}

using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Domain.Diary;

/// <summary>
/// Configurable Diary category (requirements §5) — Name/Icon/DisplayColor/
/// SortOrder so the Angular app never hardcodes a category's color, per the
/// requirement's explicit instruction. Seeded with 10 initial values by
/// DbSeeder; no admin CRUD screen exists yet for adding more (see the
/// implementation plan's Phase 1 assumption).
/// </summary>
public class DiaryCategory : AggregateRoot<DiaryCategoryId>
{
    public string Name { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public string DisplayColor { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }

    private DiaryCategory() { } // EF Core

    private DiaryCategory(DiaryCategoryId id, string name, string icon, string displayColor, int sortOrder)
        : base(id)
    {
        Name = name;
        Icon = icon;
        DisplayColor = displayColor;
        SortOrder = sortOrder;
        IsActive = true;
    }

    public static DiaryCategory Create(string name, string icon, string displayColor, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A diary category must have a name.");

        return new DiaryCategory(DiaryCategoryId.New(), name.Trim(), icon.Trim(), displayColor.Trim(), sortOrder);
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;

    /// <summary>Reconciles Icon/DisplayColor/SortOrder to a canonical value — used only by DbSeeder to backfill an already-seeded category's appearance when the canonical palette changes (requirements §5's suggested colors), never by user-facing CRUD (no admin category editor exists yet).</summary>
    public void UpdateAppearance(string icon, string displayColor, int sortOrder)
    {
        Icon = icon.Trim();
        DisplayColor = displayColor.Trim();
        SortOrder = sortOrder;
    }
}

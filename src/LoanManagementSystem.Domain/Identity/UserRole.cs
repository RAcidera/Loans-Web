namespace LoanManagementSystem.Domain.Identity;

/// <summary>
/// Matches the SRS Users table's role column exactly ("admin", "staff") and
/// SRS 1.3's intended users: Business Owner/Admin, and optionally a Staff
/// or Collector role with limited access.
/// </summary>
public enum UserRole
{
    Admin,
    Staff,
}

using LoanManagementSystem.Application.Common.Mappings;
using LoanManagementSystem.Domain.CashLedger;

namespace LoanManagementSystem.Application.CashLedger;

/// <summary>
/// Shared search/type/date-range filtering for the Cash Transactions grid,
/// used identically by GetCashLedgerPageQuery (paged rows) and
/// GetCashLedgerTotalsQuery (footer totals over that same filtered set) so
/// the two can never drift out of sync with each other.
/// </summary>
internal static class CashLedgerFilterHelper
{
    public static IEnumerable<CashLedgerEntry> Apply(
        IEnumerable<CashLedgerEntry> entries, string? search, string? transactionType, string? dateFrom, string? dateTo)
    {
        var result = entries;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            result = result.Where(e =>
                (e.ReferenceId is not null && e.ReferenceId.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                e.Remarks.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            var type = MappingExtensions.ParseCashTransactionType(transactionType);
            result = result.Where(e => e.TransactionType == type);
        }

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var from))
            result = result.Where(e => e.TransactionDate >= from);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var to))
            result = result.Where(e => e.TransactionDate <= to);

        return result;
    }
}

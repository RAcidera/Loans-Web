using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashLedgerIsCashInAndAdjustment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_cash_in",
                table: "cash_ledger",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The temporary defaultValue above (false) is wrong for every
            // pre-existing payment_received/owner_deposit row — backfill
            // using the exact same fixed-direction rule the domain applied
            // before this column existed (CashLedgerEntry.FixedDirection).
            // No pre-existing row can be an Adjustment, since that type
            // didn't exist until this migration.
            migrationBuilder.Sql(
                "UPDATE cash_ledger SET is_cash_in = CASE WHEN transaction_type IN ('payment_received', 'owner_deposit') THEN 1 ELSE 0 END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_cash_in",
                table: "cash_ledger");
        }
    }
}

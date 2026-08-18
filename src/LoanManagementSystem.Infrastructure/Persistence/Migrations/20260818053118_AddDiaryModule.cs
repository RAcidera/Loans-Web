using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiaryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diary_audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    diary_entry_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    performed_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_audit_log", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "diary_categories",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    display_color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "diary_entries",
                columns: table => new
                {
                    diary_entry_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    entry_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    loan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reminder_date = table.Column<DateOnly>(type: "date", nullable: true),
                    reminder_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_entries", x => x.diary_entry_id);
                });

            migrationBuilder.CreateTable(
                name: "diary_financial_snapshots",
                columns: table => new
                {
                    snapshot_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    diary_entry_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    gross_receivables = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    collectible_receivables = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    bad_loan_receivables = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    cash_on_hand = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    active_loan_count = table.Column<int>(type: "int", nullable: false),
                    overdue_loan_count = table.Column<int>(type: "int", nullable: false),
                    bad_loan_count = table.Column<int>(type: "int", nullable: false),
                    collections_today = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    collections_month_to_date = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    loan_releases_today = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    loan_releases_month_to_date = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    snapshot_datetime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diary_financial_snapshots", x => x.snapshot_id);
                    table.ForeignKey(
                        name: "FK_diary_financial_snapshots_diary_entries_diary_entry_id",
                        column: x => x.diary_entry_id,
                        principalTable: "diary_entries",
                        principalColumn: "diary_entry_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_diary_audit_log_diary_entry_id",
                table: "diary_audit_log",
                column: "diary_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_customer_id",
                table: "diary_entries",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_entry_date",
                table: "diary_entries",
                column: "entry_date");

            migrationBuilder.CreateIndex(
                name: "IX_diary_entries_loan_id",
                table: "diary_entries",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_diary_financial_snapshots_diary_entry_id",
                table: "diary_financial_snapshots",
                column: "diary_entry_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diary_audit_log");

            migrationBuilder.DropTable(
                name: "diary_categories");

            migrationBuilder.DropTable(
                name: "diary_financial_snapshots");

            migrationBuilder.DropTable(
                name: "diary_entries");
        }
    }
}

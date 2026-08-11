using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanLedgerAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "loan_audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    loan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    performed_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_audit_log", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "loan_ledger",
                columns: table => new
                {
                    ledger_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    loan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    reference_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    debit = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    credit = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    running_balance = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loan_ledger", x => x.ledger_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_loan_audit_log_loan_id",
                table: "loan_audit_log",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_loan_ledger_loan_id",
                table: "loan_ledger",
                column: "loan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loan_audit_log");

            migrationBuilder.DropTable(
                name: "loan_ledger");
        }
    }
}

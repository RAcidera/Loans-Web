using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromiseToPay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promise_audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    performed_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promise_audit_log", x => x.audit_log_id);
                });

            migrationBuilder.CreateTable(
                name: "promises_to_pay",
                columns: table => new
                {
                    promise_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    loan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promise_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modified_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    modified_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promises_to_pay", x => x.promise_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promise_audit_log_promise_id",
                table: "promise_audit_log",
                column: "promise_id");

            migrationBuilder.CreateIndex(
                name: "IX_promises_to_pay_customer_id",
                table: "promises_to_pay",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_promises_to_pay_loan_id",
                table: "promises_to_pay",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "IX_promises_to_pay_promise_date",
                table: "promises_to_pay",
                column: "promise_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promise_audit_log");

            migrationBuilder.DropTable(
                name: "promises_to_pay");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtensionAdditionalInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "additional_interest_amount",
                table: "loan_extensions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "additional_interest_amount",
                table: "loan_extensions",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

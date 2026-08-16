using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanPaymentTermsMonths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue is 2 (not EF's auto-generated 0) so existing rows
            // backfill to the same 2-month/60-day term Loan.Originate always
            // used before this column existed, rather than an impossible
            // "0 months" that would also make DailyPayment's denominator
            // (PaymentTermsMonths * 30) meaningless for pre-existing loans.
            migrationBuilder.AddColumn<int>(
                name: "payment_terms_months",
                table: "loans",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_terms_months",
                table: "loans");
        }
    }
}

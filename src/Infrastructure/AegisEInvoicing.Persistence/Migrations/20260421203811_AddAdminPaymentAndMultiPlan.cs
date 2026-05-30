using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPaymentAndMultiPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedPlanIds",
                table: "PendingBusinessRegistrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdminPaymentAmountNaira",
                table: "Businesses",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminPaymentReference",
                table: "Businesses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedPlanIds",
                table: "PendingBusinessRegistrations");

            migrationBuilder.DropColumn(
                name: "AdminPaymentAmountNaira",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "AdminPaymentReference",
                table: "Businesses");
        }
    }
}

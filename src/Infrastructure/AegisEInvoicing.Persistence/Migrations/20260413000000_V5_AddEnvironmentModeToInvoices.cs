using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V5_AddEnvironmentModeToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add EnvironmentMode column to Invoices table.
            // Default 2 = AppEnvironmentMode.Production, so existing live
            // invoices are treated as Production mode (the safe default).
            migrationBuilder.AddColumn<int>(
                name: "EnvironmentMode",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnvironmentMode",
                table: "Invoices");
        }
    }
}

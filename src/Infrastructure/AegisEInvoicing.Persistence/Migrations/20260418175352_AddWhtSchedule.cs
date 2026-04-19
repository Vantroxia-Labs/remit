using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWhtSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CerberusCreatedAt",
                table: "SFTPUsers",
                newName: "SFTPGoCreatedAt");

            migrationBuilder.AlterColumn<int>(
                name: "EnvironmentMode",
                table: "Invoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SFTPGoCreatedAt",
                table: "SFTPUsers",
                newName: "CerberusCreatedAt");

            migrationBuilder.AlterColumn<int>(
                name: "EnvironmentMode",
                table: "Invoices",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}

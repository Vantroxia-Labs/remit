using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V7_FreeTextInvoiceLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make BusinessItemId nullable on InvoiceItems to support vendor-submitted free-text line items
            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            // Add free-text fields for vendor-submitted line items
            migrationBuilder.AddColumn<string>(
                name: "FreeTextDescription",
                table: "InvoiceItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "InvoiceItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FreeTextDescription", table: "InvoiceItems");
            migrationBuilder.DropColumn(name: "UnitOfMeasure", table: "InvoiceItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}

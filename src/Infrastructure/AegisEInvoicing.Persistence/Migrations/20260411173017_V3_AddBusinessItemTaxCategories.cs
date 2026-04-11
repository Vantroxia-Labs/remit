using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V3_AddBusinessItemTaxCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_ProviderCode",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ActiveAppProviderCode",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ApiKeyHeaderName",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "AuthScheme",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "EncryptedProductionApiKey",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "EncryptedProductionApiSecret",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ProductionTokenEndpoint",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ProviderCode",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "SandboxTokenEndpoint",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "SignatureHeaderName",
                table: "AppProviderConfigurations");

            migrationBuilder.RenameColumn(
                name: "ProductionBaseUrl",
                table: "AppProviderConfigurations",
                newName: "BaseUrl");

            migrationBuilder.RenameColumn(
                name: "EncryptedSandboxApiSecret",
                table: "AppProviderConfigurations",
                newName: "EncryptedSandboxCredentials");

            migrationBuilder.RenameColumn(
                name: "EncryptedSandboxApiKey",
                table: "AppProviderConfigurations",
                newName: "EncryptedCredentials");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "AppProviderConfigurations",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "ActiveVendor",
                table: "Businesses",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SandboxBaseUrl",
                table: "AppProviderConfigurations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppProviderConfigurations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<int>(
                name: "Vendor",
                table: "AppProviderConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BusinessItemTaxCategories",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BusinessItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsPercentage = table.Column<bool>(type: "boolean", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    FlatAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessItemTaxCategories", x => new { x.BusinessItemId, x.Code });
                    table.ForeignKey(
                        name: "FK_BusinessItemTaxCategories_BusinessItems_BusinessItemId",
                        column: x => x.BusinessItemId,
                        principalTable: "BusinessItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_Vendor",
                table: "AppProviderConfigurations",
                column: "Vendor",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessItemTaxCategories");

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_Vendor",
                table: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ActiveVendor",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "AppProviderConfigurations");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AppProviderConfigurations",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "EncryptedSandboxCredentials",
                table: "AppProviderConfigurations",
                newName: "EncryptedSandboxApiSecret");

            migrationBuilder.RenameColumn(
                name: "EncryptedCredentials",
                table: "AppProviderConfigurations",
                newName: "EncryptedSandboxApiKey");

            migrationBuilder.RenameColumn(
                name: "BaseUrl",
                table: "AppProviderConfigurations",
                newName: "ProductionBaseUrl");

            migrationBuilder.AddColumn<string>(
                name: "ActiveAppProviderCode",
                table: "Businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SandboxBaseUrl",
                table: "AppProviderConfigurations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AppProviderConfigurations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHeaderName",
                table: "AppProviderConfigurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthScheme",
                table: "AppProviderConfigurations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedProductionApiKey",
                table: "AppProviderConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedProductionApiSecret",
                table: "AppProviderConfigurations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductionTokenEndpoint",
                table: "AppProviderConfigurations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "AppProviderConfigurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SandboxTokenEndpoint",
                table: "AppProviderConfigurations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureHeaderName",
                table: "AppProviderConfigurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_ProviderCode",
                table: "AppProviderConfigurations",
                column: "ProviderCode",
                unique: true);
        }
    }
}

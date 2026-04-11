using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V2_RemoveTaxCategory_AddItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxCategoryName",
                table: "BusinessItems");

            migrationBuilder.DropColumn(
                name: "TaxCategoryPercent",
                table: "BusinessItems");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "BusinessItems",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActiveAppProviderCode",
                table: "Businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppEnvironmentMode",
                table: "Businesses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.CreateTable(
                name: "AppProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AuthScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiKeyHeaderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SignatureHeaderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SandboxBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedSandboxApiKey = table.Column<string>(type: "text", nullable: true),
                    EncryptedSandboxApiSecret = table.Column<string>(type: "text", nullable: true),
                    SandboxTokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProductionBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedProductionApiKey = table.Column<string>(type: "text", nullable: true),
                    EncryptedProductionApiSecret = table.Column<string>(type: "text", nullable: true),
                    ProductionTokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_IsActive",
                table: "AppProviderConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_IsDeleted",
                table: "AppProviderConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_IsDeleted_IsActive",
                table: "AppProviderConfigurations",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_ProviderCode",
                table: "AppProviderConfigurations",
                column: "ProviderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "BusinessItems");

            migrationBuilder.DropColumn(
                name: "ActiveAppProviderCode",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "AppEnvironmentMode",
                table: "Businesses");

            migrationBuilder.AddColumn<string>(
                name: "TaxCategoryName",
                table: "BusinessItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxCategoryPercent",
                table: "BusinessItems",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}

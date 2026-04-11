using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V2_AppProviderConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Create AppProviderConfigurations table ───────────────────────────
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

                    // Sandbox
                    SandboxBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedSandboxApiKey = table.Column<string>(type: "text", nullable: true),
                    EncryptedSandboxApiSecret = table.Column<string>(type: "text", nullable: true),
                    SandboxTokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),

                    // Production
                    ProductionBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedProductionApiKey = table.Column<string>(type: "text", nullable: true),
                    EncryptedProductionApiSecret = table.Column<string>(type: "text", nullable: true),
                    ProductionTokenEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),

                    // Status & Audit
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
                name: "IX_AppProviderConfigurations_ProviderCode",
                table: "AppProviderConfigurations",
                column: "ProviderCode",
                unique: true);

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

            // ─── Add APP provider columns to Businesses table ─────────────────────
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveAppProviderCode",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "AppEnvironmentMode",
                table: "Businesses");

            migrationBuilder.DropTable(
                name: "AppProviderConfigurations");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V3_AppProviderVendorBlob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── AppProviderConfigurations: drop and recreate with new schema ─────
            // Old schema had: ProviderCode (string), AuthScheme (string),
            //   ApiKeyHeaderName, SignatureHeaderName, separate key/secret/endpoint columns.
            // New schema: Vendor (int enum), BaseUrl, EncryptedCredentials (JSON blob),
            //   SandboxBaseUrl, EncryptedSandboxCredentials.

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_ProviderCode",
                table: "AppProviderConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_IsActive",
                table: "AppProviderConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_IsDeleted",
                table: "AppProviderConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_IsDeleted_IsActive",
                table: "AppProviderConfigurations");

            migrationBuilder.DropTable(
                name: "AppProviderConfigurations");

            migrationBuilder.CreateTable(
                name: "AppProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Vendor = table.Column<int>(type: "integer", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedCredentials = table.Column<string>(type: "text", nullable: true),
                    SandboxBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EncryptedSandboxCredentials = table.Column<string>(type: "text", nullable: true),
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
                name: "IX_AppProviderConfigurations_Vendor",
                table: "AppProviderConfigurations",
                column: "Vendor",
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

            // ─── Businesses: replace string code with vendor int ──────────────────

            migrationBuilder.DropColumn(
                name: "ActiveAppProviderCode",
                table: "Businesses");

            migrationBuilder.AddColumn<int>(
                name: "ActiveVendor",
                table: "Businesses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProviderConfigurations");

            migrationBuilder.DropColumn(
                name: "ActiveVendor",
                table: "Businesses");

            // Restore V2 schema (abbreviated — full restore handled by re-running V2)
            migrationBuilder.AddColumn<string>(
                name: "ActiveAppProviderCode",
                table: "Businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}

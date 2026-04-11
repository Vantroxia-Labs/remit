using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V4_ReplaceVendorEnumWithAdapterKey : Migration
    {
        // Maps old AppVendor integer enum values to the new string adapter keys.
        // These strings must match IAccessPointProviderClient.ProviderCode implementations.
        private static readonly (int Value, string Key)[] VendorMap =
        [
            (1, "interswitch"),
            (2, "digitax"),
            (3, "etranzact"),
            (4, "bluebridge")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AppProviderConfigurations: int Vendor → varchar AdapterKey ────────

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_Vendor",
                table: "AppProviderConfigurations");

            // Add the new text column (nullable to allow setting values before dropping old column)
            migrationBuilder.AddColumn<string>(
                name: "AdapterKey",
                table: "AppProviderConfigurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Data migration: translate enum int → string adapter key
            foreach (var (value, key) in VendorMap)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"AppProviderConfigurations\" SET \"AdapterKey\" = '{key}' WHERE \"Vendor\" = {value};");
            }

            // Normalise any remaining rows to the default
            migrationBuilder.Sql(
                "UPDATE \"AppProviderConfigurations\" SET \"AdapterKey\" = 'interswitch' WHERE \"AdapterKey\" IS NULL;");

            // Now make it NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "AdapterKey",
                table: "AppProviderConfigurations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldNullable: true);

            // Drop the old integer column
            migrationBuilder.DropColumn(
                name: "Vendor",
                table: "AppProviderConfigurations");

            // Recreate the unique index on the new text column
            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_AdapterKey",
                table: "AppProviderConfigurations",
                column: "AdapterKey",
                unique: true);

            // ── Businesses: int ActiveVendor → varchar ActiveAdapterKey ───────────

            // Add the new text column (nullable)
            migrationBuilder.AddColumn<string>(
                name: "ActiveAdapterKey",
                table: "Businesses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Data migration: translate enum int → string adapter key
            foreach (var (value, key) in VendorMap)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"Businesses\" SET \"ActiveAdapterKey\" = '{key}' WHERE \"ActiveVendor\" = {value};");
            }

            // Drop the old integer column (null means platform default — no conversion needed)
            migrationBuilder.DropColumn(
                name: "ActiveVendor",
                table: "Businesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── AppProviderConfigurations: revert AdapterKey → Vendor (int) ───────

            migrationBuilder.DropIndex(
                name: "IX_AppProviderConfigurations_AdapterKey",
                table: "AppProviderConfigurations");

            migrationBuilder.AddColumn<int>(
                name: "Vendor",
                table: "AppProviderConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            foreach (var (value, key) in VendorMap)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"AppProviderConfigurations\" SET \"Vendor\" = {value} WHERE \"AdapterKey\" = '{key}';");
            }

            migrationBuilder.DropColumn(
                name: "AdapterKey",
                table: "AppProviderConfigurations");

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_Vendor",
                table: "AppProviderConfigurations",
                column: "Vendor",
                unique: true);

            // ── Businesses: revert ActiveAdapterKey → ActiveVendor (int) ─────────

            migrationBuilder.AddColumn<int>(
                name: "ActiveVendor",
                table: "Businesses",
                type: "integer",
                nullable: true);

            foreach (var (value, key) in VendorMap)
            {
                migrationBuilder.Sql(
                    $"UPDATE \"Businesses\" SET \"ActiveVendor\" = {value} WHERE \"ActiveAdapterKey\" = '{key}';");
            }

            migrationBuilder.DropColumn(
                name: "ActiveAdapterKey",
                table: "Businesses");
        }
    }
}

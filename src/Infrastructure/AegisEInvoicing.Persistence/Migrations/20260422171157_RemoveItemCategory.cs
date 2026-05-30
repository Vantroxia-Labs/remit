using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveItemCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessItems_ItemCategories_ItemCategoryId",
                table: "BusinessItems");

            migrationBuilder.DropTable(
                name: "BusinessItemItemCategory");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropIndex(
                name: "IX_BusinessItems_BusinessId_ItemCategoryId",
                table: "BusinessItems");

            migrationBuilder.DropIndex(
                name: "IX_BusinessItems_ItemCategoryId",
                table: "BusinessItems");

            migrationBuilder.DropColumn(
                name: "ItemCategoryId",
                table: "BusinessItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ItemCategoryId",
                table: "BusinessItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessID = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCategories_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessItemItemCategory",
                columns: table => new
                {
                    BusinessItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessItemItemCategory", x => new { x.BusinessItemId, x.ItemCategoryId });
                    table.ForeignKey(
                        name: "FK_BusinessItemItemCategory_BusinessItems_BusinessItemId",
                        column: x => x.BusinessItemId,
                        principalTable: "BusinessItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessItemItemCategory_ItemCategories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_BusinessId_ItemCategoryId",
                table: "BusinessItems",
                columns: new[] { "BusinessID", "ItemCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_ItemCategoryId",
                table: "BusinessItems",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemCategories_BusinessItemId",
                table: "BusinessItemItemCategory",
                column: "BusinessItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemCategories_ItemCategoryId",
                table: "BusinessItemItemCategory",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_BusinessId",
                table: "ItemCategories",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_BusinessId_Name",
                table: "ItemCategories",
                columns: new[] { "BusinessID", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemCategories_Name",
                table: "ItemCategories",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessItems_ItemCategories_ItemCategoryId",
                table: "BusinessItems",
                column: "ItemCategoryId",
                principalTable: "ItemCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V6_VendorBroadcast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorGroups_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorGroups_VendorGroupId",
                        column: x => x.VendorGroupId,
                        principalTable: "VendorGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceBroadcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    InvoiceTypeCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsApprovalLocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceBroadcasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceBroadcasts_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceBroadcastVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceBroadcastId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    VerificationCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VerificationCodeExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EmailVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_InvoiceBroadcastVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceBroadcastVendors_InvoiceBroadcasts_InvoiceBroadcastId",
                        column: x => x.InvoiceBroadcastId,
                        principalTable: "InvoiceBroadcasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceBroadcastVendors_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceBroadcastVendors_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Indexes — VendorGroups
            migrationBuilder.CreateIndex(
                name: "IX_VendorGroups_BusinessId",
                table: "VendorGroups",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorGroups_BusinessId_Name",
                table: "VendorGroups",
                columns: new[] { "BusinessId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorGroups_IsDeleted",
                table: "VendorGroups",
                column: "IsDeleted");

            // Indexes — Vendors
            migrationBuilder.CreateIndex(
                name: "IX_Vendors_BusinessId",
                table: "Vendors",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorGroupId",
                table: "Vendors",
                column: "VendorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_BusinessId_Email",
                table: "Vendors",
                columns: new[] { "BusinessId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_IsDeleted",
                table: "Vendors",
                column: "IsDeleted");

            // Indexes — InvoiceBroadcasts
            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_BusinessId",
                table: "InvoiceBroadcasts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_Status",
                table: "InvoiceBroadcasts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_BusinessId_Status",
                table: "InvoiceBroadcasts",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_IsDeleted",
                table: "InvoiceBroadcasts",
                column: "IsDeleted");

            // Indexes — InvoiceBroadcastVendors
            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_BroadcastId_VendorId",
                table: "InvoiceBroadcastVendors",
                columns: new[] { "InvoiceBroadcastId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_Token",
                table: "InvoiceBroadcastVendors",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_InvoiceId",
                table: "InvoiceBroadcastVendors",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_IsDeleted",
                table: "InvoiceBroadcastVendors",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InvoiceBroadcastVendors");
            migrationBuilder.DropTable(name: "InvoiceBroadcasts");
            migrationBuilder.DropTable(name: "Vendors");
            migrationBuilder.DropTable(name: "VendorGroups");
        }
    }
}

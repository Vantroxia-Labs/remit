using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class V8_AddBusinessRoleScopedPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlatformRoles_Name",
                table: "PlatformRoles");

            migrationBuilder.AddColumn<int>(
                name: "TotalInputInvoiceCount",
                table: "VatSchedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInputTaxableAmount",
                table: "VatSchedules",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInputVatAmount",
                table: "VatSchedules",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "InputVatScheduleId",
                table: "ReceivedInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "ReceivedInvoices",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WhtScheduleId",
                table: "ReceivedInvoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "PlatformRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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

            migrationBuilder.CreateTable(
                name: "InputVatScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Irn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SupplierTin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputVatScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputVatScheduleItems_VatSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "VatSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "WhtSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    MonthName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FiledAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    TotalItemCount = table.Column<int>(type: "integer", nullable: false),
                    TotalGrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalWhtAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalNrsWhtAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalStateWhtAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhtSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhtSchedules_Businesses_BusinessId",
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "WhtScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    VendorAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VendorTin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Irn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NatureOfTransaction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WhtRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    WhtAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAuthority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhtScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhtScheduleItems_WhtSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "WhtSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                        name: "FK_InvoiceBroadcastVendors_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvoiceBroadcastVendors_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_InputVatScheduleId",
                table: "ReceivedInvoices",
                column: "InputVatScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_WhtScheduleId",
                table: "ReceivedInvoices",
                column: "WhtScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_BusinessId",
                table: "PlatformRoles",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Name",
                table: "PlatformRoles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_InputVatScheduleItems_ReceivedInvoiceId",
                table: "InputVatScheduleItems",
                column: "ReceivedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InputVatScheduleItems_ScheduleId",
                table: "InputVatScheduleItems",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_BusinessId",
                table: "InvoiceBroadcasts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_BusinessId_Status",
                table: "InvoiceBroadcasts",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_IsDeleted",
                table: "InvoiceBroadcasts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcasts_Status",
                table: "InvoiceBroadcasts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_BroadcastId_VendorId",
                table: "InvoiceBroadcastVendors",
                columns: new[] { "InvoiceBroadcastId", "VendorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_InvoiceId",
                table: "InvoiceBroadcastVendors",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_IsDeleted",
                table: "InvoiceBroadcastVendors",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_Token",
                table: "InvoiceBroadcastVendors",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceBroadcastVendors_VendorId",
                table: "InvoiceBroadcastVendors",
                column: "VendorId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_BusinessId",
                table: "Vendors",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_BusinessId_Email",
                table: "Vendors",
                columns: new[] { "BusinessId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_IsDeleted",
                table: "Vendors",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorGroupId",
                table: "Vendors",
                column: "VendorGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WhtScheduleItems_Schedule_ReceivedInvoice",
                table: "WhtScheduleItems",
                columns: new[] { "ScheduleId", "ReceivedInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WhtScheduleItems_ScheduleId",
                table: "WhtScheduleItems",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WhtSchedules_Business_Period",
                table: "WhtSchedules",
                columns: new[] { "BusinessId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputVatScheduleItems");

            migrationBuilder.DropTable(
                name: "InvoiceBroadcastVendors");

            migrationBuilder.DropTable(
                name: "WhtScheduleItems");

            migrationBuilder.DropTable(
                name: "InvoiceBroadcasts");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "WhtSchedules");

            migrationBuilder.DropTable(
                name: "VendorGroups");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedInvoices_InputVatScheduleId",
                table: "ReceivedInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedInvoices_WhtScheduleId",
                table: "ReceivedInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRoles_BusinessId",
                table: "PlatformRoles");

            migrationBuilder.DropIndex(
                name: "IX_PlatformRoles_Name",
                table: "PlatformRoles");

            migrationBuilder.DropColumn(
                name: "TotalInputInvoiceCount",
                table: "VatSchedules");

            migrationBuilder.DropColumn(
                name: "TotalInputTaxableAmount",
                table: "VatSchedules");

            migrationBuilder.DropColumn(
                name: "TotalInputVatAmount",
                table: "VatSchedules");

            migrationBuilder.DropColumn(
                name: "InputVatScheduleId",
                table: "ReceivedInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "ReceivedInvoices");

            migrationBuilder.DropColumn(
                name: "WhtScheduleId",
                table: "ReceivedInvoices");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "PlatformRoles");

            migrationBuilder.DropColumn(
                name: "FreeTextDescription",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "InvoiceItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "BusinessItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Name",
                table: "PlatformRoles",
                column: "Name",
                unique: true);
        }
    }
}

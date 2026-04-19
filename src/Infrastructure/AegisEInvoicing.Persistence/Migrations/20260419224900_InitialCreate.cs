using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisEInvoicing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdapterKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "BusinessOnboardings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BusinessRegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaxIdentificationNumber_Value = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegisteredAddress_Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegisteredAddress_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegisteredAddress_State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegisteredAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegisteredAddress_PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactPersonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPersonTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeploymentType = table.Column<string>(type: "text", nullable: false),
                    OnPremiseDetails = table.Column<string>(type: "jsonb", nullable: true),
                    DomainWhitelist = table.Column<string>(type: "jsonb", nullable: true),
                    FIRSApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FIRSApiSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FIRSServiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FIRSSecretKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasFIRSCredentials = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExpectedMonthlyInvoices = table.Column<int>(type: "integer", nullable: false),
                    ExpectedUsers = table.Column<int>(type: "integer", nullable: false),
                    SpecialRequirements = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StatusReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusLastChanged = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedKMPGReviewer = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "text", nullable: true),
                    RiskAssessment = table.Column<string>(type: "text", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "text", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UploadedDocuments = table.Column<string>(type: "jsonb", nullable: true),
                    ComplianceCheckPassed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ComplianceNotes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_BusinessOnboardings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FIRSApiConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DeploymentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "text", nullable: false),
                    EncryptedApiSecret = table.Column<string>(type: "text", nullable: false),
                    Environment = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_FIRSApiConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RequestData = table.Column<string>(type: "text", nullable: false),
                    ResponseData = table.Column<string>(type: "text", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTransmissionQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Irn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestPayload = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_InvoiceTransmissionQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingBusinessRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminFirstName = table.Column<string>(type: "text", nullable: false),
                    AdminLastName = table.Column<string>(type: "text", nullable: false),
                    AdminEmail = table.Column<string>(type: "text", nullable: false),
                    AdminPhone = table.Column<string>(type: "text", nullable: false),
                    BusinessName = table.Column<string>(type: "text", nullable: false),
                    Tin = table.Column<string>(type: "text", nullable: true),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingCycle = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaystackReference = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedBusinessId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_PendingBusinessRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    permissions = table.Column<string>(type: "json", nullable: false),
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
                    table.PrimaryKey("PK_PlatformRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Tier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MonthlyPrice = table.Column<double>(type: "double precision", precision: 18, scale: 2, nullable: false),
                    AnnualPrice = table.Column<double>(type: "double precision", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "NGN"),
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
                    table.PrimaryKey("PK_PlatformSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UsageNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    MaxBusinesses = table.Column<int>(type: "integer", nullable: false),
                    Features = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SubscriptionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeploymentMode = table.Column<string>(type: "text", nullable: false),
                    IsSetupCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    SetupCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SetupCompletedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LicenseExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OrganizationContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OrganizationContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllowSelfOnboarding = table.Column<bool>(type: "boolean", nullable: false),
                    MaxBusinessesAllowed = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdditionalDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_AdditionalDocumentReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsageSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TotalRequests = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SuccessfulRequests = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FailedRequests = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalDataTransferredBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    EndpointUsage = table.Column<string>(type: "text", nullable: false),
                    EndpointCosts = table.Column<string>(type: "text", nullable: false),
                    FIRSOperationsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FIRSOperationsCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false, defaultValue: 0m),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ApiUsageSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsageTrackings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "integer", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    RequestSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ResponseSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    RequestTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApiKeyUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    FIRSInvoiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UsedAegisCredentials = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_ApiUsageTrackings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_BillingReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsHeadOffice = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BusinessRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoicePrefix = table.Column<string>(type: "text", nullable: false),
                    tax_identification_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    address_street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Industry = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FIRSApiKey = table.Column<string>(type: "text", nullable: true),
                    FIRSClientSecret = table.Column<string>(type: "text", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessFIRSApiConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    FlowRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    FIRSBusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OAuth2Token = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKeyGeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApiKeyLastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsApiKeyActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    Certificate = table.Column<string>(type: "text", nullable: true),
                    ActiveAdapterKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppEnvironmentMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Production"),
                    DeploymentMode = table.Column<int>(type: "integer", nullable: false),
                    LicenseKey = table.Column<string>(type: "text", nullable: true),
                    LicenseKeyIssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LicenseKeyExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessFIRSApiConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    FIRSApiConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_BusinessFIRSApiConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessFIRSApiConfigurations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessFIRSApiConfigurations_FIRSApiConfigurations_FIRSApi~",
                        column: x => x.FIRSApiConfigurationId,
                        principalTable: "FIRSApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlowRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 9999999999999999.99m),
                    RequiresClientAdminApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    EnableTimeBasedRules = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ActiveStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ActiveEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    active_days_of_week = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_FlowRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowRules_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
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
                name: "ItemCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BusinessID = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ItemCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCategories_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TIN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BusinessID = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parties_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceTypeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssueTime = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentCurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TaxCurrencyCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntryStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SyncDate = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierPartyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SupplierTIN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SupplierBRN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SupplierTelephone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SupplierAddress_Street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupplierAddress_City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupplierAddress_State = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SupplierAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SupplierAddress_PostalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerPartyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CustomerTIN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CustomerBRN = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CustomerTelephone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerAddress_Street = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomerAddress_City = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerAddress_State = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CustomerAddress_Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerAddress_PostalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LineExtensionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxExclusiveAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxInclusiveAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PayableRoundingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BuyerReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountingCost = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceLinesJson = table.Column<string>(type: "text", nullable: true),
                    TaxTotalJson = table.Column<string>(type: "text", nullable: true),
                    FirsBusinessId = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    InputVatScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    WhtScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsReconciled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_ReceivedInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedInvoices_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SFTPUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RootDirectoryPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WorkingDirectory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DirectoriesCreated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SftpInvoiceTransmissionEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SFTPGoCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SFTPUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SFTPUsers_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastBillingDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscriptions_PlatformSubscriptions_PlatformSubscriptionId",
                        column: x => x.PlatformSubscriptionId,
                        principalTable: "PlatformSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, collation: "case_insensitive"),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PasswordHash_Hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash_Salt = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordCreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LockedOutUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PasswordChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Preferences_Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en-US"),
                    Preferences_TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    Preferences_DateFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "yyyy-MM-dd"),
                    Preferences_NumberFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en-US"),
                    Preferences_EmailNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Preferences_SmsNotifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Preferences_InvoiceReminders = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Preferences_SecurityAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Preferences_Theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "light"),
                    Preferences_PageSize = table.Column<int>(type: "integer", nullable: false, defaultValue: 25),
                    Preferences_TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsAegisUser = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AegisRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AegisEmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AegisDepartment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastAegisActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VatSchedules",
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
                    TotalInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    TotalTaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalVatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalInputInvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    TotalInputTaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalInputVatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_VatSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatSchedules_Businesses_BusinessId",
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
                name: "BusinessItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ServiceCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ServiceCodeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BusinessID = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_BusinessItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessItems_Businesses_BusinessID",
                        column: x => x.BusinessID,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessItems_ItemCategories_ItemCategoryId",
                        column: x => x.ItemCategoryId,
                        principalTable: "ItemCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlatformRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_PlatformRoles_PlatformRoleId",
                        column: x => x.PlatformRoleId,
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EndReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DeviceInfo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssueTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    InvoiceType_Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InvoiceType_Code = table.Column<int>(type: "integer", nullable: false),
                    InvoiceKind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Currency_Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Currency_Code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    DeliveryPeriod_StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryPeriod_EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentTerms = table.Column<string>(type: "text", nullable: true),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PaymentMeans_Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentMeans_Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnvironmentMode = table.Column<int>(type: "integer", nullable: false),
                    InvoiceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QRCode_EncryptedData = table.Column<string>(type: "text", nullable: true),
                    QRCode_Base64Image = table.Column<string>(type: "text", nullable: true),
                    QRCode_GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FIRSSubmissionId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedToFIRSAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FIRSSubmissionResponseMessage = table.Column<string>(type: "text", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    VatScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_VatSchedules_VatScheduleId",
                        column: x => x.VatScheduleId,
                        principalTable: "VatSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VatScheduleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Irn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PartyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PartyTin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatScheduleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatScheduleItems_VatSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "VatSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "BusinessItemItemCategory",
                columns: table => new
                {
                    BusinessItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "BusinessItemPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    NewPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovalComments = table.Column<string>(type: "text", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_BusinessItemPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessItemPriceHistories_BusinessItems_BusinessItemId",
                        column: x => x.BusinessItemId,
                        principalTable: "BusinessItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessItemPriceHistories_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateTable(
                name: "ContractDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ContractDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDocumentReferences_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_DispatchDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchDocumentReferences_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
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
                    table.PrimaryKey("PK_InvoiceApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceApprovalHistories_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceApprovalHistories_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DiscountFee_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountFee_Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AdditionalFee_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AdditionalFee_Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    FreeTextDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_InvoiceItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_BusinessItems_BusinessItemId",
                        column: x => x.BusinessItemId,
                        principalTable: "BusinessItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OriginatorDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_OriginatorDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OriginatorDocumentReferences_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptDocumentReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IRN = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ReceiptDocumentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptDocumentReferences_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
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
                name: "IX_AdditionalDocumentReferences_InvoiceId",
                table: "AdditionalDocumentReferences",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageSummaries_BusinessId",
                table: "ApiUsageSummaries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageSummaries_BusinessId_Period",
                table: "ApiUsageSummaries",
                columns: new[] { "BusinessId", "PeriodStart", "PeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageSummaries_IsFinalized",
                table: "ApiUsageSummaries",
                column: "IsFinalized");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageSummaries_PeriodEnd",
                table: "ApiUsageSummaries",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageSummaries_PeriodStart",
                table: "ApiUsageSummaries",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_BusinessId",
                table: "ApiUsageTrackings",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_BusinessId_IsBillable",
                table: "ApiUsageTrackings",
                columns: new[] { "BusinessId", "IsBillable" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_BusinessId_RequestTimestamp",
                table: "ApiUsageTrackings",
                columns: new[] { "BusinessId", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_Endpoint",
                table: "ApiUsageTrackings",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_IsBillable",
                table: "ApiUsageTrackings",
                column: "IsBillable");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_RequestTimestamp",
                table: "ApiUsageTrackings",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageTrackings_UserId",
                table: "ApiUsageTrackings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppProviderConfigurations_AdapterKey",
                table: "AppProviderConfigurations",
                column: "AdapterKey",
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

            migrationBuilder.CreateIndex(
                name: "IX_BillingReferences_InvoiceId",
                table: "BillingReferences",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_AdminUserId",
                table: "Branches",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BusinessId",
                table: "Branches",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BusinessId_Code",
                table: "Branches",
                columns: new[] { "BusinessId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_Code",
                table: "Branches",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_IsActive",
                table: "Branches",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_IsDeleted",
                table: "Branches",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_AdminUserId",
                table: "Businesses",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_BusinessRegistrationNumber",
                table: "Businesses",
                column: "BusinessRegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_ContactEmail",
                table: "Businesses",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_FIRSBusinessId",
                table: "Businesses",
                column: "FIRSBusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_IsDeleted",
                table: "Businesses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Name",
                table: "Businesses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_ServiceId",
                table: "Businesses",
                column: "ServiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Status",
                table: "Businesses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_Business_FIRSConfig",
                table: "BusinessFIRSApiConfigurations",
                columns: new[] { "BusinessId", "FIRSApiConfigurationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_BusinessId",
                table: "BusinessFIRSApiConfigurations",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_CreatedAt",
                table: "BusinessFIRSApiConfigurations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_FIRSApiConfigurationId",
                table: "BusinessFIRSApiConfigurations",
                column: "FIRSApiConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_IsDeleted",
                table: "BusinessFIRSApiConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFIRSApiConfigurations_IsDeleted_BusinessId",
                table: "BusinessFIRSApiConfigurations",
                columns: new[] { "IsDeleted", "BusinessId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemCategories_BusinessItemId",
                table: "BusinessItemItemCategory",
                column: "BusinessItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemCategories_ItemCategoryId",
                table: "BusinessItemItemCategory",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemPriceHistories_ApproverId",
                table: "BusinessItemPriceHistories",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItemPriceHistories_BusinessItemId",
                table: "BusinessItemPriceHistories",
                column: "BusinessItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_BusinessId",
                table: "BusinessItems",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_BusinessId_ItemCategoryId",
                table: "BusinessItems",
                columns: new[] { "BusinessID", "ItemCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_BusinessId_Name",
                table: "BusinessItems",
                columns: new[] { "BusinessID", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_ItemCategoryId",
                table: "BusinessItems",
                column: "ItemCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_ItemId",
                table: "BusinessItems",
                column: "ItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessItems_Name",
                table: "BusinessItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_AssignedReviewer",
                table: "BusinessOnboardings",
                column: "AssignedKMPGReviewer");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_AssignedReviewer_Status",
                table: "BusinessOnboardings",
                columns: new[] { "AssignedKMPGReviewer", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_CompanyName",
                table: "BusinessOnboardings",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_CreatedBusinessId",
                table: "BusinessOnboardings",
                column: "CreatedBusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_DeploymentType",
                table: "BusinessOnboardings",
                column: "DeploymentType");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_IsDeleted",
                table: "BusinessOnboardings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_IsDeleted_Status",
                table: "BusinessOnboardings",
                columns: new[] { "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_Status",
                table: "BusinessOnboardings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOnboardings_Status_CreatedAt",
                table: "BusinessOnboardings",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocumentReferences_InvoiceId",
                table: "ContractDocumentReferences",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchDocumentReferences_InvoiceId",
                table: "DispatchDocumentReferences",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_Active_Default",
                table: "FIRSApiConfigurations",
                columns: new[] { "IsActive", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_CreatedAt",
                table: "FIRSApiConfigurations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_Deployment_Active",
                table: "FIRSApiConfigurations",
                columns: new[] { "DeploymentType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_DeploymentType",
                table: "FIRSApiConfigurations",
                column: "DeploymentType");

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_IsActive",
                table: "FIRSApiConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_IsDefault",
                table: "FIRSApiConfigurations",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_IsDeleted",
                table: "FIRSApiConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_IsDeleted_IsActive",
                table: "FIRSApiConfigurations",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FIRSApiConfigurations_Name",
                table: "FIRSApiConfigurations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_flow_rules_BusinessId_Name_IsDeleted",
                table: "FlowRules",
                columns: new[] { "BusinessId", "Name", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_flow_rules_BusinessId_Range_Priority",
                table: "FlowRules",
                columns: new[] { "BusinessId", "MinAmount", "MaxAmount", "Priority", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowRules_BusinessId",
                table: "FlowRules",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowRules_CreatedAt",
                table: "FlowRules",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FlowRules_CreatedBy",
                table: "FlowRules",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FlowRules_IsDeleted",
                table: "FlowRules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InputVatScheduleItems_ReceivedInvoiceId",
                table: "InputVatScheduleItems",
                column: "ReceivedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InputVatScheduleItems_ScheduleId",
                table: "InputVatScheduleItems",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_CorrelationId",
                table: "IntegrationLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_ExternalSystem",
                table: "IntegrationLogs",
                column: "ExternalSystem");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_ExternalSystem_Operation",
                table: "IntegrationLogs",
                columns: new[] { "ExternalSystem", "Operation" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_IsSuccess",
                table: "IntegrationLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_IsSuccess_StartedAt",
                table: "IntegrationLogs",
                columns: new[] { "IsSuccess", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_Operation",
                table: "IntegrationLogs",
                column: "Operation");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_Operation_StartedAt",
                table: "IntegrationLogs",
                columns: new[] { "Operation", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationLogs_StartedAt",
                table: "IntegrationLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_CreatedAt",
                table: "InvoiceApprovalHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_CreatedBy",
                table: "InvoiceApprovalHistories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_InvoiceId",
                table: "InvoiceApprovalHistories",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_InvoiceId_CreatedAt",
                table: "InvoiceApprovalHistories",
                columns: new[] { "InvoiceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_InvoiceStatus",
                table: "InvoiceApprovalHistories",
                column: "InvoiceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceApprovalHistories_IsDeleted",
                table: "InvoiceApprovalHistories",
                column: "IsDeleted");

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
                name: "IX_InvoiceItems_BusinessItemId",
                table: "InvoiceItems",
                column: "BusinessItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId",
                table: "InvoiceItems",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_InvoiceId_BusinessItemId",
                table: "InvoiceItems",
                columns: new[] { "InvoiceId", "BusinessItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessId",
                table: "Invoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessId_IssueDate",
                table: "Invoices",
                columns: new[] { "BusinessId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessId_Status",
                table: "Invoices",
                columns: new[] { "BusinessId", "InvoiceStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedBy",
                table: "Invoices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FIRSSubmissionId",
                table: "Invoices",
                column: "FIRSSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceId",
                table: "Invoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IRN",
                table: "Invoices",
                column: "IRN",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_IssueDate",
                table: "Invoices",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PartyId",
                table: "Invoices",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentStatus",
                table: "Invoices",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "InvoiceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VatScheduleId",
                table: "Invoices",
                column: "VatScheduleId",
                filter: "\"VatScheduleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_BusinessId",
                table: "InvoiceTransmissionQueues",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_BusinessId_ProcessingStatus",
                table: "InvoiceTransmissionQueues",
                columns: new[] { "BusinessId", "ProcessingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_CompletedAt",
                table: "InvoiceTransmissionQueues",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_CreatedAt",
                table: "InvoiceTransmissionQueues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_Irn",
                table: "InvoiceTransmissionQueues",
                column: "Irn");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_Irn_Status",
                table: "InvoiceTransmissionQueues",
                columns: new[] { "Irn", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_IsDeleted",
                table: "InvoiceTransmissionQueues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_IsDeleted_ProcessingStatus",
                table: "InvoiceTransmissionQueues",
                columns: new[] { "IsDeleted", "ProcessingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_ProcessAfter",
                table: "InvoiceTransmissionQueues",
                column: "ProcessAfter");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_ProcessingStatus",
                table: "InvoiceTransmissionQueues",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_ProcessingStatus_AttemptCount",
                table: "InvoiceTransmissionQueues",
                columns: new[] { "ProcessingStatus", "AttemptCount" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_ProcessingStatus_ProcessAfter",
                table: "InvoiceTransmissionQueues",
                columns: new[] { "ProcessingStatus", "ProcessAfter" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_Status",
                table: "InvoiceTransmissionQueues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceTransmissionQueues_UserId",
                table: "InvoiceTransmissionQueues",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_OriginatorDocumentReferences_InvoiceId",
                table: "OriginatorDocumentReferences",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_CreatedAt",
                table: "OutboxEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_OccurredOnUtc",
                table: "OutboxEvents",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedOnUtc",
                table: "OutboxEvents",
                column: "ProcessedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status",
                table: "OutboxEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_CreatedAt",
                table: "OutboxEvents",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Status_RetryCount",
                table: "OutboxEvents",
                columns: new[] { "Status", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_BusinessId",
                table: "Parties",
                column: "BusinessID");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_BusinessId_Email",
                table: "Parties",
                columns: new[] { "BusinessID", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_BusinessId_Name",
                table: "Parties",
                columns: new[] { "BusinessID", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Email",
                table: "Parties",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Name",
                table: "Parties",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_TIN",
                table: "Parties",
                column: "TIN");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_BusinessId",
                table: "PlatformRoles",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Category",
                table: "PlatformRoles",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Category_SortOrder",
                table: "PlatformRoles",
                columns: new[] { "Category", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_CreatedAt",
                table: "PlatformRoles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_IsActive",
                table: "PlatformRoles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_IsDeleted",
                table: "PlatformRoles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_IsDeleted_IsActive",
                table: "PlatformRoles",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_IsSystemRole",
                table: "PlatformRoles",
                column: "IsSystemRole");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Name",
                table: "PlatformRoles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_SortOrder",
                table: "PlatformRoles",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSubscriptions_CreatedAt",
                table: "PlatformSubscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSubscriptions_IsDeleted",
                table: "PlatformSubscriptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSubscriptions_PlanName",
                table: "PlatformSubscriptions",
                column: "PlanName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSubscriptions_Tier",
                table: "PlatformSubscriptions",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSubscriptions_Tier_IsDeleted",
                table: "PlatformSubscriptions",
                columns: new[] { "Tier", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptDocumentReferences_InvoiceId",
                table: "ReceiptDocumentReferences",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_Business_IssueDate",
                table: "ReceivedInvoices",
                columns: new[] { "BusinessId", "IssueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_BusinessId",
                table: "ReceivedInvoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_InputVatScheduleId",
                table: "ReceivedInvoices",
                column: "InputVatScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_IsReconciled",
                table: "ReceivedInvoices",
                column: "IsReconciled");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_IssueDate",
                table: "ReceivedInvoices",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_PaymentStatus",
                table: "ReceivedInvoices",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedInvoices_WhtScheduleId",
                table: "ReceivedInvoices",
                column: "WhtScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedAt",
                table: "RefreshTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedAt",
                table: "RefreshTokens",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "Token", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SFTPUsers_BusinessId",
                table: "SFTPUsers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SFTPUsers_BusinessId_Username",
                table: "SFTPUsers",
                columns: new[] { "BusinessId", "Username" });

            migrationBuilder.CreateIndex(
                name: "IX_SFTPUsers_IsDeleted",
                table: "SFTPUsers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SFTPUsers_Status",
                table: "SFTPUsers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SFTPUsers_Username",
                table: "SFTPUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_ContactEmail",
                table: "SubscriptionKeys",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_ExpiryDate",
                table: "SubscriptionKeys",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsActive",
                table: "SubscriptionKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsActive_ExpiryDate",
                table: "SubscriptionKeys",
                columns: new[] { "IsActive", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsDeleted",
                table: "SubscriptionKeys",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsDeleted_IsActive",
                table: "SubscriptionKeys",
                columns: new[] { "IsDeleted", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsUsed",
                table: "SubscriptionKeys",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_IsUsed_UsedAt",
                table: "SubscriptionKeys",
                columns: new[] { "IsUsed", "UsedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionKeys_Key",
                table: "SubscriptionKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_BusinessId",
                table: "Subscriptions",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_EndDate",
                table: "Subscriptions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsDeleted",
                table: "Subscriptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsDeleted_Status",
                table: "Subscriptions",
                columns: new[] { "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlatformSubscriptionId",
                table: "Subscriptions",
                column: "PlatformSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_StartDate",
                table: "Subscriptions",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status_EndDate",
                table: "Subscriptions",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "ix_system_configurations_deployment_mode",
                table: "SystemConfigurations",
                column: "DeploymentMode");

            migrationBuilder.CreateIndex(
                name: "ix_system_configurations_is_setup_completed",
                table: "SystemConfigurations",
                column: "IsSetupCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsDeleted",
                table: "SystemConfigurations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Unique",
                table: "SystemConfigurations",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_AssignedAt",
                table: "UserRoleAssignments",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_ExpiresAt",
                table: "UserRoleAssignments",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_IsActive",
                table: "UserRoleAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_IsActive_ExpiresAt",
                table: "UserRoleAssignments",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_PlatformRoleId",
                table: "UserRoleAssignments",
                column: "PlatformRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RevokedAt",
                table: "UserRoleAssignments",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId",
                table: "UserRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_IsActive",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_UserId_PlatformRoleId_Active",
                table: "UserRoleAssignments",
                columns: new[] { "UserId", "PlatformRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BusinessId",
                table: "Users",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted_Status",
                table: "Users",
                columns: new[] { "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_EndedAt",
                table: "UserSessions",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive_LastActivity",
                table: "UserSessions",
                columns: new[] { "IsActive", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_LastActivityAt",
                table: "UserSessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_StartedAt",
                table: "UserSessions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_IsActive",
                table: "UserSessions",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VatScheduleItems_Schedule_Invoice",
                table: "VatScheduleItems",
                columns: new[] { "ScheduleId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VatScheduleItems_ScheduleId",
                table: "VatScheduleItems",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_VatSchedules_Business_Period",
                table: "VatSchedules",
                columns: new[] { "BusinessId", "Year", "Month" },
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_AdditionalDocumentReferences_Invoices_InvoiceId",
                table: "AdditionalDocumentReferences",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUsageSummaries_Businesses_BusinessId",
                table: "ApiUsageSummaries",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUsageTrackings_Businesses_BusinessId",
                table: "ApiUsageTrackings",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApiUsageTrackings_Users_UserId",
                table: "ApiUsageTrackings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BillingReferences_Invoices_InvoiceId",
                table: "BillingReferences",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Businesses_BusinessId",
                table: "Branches",
                column: "BusinessId",
                principalTable: "Businesses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Users_AdminUserId",
                table: "Branches",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Businesses_Users_AdminUserId",
                table: "Businesses",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Businesses_BusinessId",
                table: "Branches");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Businesses_BusinessId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Users_AdminUserId",
                table: "Branches");

            migrationBuilder.DropTable(
                name: "AdditionalDocumentReferences");

            migrationBuilder.DropTable(
                name: "ApiUsageSummaries");

            migrationBuilder.DropTable(
                name: "ApiUsageTrackings");

            migrationBuilder.DropTable(
                name: "AppProviderConfigurations");

            migrationBuilder.DropTable(
                name: "BillingReferences");

            migrationBuilder.DropTable(
                name: "BusinessFIRSApiConfigurations");

            migrationBuilder.DropTable(
                name: "BusinessItemItemCategory");

            migrationBuilder.DropTable(
                name: "BusinessItemPriceHistories");

            migrationBuilder.DropTable(
                name: "BusinessItemTaxCategories");

            migrationBuilder.DropTable(
                name: "BusinessOnboardings");

            migrationBuilder.DropTable(
                name: "ContractDocumentReferences");

            migrationBuilder.DropTable(
                name: "DispatchDocumentReferences");

            migrationBuilder.DropTable(
                name: "FlowRules");

            migrationBuilder.DropTable(
                name: "InputVatScheduleItems");

            migrationBuilder.DropTable(
                name: "IntegrationLogs");

            migrationBuilder.DropTable(
                name: "InvoiceApprovalHistories");

            migrationBuilder.DropTable(
                name: "InvoiceBroadcastVendors");

            migrationBuilder.DropTable(
                name: "InvoiceItems");

            migrationBuilder.DropTable(
                name: "InvoiceTransmissionQueues");

            migrationBuilder.DropTable(
                name: "OriginatorDocumentReferences");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "PendingBusinessRegistrations");

            migrationBuilder.DropTable(
                name: "ReceiptDocumentReferences");

            migrationBuilder.DropTable(
                name: "ReceivedInvoices");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SFTPUsers");

            migrationBuilder.DropTable(
                name: "SubscriptionKeys");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "VatScheduleItems");

            migrationBuilder.DropTable(
                name: "WhtScheduleItems");

            migrationBuilder.DropTable(
                name: "FIRSApiConfigurations");

            migrationBuilder.DropTable(
                name: "InvoiceBroadcasts");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "BusinessItems");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "PlatformSubscriptions");

            migrationBuilder.DropTable(
                name: "PlatformRoles");

            migrationBuilder.DropTable(
                name: "WhtSchedules");

            migrationBuilder.DropTable(
                name: "VendorGroups");

            migrationBuilder.DropTable(
                name: "ItemCategories");

            migrationBuilder.DropTable(
                name: "Parties");

            migrationBuilder.DropTable(
                name: "VatSchedules");

            migrationBuilder.DropTable(
                name: "Businesses");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");
        }
    }
}

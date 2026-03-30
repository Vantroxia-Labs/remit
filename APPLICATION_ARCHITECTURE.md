# Application Architecture

## Overview

The **EInvoice Integrator** is an enterprise-grade e-invoice management system built with **Clean Architecture + Domain-Driven Design (DDD) + CQRS** patterns. It integrates with Nigeria's Federal Inland Revenue Service (FIRS) for electronic invoice submission and validation.

### Architecture Type
**Multi-deployment Web API + Background Service System** supporting both SaaS and On-Premise deployments

---

## Architectural Layers

### 1. Domain Layer
**Location**: `src/Core/EInvoiceIntegrator.Domain/`

The business logic core containing:

#### Aggregate Roots
- **Business**: Represents registered businesses with branches, users, invoices, subscriptions, and FIRS configurations
- **Invoice**: Created invoices with items, approval history, and status workflow (Draft → Pending → Approved → Submitted)
- **User**: Platform users with authentication, roles, permissions, sessions, and TOTP 2FA support

#### Value Objects
- **TIN**: Tax Identification Number
- **IRN**: Invoice Reference Number
- **Address**: Registered/delivery address
- **Currency**: Invoice currency
- **DeliveryPeriod**: Delivery timeline
- **InvoiceType**: Enum (Invoice, Debit Note, Credit Note)
- **PaymentMeans**: Payment method
- **DigitalSignature**: ECDSA/HMAC signature
- **QRCode**: Encrypted QR code data

#### Domain Events
- **InvoiceCreatedEvent**: Raised when invoice is created
- **InvoiceSubmittedEvent**: Raised when invoice is submitted to FIRS
- **InvoiceApprovedEvent**: Raised when invoice is approved
- **BusinessOnboardedEvent**: Raised when business completes onboarding

#### Base Classes
- `Entity<TId>`: Base entity with identity
- `AggregateRoot`: Root entity with version field for optimistic concurrency
- `AuditableEntity`: Adds CreatedBy, UpdatedBy, DeletedAt tracking
- `AuditableAggregateRoot`: Combines both
- `DomainEventBase`: Base for domain events

#### Technologies
- FluentValidation
- MediatR.Contracts
- Ardalis.GuardClauses
- Ardalis.SmartEnum
- ExcelDataReader

---

### 2. Application Layer
**Location**: `src/Core/EInvoiceIntegrator.Application/`

CQRS pattern implementation with feature-based organization:

#### Feature Structure
```
Features/
├── Authentication/
│   ├── Commands: LoginCommand, LogoutCommand, RefreshTokenCommand, EnableTotpCommand, VerifyTotpCommand
│   └── Queries: GetCurrentUserQuery
├── BusinessManagement/
│   ├── Commands: CreateBusinessCommand, UpdateBusinessCommand, DeactivateBusinessCommand
│   └── Queries: GetBusinessByIdQuery, GetBusinessListQuery
├── InvoiceManagement/
│   ├── Commands: CreateInvoiceCommand, UpdateInvoiceCommand, SubmitInvoiceCommand, ApproveInvoiceCommand, CancelInvoiceCommand
│   ├── Queries: GetInvoiceByIdQuery, GetInvoiceListQuery, GetInvoiceHistoryQuery, SearchInvoicesQuery
│   └── DTOs: CreateInvoiceItemDto, InvoiceDto, InvoiceApprovalDto
├── BusinessItemManagement/
├── PartyManagement/
├── UserManagement/
├── SFTPUserManagement/
├── DashboardAnalytics/
├── AccessPointProviders/
├── PlatformSubscriptions/
├── SubscriptionKeys/
└── SystemIntegrationOperations/
```

Each feature contains:
- **Commands & CommandHandlers**: For state-changing operations (with validation)
- **Queries & QueryHandlers**: For data retrieval
- **DTOs**: Data Transfer Objects for request/response
- **Validators**: FluentValidation rules
- **Results/Response objects**: Standardized responses

#### MediatR Pipeline Behaviors
Executed in order for every command/query:

1. **ValidationBehavior**: Runs FluentValidation validators
2. **LoggingBehavior**: Logs request/response with Serilog
3. **PerformanceBehavior**: Detects slow queries (logs if > 500ms)
4. **TransactionBehavior**: Wraps handlers in database transaction
5. **CachingBehavior**: Caches query results in Redis

#### Application Service Interfaces
```
Common/Interfaces/
├── IApplicationDbContext - Database access abstraction
├── IJwtTokenService - JWT generation/validation
├── ICurrentUserService - Current user context
├── ICacheService - Redis distributed cache
├── IEncryptionService - AES-256-GCM encryption/decryption
├── IEventBus - MassTransit event publishing
├── IFIRSApiKeyService - API key management with encryption
├── IFlowRuleExecutionService - Business rule engine
├── IInvoiceValidationService - Invoice validation rules
├── ICerberusSFTPConnectorService - SFTP file operations
├── ITotpService - 2FA Time-based OTP
├── IDeploymentConfigurationService - SaaS vs On-Premise
└── IDateTime - Abstracted date/time
```

#### Technologies
- MediatR 13.0.0
- FluentValidation 12.0.0
- AutoMapper 12.0.1
- Polly 8.6.2
- SSH.NET 2025.0.0
- QRCoder 1.7.0
- QuestPDF 2025.7.1
- System.Linq.Dynamic.Core

---

### 3. Infrastructure Layer
**Location**: `src/Infrastructure/EInvoiceIntegrator.Infrastructure/`

Cross-cutting concerns and external integrations:

#### Services Provided

**1. Encryption Service** (AES-256-GCM)
- Encrypts/decrypts FIRS API keys and secrets
- Secures API certificates
- Supports key rotation

**2. JWT Token Service**
- Generates access tokens (15-30 min expiry)
- Generates refresh tokens (7 days expiry)
- Validates and extracts claims
- HS256 or RS256 signing algorithms

**3. Cache Service** (Redis)
- Distributed caching via StackExchange.Redis
- Supports multi-instance deployments
- Query result caching with TTL

**4. SFTP Service** (SSH.NET)
- File upload/download operations
- Directory monitoring
- Connection pooling
- Secure credential storage

**5. Email Service**
- MailKit (SMTP) implementation
- AWS SES integration
- HTML/text templates
- Async delivery

**6. Event Bus** (MassTransit/RabbitMQ)
- Publishes domain events to message broker
- Enables eventual consistency
- Supports multiple subscribers

**7. Resilience Patterns** (Polly)
- Retry with exponential backoff
- Circuit breaker for failing services
- Timeout policies
- Bulkhead isolation

**8. TOTP Service**
- Time-based one-time password generation
- 2-Factor authentication support
- QR code generation for authenticator apps

#### Background Services
- **OutboxPublisherBackgroundService**: Publishes domain events from outbox table to RabbitMQ
- **SFTPMonitoringService**: Monitors SFTP directories for incoming files
- **InvoiceTransmissionService**: Processes invoice transmission queue with retry logic

#### Technologies
- SSH.NET 2025.0.0
- RabbitMQ.Client 7.1.2
- MassTransit 8.5.2
- MailKit 4.13.0
- AWSSDK.SimpleEmail 4.0.1.1
- StackExchange.Redis 2.7.27
- Polly 8.6.2
- AspNetCore.Totp 2.3.0
- System.IdentityModel.Tokens.Jwt 8.2.1

---

### 4. Persistence Layer
**Location**: `src/Infrastructure/EInvoiceIntegrator.Persistence/`

Data access with Entity Framework Core:

#### Database Features
- **Auditing**: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedAt, DeletedBy automatically managed
- **Soft Deletes**: IsDeleted flag (records never hard-deleted)
- **Optimistic Concurrency**: Version field on aggregate roots prevents lost updates
- **Domain Events**: Dispatched automatically on SaveChangesAsync
- **UUID Support**: pgcrypto and uuid-ossp PostgreSQL extensions
- **Encryption**: Encrypted columns for sensitive data (API keys, secrets)

#### ApplicationDbContext
- Implements IApplicationDbContext interface
- DbSets for 20+ entity types
- Database transaction support
- Automatic audit field population
- Domain event dispatch logic

#### Entity Configurations (Fluent API)
Type configurations for:
- BusinessConfiguration, BranchConfiguration, PartyConfiguration
- InvoiceConfiguration, InvoiceItemConfiguration, InvoiceApprovalHistoryConfiguration
- UserConfiguration, PlatformRoleConfiguration, UserRoleAssignmentConfiguration
- SubscriptionConfiguration, SFTPUserConfiguration, FlowRuleConfiguration
- FIRSApiConfigurationConfiguration, BusinessFIRSApiConfigurationConfiguration

#### Repositories
- Generic IRepository<T> interface
- EF Core-based implementations
- Query building and materialization

#### Migrations
- EF Core Code First migrations
- Database schema versioning
- Migration scripts for deployment

#### Technologies
- Entity Framework Core 9.0.8
- Npgsql.EntityFrameworkCore.PostgreSQL 9.0.0
- Microsoft.EntityFrameworkCore.Tools
- EFCore.NamingConventions 9.0.0 (snake_case naming)
- MassTransit 8.5.2 (outbox integration)

---

### 5. Presentation Layer

#### 5.1 Main REST API
**Location**: `src/Presentation/EInvoiceIntegrator.API/`

ASP.NET Core 9 HTTP server with:

**Controllers** (Feature-based organization):
```
Controllers/
├── BaseApiController (Common base with MediatR, AutoMapper, Logger)
├── AuthenticationController (Login, Logout, RefreshToken, TOTP)
├── BusinessController + Partials:
│   ├── BusinessController.cs (CRUD operations)
│   ├── BusinessController.FlowRule.cs (Flow rule management)
│   ├── BusinessController.Onboarding.cs (Business onboarding)
│   ├── BusinessController.Sftp.cs (SFTP user management)
│   └── BusinessController.Subscription.cs (Subscription management)
├── InvoiceController + Partials:
│   ├── InvoiceController.cs (CRUD operations)
│   └── InvoiceController.ApprovalHistory.cs (Approval tracking)
├── FIRSController (FIRS submission, validation)
├── FIRSGetController (FIRS query operations)
├── DashboardAnalyticsController (Metrics, reports)
├── BusinessItemController
├── PartyController
├── ItemCategoryController
└── AccessPointProvidersController
```

**Middleware & Filters**:
```
Middleware/
├── GlobalExceptionHandler (Maps exceptions to HTTP responses)
├── SubscriptionValidationMiddleware (Validates business subscriptions)
└── OpenApiVersionMiddleware (Maintains API version compatibility)

Filters/
├── GlobalExceptionFilter (Secondary exception handling)
├── ValidationFilter (Model validation)
├── ApiResponseFilter (Standardizes response format)
└── [Authorize] attributes for security
```

**API Features**:

1. **API Versioning** (Asp.Versioning 8.1.0)
   - Support for v1 and v2 endpoints
   - URL-based or header-based versioning
   - Backward compatibility

2. **Swagger/OpenAPI** (Swashbuckle 9.0.3)
   - Auto-generated API documentation
   - OpenAPI 3.0.3 compatible (IBM API Connect support)
   - Interactive testing UI

3. **Health Checks**
   - Database connectivity check
   - Redis availability
   - RabbitMQ connectivity
   - External FIRS API connectivity

4. **Security**
   - CORS policy enforcement
   - JWT Bearer authentication
   - HTTPS redirection
   - Rate limiting (per user/IP)
   - Request size limits (500MB for large uploads)
   - Subscription validation

5. **Performance**
   - Response compression (gzip)
   - Response caching
   - Connection keep-alive
   - Async/await throughout

6. **Observability**
   - Serilog structured logging
   - Elasticsearch sink for log aggregation
   - OpenTelemetry for distributed tracing
   - Request/response logging
   - Performance metrics collection

**Standardized Response Model**:
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

**Technologies**:
- ASP.NET Core 9.0
- Asp.Versioning 8.1.0
- Swashbuckle 9.0.3
- Serilog 4.3.0
- Elastic.Serilog.Sinks 9.0.0
- OpenTelemetry 1.12.0
- AspNetCore.HealthChecks 9.0.0

#### 5.2 SaaS API
**Location**: `src/Presentation/EInvoiceIntegratorSaas.API/`

Multi-tenant SaaS-specific deployment with:
- Multi-tenant isolation
- Centralized FIRS credential management
- Subscription-based access control
- Usage metering and billing
- Enhanced audit logging
- Shared infrastructure

#### 5.3 Background Service
**Location**: `src/Presentation/EInvoiceIntegrator.SFTPBackgroundService/`

Async processing with Quartz.NET scheduled jobs:

**Jobs**:

1. **InvoiceTransmissionJob**
   - Monitors invoice_transmission_queue table
   - Retries failed submissions with exponential backoff
   - Updates invoice status
   - Sends completion notifications

2. **SFTPSyncJob**
   - Monitors configured SFTP directories
   - Downloads incoming invoice files
   - Uploads processed results
   - Tracks file processing status

3. **OutboxPublisherJob**
   - Publishes domain events from OutboxEvent table
   - Sends to RabbitMQ via MassTransit
   - Marks events as published
   - Retries failed publishing

4. **ApiUsageTrackingJob**
   - Aggregates daily/monthly API usage
   - Tracks per-business usage
   - Enforces rate limits
   - Alerts on limit violations

**Quartz Configuration**:
- Configurable schedules (cron expressions)
- Job persistence for reliability
- Cluster-aware scheduling (distributed)
- Trigger definitions

**Technologies**:
- Quartz.AspNetCore 3.15.0
- SSH.NET 2025.0.0
- Polly 8.6.2
- MassTransit 8.5.2

---

### 6. Integration Layer

#### 6.1 FIRS Access Point
**Location**: `src/Integrations/EInvoiceIntegrator.FIRSAccessPoint/`

Tenant-agnostic integration with FIRS e-Invoice platform:

**Architecture**:
```
FIRSAccessPoint/
├── Interfaces/
│   ├── IIntegrationService (core abstraction)
│   ├── IFIRSHttpClient (HTTP communication)
│   └── IFIRSTokenProvider (OAuth 2.0 tokens)
├── Services/
│   ├── FIRSHttpClient (HTTP operations)
│   ├── FIRSIntegrationService (orchestration)
│   └── FIRSTokenService (token management)
├── Models/
│   ├── Requests: AuthenticationRequest, ReportInvoiceRequest, ValidateInvoiceDataRequest
│   └── Responses: AuthenticationResponse, ReportInvoiceResponse, ValidationResponse
├── Security/
│   ├── HmacSigner (HMAC-SHA256 signing)
│   ├── DigitalSignatureValidator (response validation)
│   └── CertificateManager (certificate handling)
└── Exceptions/
    ├── FIRSIntegrationException
    ├── AuthenticationException
    └── ValidationException
```

**Features**:
- OAuth 2.0 authentication with FIRS
- Invoice submission with UBL compliance
- Pre-submission validation
- Digital signing (HMAC/ECDSA)
- Error handling with FIRS-specific error codes
- Retry logic for transient failures
- Request/response logging

**Key Interface**:
```csharp
public interface IIntegrationService
{
    Task<string> SendDataAsync(HttpMethod method, string url, string data,
        string apiKey, string apiSecret, CancellationToken cancellationToken);

    Task<T> GetDataAsync<T>(string endpoint, CancellationToken cancellationToken);

    Task<T> GetDataAsync<T>(string endpoint, string apiKey, string apiSecret,
        CancellationToken cancellationToken);

    Task<bool> ValidateConnectionAsync(string apiKey, string apiSecret,
        CancellationToken cancellationToken);
}
```

**Technologies**:
- BouncyCastle.Cryptography 2.6.2
- Microsoft.Extensions.DependencyInjection
- System.ComponentModel.Annotations

#### 6.2 Notification Service
**Location**: `src/Integrations/EInvoiceIntegrator.NotificationService/`

Email and SMS notification delivery:

**Services**:
- `MailKitEmailService`: SMTP-based email via MailKit
- `AwsSesEmailService`: AWS Simple Email Service
- Fallback to alternative providers

**Features**:
- HTML and plain text email support
- Attachment handling
- Template rendering
- Async delivery
- Health checks for SMTP/SES connectivity
- Customizable sender configuration

**Technologies**:
- MailKit 4.13.0
- AWSSDK.SimpleEmail 4.0.1.1
- Microsoft.Extensions.HealthChecks

---

## Key Architectural Patterns

### 1. CQRS (Command Query Responsibility Segregation)

Separates read and write operations:

**Commands** (State-changing operations):
- `CreateInvoiceCommand`
- `SubmitInvoiceCommand`
- `ApproveInvoiceCommand`
- `UpdateBusinessCommand`

**Queries** (Data retrieval):
- `GetInvoiceByIdQuery`
- `GetInvoiceListQuery`
- `GetBusinessByIdQuery`
- `SearchInvoicesQuery`

**MediatR** acts as the dispatcher for both commands and queries, routing them to their respective handlers.

### 2. Domain-Driven Design (DDD)

**Ubiquitous Language**: Business terminology embedded in code (Invoice, IRN, TIN, Approval, Submission)

**Bounded Contexts**: Clear separation between:
- Business Management Context
- Invoice Management Context
- User Management Context
- FIRS Integration Context

**Aggregates**:
- **Invoice Aggregate**: Invoice (root) + InvoiceItems + ApprovalHistory
- **Business Aggregate**: Business (root) + Branches + Users + Subscriptions
- **User Aggregate**: User (root) + Sessions + RefreshTokens

**Value Objects**: Immutable, side-effect-free objects representing domain concepts (TIN, IRN, Address, Currency)

**Domain Events**: Business events that trigger side effects (InvoiceCreatedEvent, InvoiceSubmittedEvent)

### 3. Transactional Outbox Pattern

Ensures reliable event publishing with transactional guarantees:

**Flow**:
1. Domain events are raised when aggregates change state
2. Events are stored in `outbox_events` table **in the same transaction** as aggregate changes
3. Background job (`OutboxPublisherJob`) reads unpublished events
4. Events are published to RabbitMQ via MassTransit
5. Events marked as published in database

**Benefits**:
- Prevents dual-write problem
- Ensures events are never lost
- Guarantees eventual consistency
- Enables reliable messaging

### 4. Repository Pattern

Abstracts data access from business logic:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken);
    Task UpdateAsync(T entity, CancellationToken cancellationToken);
    Task DeleteAsync(T entity, CancellationToken cancellationToken);
}
```

### 5. Mediator Pattern

MediatR decouples controllers from business logic:

**Without MediatR**:
```csharp
// Controller tightly coupled to multiple services
public InvoiceController(IInvoiceService invoiceService,
    IValidationService validationService, IEmailService emailService) { }
```

**With MediatR**:
```csharp
// Controller only depends on MediatR
public InvoiceController(IMediator mediator) { }

// Send command
await _mediator.Send(new CreateInvoiceCommand { ... });
```

### 6. Pipeline Behavior Pattern

Cross-cutting concerns applied to all requests:

```
Request → ValidationBehavior
       → LoggingBehavior
       → PerformanceBehavior
       → TransactionBehavior
       → CachingBehavior
       → Handler
       → Response
```

---

## Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Framework** | .NET 9.0, C# 13, ASP.NET Core 9.0 |
| **Database** | PostgreSQL 15, Entity Framework Core 9.0.8, Npgsql 9.0.0 |
| **Messaging** | RabbitMQ 3.12, MassTransit 8.5.2 |
| **Caching** | Redis 7, StackExchange.Redis 2.7.27 |
| **CQRS** | MediatR 13.0.0, FluentValidation 12.0.0, AutoMapper 12.0.1 |
| **Security** | System.IdentityModel.Tokens.Jwt 8.2.1, BouncyCastle.Cryptography 2.6.2, AspNetCore.Totp 2.3.0 |
| **API** | Swashbuckle.AspNetCore 9.0.3, Asp.Versioning 8.1.0 |
| **Logging** | Serilog 4.3.0, Elastic.Serilog.Sinks 9.0.0, OpenTelemetry 1.12.0 |
| **Resilience** | Polly 8.6.2 |
| **Background Jobs** | Quartz.AspNetCore 3.15.0 |
| **SFTP** | SSH.NET 2025.0.0 |
| **Email** | MailKit 4.13.0, AWSSDK.SimpleEmail 4.0.1.1 |
| **Documents** | QuestPDF 2025.7.1, EPPlus 7.0.0, NPOI 2.5.3, QRCoder 1.7.0 |

---

## Component Interaction Flow

### Example: Invoice Submission to FIRS

```
1. HTTP Request
   POST /api/v1/invoices/{id}/submit

2. Presentation Layer
   InvoiceController receives request
   ↓
   await _mediator.Send(new SubmitInvoiceCommand { InvoiceId = id })

3. MediatR Pipeline Behaviors (executed in order)
   ↓
   ValidationBehavior
   ├─ Validate InvoiceId is not empty
   ├─ Validate user has permission to submit
   └─ FluentValidation rules checked
   ↓
   LoggingBehavior
   ├─ Log request with Serilog
   └─ Log user context (UserId, Email)
   ↓
   PerformanceBehavior
   └─ Start timer to measure execution time
   ↓
   TransactionBehavior
   └─ Begin database transaction

4. Handler: SubmitInvoiceCommandHandler
   ↓
   Load Invoice aggregate from database
   ├─ Check Invoice.Status == Approved (only approved invoices can be submitted)
   ├─ Load Business.FIRSApiConfiguration (API credentials)
   └─ Validate configuration exists and is active
   ↓
   Call FIRSIntegrationService.ProcessInvoiceAsync()
   ├─ Map Invoice entity to ReportInvoiceRequest DTO
   ├─ Include invoice items, amounts, taxes, parties
   ├─ Generate digital signature (HMAC-SHA256)
   ├─ Generate QR code (encrypted IRN + timestamp)
   └─ Build UBL-compliant XML payload
   ↓
   HTTP POST to FIRS API (with Polly resilience)
   ├─ Retry on transient failures (5xx errors)
   │  └─ Exponential backoff: 2s, 4s, 8s
   ├─ Circuit breaker after 3 consecutive failures
   └─ Timeout after 30 seconds
   ↓
   FIRS Response Handling
   ├─ Success (200 OK):
   │  ├─ Extract IRN from response
   │  ├─ Update Invoice.Status = Submitted
   │  ├─ Store Invoice.IRN = response.IRN
   │  ├─ Set Invoice.SubmittedAt = DateTime.UtcNow
   │  └─ Raise InvoiceSubmittedEvent domain event
   │
   └─ Failure (4xx/5xx):
      ├─ Parse FIRS error code and message
      ├─ Log error details
      ├─ Create InvoiceTransmissionQueue entry for retry
      └─ Raise SubmissionFailedEvent domain event

5. Persist Changes
   ↓
   await _dbContext.SaveChangesAsync()
   ├─ EF Core change tracking detects modifications
   ├─ SQL UPDATE executed for Invoice table
   ├─ Audit fields updated (UpdatedBy, UpdatedAt)
   ├─ Domain events dispatched to handlers
   ├─ OutboxEvent created with serialized InvoiceSubmittedEvent
   └─ Transaction committed

6. Response
   ↓
   Handler returns SubmitInvoiceResult { Success = true, IRN = "..." }
   ├─ ApiResponseFilter wraps in ApiResponse<SubmitInvoiceResult>
   ├─ JSON serialized
   └─ HTTP 200 OK returned to client

7. Background Processing
   ↓
   OutboxPublisherJob (Quartz.NET scheduled task)
   ├─ Query outbox_events WHERE is_published = false
   ├─ Deserialize InvoiceSubmittedEvent
   ├─ Publish to RabbitMQ via MassTransit
   ├─ Update outbox_events SET is_published = true
   └─ Commit transaction
   ↓
   RabbitMQ Message Bus
   ├─ Route InvoiceSubmittedEvent to subscribers
   │
   ├─ Subscriber: SendInvoiceSubmittedEmailService
   │  ├─ Render email template with invoice details
   │  ├─ Send via MailKit SMTP or AWS SES
   │  └─ Log email sent
   │
   ├─ Subscriber: UpdateDashboardAnalyticsService
   │  ├─ Increment submitted_invoices_count
   │  ├─ Update monthly submission metrics
   │  └─ Update cache (Redis)
   │
   └─ Subscriber: AuditLogService
      ├─ Record submission in audit log
      └─ Store event details for compliance
```

### Example: Authentication Flow

```
1. Login Request
   POST /api/v1/auth/login
   {
     "email": "user@example.com",
     "password": "SecurePassword123!",
     "totpCode": "123456"
   }

2. AuthenticationController → MediatR
   await _mediator.Send(new LoginCommand { Email, Password, TotpCode })

3. LoginCommandHandler
   ↓
   Load User by email from database
   ├─ Check user exists (throw if not found)
   ├─ Check user is active (throw if deactivated)
   └─ Check user is not locked out
   ↓
   Verify password hash
   ├─ Use bcrypt to compare provided password with stored hash
   └─ Throw UnauthorizedException if mismatch
   ↓
   Check 2FA requirement
   ├─ If User.TotpEnabled == true
   │  ├─ Verify TOTP code using ITotpService
   │  └─ Throw InvalidTotpException if invalid
   └─ Skip if 2FA not enabled
   ↓
   Generate JWT tokens (IJwtTokenService)
   ├─ Create JWT claims:
   │  ├─ sub: User.Id
   │  ├─ email: User.Email
   │  ├─ name: User.FullName
   │  └─ roles: ["BusinessOwner", "Manager"]
   │
   ├─ Generate Access Token
   │  ├─ Sign with HS256 using secret key
   │  ├─ Set expiry: UtcNow + 30 minutes
   │  └─ Token: "eyJhbGciOiJIUzI1NiIs..."
   │
   └─ Generate Refresh Token
      ├─ Create cryptographically random bytes
      ├─ Set expiry: UtcNow + 7 days
      ├─ Store in RefreshToken table
      └─ Token: "550e8400-e29b-41d4..."
   ↓
   Update user session tracking
   ├─ Create UserSession record
   ├─ Store IP address, user agent, login timestamp
   └─ Soft-delete expired sessions
   ↓
   Return LoginResult
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIs...",
     "refreshToken": "550e8400-e29b-41d4...",
     "expiresIn": 1800,
     "user": { ... }
   }

4. Subsequent Authenticated Requests
   ↓
   Client sends: Authorization: Bearer {accessToken}
   ↓
   ASP.NET Core Authentication Middleware
   ├─ Extract token from Authorization header
   ├─ Validate token signature with secret key
   ├─ Check expiry (reject if expired)
   ├─ Extract claims (sub, email, roles)
   └─ Create ClaimsPrincipal
   ↓
   ICurrentUserService
   ├─ Read User.Id from ClaimsPrincipal
   ├─ Read User.Email from claims
   ├─ Read User.Roles from claims
   └─ Provide to handlers for authorization checks
   ↓
   Authorization Checks in Handlers
   ├─ [Authorize] attribute: Requires authentication
   ├─ [Authorize(Roles = "BusinessOwner")]: Requires specific role
   └─ Custom authorization: Business ownership validation
```

---

## Database Architecture

### PostgreSQL Schema

#### Core Business Tables
- **businesses**: Multi-tenant business entities
  - Columns: id, tin, name, email, phone, address, status, subscription_id, created_at, version

- **branches**: Business office locations
  - Columns: id, business_id, name, address, is_headquarters, created_at

- **users**: Platform users
  - Columns: id, email, password_hash, first_name, last_name, totp_enabled, totp_secret, is_active, created_at

- **parties**: Suppliers and buyers
  - Columns: id, business_id, tin, name, address, type (Supplier/Buyer), created_at

- **platform_roles**: Defined roles
  - Roles: SystemAdministrator, BusinessOwner, BusinessManager, Operator, Viewer

- **user_role_assignments**: User-to-role mappings
  - Columns: id, user_id, role_id, business_id, assigned_at

#### Invoice Management Tables
- **invoices**: Created invoices
  - Columns: id, business_id, irn, invoice_number, invoice_date, total_amount, currency, status, submitted_at, created_at, version

- **invoice_items**: Line items
  - Columns: id, invoice_id, description, quantity, unit_price, tax_amount, total_amount, created_at

- **invoice_approval_histories**: Audit trail
  - Columns: id, invoice_id, approver_id, action (Approved/Rejected), comments, approved_at

- **received_invoices**: Invoices received from suppliers
  - Columns: id, business_id, supplier_irn, invoice_data, received_at

#### Business Configuration Tables
- **subscriptions**: Business subscription plans
  - Columns: id, plan_name, api_rate_limit, max_invoices_per_month, price, is_active

- **subscription_keys**: API keys for subscription access
  - Columns: id, subscription_id, api_key, is_active, expires_at

- **firs_api_configurations**: Global FIRS setup
  - Columns: id, environment (Production/Sandbox), base_url, api_key_encrypted, api_secret_encrypted, is_active

- **business_firs_api_configurations**: Business-specific FIRS setup
  - Columns: id, business_id, firs_config_id, service_id, api_key_encrypted, certificate_encrypted, is_approved

- **flow_rules**: Custom business logic
  - Columns: id, business_id, rule_name, condition_json, action_json, is_active

- **sftp_users**: SFTP credentials
  - Columns: id, business_id, username, password_encrypted, directory_path, is_active

#### System Tables
- **outbox_events**: Transactional outbox
  - Columns: id, event_type, event_data, is_published, published_at, created_at

- **invoice_transmission_queue**: Pending FIRS submissions
  - Columns: id, invoice_id, retry_count, last_error, status, created_at

- **api_usage_tracking**: Real-time API usage
  - Columns: id, business_id, endpoint, request_count, timestamp

- **api_usage_summaries**: Aggregated usage
  - Columns: id, business_id, period_start, period_end, total_requests, total_invoices_submitted

- **integration_logs**: FIRS API call logs
  - Columns: id, business_id, request_payload, response_payload, status_code, logged_at

- **system_configurations**: Platform-wide settings
  - Columns: id, key, value, description, updated_at

### Database Features

#### UUID Primary Keys
All tables use UUID (GUID) as primary keys via PostgreSQL `uuid-ossp` extension:
```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
ALTER TABLE invoices ALTER COLUMN id SET DEFAULT uuid_generate_v4();
```

#### Soft Deletes
Records are never physically deleted, marked as deleted instead:
```sql
-- All tables have:
is_deleted BOOLEAN DEFAULT FALSE
deleted_at TIMESTAMP NULL
deleted_by UUID NULL
```

#### Audit Columns
Automatic tracking of create/update operations:
```sql
-- All tables have:
created_by UUID NOT NULL
created_at TIMESTAMP NOT NULL DEFAULT NOW()
updated_by UUID NULL
updated_at TIMESTAMP NULL
```

#### Optimistic Concurrency
Aggregate roots have version field to prevent lost updates:
```sql
-- Aggregate root tables have:
version INTEGER NOT NULL DEFAULT 1
```

EF Core automatically increments version on update and throws `DbUpdateConcurrencyException` if version mismatch.

#### Encrypted Sensitive Columns
Sensitive data encrypted at rest using AES-256-GCM:
```sql
api_key_encrypted BYTEA NOT NULL
api_secret_encrypted BYTEA NOT NULL
certificate_encrypted BYTEA NULL
```

#### Snake Case Naming Convention
All table and column names use snake_case via `EFCore.NamingConventions`:
- Entity: `BusinessFIRSApiConfiguration`
- Table: `business_firs_api_configurations`
- Column: `api_key_encrypted`

---

## Security Architecture

### Authentication

#### JWT Bearer Tokens
- **Access Tokens**: Short-lived (15-30 minutes), HS256/RS256 signed
- **Refresh Tokens**: Long-lived (7-30 days), stored in database
- **Token Claims**: sub (userId), email, name, roles, permissions

#### TOTP 2-Factor Authentication
- Time-based One-Time Password (TOTP) via `AspNetCore.Totp`
- QR code generation for authenticator apps (Google Authenticator, Authy)
- 6-digit codes with 30-second expiry
- Recovery codes for account recovery

#### Password Security
- Bcrypt password hashing (cost factor: 12)
- Minimum password requirements: 8 characters, uppercase, lowercase, number, special character
- Password history tracking (prevent reuse of last 5 passwords)
- Account lockout after 5 failed login attempts

### Authorization

#### Role-Based Access Control (RBAC)
Hierarchical roles with different privilege levels:

**System Level**:
- **SystemAdministrator**: Full platform access, manage all businesses, system configuration

**Business Level** (scoped to specific business):
- **BusinessOwner**: Full business access, manage users, subscriptions, FIRS configuration
- **BusinessManager**: Manage invoices, approve submissions, view reports
- **Operator**: Create and update invoices, view business data
- **Viewer**: Read-only access to invoices and reports

#### Permission-Based Authorization
Fine-grained permissions beyond roles:
- `invoices.create`
- `invoices.submit`
- `invoices.approve`
- `business.manage_users`
- `business.manage_subscription`
- `firs.manage_configuration`

### Encryption

#### Data at Rest
- **AES-256-GCM** encryption for sensitive fields:
  - FIRS API keys and secrets
  - SFTP passwords
  - Digital certificates
  - Encryption keys stored in Azure Key Vault or AWS Secrets Manager

#### Data in Transit
- **TLS 1.2+** for all HTTP traffic
- **HTTPS** enforced via `UseHttpsRedirection()` middleware
- **HSTS** (HTTP Strict Transport Security) enabled

#### Digital Signatures
- **HMAC-SHA256** for FIRS API request signing
- **ECDSA** for invoice digital signatures (optional)
- Certificate-based signing for enhanced security

### API Security

#### CORS (Cross-Origin Resource Sharing)
Configurable CORS policy:
```csharp
services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", builder =>
    {
        builder.WithOrigins("https://app.example.com")
               .WithMethods("GET", "POST", "PUT", "DELETE")
               .WithHeaders("Authorization", "Content-Type")
               .AllowCredentials();
    });
});
```

#### Rate Limiting
Per-user and per-IP rate limiting:
- **SaaS Mode**: 1000 requests/hour per business
- **On-Premise Mode**: 500 requests/hour per business
- Configurable limits per subscription plan
- 429 Too Many Requests response when exceeded

#### Request Size Limits
- Default: 500MB max request size (for large Excel uploads)
- Configurable per endpoint
- Protection against memory exhaustion attacks

#### Input Validation
- **FluentValidation** rules on all commands
- Model binding validation
- SQL injection prevention via parameterized queries (EF Core)
- XSS prevention via input sanitization

### Subscription Validation

#### Middleware: `SubscriptionValidationMiddleware`
Validates business subscription status on every request:
1. Extract business ID from request context
2. Check subscription status (Active, Expired, Suspended)
3. Validate API usage limits (rate limiting)
4. Check invoice submission quotas
5. Reject request if subscription invalid (402 Payment Required)

### Deployment Mode Security

#### SaaS Mode
- KPMG manages all FIRS credentials (stored in central vault)
- Businesses cannot access raw API keys
- Higher security assurance
- Centralized key rotation

#### On-Premise Mode
- Customers provide their own FIRS credentials
- KPMG approval required for configuration
- Encrypted storage in customer database
- Customer responsible for key rotation

### Audit Logging

#### Request/Response Logging
All API requests logged with:
- User ID and email
- IP address and user agent
- Request path and method
- Request/response payloads (sensitive data masked)
- Timestamp and duration

#### Integration Logs
All FIRS API calls logged in `integration_logs` table:
- Request payload (invoice data)
- Response payload (IRN, error codes)
- HTTP status code
- Timestamp

#### Domain Event Audit
All domain events stored in `outbox_events`:
- Event type (InvoiceCreatedEvent, InvoiceSubmittedEvent)
- Event data (serialized JSON)
- Timestamp and user context

---

## Deployment Modes

### 1. SaaS Mode
**Target**: Multi-tenant SaaS deployment managed by KPMG

**Characteristics**:
- Centralized FIRS credential management
- KPMG stores all API keys in Azure Key Vault
- Businesses identified by `business_id` (tenant isolation)
- Shared infrastructure (database, Redis, RabbitMQ)
- Higher usage limits (1000 req/hour, unlimited invoices/month)
- Automatic updates and maintenance
- Subscription-based pricing

**Configuration**:
```json
{
  "DeploymentMode": "SaaS",
  "TenantIsolation": "RowLevelSecurity",
  "FIRSCredentialManagement": "Centralized",
  "UsageLimits": {
    "RequestsPerHour": 1000,
    "InvoicesPerMonth": -1
  }
}
```

**API**: `EInvoiceIntegratorSaas.API`

### 2. On-Premise Mode
**Target**: Single-tenant deployment on customer infrastructure

**Characteristics**:
- Customer-managed FIRS credentials
- KPMG approval required for FIRS configuration
- Dedicated infrastructure (customer-owned servers)
- Lower usage limits (500 req/hour, 1000 invoices/month)
- Customer responsible for updates and maintenance
- One-time licensing fee

**Configuration**:
```json
{
  "DeploymentMode": "OnPremise",
  "TenantIsolation": "None",
  "FIRSCredentialManagement": "CustomerManaged",
  "ApprovalRequired": true,
  "UsageLimits": {
    "RequestsPerHour": 500,
    "InvoicesPerMonth": 1000
  }
}
```

**API**: `EInvoiceIntegrator.API`

### 3. Hybrid Mode
**Target**: On-premise deployment with cloud integration

**Characteristics**:
- Customer infrastructure with cloud backup
- Optional KPMG-managed FIRS credentials
- Hybrid authentication (local + Azure AD)
- Configurable usage limits
- Sync to cloud for analytics and reporting

---

## Scalability & Performance

### Horizontal Scaling

#### Stateless API Servers
- Multiple API instances behind load balancer
- No in-memory session state (JWT tokens)
- Distributed cache for shared state (Redis)

#### Database Read Replicas
- PostgreSQL read replicas for query scaling
- Write operations to primary instance
- Read operations to replicas (eventual consistency)

#### Message Queue Scaling
- RabbitMQ cluster for high availability
- Multiple consumers for invoice transmission queue
- Parallel processing of domain events

### Caching Strategy

#### Redis Distributed Cache
**Cached Data**:
- User permissions and roles (TTL: 15 minutes)
- Business configuration (TTL: 30 minutes)
- Query results for dashboard analytics (TTL: 5 minutes)
- FIRS API tokens (TTL: token expiry - 5 minutes)

**Cache Invalidation**:
- Time-based expiration (TTL)
- Event-based invalidation (on business update, invalidate cache)
- Manual cache clear endpoint for admins

#### Response Caching
HTTP response caching for static data:
```csharp
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "businessId" })]
public async Task<IActionResult> GetBusinessById(Guid businessId)
```

### Database Optimization

#### Indexes
Critical indexes for performance:
```sql
-- Invoice queries
CREATE INDEX idx_invoices_business_id ON invoices(business_id);
CREATE INDEX idx_invoices_status ON invoices(status);
CREATE INDEX idx_invoices_created_at ON invoices(created_at DESC);
CREATE INDEX idx_invoices_irn ON invoices(irn);

-- User queries
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_user_role_assignments_user_id ON user_role_assignments(user_id);

-- Outbox queries
CREATE INDEX idx_outbox_events_is_published ON outbox_events(is_published, created_at);
```

#### Connection Pooling
EF Core connection pooling configuration:
```csharp
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.MinBatchSize(2);
    npgsqlOptions.MaxBatchSize(100);
    npgsqlOptions.CommandTimeout(30);
});
```

### Async/Await Throughout
All I/O operations are asynchronous:
- Database queries: `await _dbContext.Invoices.ToListAsync()`
- HTTP calls: `await _httpClient.PostAsync()`
- File I/O: `await File.ReadAllTextAsync()`

### Background Processing
Long-running tasks offloaded to background jobs:
- Invoice transmission to FIRS
- SFTP file synchronization
- Event publishing from outbox
- API usage aggregation

---

## Observability

### Structured Logging (Serilog)

#### Log Levels
- **Verbose**: Detailed diagnostic information
- **Debug**: Internal system events
- **Information**: General application flow
- **Warning**: Abnormal or unexpected events
- **Error**: Error events with stack traces
- **Fatal**: Critical failures requiring immediate attention

#### Log Enrichment
Logs enriched with contextual information:
```csharp
Log.ForContext("UserId", userId)
   .ForContext("BusinessId", businessId)
   .ForContext("RequestId", requestId)
   .Information("Invoice {InvoiceId} submitted to FIRS", invoiceId);
```

#### Log Sinks
- **Console**: Development environment
- **File**: Rolling log files (1 file per day, retain 30 days)
- **Elasticsearch**: Centralized log aggregation and search
- **Application Insights**: Azure monitoring (SaaS mode)

### Distributed Tracing (OpenTelemetry)

#### Instrumentation
Auto-instrumentation for:
- ASP.NET Core (HTTP requests)
- Entity Framework Core (database queries)
- HttpClient (external API calls)
- MassTransit (message publishing)

#### Trace Context Propagation
W3C Trace Context standard:
```
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
```

Traces span across:
1. API request → Handler → Database query
2. API request → FIRS integration → FIRS API
3. Domain event → RabbitMQ → Event subscriber

### Health Checks

#### Health Check Endpoints
- `/health`: Summary (Healthy/Unhealthy)
- `/health/ready`: Readiness check (for Kubernetes)
- `/health/live`: Liveness check

#### Monitored Components
- **Database**: PostgreSQL connectivity
- **Cache**: Redis availability
- **Message Queue**: RabbitMQ connectivity
- **FIRS API**: External API reachability
- **SFTP**: SFTP server connectivity

#### Health Check UI
AspNetCore.HealthChecks.UI dashboard:
- Visual status indicators
- Historical health data
- Webhook notifications on failure

### Performance Monitoring

#### Performance Behavior
Logs slow requests (> 500ms):
```csharp
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, ...)
    {
        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        if (timer.ElapsedMilliseconds > 500)
        {
            Log.Warning("Long Running Request: {Name} ({ElapsedMilliseconds} ms)",
                typeof(TRequest).Name, timer.ElapsedMilliseconds);
        }

        return response;
    }
}
```

#### Metrics Collection
OpenTelemetry metrics:
- Request count per endpoint
- Request duration (P50, P95, P99)
- Error rate
- Database query count and duration
- Cache hit/miss ratio

---

## Testing Strategy

### Unit Tests
**Location**: `tests/EInvoiceIntegrator.UnitTests/`

Test coverage:
- Domain entities and value objects
- Command/query handlers
- Validators (FluentValidation)
- Domain events
- Services (encryption, JWT, TOTP)

**Frameworks**:
- xUnit
- Moq (mocking)
- FluentAssertions (assertions)

### Integration Tests
**Location**: `tests/EInvoiceIntegrator.IntegrationTests/`

Test coverage:
- Database operations (EF Core)
- API endpoints (WebApplicationFactory)
- FIRS integration
- Email service
- SFTP operations

**Test Database**:
- PostgreSQL test container (Testcontainers)
- Database migrations applied automatically
- Test data seeded before each test

### End-to-End Tests
**Location**: `tests/EInvoiceIntegrator.E2ETests/`

Test scenarios:
- Complete invoice submission flow (create → approve → submit → FIRS)
- Authentication flow (login → 2FA → refresh token)
- Business onboarding flow
- SFTP file processing flow

---

## Summary

The **EInvoice Integrator** demonstrates enterprise-grade software architecture with:

✅ **Clean Architecture**: Clear separation of concerns across layers
✅ **Domain-Driven Design**: Rich domain models with business logic
✅ **CQRS**: Separation of read and write operations
✅ **Event-Driven**: Reliable event publishing with outbox pattern
✅ **Scalability**: Horizontal scaling, caching, async processing
✅ **Security**: Encryption, JWT, 2FA, RBAC, audit logging
✅ **Resilience**: Retry logic, circuit breakers, health checks
✅ **Observability**: Structured logging, distributed tracing, metrics
✅ **Maintainability**: Feature-based organization, dependency injection, testability

The system is **production-ready** with comprehensive error handling, database migrations, containerization (Docker), API documentation (Swagger/OpenAPI), and multi-deployment support (SaaS + On-Premise).

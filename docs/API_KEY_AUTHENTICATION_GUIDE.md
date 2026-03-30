# API Key Authentication and ICurrentUserService

## Overview

The **EInvoiceIntegratorSaas.API** uses API Key authentication instead of JWT tokens. This document explains how `ICurrentUserService` is populated when using API Key authentication.

## Authentication Flow

```
┌────────────┐
│   Client   │
└─────┬──────┘
      │ X-API-Key: sk_live_xxxxx
      ▼
┌─────────────────────────────────────┐
│  ApiKeyAuthenticationHandler        │
│  (Authentication Middleware)        │
└─────────┬───────────────────────────┘
          │ Extract API Key from header/query
          ▼
┌─────────────────────────────────────┐
│  ApiKeyAuthenticationService        │
│  ValidateApiKeyAsync()              │
└─────────┬───────────────────────────┘
          │ 1. Find Business by API Key
          │ 2. Validate subscription
          │ 3. Create Claims
          ▼
┌─────────────────────────────────────┐
│  ClaimsPrincipal Created            │
│  With Business Claims               │
└─────────┬───────────────────────────┘
          │ Set HttpContext.User
          ▼
┌─────────────────────────────────────┐
│  CurrentUserService                 │
│  Reads Claims from HttpContext      │
└─────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│  ICurrentUserService Properties     │
│  UserId, Email, BusinessId, etc.    │
└─────────────────────────────────────┘
```

## Claims Mapping

When a valid API Key is authenticated, the following claims are created:

### Standard Identity Claims

| Claim Type | Value | ICurrentUserService Property |
|------------|-------|------------------------------|
| `ClaimTypes.NameIdentifier` | `Business.Id` (Guid) | `UserId` |
| `ClaimTypes.Name` | `Business.Name` | `UserName` |
| `ClaimTypes.Email` | `Business.ContactEmail` | `Email` |
| `ClaimTypes.Role` | `"ApiClient"` | `Roles` |

### Business-Specific Claims

| Claim Type | Value | ICurrentUserService Property |
|------------|-------|------------------------------|
| `"businessId"` | `Business.Id` (Guid) | `BusinessId` |
| `"BusinessId"` | `Business.Id` (Guid) | - |
| `"BusinessName"` | `Business.Name` | - |

### Level Claims

| Claim Type | Value | ICurrentUserService Property |
|------------|-------|------------------------------|
| `"isBusinessLevel"` | `"true"` | `IsBusinessLevel` |
| `"isBranchLevel"` | `"false"` | `IsBranchLevel` |

### KPMG Claims

| Claim Type | Value | ICurrentUserService Property |
|------------|-------|------------------------------|
| `"isKpmgUser"` | `"false"` | `IsKpmgUser` |
| `"kpmgRole"` | Not set | `KpmgRole` (null) |
| `"kpmgEmployeeId"` | Not set | `KpmgEmployeeId` (null) |
| `"kpmgDepartment"` | Not set | `KpmgDepartment` (null) |

### Permission Claims

| Claim Type | Value | ICurrentUserService Property |
|------------|-------|------------------------------|
| `"permission"` | `"api.read"` | `Permissions` |
| `"permission"` | `"api.write"` | `Permissions` |
| `"permission"` | `"invoice.create"` | `Permissions` |
| `"permission"` | `"invoice.read"` | `Permissions` |
| `"permission"` | `"invoice.update"` | `Permissions` |
| `"permission"` | `"invoice.transmit"` | `Permissions` |

### Additional Claims

| Claim Type | Value | Purpose |
|------------|-------|---------|
| `"SubscriptionTier"` | `SubscriptionTier.ToString()` | Track subscription level |
| `"ApiKey"` | `apiKey.Substring(0, 10) + "..."` | Audit trail (truncated) |

## ICurrentUserService Properties

When using API Key authentication, `ICurrentUserService` provides:

```csharp
// Identity
UserId          → Business.Id (as a user surrogate)
UserName        → Business.Name
Email           → Business.ContactEmail
IsAuthenticated → true

// Business Context
BusinessId      → Business.Id
BranchId        → null (API clients are business-level)
IsBusinessLevel → true
IsBranchLevel   → false

// Roles & Permissions
Roles           → ["ApiClient"]
Permissions     → ["api.read", "api.write", "invoice.create", etc.]

// KPMG Flags
IsKpmgUser      → false
IsPlatformAdmin → false
KpmgRole        → null
KpmgEmployeeId  → null
KpmgDepartment  → null
```

## Code Example: Using ICurrentUserService

### In Controllers

```csharp
public class InvoiceController : BaseApiController
{
    private readonly ICurrentUserService _currentUser;

    public InvoiceController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest request)
    {
        // Get the authenticated business ID
        var businessId = _currentUser.BusinessId; // Returns Business.Id from API Key

        // Check if user has permission
        if (!_currentUser.HasPermission("invoice.create"))
        {
            return Forbid();
        }

        // The business making the API call
        _logger.LogInformation(
            "Business {BusinessName} ({BusinessId}) creating invoice via API",
            _currentUser.UserName,
            businessId
        );

        // Use businessId for business-scoped operations
        var invoice = await CreateInvoiceForBusiness(businessId, request);

        return Ok(invoice);
    }
}
```

### In Command Handlers

```csharp
public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public async Task<Result> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        // Automatically get business context from API Key
        var businessId = _currentUser.BusinessId
            ?? throw new UnauthorizedException("Business context not found");

        // Ensure the invoice belongs to the authenticated business
        var invoice = new Invoice
        {
            BusinessId = businessId,
            // ... other properties
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
```

### Authorization Checks

```csharp
// Check if API client
if (_currentUser.HasRole("ApiClient"))
{
    // Special handling for API clients
}

// Check permissions
if (!_currentUser.HasPermission("invoice.transmit"))
{
    throw new ForbiddenException("API key does not have transmit permission");
}

// Ensure business-level access
if (!_currentUser.IsBusinessLevel)
{
    throw new UnauthorizedException("API keys must be business-level");
}
```

## API Key Authentication Handler

The authentication is handled by [ApiKeyAuthenticationHandler.cs](../src/Presentation/EInvoiceIntegratorSaas.API/Authentication/ApiKeyAuthenticationHandler.cs):

1. **Extract API Key**
   - From `X-API-Key` header (preferred)
   - From `api_key` query parameter (fallback)

2. **Validate API Key**
   - Calls `ApiKeyAuthenticationService.ValidateApiKeyAsync()`
   - Returns `ApiKeyValidationResult` with claims

3. **Create Authentication Ticket**
   - Creates `ClaimsIdentity` with business claims
   - Creates `ClaimsPrincipal`
   - Sets `HttpContext.User`

4. **Store Context Items**
   - `Context.Items["BusinessId"]` - For direct access
   - `Context.Items["RateLimitTier"]` - For rate limiting

## API Key Validation Service

The validation logic is in [ApiKeyAuthenticationService.cs](../src/Core/EInvoiceIntegrator.Application/Services/Authentication/ApiKeyAuthenticationService.cs):

### Validation Steps

1. **Find Business**
   ```csharp
   var business = await _dbContext.Businesses
       .Include(b => b.Subscription)
           .ThenInclude(s => s!.PlatformSubscription)
       .FirstOrDefaultAsync(b => b.ApiKey == apiKey && b.IsApiKeyActive);
   ```

2. **Check Business Status**
   - Must be `Active`

3. **Check Subscription**
   - Must be active and not expired
   - Must have `ApiOnly` or `SaaS` tier

4. **Create Claims**
   - Populate all claims for ICurrentUserService
   - Add API-specific permissions

5. **Return Result**
   - Success with claims
   - Or failure with error message

## Differences from JWT Authentication

| Aspect | JWT (Main API) | API Key (SaaS API) |
|--------|----------------|---------------------|
| **Authentication** | Bearer token | X-API-Key header |
| **UserId** | Actual user ID | Business ID (surrogate) |
| **UserName** | User's name | Business name |
| **Email** | User's email | Business contact email |
| **Roles** | User roles | "ApiClient" |
| **BranchId** | User's branch | null (business-level) |
| **Expiration** | Token TTL | Subscription expiry |
| **Revocation** | Token blacklist | API key active flag |

## Security Considerations

### API Key Storage

- API keys are stored in plain text in the database (for validation)
- Consider hashing API keys like passwords in production
- Use secure transmission (HTTPS only)

### Rate Limiting

- API keys are rate-limited based on subscription tier
- Rate limit tier is stored in `HttpContext.Items["RateLimitTier"]`
- Configure in rate limiting middleware

### Permissions

- API clients have predefined permissions
- Cannot escalate permissions beyond API key scope
- All operations are business-scoped

### Audit Trail

- API key prefix is logged (first 10 characters)
- Full key is never logged
- Business ID is logged for audit

## Testing

### Example API Request

```bash
# Using header (recommended)
curl -X POST https://api.example.com/api/v1/invoices \
  -H "X-API-Key: sk_live_abcdef1234567890" \
  -H "Content-Type: application/json" \
  -d '{"invoiceNumber": "INV-001", ...}'

# Using query parameter (fallback)
curl -X POST "https://api.example.com/api/v1/invoices?api_key=sk_live_abcdef1234567890" \
  -H "Content-Type: application/json" \
  -d '{"invoiceNumber": "INV-001", ...}'
```

### Expected ICurrentUserService Values

```csharp
// After successful authentication
UserId          = Guid("business-guid-here")
UserName        = "Acme Corporation"
Email           = "contact@acmecorp.com"
IsAuthenticated = true
BusinessId      = Guid("business-guid-here")
Roles           = ["ApiClient"]
HasRole("ApiClient") = true
HasPermission("invoice.create") = true
IsBusinessLevel = true
IsKpmgUser      = false
```

## Troubleshooting

### ICurrentUserService Returns Null Values

**Problem**: Properties like `UserId`, `BusinessId` are null

**Solution**:
- Verify API key is valid and active
- Check that claims are being created in `ApiKeyAuthenticationService.ValidateApiKeyAsync()`
- Ensure `ApiKeyAuthenticationHandler` is setting `HttpContext.User`

### Authentication Fails

**Problem**: 401 Unauthorized

**Causes**:
- Invalid API key
- Business is inactive
- Subscription expired
- Subscription tier doesn't include API access

**Solution**: Check validation result error message

### Missing Permissions

**Problem**: `HasPermission()` returns false

**Solution**: Add missing permission claims in `ApiKeyAuthenticationService` (lines 130-136)

### BusinessId Mismatch

**Problem**: `ICurrentUserService.BusinessId` doesn't match API key's business

**Solution**: Ensure lowercase `"businessId"` claim is set (line 113 in ApiKeyAuthenticationService)

## Related Files

- [ICurrentUserService.cs](../src/Core/EInvoiceIntegrator.Application/Common/Interfaces/ICurrentUserService.cs)
- [CurrentUserService.cs](../src/Infrastructure/EInvoiceIntegrator.Infrastructure/Services/CurrentUserService.cs)
- [ApiKeyAuthenticationHandler.cs](../src/Presentation/EInvoiceIntegratorSaas.API/Authentication/ApiKeyAuthenticationHandler.cs)
- [ApiKeyAuthenticationService.cs](../src/Core/EInvoiceIntegrator.Application/Services/Authentication/ApiKeyAuthenticationService.cs)
- [Business.cs](../src/Core/EInvoiceIntegrator.Domain/Entities/BusinessManagement/Business.cs)

## Summary

For the SaaS API with API Key authentication:

1. ✅ **API Key is validated** against Business entity
2. ✅ **Claims are created** with business information
3. ✅ **ClaimsPrincipal is set** on HttpContext
4. ✅ **ICurrentUserService reads claims** from HttpContext.User
5. ✅ **Business ID** is available via `ICurrentUserService.BusinessId`
6. ✅ **All properties populated** except KPMG-specific ones
7. ✅ **Permissions** are predefined for API clients

The key insight: **The Business acts as the "user"** in API Key authentication, so `UserId` and `BusinessId` are the same value.

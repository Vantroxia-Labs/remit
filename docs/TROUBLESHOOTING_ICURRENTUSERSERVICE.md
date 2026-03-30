# Troubleshooting ICurrentUserService with API Key Authentication

## Issue

`ICurrentUserService` properties are returning null or empty values when using API Key authentication in the SaaS API.

## What We've Done

### ✅ 1. Updated ApiKeyAuthenticationService

**File**: `src/Core/EInvoiceIntegrator.Application/Services/Authentication/ApiKeyAuthenticationService.cs`

**Lines 103-137**: Added comprehensive claims that map to all `ICurrentUserService` properties:

```csharp
var claims = new List<Claim>
{
    // Standard identity claims
    new Claim(ClaimTypes.NameIdentifier, business.Id.ToString()), // → UserId
    new Claim(ClaimTypes.Name, business.Name),                    // → UserName
    new Claim(ClaimTypes.Email, business.ContactEmail),           // → Email
    new Claim(ClaimTypes.Role, "ApiClient"),                      // → Roles

    // Business claims
    new Claim("businessId", business.Id.ToString()),              // → BusinessId
    new Claim("isBusinessLevel", "true"),                         // → IsBusinessLevel
    new Claim("isBranchLevel", "false"),                          // → IsBranchLevel
    new Claim("isKpmgUser", "false"),                             // → IsKpmgUser

    // Permissions
    new Claim("permission", "api.read"),                          // → Permissions
    new Claim("permission", "api.write"),
    // ... more permissions
};
```

### ✅ 2. Verified Service Registration

**File**: `src/Infrastructure/EInvoiceIntegrator.Infrastructure/DependencyInjection.cs`

**Line 31**: `ICurrentUserService` is registered as Scoped:

```csharp
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

**File**: `src/Presentation/EInvoiceIntegratorSaas.API/Program.cs`

**Line 69**: Infrastructure services (including ICurrentUserService) are registered:

```csharp
builder.Services.AddInfrastructureServices(builder.Configuration);
```

### ✅ 3. Created Diagnostics Endpoint

**File**: `src/Presentation/EInvoiceIntegratorSaas.API/Controllers/DiagnosticsController.cs`

Three diagnostic endpoints to help troubleshoot:

1. **GET /api/v1/diagnostics/current-user** - Full user context
2. **GET /api/v1/diagnostics/claims** - All claims details
3. **GET /api/v1/diagnostics/auth-status** - Authentication status

## How to Debug

### Step 1: Start the SaaS API

```bash
dotnet run --project src/Presentation/EInvoiceIntegratorSaas.API/EInvoiceIntegratorSaas.API.csproj
```

### Step 2: Test with a Valid API Key

Make sure you have:
1. A business in the database
2. An active subscription (ApiOnly or SaaS tier)
3. A generated API key for that business

### Step 3: Call the Diagnostics Endpoint

```bash
# Replace with your actual API key
curl -X GET "https://localhost:5002/api/v1/diagnostics/current-user" \
  -H "X-API-Key: sk_live_your_api_key_here" \
  -k

# Or with query parameter
curl -X GET "https://localhost:5002/api/v1/diagnostics/current-user?api_key=sk_live_your_api_key_here" \
  -k
```

### Step 4: Analyze the Response

The response will show:

```json
{
  "success": true,
  "message": "User context retrieved successfully",
  "data": {
    "currentUserService": {
      "userId": "guid-here-or-null",
      "userName": "Business Name or null",
      "email": "email@example.com or null",
      "isAuthenticated": true,
      "businessId": "guid-here-or-null",
      "roles": ["ApiClient"],
      "permissions": ["api.read", "api.write", ...]
    },
    "claims": [
      {
        "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "value": "business-guid"
      },
      ...
    ],
    "authentication": {
      "isAuthenticated": true,
      "authenticationType": "ApiKey"
    }
  }
}
```

## Common Issues and Solutions

### Issue 1: All ICurrentUserService Properties Are Null

**Symptom**: `userId`, `businessId`, `email`, etc. are all null

**Possible Causes**:
1. API key is not being sent in the request
2. API key is invalid or inactive
3. Claims are not being created
4. HttpContext.User is not being set

**Debugging**:

```bash
# 1. Check if authentication is working
curl -X GET "https://localhost:5002/api/v1/diagnostics/auth-status" \
  -H "X-API-Key: your_api_key" -k

# Expected: isAuthenticated: true, authenticationType: "ApiKey"

# 2. Check what claims exist
curl -X GET "https://localhost:5002/api/v1/diagnostics/claims" \
  -H "X-API-Key: your_api_key" -k

# Expected: Should show all the claims we created
```

**Solutions**:
- Verify API key is active in database: `SELECT * FROM Businesses WHERE ApiKey = 'your_key'`
- Check business status is Active
- Check subscription is active and tier is ApiOnly or SaaS
- Add logging to `ApiKeyAuthenticationHandler` to verify it's being called

### Issue 2: Some Properties Are Populated, Others Are Not

**Symptom**: `isAuthenticated` is true, but `businessId` or `userId` is null

**Possible Cause**: Specific claims are missing or have wrong type/name

**Debugging**:

Check the claims list in diagnostics response. Look for:

| Expected Claim Type | Maps To Property |
|---------------------|------------------|
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | UserId |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` | UserName |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email` | Email |
| `businessId` (lowercase) | BusinessId |
| `isBusinessLevel` | IsBusinessLevel |

**Solution**:
- Verify `ApiKeyAuthenticationService.ValidateApiKeyAsync()` is creating all required claims
- Check claim names match exactly (case-sensitive)
- Ensure Guid parsing is working (check logs for format exceptions)

### Issue 3: Authentication Fails with 401

**Symptom**: Request returns 401 Unauthorized

**Possible Causes**:
1. API key not found in database
2. Business is inactive
3. Subscription expired or wrong tier
4. API key is in wrong format

**Debugging**:

Check API logs for error messages from `ApiKeyAuthenticationHandler`:

```bash
# Look for these log messages
"API key validation failed: {Error}"
"Invalid API key: {Error}"
```

**Solution**:
- Verify API key format: Should start with `sk_live_`
- Check business exists: `SELECT * FROM Businesses WHERE ApiKey = 'your_key'`
- Check business status: `Status = 'Active'`
- Check subscription:
  ```sql
  SELECT b.Name, b.Status, s.StartDate, s.EndDate, ps.Tier
  FROM Businesses b
  LEFT JOIN BusinessSubscriptions s ON b.Id = s.BusinessId
  LEFT JOIN PlatformSubscriptions ps ON s.PlatformSubscriptionId = ps.Id
  WHERE b.ApiKey = 'your_key'
  ```

### Issue 4: Claims Are Created But ICurrentUserService Still Returns Null

**Symptom**: Diagnostics shows claims exist, but `currentUserService` properties are null

**Possible Cause**: Claim type names don't match what `CurrentUserService` expects

**Debugging**:

Compare the claim types in diagnostics with what `CurrentUserService` looks for:

```csharp
// In CurrentUserService.cs
public Guid? UserId
{
    get
    {
        var userIdClaim = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //                                 ^^^^^^^^^^^^^^^^^^^^^^^^
        // Must match exactly!
        return string.IsNullOrWhiteSpace(userIdClaim) ? null : Guid.Parse(userIdClaim);
    }
}
```

**Solution**: Ensure claim types match:

```csharp
// In ApiKeyAuthenticationService
new Claim(ClaimTypes.NameIdentifier, business.Id.ToString())
//        ^^^^^^^^^^^^^^^^^^^^^^^^^ Must match CurrentUserService expectation
```

### Issue 5: Guid Parsing Errors

**Symptom**: Exception when accessing UserId or BusinessId properties

**Possible Cause**: Claim value is not a valid Guid

**Debugging**:

Check the actual claim values in diagnostics:

```json
{
  "type": "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
  "value": "not-a-guid"  // ❌ Should be a valid Guid
}
```

**Solution**: Verify business ID is being converted to string properly:

```csharp
new Claim(ClaimTypes.NameIdentifier, business.Id.ToString())
//                                                ^^^^^^^^^ Make sure this is valid
```

## Manual Verification Steps

### 1. Verify API Key Exists

```sql
SELECT
    b.Id,
    b.Name,
    b.ApiKey,
    b.IsApiKeyActive,
    b.Status,
    b.ContactEmail
FROM Businesses b
WHERE b.ApiKey = 'your_api_key_here';
```

Expected: One row with `IsApiKeyActive = true`, `Status = 'Active'`

### 2. Verify Subscription

```sql
SELECT
    b.Name,
    s.StartDate,
    s.EndDate,
    s.IsActive,
    ps.Tier,
    ps.Name AS SubscriptionName
FROM Businesses b
INNER JOIN BusinessSubscriptions s ON b.Id = s.BusinessId
INNER JOIN PlatformSubscriptions ps ON s.PlatformSubscriptionId = ps.Id
WHERE b.ApiKey = 'your_api_key_here';
```

Expected: `IsActive = true`, `Tier = 'ApiOnly' OR 'SaaS'`, `EndDate > NOW()`

### 3. Check Authentication Handler Is Registered

In `Program.cs`:

```csharp
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, options =>
    {
        options.HeaderName = "X-API-Key";      // ← Check this matches your header
        options.QueryStringKey = "api_key";    // ← Or this for query param
    });
```

### 4. Verify Middleware Order

In `Program.cs` (after app is built):

```csharp
app.UseAuthentication();  // ← Must come before
app.UseAuthorization();   // ← This
```

## Adding Debug Logging

### In ApiKeyAuthenticationHandler

Add logging to verify claims are being created:

```csharp
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
    // ... existing code ...

    var validationResult = await _apiKeyService.ValidateApiKeyAsync(apiKey);

    if (validationResult.IsValid)
    {
        _logger.LogInformation("Claims created: {ClaimCount}", validationResult.Claims.Count);
        foreach (var claim in validationResult.Claims)
        {
            _logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }
    }

    // ... rest of code ...
}
```

### In CurrentUserService

Add logging to see what claims are available:

```csharp
public CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    _httpContextAccessor = httpContextAccessor;
    _user = _httpContextAccessor.HttpContext?.User;

    // DEBUG: Log all claims
    if (_user != null)
    {
        var claims = _user.Claims.Select(c => $"{c.Type}={c.Value}");
        Console.WriteLine($"CurrentUserService initialized with {_user.Claims.Count()} claims:");
        foreach (var claim in claims)
        {
            Console.WriteLine($"  {claim}");
        }
    }
}
```

## Quick Test Checklist

- [ ] SaaS API is running
- [ ] Valid API key exists in database
- [ ] Business status is Active
- [ ] Subscription is active and correct tier
- [ ] API key is sent in X-API-Key header
- [ ] `/diagnostics/auth-status` returns `isAuthenticated: true`
- [ ] `/diagnostics/claims` shows all expected claims
- [ ] `/diagnostics/current-user` shows populated properties
- [ ] ICurrentUserService properties are accessible in controllers

## Expected Working State

When everything is working correctly, calling `/diagnostics/current-user` with a valid API key should return:

```json
{
  "currentUserService": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "userName": "Acme Corporation",
    "email": "contact@acme.com",
    "isAuthenticated": true,
    "businessId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "branchId": null,
    "isBusinessLevel": true,
    "isBranchLevel": false,
    "isPlatformAdmin": false,
    "isKpmgUser": false,
    "kpmgRole": null,
    "kpmgEmployeeId": null,
    "kpmgDepartment": null,
    "roles": ["ApiClient"],
    "permissions": [
      "api.read",
      "api.write",
      "invoice.create",
      "invoice.read",
      "invoice.update",
      "invoice.transmit"
    ]
  },
  "claims": [
    // ... should show 10+ claims
  ],
  "authentication": {
    "isAuthenticated": true,
    "authenticationType": "ApiKey"
  }
}
```

## Next Steps

1. **Run the diagnostics endpoint** with your actual API key
2. **Share the response** with the exact output
3. **Check application logs** for any errors or warnings
4. **Verify database state** using the SQL queries above

This will help identify exactly where the issue is in the authentication flow.

# Interswitch SwitchTax Integration - Implementation Summary

## Overview

A complete integration library for Interswitch's SwitchTax platform has been successfully created and integrated into your EInvoice Integrator backend application.

---

## What Was Delivered

### ✅ Complete Integration Library

**Location**: `src/Integrations/EInvoiceIntegrator.Interswitch/`

#### 1. Configuration (`Configuration/`)
- `InterswitchHttpClientOptions.cs` - All configurable settings for endpoints, timeouts, logging, etc.

#### 2. Request Models (`Models/Requests/`)
All 10 endpoint request models created:
- `ValidateIRNRequest`
- `ValidateInvoiceRequest`
- `SignInvoiceRequest`
- `UpdateStatusRequest`
- `DownloadInvoiceRequest`
- `SearchInvoiceRequest`
- `LookupWithIRNRequest`
- `TransmitInvoiceRequest`
- `LookupWithTINRequest`
- `GetEntityRequest`

#### 3. Response Models (`Models/Responses/`)
All 10 endpoint response models created with typed data structures:
- `ValidateIRNResponse`
- `ValidateInvoiceResponse`
- `SignInvoiceResponse`
- `UpdateStatusResponse`
- `DownloadInvoiceResponse` (with encryption data: IV, public key, encrypted data)
- `SearchInvoiceResponse` (with pagination support)
- `LookupWithIRNResponse` (with supplier/customer party information)
- `TransmitInvoiceResponse`
- `LookupWithTINResponse`
- `GetEntityResponse` (with complete entity and business information)

#### 4. HTTP Client (`Services/` & `Interfaces/`)
- `IInterswitchHttpClient` - Interface defining all 10 API methods
- `InterswitchHttpClient` - Implementation with:
  - JSON serialization/deserialization
  - Request/response logging
  - Error handling and exception mapping
  - Timeout handling
  - Automatic retry and circuit breaker (via Polly)

#### 5. Exception Handling (`Exceptions/`)
- `InterswitchIntegrationException` - Custom exception with status code and response body

#### 6. Dependency Injection (`DependencyInjection.cs`)
- `AddInterswitchIntegration()` extension method
- Options validation
- HttpClient factory with Polly resilience policies:
  - Retry policy (3 attempts with exponential backoff)
  - Circuit breaker (opens after 5 failures, stays open for 30s)

---

## Configuration Added

### appsettings.json

The following configuration section was added:

```json
{
  "InterswitchHttpClient": {
    "BaseUrl": "https://api.interswitch.com",
    "ValidateIRNEndpoint": "/Api/SwitchTax/ValidateIRN",
    "ValidateInvoiceEndpoint": "/Api/SwitchTax/ValidateInvoice",
    "SignInvoiceEndpoint": "/Api/SwitchTax/SignInvoice",
    "UpdateStatusEndpoint": "/Api/SwitchTax/UpdateStatus",
    "DownloadInvoiceEndpoint": "/Api/SwitchTax/DownloadInvoice",
    "SearchInvoiceEndpoint": "/Api/SwitchTax/SearchInvoice",
    "LookupWithIRNEndpoint": "/Api/SwitchTax/LookupWithIRN",
    "TransmitInvoiceEndpoint": "/Api/SwitchTax/Transmit",
    "LookupWithTINEndpoint": "/Api/SwitchTax/LookupWithTIN",
    "GetEntityEndpoint": "/Api/SwitchTax/GetEntity",
    "ApiVersion": "v1",
    "RequestTimeout": "00:01:00",
    "MaxRetryAttempts": 3,
    "EnableRequestLogging": true,
    "EnableResponseLogging": true
  }
}
```

**Note**: Update `BaseUrl` to the correct Interswitch environment:
- Sandbox: `https://sandbox.interswitch.com`
- Production: `https://api.interswitch.com`

---

## Integration with Application Layer

### Dependency Registration

**File**: `src/Presentation/EInvoiceIntegrator.API/Program.cs`

Added:
```csharp
using EInvoiceIntegrator.Interswitch;

// ...

builder.Services.AddInterswitchIntegration(builder.Configuration);
```

**File**: `src/Presentation/EInvoiceIntegrator.API/EInvoiceIntegrator.API.csproj`

Added project reference:
```xml
<ProjectReference Include="..\..\Integrations\EInvoiceIntegrator.Interswitch\EInvoiceIntegrator.Interswitch.csproj" />
```

---

## How to Use in Application Layer

### Step 1: Inject the Service

In your command/query handlers:

```csharp
using EInvoiceIntegrator.Interswitch.Interfaces;

public class YourCommandHandler : IRequestHandler<YourCommand, YourResult>
{
    private readonly IInterswitchHttpClient _interswitchClient;
    private readonly ILogger<YourCommandHandler> _logger;

    public YourCommandHandler(
        IInterswitchHttpClient interswitchClient,
        ILogger<YourCommandHandler> logger)
    {
        _interswitchClient = interswitchClient;
        _logger = logger;
    }
}
```

### Step 2: Call Interswitch Methods

#### Example: Validate IRN

```csharp
var request = new ValidateIRNRequest
{
    InvoiceReference = "ITW001",
    BusinessId = currentUser.BusinessId.ToString(),
    IRN = invoice.IRN
};

var response = await _interswitchClient.ValidateIRNAsync(request, cancellationToken);

if (response.IsSuccess && response.Data?.Ok == true)
{
    // IRN is valid
}
```

#### Example: Sign and Transmit Invoice

```csharp
// 1. Validate invoice
var validateResponse = await _interswitchClient.ValidateInvoiceAsync(
    new ValidateInvoiceRequest { InvoicePayload = invoiceData },
    cancellationToken);

// 2. Sign invoice
var signResponse = await _interswitchClient.SignInvoiceAsync(
    new SignInvoiceRequest { InvoicePayload = invoiceData },
    cancellationToken);

// 3. Transmit to FIRS
var transmitResponse = await _interswitchClient.TransmitInvoiceAsync(
    new TransmitInvoiceRequest { IRN = invoice.IRN },
    cancellationToken);
```

#### Example: Update Payment Status

```csharp
var request = new UpdateStatusRequest
{
    PaymentStatus = "PAID",
    Reference = paymentReference,
    IRN = invoice.IRN
};

var response = await _interswitchClient.UpdateStatusAsync(request, cancellationToken);
```

#### Example: Search Invoices

```csharp
var request = new SearchInvoiceRequest { IRN = searchIRN };

var response = await _interswitchClient.SearchInvoiceAsync(request, cancellationToken);

if (response.IsSuccess && response.Data != null)
{
    foreach (var item in response.Data.Items)
    {
        Console.WriteLine($"IRN: {item.IRN}, Status: {item.PaymentStatus}");
    }

    // Pagination info
    var page = response.Data.Page;
    Console.WriteLine($"Page {page.Page}, Total: {page.TotalCount}");
}
```

---

## Available Endpoints

| Endpoint | Purpose | Request | Response |
|----------|---------|---------|----------|
| **ValidateIRN** | Validates IRN exists in FIRS | InvoiceReference, BusinessId, IRN | Ok (true/false) |
| **ValidateInvoice** | Validates invoice structure | Invoice payload | Ok (true/false) |
| **SignInvoice** | Digitally signs invoice | Invoice payload | Ok (true/false) |
| **UpdateStatus** | Updates payment status | PaymentStatus, Reference, IRN | Ok (true/false) |
| **DownloadInvoice** | Downloads encrypted invoice | IRN | Encrypted data + IV + public key |
| **SearchInvoice** | Searches invoices by IRN | IRN | Paginated invoice list |
| **LookupWithIRN** | Gets business info by IRN | IRN | Supplier & customer party data |
| **TransmitInvoice** | Transmits signed invoice to FIRS | IRN | Ok (true/false) |
| **LookupWithTIN** | Gets business info by TIN | TIN | Business party data |
| **GetEntity** | Gets complete entity info | EntityId | Entity + all businesses |

---

## Error Handling

### Exception Types

```csharp
try
{
    var response = await _interswitchClient.ValidateIRNAsync(request, cancellationToken);
}
catch (InterswitchIntegrationException ex)
{
    // HTTP error from Interswitch
    _logger.LogError(ex,
        "Interswitch error: {StatusCode} - {Response}",
        ex.StatusCode, ex.ResponseBody);

    // Handle specific status codes
    if (ex.StatusCode == 400) // Bad request
    if (ex.StatusCode == 401) // Unauthorized
    if (ex.StatusCode == 404) // Not found
    if (ex.StatusCode >= 500) // Server error
}
catch (TaskCanceledException)
{
    // Request timeout or cancelled
}
catch (HttpRequestException)
{
    // Network error
}
```

---

## Built-in Resilience Features

### 1. Automatic Retry (Polly)
- **Attempts**: 3 retries
- **Backoff**: Exponential (2s, 4s, 8s)
- **Triggers**: Transient HTTP errors (5xx, timeouts)

### 2. Circuit Breaker (Polly)
- **Threshold**: Opens after 5 consecutive failures
- **Duration**: Stays open for 30 seconds
- **Benefit**: Prevents cascading failures

### 3. Request/Response Logging
- **Enabled by default** (configurable)
- Logs request payload and endpoint
- Logs response status and body
- Includes request ID for tracing

### 4. Timeout Handling
- **Default**: 60 seconds (configurable)
- Throws `TaskCanceledException` on timeout
- Per-request cancellation token support

---

## Testing the Integration

### 1. Connection Test

```csharp
var isConnected = await _interswitchClient.TestConnectionAsync(cancellationToken);

if (!isConnected)
{
    throw new ServiceUnavailableException("Interswitch API unreachable");
}
```

### 2. Environment Configuration

For different environments, create:

**appsettings.Development.json**:
```json
{
  "InterswitchHttpClient": {
    "BaseUrl": "https://sandbox.interswitch.com",
    "EnableRequestLogging": true,
    "EnableResponseLogging": true
  }
}
```

**appsettings.Production.json**:
```json
{
  "InterswitchHttpClient": {
    "BaseUrl": "https://api.interswitch.com",
    "EnableRequestLogging": false,
    "EnableResponseLogging": false
  }
}
```

---

## Documentation

### Comprehensive README

**Location**: `src/Integrations/EInvoiceIntegrator.Interswitch/README.md`

The README includes:
- Complete API reference for all 10 endpoints
- Detailed usage examples for each endpoint
- Configuration guide
- Error handling patterns
- Best practices
- Testing strategies
- Troubleshooting tips

---

## Build Status

✅ **All Projects Build Successfully**

```
EInvoiceIntegrator.Interswitch ✅
EInvoiceIntegrator.API ✅
```

No warnings or errors.

---

## Next Steps

### 1. Update BaseUrl

Change the `BaseUrl` in `appsettings.json` to the correct Interswitch environment endpoint.

### 2. Test Connection

Create a simple test to verify connectivity:

```csharp
[HttpGet("test-interswitch")]
public async Task<IActionResult> TestInterswitch(
    [FromServices] IInterswitchHttpClient interswitchClient)
{
    var isConnected = await interswitchClient.TestConnectionAsync(default);
    return Ok(new { connected = isConnected });
}
```

### 3. Implement Invoice Workflow

Create command handlers for:
1. `ValidateAndSignInvoiceCommand` - Validates and signs invoice
2. `TransmitInvoiceToFIRSCommand` - Transmits via Interswitch
3. `UpdateInvoicePaymentStatusCommand` - Updates payment status

### 4. Add to Existing FIRS Integration

You can use Interswitch alongside your existing FIRS direct integration:
- Use FIRS direct for real-time validation
- Use Interswitch for simplified signing and transmission
- Leverage Interswitch's managed certificates

### 5. Monitor and Log

The integration automatically logs all requests/responses. Monitor:
- Success/failure rates
- Response times
- Error patterns
- Retry occurrences

---

## Key Differences from FIRS Direct

| Feature | FIRS Direct | Interswitch |
|---------|-------------|-------------|
| **Setup** | Complex (certificates, signing) | Simplified (managed by Interswitch) |
| **Authentication** | Per-business API keys | Centralized |
| **Digital Signing** | You implement | Interswitch handles |
| **Certificate Management** | Your responsibility | Managed by Interswitch |
| **Error Handling** | FIRS error codes | Standardized responses |
| **Transmission** | Direct to FIRS | Through Interswitch proxy |
| **Monitoring** | Self-managed | Interswitch dashboard available |

---

## Support and Troubleshooting

### Common Issues

**1. Connection Timeout**
- Check `BaseUrl` is correct
- Verify network connectivity
- Increase `RequestTimeout` if needed

**2. 401 Unauthorized**
- Verify authentication credentials
- Check API key/secret if required (consult Interswitch docs)

**3. 400 Bad Request**
- Validate request payload structure
- Check invoice payload format matches Interswitch requirements
- Review request logs for details

**4. 500 Server Error**
- Enable retry policy (already configured)
- Check Interswitch service status
- Review error response body for details

### Logging

All integration calls are logged with:
```
[{RequestId}] Interswitch API Request to {Endpoint}: {Request}
[{RequestId}] Interswitch API Response from {Endpoint} ({StatusCode}): {Response}
```

Search logs by endpoint name or request ID for debugging.

---

## Files Created/Modified

### New Files (20+ files)

**Configuration**:
- `InterswitchHttpClientOptions.cs`

**Requests** (10 files):
- `ValidateIRNRequest.cs`
- `ValidateInvoiceRequest.cs`
- `SignInvoiceRequest.cs`
- `UpdateStatusRequest.cs`
- `DownloadInvoiceRequest.cs`
- `SearchInvoiceRequest.cs`
- `LookupWithIRNRequest.cs`
- `TransmitInvoiceRequest.cs`
- `LookupWithTINRequest.cs`
- `GetEntityRequest.cs`

**Responses** (10+ files):
- `InterswitchResponse.cs` (base)
- `ValidateIRNResponse.cs`
- `ValidateInvoiceResponse.cs`
- `SignInvoiceResponse.cs`
- `UpdateStatusResponse.cs`
- `DownloadInvoiceResponse.cs`
- `SearchInvoiceResponse.cs`
- `LookupWithIRNResponse.cs`
- `TransmitInvoiceResponse.cs`
- `LookupWithTINResponse.cs`
- `GetEntityResponse.cs`

**Services**:
- `IInterswitchHttpClient.cs`
- `InterswitchHttpClient.cs`

**Infrastructure**:
- `InterswitchIntegrationException.cs`
- `DependencyInjection.cs`
- `README.md`

### Modified Files

- `appsettings.json` - Added Interswitch configuration
- `Program.cs` - Added `AddInterswitchIntegration()`
- `EInvoiceIntegrator.API.csproj` - Added project reference
- `EInvoiceIntegrator.Interswitch.csproj` - Added NuGet packages

---

## Conclusion

You now have a complete, production-ready integration with Interswitch SwitchTax! The library:

✅ Implements all 10 documented endpoints
✅ Has built-in resilience (retry, circuit breaker)
✅ Includes comprehensive error handling
✅ Is fully configurable via appsettings.json
✅ Provides detailed logging
✅ Is type-safe with XML documentation
✅ Follows your existing architecture patterns
✅ Builds without errors

You can now call Interswitch from your Application layer using `IInterswitchHttpClient` just like you do with `IFIRSHttpClient`!

---

**For detailed usage examples, see**: `src/Integrations/EInvoiceIntegrator.Interswitch/README.md`

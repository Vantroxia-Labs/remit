# Invoice Data Mapping Integration

This document describes the mapping class and integration services created to bridge the gap between FIRS `ValidateInvoiceDataRequest` and the internal `CreateInvoiceRequest` for database operations.

## Overview

The integration provides two main components:

1. **InvoiceDataMapping**: Static mapping class for converting between FIRS and internal request formats
2. **UnifiedResponseDtos**: Standardized response DTOs that work with both internal systems and FIRS responses
3. **InvoiceProcessingService**: Service that orchestrates FIRS validation with database operations
4. **InvoiceIntegrationController**: Example controller showing practical usage

## Key Files

- `InvoiceDataMapping.cs` - Core mapping functionality
- `InvoiceProcessingService.cs` - Invoice processing service implementation
- `InvoiceIntegrationController.cs` - Example controller usage
- Updated `FIRSGetController.cs` - Now returns unified response DTOs

## Mapping Class Usage

### Converting FIRS Request to Create Invoice Request

```csharp
// FIRS validation request from external source
ValidateInvoiceDataRequest firsRequest = GetFirsRequest();
Guid partyId = GetPartyId();

// Map to internal create request
CreateInvoiceRequest createRequest = InvoiceDataMapping.MapToCreateInvoiceRequest(
    firsRequest, 
    partyId);

// Now you can use createRequest to insert into database
```

### Converting Create Invoice Request to FIRS Request

```csharp
// Internal create request
CreateInvoiceRequest createRequest = GetCreateRequest();
string businessId = "BUS123";
string irn = "INV-2024-001";

// Map to FIRS validation request
ValidateInvoiceDataRequest firsRequest = InvoiceDataMapping.MapToValidateInvoiceDataRequest(
    createRequest, 
    businessId, 
    irn);

// Now you can send firsRequest to FIRS for validation
```

## Integration Service Usage

### Approach 1: Validate First, Then Create

This approach validates with FIRS first, then creates the invoice in the database only if validation passes.

```csharp
public class YourController : BaseApiController
{
    private readonly IInvoiceProcessingService _integrationService;

    [HttpPost("validate-first")]
    public async Task<IActionResult> ValidateFirst(
        ValidateInvoiceDataRequest firsRequest,
        Guid partyId,
        string businessId)
    {
        var result = await _integrationService.ValidateAndCreateInvoiceAsync(
            firsRequest, 
            partyId, 
            businessId);

        if (result.IsSuccess)
        {
            return CreatedAtAction("GetInvoice", new { id = result.InvoiceId }, result);
        }

        return BadRequest(result);
    }
}
```

**Benefits:**
- Ensures FIRS compliance before database insertion
- Prevents invalid data from entering the database
- Cleaner rollback (no database cleanup needed)

**Drawbacks:**
- Invoice is lost if database creation fails after successful validation
- No audit trail of failed invoices

### Approach 2: Create First, Then Validate

This approach creates the invoice in the database first, then validates with FIRS.

```csharp
[HttpPost("create-first")]
public async Task<IActionResult> CreateFirst(
    CreateInvoiceRequest createRequest,
    string businessId,
    string irn)
{
    var result = await _integrationService.CreateAndValidateInvoiceAsync(
        createRequest, 
        businessId, 
        irn);

    if (result.IsSuccess)
    {
        return CreatedAtAction("GetInvoice", new { id = result.InvoiceId }, result);
    }

    // Even if FIRS validation fails, invoice might still be in database
    if (result.Status == InvoiceIntegrationStatus.ValidationFailedAfterCreation)
    {
        return StatusCode(207, result); // Multi-status: partial success
    }

    return BadRequest(result);
}
```

**Benefits:**
- Invoice is preserved in database even if FIRS validation fails
- Better audit trail and recovery options
- Can retry FIRS validation later

**Drawbacks:**
- May store non-compliant data temporarily
- Requires cleanup processes for failed validations

## Unified Response DTOs

All FIRS GET endpoints now return unified DTOs that are consistent and work with both systems:

### Updated Endpoints

All these endpoints now return standardized response DTOs:

- `GET /api/v1/firs/gettaxcategories` → `List<TaxCategoryResponseDto>`
- `GET /api/v1/firs/getallcountries` → `List<CountryResponseDto>`
- `GET /api/v1/firs/getallcurrencies` → `List<CurrencyResponseDto>`
- `GET /api/v1/firs/getpaymentmeans` → `List<PaymentMeansResponseDto>`
- `GET /api/v1/firs/getinvoicetypes` → `List<InvoiceTypeResponseDto>`
- `GET /api/v1/firs/getservicecodes` → `List<ServiceCodeResponseDto>`
- `GET /api/v1/firs/getvatexemptions` → `List<VatExemptionResponseDto>`

### Example Usage

```csharp
// The responses are now consistent across all endpoints
var currencies = await httpClient.GetFromJsonAsync<ApiResponse<List<CurrencyResponseDto>>>(
    "/api/v1/firs/getallcurrencies");

var invoiceTypes = await httpClient.GetFromJsonAsync<ApiResponse<List<InvoiceTypeResponseDto>>>(
    "/api/v1/firs/getinvoicetypes");

// All responses follow the same structure with Code, Name, and additional fields
foreach (var currency in currencies.Data)
{
    Console.WriteLine($"{currency.Code}: {currency.Name}");
}
```

## Error Handling

The integration service provides detailed error information:

```csharp
public enum InvoiceIntegrationStatus
{
    Success,
    ValidationFailed,                    // FIRS validation failed
    DatabaseCreationFailed,             // Database operation failed
    ValidationFailedAfterCreation,      // DB success, FIRS failed
    UnexpectedError                     // System error
}
```

Handle different scenarios appropriately:

```csharp
var result = await _integrationService.ValidateAndCreateInvoiceAsync(...);

switch (result.Status)
{
    case InvoiceIntegrationStatus.Success:
        // All good - invoice created and validated
        return Ok(result);
        
    case InvoiceIntegrationStatus.ValidationFailed:
        // FIRS rejected the invoice - fix data and retry
        return UnprocessableEntity(result);
        
    case InvoiceIntegrationStatus.DatabaseCreationFailed:
        // System issue - retry or escalate
        return StatusCode(500, result);
        
    case InvoiceIntegrationStatus.ValidationFailedAfterCreation:
        // Invoice in DB but FIRS failed - manual review needed
        return StatusCode(207, result); // Multi-status
        
    default:
        return StatusCode(500, result);
}
```

## Configuration

Register the integration service in your DI container:

```csharp
// In Program.cs or Startup.cs
services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();
```

## Best Practices

1. **Choose the Right Approach**: 
   - Use "validate-first" for strict compliance requirements
   - Use "create-first" when you need audit trails and recovery options

2. **Handle Partial Failures**: 
   - Always check the `InvoiceIntegrationStatus` 
   - Implement appropriate retry mechanisms
   - Log detailed error information

3. **Monitor Integration Health**:
   - Track success/failure rates
   - Monitor FIRS response times
   - Set up alerts for high failure rates

4. **Data Validation**:
   - Validate required fields before calling integration service
   - Ensure business context data is available for mapping
   - Handle missing optional fields gracefully

## Limitations and Considerations

1. **Business Context**: Some mappings require business context (like resolving BusinessItemId from FIRS item codes) that isn't implemented in this basic mapping

2. **Incomplete FIRS Mapping**: The reverse mapping (CreateInvoiceRequest → ValidateInvoiceDataRequest) has placeholder values for supplier/customer party information that would need to be populated from business context

3. **Error Recovery**: The service doesn't implement automatic retry or rollback mechanisms - these should be added based on your requirements

4. **Performance**: For high-volume scenarios, consider implementing async processing and caching mechanisms

## Testing

Ensure you test both success and failure scenarios:

```csharp
// Test successful integration
var validFirsRequest = CreateValidFirsRequest();
var result = await integrationService.ValidateAndCreateInvoiceAsync(...);
Assert.True(result.IsSuccess);

// Test FIRS validation failure
var invalidFirsRequest = CreateInvalidFirsRequest();
var result = await integrationService.ValidateAndCreateInvoiceAsync(...);
Assert.Equal(InvoiceIntegrationStatus.ValidationFailed, result.Status);

// Test database failure scenarios
// Mock database failure and verify proper error handling
```

This integration provides a solid foundation for bridging FIRS validation with your internal invoice database operations while maintaining flexibility for different business requirements.
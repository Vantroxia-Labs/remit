# FIRS Access Point Integration

This project provides integration with the Federal Inland Revenue Service (FIRS) e-invoice system.

## Tenant-Agnostic Architecture

**IMPORTANT**: The FIRS Access Point is exempt from the multi-tenant isolation mechanisms used throughout the rest of the EInvoice Integrator system.

### Why Tenant-Agnostic?

FIRS integration operates as a **shared service** across all tenants for the following reasons:

1. **Single FIRS Account**: Organizations typically have one FIRS registration regardless of internal tenant structure
2. **Shared Tax Authority**: All invoices report to the same government tax authority
3. **Unified Compliance**: Tax compliance requirements are consistent across tenant boundaries
4. **Resource Efficiency**: Avoids duplicate FIRS configurations and authentication tokens
5. **Simplified Management**: Single point of configuration for FIRS credentials and endpoints

### Implementation Details

The tenant exemption is implemented through:

#### 1. TenantAgnosticAttribute
```csharp
[TenantAgnostic("FIRS integration is a shared service that operates independently of tenant boundaries")]
public interface IFIRSHttpClient
```

This attribute marks components that should bypass tenant isolation.

#### 2. Portal Authorization
FIRS operations use the standard EInvoice Portal role-based authorization:

- **Admin**: Full access to all FIRS operations
- **Accountant**: Can submit and retrieve invoices
- **Auditor**: Read-only access to invoice data
- **Viewer**: Read-only access to invoice data

#### 3. Shared Configuration
FIRS settings are configured globally in `appsettings.json`:

```json
{
  "FIRS": {
    "BaseUrl": "https://api.firs.gov.ng",
    "IsTenantAgnostic": true
  }
}
```

### Security Considerations

While FIRS integration is tenant-agnostic, security is maintained through:

1. **Role-based access control** ensures only authorized users can perform operations
2. **Audit logging** tracks all FIRS operations with user context
3. **Request validation** ensures data integrity
4. **Secure communication** with FIRS APIs using HTTPS and authentication tokens

### Usage Guidelines

When implementing FIRS functionality:

- ✅ Use standard EInvoice Portal role authorization (`Admin`, `Accountant`, `Auditor`, `Viewer`)
- ✅ Log operations with user context for audit trails
- ✅ Handle FIRS responses consistently across all tenants
- ❌ Don't add tenant-specific logic to FIRS operations
- ❌ Don't create per-tenant FIRS configurations
- ❌ Don't include tenant identifiers in FIRS API calls

### Architecture Diagram

```
┌─────────────┐    ┌──────────────┐    ┌─────────────┐
│   Tenant A  │    │   Tenant B   │    │   Tenant C  │
│   Users     │    │   Users      │    │   Users     │
└──────┬──────┘    └──────┬───────┘    └──────┬──────┘
       │                  │                   │
       └──────────────────┼───────────────────┘
                          │
                    ┌─────▼─────┐
                    │   FIRS    │
                    │ Controller │ (Role-based Auth)
                    │ (Shared)  │
                    └─────┬─────┘
                          │
                    ┌─────▼─────┐
                    │   FIRS    │
                    │HttpClient │ (Tenant-Agnostic)
                    │ Service   │
                    └─────┬─────┘
                          │
                    ┌─────▼─────┐
                    │   FIRS    │
                    │    API    │
                    └───────────┘
```

This architecture ensures all tenants share the same FIRS integration while maintaining proper access control and audit capabilities.
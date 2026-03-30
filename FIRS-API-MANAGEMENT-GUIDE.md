# FIRS API Key Management System

## Overview

This system provides secure, multi-tenant FIRS API key management for both **SaaS** (KPMG-managed) and **On-Premise** (customer-managed, KMPG-controlled) deployments.

## Architecture

### SaaS Deployment (KPMG Managed)
- KMPG owns and manages all FIRS API keys
- Centralized configuration management
- Higher usage limits
- Automatic updates and monitoring

### On-Premise Deployment (Customer Managed, KMPG Controlled)
- Customers provide their own FIRS API keys
- KPMG must approve all configurations before activation
- Lower usage limits
- Subscription validation with KMPG systems

## Features

✅ **Secure Encryption**: AES-256-GCM encryption for API keys and secrets  
✅ **Multi-Tenant Support**: Separate configurations per deployment type  
✅ **Usage Tracking**: Daily and monthly API usage monitoring  
✅ **Approval Workflow**: KMPG approval required for On-Premise setups  
✅ **Subscription Validation**: License validation for On-Premise deployments  
✅ **Configuration Management**: RESTful API for managing configurations  
✅ **Auto-Expiry**: Time-based configuration expiration  
✅ **Domain Restrictions**: Allowed domains for On-Premise deployments  

## Database Schema

The system adds a new `FIRSApiConfiguration` entity with the following key fields:

- **Security**: Encrypted API key/secret storage
- **Multi-tenancy**: Deployment type (SaaS/OnPremise)
- **Usage Control**: Daily/monthly limits and tracking
- **Approval**: KMPG approval workflow for On-Premise
- **Auditing**: Full audit trail with created/updated/deleted tracking

## Setup Instructions

### 1. Generate Encryption Key

Generate a secure 256-bit encryption key for protecting API credentials:

```bash
# Generate encryption key using PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

# Or using OpenSSL
openssl rand -base64 32
```

### 2. Configure Application Settings

#### For SaaS Deployment
Create/update `appsettings.SaaS.json`:

```json
{
  "DeploymentMode": "SaaS",
  "Encryption": {
    "Key": "YOUR_GENERATED_256BIT_KEY_HERE"
  },
  "FIRSApiConfiguration": {
    "ManagementMode": "KPMG_Managed",
    "DefaultConfiguration": {
      "Name": "KPMG Production SaaS",
      "Description": "KPMG-managed FIRS API configuration for SaaS deployments",
      "Environment": "Production",
      "BaseUrl": "https://api.firs.gov.ng",
      "ApiKey": "YOUR_KPMG_FIRS_API_KEY",
      "ApiSecret": "YOUR_KMPG_FIRS_API_SECRET",
      "DailyRequestLimit": 50000,
      "MonthlyRequestLimit": 1500000
    }
  }
}
```

#### For On-Premise Deployment
Create/update `appsettings.OnPremise.json`:

```json
{
  "DeploymentMode": "OnPremise",
  "Encryption": {
    "Key": "YOUR_GENERATED_256BIT_KEY_HERE"
  },
  "FIRSApiConfiguration": {
    "ManagementMode": "Customer_Managed_KPMG_Controlled",
    "RequiresApproval": true,
    "DefaultConfiguration": {
      "Name": "Customer On-Premise Configuration",
      "Description": "Customer-managed FIRS API configuration",
      "Environment": "Production",
      "BaseUrl": "https://api.firs.gov.ng",
      "ApiKey": "CUSTOMER_FIRS_API_KEY",
      "ApiSecret": "CUSTOMER_FIRS_API_SECRET",
      "AllowedDomains": "[\"customer-domain.com\", \"*.customer-domain.com\"]",
      "DailyRequestLimit": 10000,
      "MonthlyRequestLimit": 300000
    },
    "KMPGApproval": {
      "Required": true,
      "ContactEmail": "firs-approval@kpmg.com",
      "Instructions": "Submit FIRS API credentials to KMPG for approval"
    }
  }
}
```

### 3. Database Migration

The system will automatically create the required database tables on startup. Ensure your connection string is configured correctly.

### 4. Initial Configuration Setup

#### For SaaS (KMPG Admin)

```bash
# Create SaaS configuration via API
POST /api/v1/firs-api-configuration/saas
{
  "name": "KPMG Production SaaS",
  "description": "KPMG-managed FIRS API configuration for SaaS deployments",
  "environment": "Production",
  "baseUrl": "https://api.firs.gov.ng",
  "apiKey": "YOUR_KPMG_FIRS_API_KEY",
  "apiSecret": "YOUR_KMPG_FIRS_API_SECRET",
  "dailyRequestLimit": 50000,
  "monthlyRequestLimit": 1500000
}
```

#### For On-Premise (Customer)

```bash
# Create On-Premise configuration
POST /api/v1/firs-api-configuration/on-premise
{
  "name": "Customer On-Premise Configuration",
  "description": "Customer-managed FIRS API configuration",
  "environment": "Production",
  "baseUrl": "https://api.firs.gov.ng",
  "apiKey": "CUSTOMER_FIRS_API_KEY",
  "apiSecret": "CUSTOMER_FIRS_API_SECRET",
  "allowedDomains": "[\"customer-domain.com\", \"*.customer-domain.com\"]",
  "dailyRequestLimit": 10000,
  "monthlyRequestLimit": 300000
}

# KMPG Admin approves the configuration
POST /api/v1/firs-api-configuration/{configurationId}/approve
{
  "approvalNotes": "Verified customer credentials and domain ownership"
}
```

## API Endpoints

### Configuration Management

| Method | Endpoint | Description | Access |
|--------|----------|-------------|---------|
| GET | `/api/v1/firs-api-configuration/active` | Get active configuration | Authenticated |
| GET | `/api/v1/firs-api-configuration` | Get all configurations | Admin |
| POST | `/api/v1/firs-api-configuration/saas` | Create SaaS configuration | KMPG Admin |
| POST | `/api/v1/firs-api-configuration/on-premise` | Create On-Premise configuration | Authenticated |
| POST | `/api/v1/firs-api-configuration/{id}/approve` | Approve On-Premise configuration | KMPG Admin |
| POST | `/api/v1/firs-api-configuration/{id}/revoke` | Revoke configuration approval | KMPG Admin |
| POST | `/api/v1/firs-api-configuration/{id}/set-default` | Set as default configuration | Admin |
| GET | `/api/v1/firs-api-configuration/{id}/usage` | Get usage statistics | Authenticated |
| PUT | `/api/v1/firs-api-configuration/{id}/credentials` | Update credentials | Admin |
| GET | `/api/v1/firs-api-configuration/validate-subscription` | Validate subscription | Authenticated |

### Usage Monitoring

The system automatically tracks:
- Daily API request count per configuration
- Monthly API request count per configuration
- Last usage timestamp
- Usage limit enforcement

## Security Features

### Encryption
- **AES-256-GCM** encryption for API keys and secrets
- **Base64 encoding** for secure transport
- **Key rotation support** for enhanced security

### Access Control
- **Role-based permissions** (User, Admin, KMPG Admin)
- **Domain restrictions** for On-Premise deployments
- **Subscription validation** for license enforcement

### Audit Trail
- **Full audit logging** of all configuration changes
- **Usage tracking** and monitoring
- **Approval workflow** with notes and timestamps

## Deployment Scenarios

### Scenario 1: KMPG SaaS Platform
1. KMPG creates and manages all FIRS API configurations
2. High usage limits (50K daily, 1.5M monthly)
3. Centralized monitoring and management
4. Automatic subscription validation

### Scenario 2: Customer On-Premise Installation
1. Customer provides their FIRS API credentials
2. KMPG reviews and approves configuration
3. Lower usage limits (10K daily, 300K monthly)
4. Domain restrictions and subscription validation
5. Regular license validation with KMPG systems

## Monitoring and Maintenance

### Daily Tasks (Automated)
- Reset daily usage counters
- Check configuration expiry
- Validate active subscriptions

### Monthly Tasks (Automated)  
- Reset monthly usage counters
- Generate usage reports
- Review expired configurations

### KMPG Admin Tasks
- Review and approve On-Premise configurations
- Monitor usage patterns
- Manage configuration expiry dates
- Update API credentials as needed

## Troubleshooting

### Common Issues

**Configuration Not Found**
- Verify configuration exists and is active
- Check deployment mode matches configuration type

**Usage Limit Exceeded**
- Check daily/monthly usage via API
- Contact KMPG for limit increases if needed

**Invalid Credentials**
- Verify API key/secret are correct
- Check if configuration approval is pending

**Subscription Invalid**
- Verify business subscription status
- Check On-Premise license validation

### Support Contacts

- **SaaS Issues**: KMPG Support Team
- **On-Premise Setup**: firs-approval@kpmg.com
- **Technical Issues**: Development Team

## Future Enhancements

- [ ] Automated key rotation
- [ ] Advanced usage analytics
- [ ] Multi-region configuration support
- [ ] Real-time usage alerts
- [ ] Configuration backup and restore
- [ ] API key health monitoring

---

## Important Security Notes

⚠️ **Never store API keys in plain text**  
⚠️ **Always use HTTPS for API communications**  
⚠️ **Regularly rotate encryption keys**  
⚠️ **Monitor usage patterns for anomalies**  
⚠️ **Keep audit logs for compliance**
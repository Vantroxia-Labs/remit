# 🚀 E-Invoice Integrator - SFTP & Frontend Testing Guide

## 📋 Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Prerequisites](#prerequisites)
4. [Quick Start (5 Minutes)](#quick-start)
5. [SFTP Testing Methods](#sftp-testing-methods)
   - [PowerShell Script](#method-1-powershell-script-recommended)
   - [WinSCP GUI](#method-2-winscp-gui)
6. [Frontend Testing](#frontend-testing)
7. [End-to-End Testing Workflow](#end-to-end-testing-workflow)
8. [Troubleshooting](#troubleshooting)
9. [Configuration Reference](#configuration-reference)

---

## Overview

The E-Invoice Integrator system supports **two methods** for submitting invoices:

| Method | Protocol | Port | Tool | Use Case |
|--------|----------|------|------|----------|
| **SFTP** | SSH File Transfer | 2222 | WinSCP, PowerShell, FileZilla | Bulk file uploads, automation |
| **Web Portal** | HTTPS | 5001 | Browser (React/Angular) | Manual invoice management |

### How They Work Together

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   SFTP Client   │────▶│  BackgroundSvc  │────▶│    Database     │
│ (WinSCP/Script) │     │   (Port 5138)   │     │   (PostgreSQL)  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        │                       ▼                       │
        │               ┌─────────────────┐             │
        │               │     SFTPGo      │             │
        │               │   (Port 2222)   │             │
        │               └─────────────────┘             │
        │                                               │
        │                                               ▼
        │                                       ┌─────────────────┐
        └──────────────────────────────────────▶│  Web Frontend   │
                                                │ (View invoices) │
                                                └─────────────────┘
```

---

## Architecture

### Components

| Component | Port | Description |
|-----------|------|-------------|
| **SFTPGo** | 2222 | SFTP server (receives file uploads) |
| **BackgroundService** | 5138 | Authenticates users, processes files |
| **Portal API** | 5001 | Web API for frontend |
| **SaaS API** | 5001 | API for external integrations |
| **Frontend** | 3000/4200 | React/Angular web application |
| **PostgreSQL** | 5432 | Database |

### Authentication Flow

```
1. User connects to SFTPGo (port 2222) with username/password
2. SFTPGo calls BackgroundService: POST /api/sftp-auth/check-credentials
3. BackgroundService validates against database
4. If valid: User gets access to their business folder
5. User uploads XML invoice files
6. BackgroundService detects and processes files
7. Processed invoices appear in web frontend
```

---

## Prerequisites

### Required Software

| Software | Download | Purpose |
|----------|----------|---------|
| **SFTPGo** | [GitHub Releases](https://github.com/drakkan/sftpgo/releases) | SFTP server |
| **WinSCP** | [winscp.net](https://winscp.net/download/WinSCP-Setup.exe) | GUI SFTP client |
| **Visual Studio 2022** | Required | Run BackgroundService |
| **PostgreSQL** | Required | Database |
| **Node.js** (optional) | For frontend | Run React/Angular app |

### Required Services Running

| Service | How to Start | How to Verify |
|---------|--------------|---------------|
| BackgroundService | F5 in Visual Studio | http://localhost:5138/health |
| SFTPGo | `sftpgo.exe serve` | Port 2222 listening |
| Portal API (for frontend) | F5 in Visual Studio | http://localhost:5001/health |
| Frontend (optional) | `npm start` | http://localhost:3000 |

---

## Quick Start

### Step 1: Run the Test Script

```powershell
# Navigate to project root
cd "C:\Users\i\source\repos\eInvoice\EInvoiceIntegratorAPI"

# Run the comprehensive test script
.\sftp-test.ps1
```

The script will:
- ✅ Check all services are running
- ✅ Test authentication
- ✅ Connect via SFTP
- ✅ Upload a test file
- ✅ Verify the upload

### Step 2: View in Frontend (Optional)

After SFTP upload succeeds:
1. Open browser: `http://localhost:3000` (or your frontend URL)
2. Login with portal credentials
3. Navigate to **Invoices** section
4. Find the processed invoice

---

## SFTP Testing Methods

### Method 1: PowerShell Script (Recommended)

Run the included `sftp-test.ps1` script:

```powershell
.\sftp-test.ps1
```

**What it does:**
- Checks all prerequisites
- Tests authentication endpoint
- Connects via SFTP
- Uploads test file
- Verifies upload
- Provides detailed diagnostics

**Successful Output:**
```
========================================
  ✓ ALL TESTS PASSED!
========================================
- BackgroundService: Running
- SFTPGo: Running
- Authentication: Success
- SFTP Connection: Success
- File Upload: Success
```

---

### Method 2: WinSCP GUI

#### Connection Settings

| Setting | Value |
|---------|-------|
| File Protocol | **SFTP** |
| Host name | **localhost** |
| Port number | **2222** |
| User name | **testsftp** |
| Password | **TestPassword123** |

#### Step-by-Step

1. **Open WinSCP**
2. **Click "New Site"**
3. **Enter settings** (see table above)
4. **Click "Login"**
5. **Accept host key** (first time only)
6. **Upload files** via drag & drop

#### Screenshot Reference
```
┌──────────────────────────────────────────┐
│ Login                                    │
├──────────────────────────────────────────┤
│ File protocol: [SFTP          ▼]         │
│                                          │
│ Host name:     [localhost        ]       │
│ Port number:   [2222  ]                  │
│                                          │
│ User name:     [testsftp         ]       │
│ Password:      [●●●●●●●●●●●●●    ]       │
│                                          │
│        [Save ▼]      [Login]  [Cancel]   │
└──────────────────────────────────────────┘
```

---

## Frontend Testing

### ⚠️ Important Note

**You CANNOT test SFTP directly from a web browser or frontend application.**

SFTP uses SSH protocol (port 22/2222), not HTTP/HTTPS. Web browsers only support HTTP/HTTPS.

### What You CAN Test in Frontend

| Feature | How to Test |
|---------|-------------|
| **View uploaded invoices** | After SFTP upload, check Invoices list |
| **Invoice details** | Click on invoice to see XML data |
| **Processing status** | Check invoice status (Pending/Validated/Signed) |
| **Error messages** | View processing errors |
| **Download invoice** | Export invoice as PDF/XML |

### Frontend Testing Steps

#### Step 1: Start Frontend

```bash
# React
cd frontend
npm install
npm start

# OR Angular
cd frontend
npm install
ng serve
```

#### Step 2: Login

1. Open browser: `http://localhost:3000`
2. Login with credentials:
   - Email: `sftptest@test.com`
   - Password: `TestPassword123`

#### Step 3: Navigate to Invoices

1. Click **Invoices** in sidebar
2. You should see invoices uploaded via SFTP
3. Click on an invoice to view details

#### Step 4: Verify Processing

Check invoice status:
- **Pending** - Just uploaded, not processed yet
- **Validated** - Passed validation
- **Signed** - Digitally signed
- **Transmitted** - Sent to FIRS
- **Error** - Processing failed (check error message)

---

## End-to-End Testing Workflow

### Complete Test Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    END-TO-END TEST FLOW                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. SETUP                                                   │
│     ├── Run: .\sftp-test.ps1 -Setup                        │
│     └── Creates test user, directories, configuration      │
│                                                             │
│  2. SFTP UPLOAD                                             │
│     ├── Run: .\sftp-test.ps1 -Test                         │
│     ├── OR use WinSCP to upload invoice XML                │
│     └── File goes to: C:\ftproot\uploads\{BusinessId}\     │
│                                                             │
│  3. PROCESSING (Automatic)                                  │
│     ├── BackgroundService detects new file                 │
│     ├── Parses XML invoice                                 │
│     ├── Validates data                                     │
│     ├── Saves to database                                  │
│     └── Moves file to PROCESSED folder                     │
│                                                             │
│  4. FRONTEND VERIFICATION                                   │
│     ├── Open: http://localhost:3000                        │
│     ├── Login with test credentials                        │
│     ├── Navigate to Invoices                               │
│     └── Verify invoice appears with correct data           │
│                                                             │
│  5. API VERIFICATION (Optional)                             │
│     ├── GET /api/v1/invoices                               │
│     └── Verify invoice in JSON response                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Test Invoice XML Sample

Create a file named `test-invoice.xml`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">
    <ID>INV-TEST-001</ID>
    <IssueDate>2025-01-15</IssueDate>
    <InvoiceTypeCode>380</InvoiceTypeCode>
    <DocumentCurrencyCode>NGN</DocumentCurrencyCode>
    <AccountingSupplierParty>
        <Party>
            <PartyName><Name>Test Supplier Ltd</Name></PartyName>
            <PartyTaxScheme>
                <CompanyID>123456789012</CompanyID>
            </PartyTaxScheme>
        </Party>
    </AccountingSupplierParty>
    <AccountingCustomerParty>
        <Party>
            <PartyName><Name>Test Customer Ltd</Name></PartyName>
        </Party>
    </AccountingCustomerParty>
    <LegalMonetaryTotal>
        <PayableAmount currencyID="NGN">10000.00</PayableAmount>
    </LegalMonetaryTotal>
</Invoice>
```

---

## Troubleshooting

### Common Issues

#### ❌ "Connection refused" in WinSCP

**Cause:** SFTPGo is not running

**Solution:**
```powershell
cd "C:\Program Files\SFTPGo"
.\sftpgo.exe serve
```

---

#### ❌ "Access denied" / "Authentication failed"

**Cause:** User doesn't exist or SFTP not enabled

**Solution:**
1. Run the test script with `-Setup`:
   ```powershell
   .\sftp-test.ps1 -Setup
   ```
2. Or manually check database:
   ```sql
   SELECT Email, SftpUsername, IsSftpEnabled, IsActive 
   FROM Users WHERE SftpUsername = 'testsftp'
   ```

---

#### ❌ BackgroundService won't start

**Cause:** Missing service registration

**Solution:** Check for errors in Visual Studio Output window. Common fixes:
- Ensure all NuGet packages restored
- Check connection string in `.env` file
- Run `dotnet restore`

---

#### ❌ Invoice uploaded but not appearing in frontend

**Cause:** Processing error or wrong business ID

**Solution:**
1. Check BackgroundService logs:
   ```powershell
   Get-Content "src\Presentation\EInvoiceIntegrator.BackgroundService\logs\*.txt" -Tail 50
   ```
2. Verify file is in correct folder:
   ```powershell
   Get-ChildItem "C:\ftproot\uploads" -Recurse
   ```

---

#### ❌ "Unable to resolve service" errors

**Cause:** Missing dependency injection registration

**Solution:** Ensure you have the latest code with all DI fixes:
```powershell
git pull origin develop
dotnet build
```

---

## Configuration Reference

### Environment Variables (.env)

**BackgroundService** (`src/Presentation/EInvoiceIntegrator.BackgroundService/.env`):

```env
# Database
ConnectionStrings__DefaultConnection=Host=172.206.198.128;Port=5432;Database=eInvoicing;Username=postgres;Password=Password12*;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true

# Email (Azure Communication Service)
EmailSettings__Provider=azure
AzureCommunicationService__ConnectionString=your-connection-string

# Licensing
LicensingService__BaseUrl=https://apps.ng.kpmg.com/licensing-service/
```

### SFTPGo Configuration

**Location:** `C:\Program Files\SFTPGo\sftpgo.json`

```json
{
  "sftpd": {
    "bindings": [{ "port": 2222, "address": "0.0.0.0" }]
  },
  "data_provider": {
    "external_auth_hook": "http://localhost:5138/api/sftp-auth/check-credentials"
  }
}
```

### Test Credentials

| Type | Username | Password |
|------|----------|----------|
| SFTP | `testsftp` | `TestPassword123` |
| Portal | `sftptest@test.com` | `TestPassword123` |

---

## Quick Reference Commands

```powershell
# Run comprehensive test
.\sftp-test.ps1

# Setup only (creates user, directories)
.\sftp-test.ps1 -Setup

# Test SFTP only
.\sftp-test.ps1 -TestSFTP

# Check services status
.\sftp-test.ps1 -Status

# View BackgroundService logs
Get-Content "src\Presentation\EInvoiceIntegrator.BackgroundService\logs\*.txt" -Tail 100

# Start SFTPGo
cd "C:\Program Files\SFTPGo"; .\sftpgo.exe serve

# Check what's listening on port 2222
Get-NetTCPConnection -LocalPort 2222 -State Listen
```

---

## Support

- **Documentation:** See `SFTP-Testing-FAQ.md` for more Q&A
- **Logs:** `src/Presentation/EInvoiceIntegrator.BackgroundService/logs/`
- **SFTPGo Admin:** http://localhost:8080 (admin/admin)
- **Repository:** https://github.com/kpmg-digital-factory/EInvoiceIntegratorAPI

---

**Last Updated:** January 2025

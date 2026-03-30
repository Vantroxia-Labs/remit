# Cerberus SFTP Configuration Guide

## Overview

This document describes the configuration required for proper directory creation on the remote Cerberus FTP server.

## Problem Solved

Previously, user directories were not being physically created on the Cerberus server because:

1. **Architecture Mismatch**: The application was trying to create directories on the local application server instead of the remote Cerberus server
2. **Missing SFTP Connection**: There was no SFTP client connection to the Cerberus server for directory management
3. **Configuration Issues**: Different configuration paths were being used inconsistently

## Solution

The `CerberusSFTPConnectorService` now:

1. **Creates user in Cerberus** first using the SOAP API with admin credentials
2. **Uses user's own credentials** to connect via SFTP and create their directories
3. **Creates directory structure** in the user's allocated space on the Cerberus server
4. **Verifies directory creation** by checking if directories exist using user credentials
5. **Handles errors gracefully** without breaking user onboarding

## Configuration Required

Add the following configuration to your `appsettings.json`:

```json
{
  "CerberusService": {
    "Endpoint": "http://your-cerberus-server:10001/service/cerberusftpservice",
    "Username": "Admin",
    "Password": "your-admin-password",
    "BaseDirectory": "C:\\CerberusData",
    "SftpHost": "your-cerberus-server-ip",
    "SftpPort": 22
  }
}
```

### Configuration Parameters

| Parameter | Description | Example | Required |
|-----------|-------------|---------|----------|
| `Endpoint` | Cerberus SOAP service URL | `http://192.168.1.100:10001/service/cerberusftpservice` | Yes |
| `Username` | Cerberus admin username | `Admin` | Yes |
| `Password` | Cerberus admin password | `YourSecurePassword` | Yes |
| `BaseDirectory` | Root directory on Cerberus server | `C:\\CerberusData` | Yes |
| `SftpHost` | Cerberus server IP/hostname for SFTP | `192.168.1.100` | Optional* |
| `SftpPort` | SFTP port on Cerberus server | `22` | Optional |

*If `SftpHost` is not provided, it will be automatically extracted from the `Endpoint` URL.

## Directory Structure Created

For each SFTP user, the following directory structure is created on the Cerberus server:

```
{BaseDirectory}/
└── {username}/
    ├── PROCESSED/
    ├── NACK/
    └── ACK/
```

For example, if `BaseDirectory` is `C:\\CerberusData` and username is `testcompany`, the structure would be:

```
C:\\CerberusData/
└── testcompany/
    ├── PROCESSED/
    ├── NACK/
    └── ACK/
```

## How It Works

1. **User Creation**: When a business is onboarded, `OnboardBusinessCommandHandler` calls `CerberusSFTPConnectorService.AddUserAsync()`

2. **Cerberus User Creation**: The service creates the user in Cerberus via SOAP API using admin credentials

3. **User Permission Setup**: The user is granted `allowDirectoryCreation = true` permission in Cerberus

4. **Directory Creation**: The service connects via SFTP using the **new user's credentials** (not admin)

5. **Directory Structure**: Creates subdirectories (`PROCESSED`, `NACK`, `ACK`) in the user's allocated space

6. **Verification**: Verifies directories exist using the user's own SFTP connection

7. **Database Update**: Marks `DirectoriesCreated = true` in the database

## Troubleshooting

### Common Issues

1. **SFTP Connection Failed**
   - Verify `SftpHost` and `SftpPort` are correct
   - Ensure SFTP service is running on Cerberus server
   - Check firewall settings

2. **Authentication Failed**
   - Verify admin `Username` and `Password` are correct
   - Ensure admin user has SFTP access permissions

3. **Directory Creation Failed**
   - Check that admin user has write permissions on `BaseDirectory`
   - Verify `BaseDirectory` exists on Cerberus server
   - Check available disk space

### Logs to Check

The service provides detailed logging at these levels:

- **Information**: Major operations (user creation, directory creation success)
- **Warning**: Directory verification failures, authentication issues
- **Error**: Connection failures, directory creation errors
- **Debug**: Detailed SFTP operations, directory checks

Example log entries:

```
[INFO] Creating directories on Cerberus server for user: testcompany at path: C:\CerberusData\testcompany
[DEBUG] Connecting to Cerberus SFTP server at 192.168.1.100:22
[DEBUG] Created directory on Cerberus server: C:\CerberusData\testcompany
[DEBUG] Created directory on Cerberus server: C:\CerberusData\testcompany\PROCESSED
[INFO] Successfully created directory structure on Cerberus server for user: testcompany
```

### Manual Directory Creation

If automatic directory creation fails, you can manually create the directories on the Cerberus server:

1. Connect to the Cerberus server
2. Navigate to the `BaseDirectory` (e.g., `C:\CerberusData`)
3. Create the user directory (e.g., `testcompany`)
4. Create subdirectories: `PROCESSED`, `NACK`, `ACK`
5. Set appropriate permissions for the Cerberus FTP service

## Security Considerations

1. **Admin Credentials**: Admin credentials are only used for SOAP API user creation, not for SFTP operations
2. **User Isolation**: Each user creates their own directories using their own credentials (principle of least privilege)
3. **Network Security**: Ensure SFTP traffic is properly secured between application and Cerberus server
4. **Directory Permissions**: Users can only create directories in their own allocated space
5. **Credential Storage**: Store admin credentials securely, consider using Azure Key Vault or similar
6. **Logging**: Be careful not to log sensitive credentials in debug logs

## Testing

To test the configuration:

1. **Run Business Onboarding**: Create a new business and check logs
2. **Verify Directories**: Connect to Cerberus server and verify directories exist
3. **SFTP Test**: Try connecting to the created user via SFTP client
4. **File Operations**: Test uploading/downloading files to verify full functionality

## Migration from Old Implementation

If you're migrating from the old implementation:

1. **Update Configuration**: Add the new `SftpHost` and `SftpPort` settings
2. **Recreate Directories**: Existing users may need directories created manually
3. **Verify Settings**: Test with a new user to ensure everything works
4. **Monitor Logs**: Watch for any errors during the transition

## Dependencies

This solution requires:

- **SSH.NET (Renci.SshNet)**: For SFTP client functionality
- **Cerberus FTP Server**: With SFTP service enabled
- **Network Connectivity**: Between application server and Cerberus server on SFTP port

## Example Full Configuration

```json
{
  "CerberusService": {
    "Endpoint": "http://192.168.1.100:10001/service/cerberusftpservice",
    "Username": "Admin",
    "Password": "SecureAdminPassword2024!",
    "BaseDirectory": "C:\\CerberusData",
    "SftpHost": "192.168.1.100",
    "SftpPort": 22
  },
  "SftpConfiguration": {
    "FtpRootPath": "C:\\ftproot",
    "Host": "192.168.1.100",
    "Port": 22,
    "NackDirectory": "NACK",
    "AckDirectory": "ACK",
    "ProcessedDirectory": "PROCESSED",
    "FilePattern": "*.xml"
  }
}
```

This ensures consistent configuration between the Cerberus management service and the SFTP file processing services.
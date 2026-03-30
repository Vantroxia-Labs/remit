# User-Credential Based Directory Creation

## Overview

This document explains the improved approach where directories are created using the newly created user's own SFTP credentials instead of admin credentials.

## Why This Approach is Better

### Security Benefits
- **Principle of Least Privilege**: Users only have access to their own directories
- **Reduced Admin Exposure**: Admin credentials are not used for SFTP operations
- **User Isolation**: Each user operates in their own sandbox
- **Audit Trail**: Directory operations are traceable to specific users

### Technical Benefits
- **Simpler Configuration**: No need for admin SFTP access configuration
- **Better Error Handling**: SFTP errors are user-specific and easier to diagnose
- **Natural Permissions**: Directories are created with the user's natural permissions

## Implementation Flow

```
1. Business Onboarding Request
   ↓
2. Create Cerberus User (SOAP API with admin credentials)
   ↓
3. Wait for user creation to complete (2-second delay)
   ↓
4. Connect via SFTP using NEW USER credentials
   ↓
5. Create subdirectories: PROCESSED, NACK, ACK
   ↓
6. Verify directories exist using same user connection
   ↓
7. Mark DirectoriesCreated = true in database
```

## Configuration Required

Only the SFTP host and port are needed for the user connections:

```json
{
  "CerberusService": {
    "Endpoint": "http://your-cerberus-server:10001/service/cerberusftpservice",
    "Username": "Admin",
    "Password": "admin-password",
    "BaseDirectory": "C:\\CerberusData",
    "SftpHost": "your-cerberus-server-ip",  // For user SFTP connections
    "SftpPort": 22
  }
}
```

## User Permission Requirements

When creating users in Cerberus, they must have:

- `allowDirectoryCreation = true` (enables directory creation)
- `allowListDir = true` (enables directory verification)
- Standard file operations permissions

## Example Log Output

```
[INFO] User testcompany created successfully in Cerberus
[INFO] Creating directories on Cerberus server using user credentials for: testcompany
[DEBUG] Connecting to Cerberus SFTP server as user testcompany at 192.168.1.100:22
[DEBUG] Connected to Cerberus SFTP server successfully as user: testcompany
[DEBUG] User testcompany connected to SFTP, working directory: /testcompany
[DEBUG] Created subdirectory on Cerberus server: PROCESSED
[DEBUG] Created subdirectory on Cerberus server: NACK
[DEBUG] Created subdirectory on Cerberus server: ACK
[INFO] Successfully created directory structure on Cerberus server using user credentials for: testcompany
```

## Directory Structure Created

Each user gets their own isolated directory structure:

```
Cerberus Server Root (e.g., C:\CerberusData)
├── testcompany/                    # User's root directory (created by Cerberus)
│   ├── PROCESSED/                  # Created by user via SFTP
│   ├── NACK/                       # Created by user via SFTP
│   └── ACK/                        # Created by user via SFTP
├── anothercompany/
│   ├── PROCESSED/
│   ├── NACK/
│   └── ACK/
└── ...
```

## Error Handling

### User Creation Fails
- SOAP API returns error
- No SFTP connection attempted
- User onboarding fails gracefully

### SFTP Connection Fails
- User was created but cannot connect via SFTP
- Directory creation is skipped
- `DirectoriesCreated = false` in database
- User onboarding continues (directories can be created manually)

### Directory Creation Fails
- User connects but cannot create directories
- Check user permissions in Cerberus
- Verify disk space and path permissions on server

## Troubleshooting

### "Authentication failed" errors
- Verify user was actually created in Cerberus
- Check if there's a delay needed after user creation
- Verify SFTP service is running on Cerberus server

### "Permission denied" errors
- Ensure `allowDirectoryCreation = true` in user permissions
- Check that the user's root directory exists and is writable
- Verify Cerberus FTP service has proper file system permissions

### "Directory already exists" warnings
- Normal if directories were created previously
- Verification will still check that all required directories exist

## Migration from Admin-Credential Approach

If you're migrating from the admin-credential approach:

1. **Update Code**: Deploy the new user-credential implementation
2. **Test New Users**: Verify new user onboarding works correctly
3. **Existing Users**: May need manual directory creation or re-onboarding
4. **Remove Admin SFTP Config**: No longer need admin SFTP credentials

## Benefits Summary

✅ **More Secure**: Users operate in their own space with minimal privileges
✅ **Easier Configuration**: No admin SFTP credentials needed
✅ **Better Isolation**: Users cannot access each other's directories
✅ **Cleaner Audit Trail**: All operations are traceable to specific users
✅ **Simpler Troubleshooting**: User-specific errors are easier to diagnose
✅ **Follows Best Practices**: Implements principle of least privilege
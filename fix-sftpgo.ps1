# =========================================================================
# SFTPGo Fix Script
# =========================================================================
# Run this script as ADMINISTRATOR to fix SFTPGo configuration and templates
# =========================================================================

$sftpGoDir = "C:\Program Files\SFTPGo"
$configFile = "$sftpGoDir\sftpgo.json"
$templatesDir = "$sftpGoDir\templates\email"
$localConfig = ".\sftpgo-config.json"

# Check Admin Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "   Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# 1. Create Templates Directory
Write-Host "Creating templates directory..." -ForegroundColor Yellow
if (-not (Test-Path $templatesDir)) {
    New-Item -Path $templatesDir -ItemType Directory -Force | Out-Null
    Write-Host "✓ Created directory: $templatesDir" -ForegroundColor Green
}

# 2. Create Dummy Template Files
$dummyContent = "<!DOCTYPE html><html><body>SFTPGo Notification</body></html>"
$templates = @("password-expiration.html", "reset-password.html", "email-verification.html", "account-disabled.html")

foreach ($tpl in $templates) {
    $path = "$templatesDir\$tpl"
    if (-not (Test-Path $path)) {
        Set-Content -Path $path -Value $dummyContent -Encoding UTF8
        Write-Host "✓ Created template: $tpl" -ForegroundColor Green
    }
}

# 3. Clean up Database Files
Write-Host "Cleaning up database files..." -ForegroundColor Yellow
$dbFiles = @("sftpgo.db", "sftpgo.db-shm", "sftpgo.db-wal")
foreach ($file in $dbFiles) {
    $path = "$sftpGoDir\$file"
    if (Test-Path $path) {
        Remove-Item -Path $path -Force
        Write-Host "✓ Deleted $file" -ForegroundColor Green
    }
}

# 4. Create Empty Users File
$usersFile = "$sftpGoDir\sftpgo_users.json"
if (-not (Test-Path $usersFile)) {
    Set-Content -Path $usersFile -Value "{}" -Encoding UTF8
    Write-Host "✓ Created empty users file: $usersFile" -ForegroundColor Green
}

# 5. Overwrite Configuration File
Write-Host "Updating configuration file..." -ForegroundColor Yellow
if (Test-Path $localConfig) {
    Copy-Item -Path $localConfig -Destination $configFile -Force
    Write-Host "✓ Overwrote sftpgo.json with local config" -ForegroundColor Green
} else {
    Write-Host "❌ Local config file not found: $localConfig" -ForegroundColor Red
}

# 6. Verify Users Directory
$usersDir = "C:\ftproot"
if (-not (Test-Path $usersDir)) {
    New-Item -Path $usersDir -ItemType Directory -Force | Out-Null
    Write-Host "✓ Created users directory: $usersDir" -ForegroundColor Green
}
# Grant permissions
icacls $usersDir /grant Everyone:F /T /Q | Out-Null
Write-Host "✓ Permissions granted on $usersDir" -ForegroundColor Green

# Grant permissions on SFTPGo directory (for templates access)
icacls "$sftpGoDir" /grant Everyone:R /T /Q | Out-Null
Write-Host "✓ Permissions granted on SFTPGo directory" -ForegroundColor Green

Write-Host ""
Write-Host "✅ Fix complete! You can now start SFTPGo:" -ForegroundColor Green
Write-Host "   cd '$sftpGoDir'" -ForegroundColor Gray
Write-Host "   .\sftpgo.exe serve" -ForegroundColor Gray

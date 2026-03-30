# =========================================================================
# Restart SFTPGo with Updated Configuration
# RUN AS ADMINISTRATOR
# =========================================================================

# Check Admin Privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "❌ ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "   Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "  SFTPGo Restart Script" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan

# 1. Stop SFTPGo processes
Write-Host "`n[1/4] Stopping SFTPGo processes..." -ForegroundColor Yellow
Get-Process sftpgo -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Write-Host "✓ SFTPGo stopped" -ForegroundColor Green

# 2. Update configuration
Write-Host "`n[2/4] Updating SFTPGo configuration..." -ForegroundColor Yellow
$configPath = "C:\Program Files\SFTPGo\sftpgo.json"
$config = Get-Content $configPath | ConvertFrom-Json

# Add create_default_dir if not present
if (-not $config.common.PSObject.Properties['create_default_dir']) {
    $config.common | Add-Member -NotePropertyName "create_default_dir" -NotePropertyValue $true -Force
}
else {
    $config.common.create_default_dir = $true
}

$config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
Write-Host "✓ Configuration updated with create_default_dir: true" -ForegroundColor Green

# 3. Verify the BusinessId folder structure exists
Write-Host "`n[3/4] Verifying directory structure..." -ForegroundColor Yellow
$businessId = "019c754d-c4aa-794c-9f12-3a98b629feb3"
$basePath = "C:\ftproot\uploads\$businessId"

if (Test-Path $basePath) {
    $folders = Get-ChildItem $basePath -Directory | Select-Object -ExpandProperty Name
    Write-Host "✓ Home directory exists: $basePath" -ForegroundColor Green
    Write-Host "  Subdirectories: $($folders -join ', ')" -ForegroundColor Gray
}
else {
    Write-Host "⚠ Warning: Home directory does not exist: $basePath" -ForegroundColor Yellow
}

# 4. Restart SFTPGo
Write-Host "`n[4/4] Starting SFTPGo..." -ForegroundColor Yellow
$sftpgoExe = "C:\Program Files\SFTPGo\sftpgo.exe"

if (Test-Path $sftpgoExe) {
    Start-Process -FilePath $sftpgoExe -ArgumentList "serve" -WorkingDirectory "C:\Program Files\SFTPGo" -WindowStyle Hidden
    Start-Sleep -Seconds 3
    
    $process = Get-Process sftpgo -ErrorAction SilentlyContinue
    if ($process) {
        Write-Host "✓ SFTPGo started successfully (PID: $($process.Id))" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Failed to start SFTPGo" -ForegroundColor Red
    }
}
else {
    Write-Host "❌ SFTPGo executable not found at: $sftpgoExe" -ForegroundColor Red
}

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host "  Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Wait 5 seconds for SFTPGo to fully start" -ForegroundColor White
Write-Host "  2. Disconnect and reconnect in WinSCP" -ForegroundColor White
Write-Host "  3. The folders should now appear" -ForegroundColor White
Write-Host "==================================================================" -ForegroundColor Cyan

pause

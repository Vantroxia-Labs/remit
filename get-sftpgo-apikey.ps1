# Get SFTPGo API Key
# Run this after restarting SFTPGo with web admin enabled

$adminUser = "admin"
$adminPass = "Sftp@Admin123"

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "     SFTPGo API Key Generator" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 1: Initializing SFTPGo admin user..." -ForegroundColor Yellow
try {
    $initOutput = & "C:\Program Files\SFTPGo\sftpgo.exe" initprovider --admin-username $adminUser --admin-password $adminPass 2>&1
    Write-Host "  Admin user initialized" -ForegroundColor Green
}
catch {
    Write-Host "  Warning: Could not run initprovider (may already exist)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Step 2: Restarting SFTPGo service..." -ForegroundColor Yellow
try {
    Restart-Service -Name "SFTPGo" -ErrorAction Stop
    Write-Host "  Service restarted" -ForegroundColor Green
    Start-Sleep -Seconds 5
}
catch {
    Write-Host "  Failed to restart service: $_" -ForegroundColor Red
    Write-Host "  Please restart SFTPGo manually" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Step 3: Getting JWT token..." -ForegroundColor Yellow
try {
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${adminUser}:${adminPass}"))
    $tokenResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v2/token" -Method Get -Headers @{
        Authorization = "Basic $base64Auth"
    } -ErrorAction Stop
    $token = $tokenResponse.access_token
    Write-Host "  Token acquired" -ForegroundColor Green
}
catch {
    Write-Host "  Failed to get token" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Fallback: Use Web Admin UI" -ForegroundColor Yellow
    Write-Host "  1. Go to: http://localhost:8080/web/admin" -ForegroundColor White
    Write-Host "  2. Login with: admin / $adminPass" -ForegroundColor White
    Write-Host "  3. Navigate to: API Keys > Add" -ForegroundColor White
    Write-Host "  4. Copy the generated key to appsettings.json" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "Step 4: Creating API key..." -ForegroundColor Yellow
try {
    $apiKeyPayload = @{
        name = "einvoice-background-service"
        scope = 1
        description = "API key for EInvoice Background Service to manage SFTP users"
    } | ConvertTo-Json

    $apiKeyResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v2/apikeys" -Method Post -Headers @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } -Body $apiKeyPayload -ErrorAction Stop

    Write-Host "  API key created!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "       SUCCESS! Your API Key" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host $apiKeyResponse.key -ForegroundColor Yellow
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Add this to appsettings.json:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host @"
"SftpGo": {
  "AdminApiUrl": "http://localhost:8080",
  "ApiKey": "$($apiKeyResponse.key)"
}
"@ -ForegroundColor White

    # Copy to clipboard
    $apiKeyResponse.key | Set-Clipboard
    Write-Host ""
    Write-Host "(✓ Key copied to clipboard)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Admin credentials: $adminUser / $adminPass" -ForegroundColor Gray
    Write-Host "Web Admin: http://localhost:8080/web/admin" -ForegroundColor Gray
}
catch {
    Write-Host "  Failed to create API key" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Fallback: Use Web Admin UI" -ForegroundColor Yellow
    Write-Host "  1. Go to: http://localhost:8080/web/admin" -ForegroundColor White
    Write-Host "  2. Login with: admin / $adminPass" -ForegroundColor White
    Write-Host "  3. Navigate to: API Keys > Add" -ForegroundColor White
    Write-Host "  4. Copy the generated key to appsettings.json" -ForegroundColor White
    exit 1
}

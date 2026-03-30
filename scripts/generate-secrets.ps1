# =================================================================
# PowerShell Script to Generate Secure Keys for Production
# =================================================================
# Run this script to generate cryptographically secure keys
# Usage: .\generate-secrets.ps1
# =================================================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "E-Invoice Integrator - Secret Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to generate random base64 string
function Generate-SecureKey {
    param (
        [int]$ByteLength
    )
    $bytes = New-Object byte[] $ByteLength
    $rng = [System.Security.Cryptography.RNGCryptoServiceProvider]::Create()
    $rng.GetBytes($bytes)
    $base64 = [Convert]::ToBase64String($bytes)
    return $base64
}

# Function to generate random password
function Generate-SecurePassword {
    param (
        [int]$Length = 24
    )
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?"
    $password = -join ((1..$Length) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
    return $password
}

Write-Host "GENERATED SECURE KEYS:" -ForegroundColor Green
Write-Host "======================" -ForegroundColor Green
Write-Host ""

# JWT Secret Key (256-bit)
$jwtKey = Generate-SecureKey -ByteLength 64
Write-Host "JWT_SECRET_KEY:" -ForegroundColor Yellow
Write-Host $jwtKey -ForegroundColor White
Write-Host ""

# Encryption Key (256-bit, exactly 44 chars for AES-256)
$encryptionKey = Generate-SecureKey -ByteLength 32
Write-Host "ENCRYPTION_KEY:" -ForegroundColor Yellow
Write-Host $encryptionKey -ForegroundColor White
Write-Host ""

# Backup Encryption Key
$backupKey = Generate-SecureKey -ByteLength 32
Write-Host "BACKUP_ENCRYPTION_KEY:" -ForegroundColor Yellow
Write-Host $backupKey -ForegroundColor White
Write-Host ""

# Database Password
$dbPassword = Generate-SecurePassword -Length 24
Write-Host "DATABASE_PASSWORD:" -ForegroundColor Yellow
Write-Host $dbPassword -ForegroundColor White
Write-Host ""

# Redis Password
$redisPassword = Generate-SecurePassword -Length 20
Write-Host "REDIS_PASSWORD:" -ForegroundColor Yellow
Write-Host $redisPassword -ForegroundColor White
Write-Host ""

# RabbitMQ Password
$rabbitPassword = Generate-SecurePassword -Length 20
Write-Host "RABBITMQ_PASSWORD:" -ForegroundColor Yellow
Write-Host $rabbitPassword -ForegroundColor White
Write-Host ""

# SMTP Password
$smtpPassword = Generate-SecurePassword -Length 20
Write-Host "SMTP_PASSWORD:" -ForegroundColor Yellow
Write-Host $smtpPassword -ForegroundColor White
Write-Host ""

# OAuth Client Secret
$oauthSecret = Generate-SecureKey -ByteLength 32
Write-Host "OAUTH_CLIENT_SECRET:" -ForegroundColor Yellow
Write-Host $oauthSecret -ForegroundColor White
Write-Host ""

# SSL Certificate Password
$sslPassword = Generate-SecurePassword -Length 16
Write-Host "SSL_CERT_PASSWORD:" -ForegroundColor Yellow
Write-Host $sslPassword -ForegroundColor White
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IMPORTANT SECURITY NOTES:" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "1. Copy these values to your .env.production file" -ForegroundColor White
Write-Host "2. Store them securely in a password manager" -ForegroundColor White
Write-Host "3. Never commit these values to version control" -ForegroundColor White
Write-Host "4. Rotate these keys every 90 days" -ForegroundColor White
Write-Host "5. Use different keys for each environment" -ForegroundColor White
Write-Host ""

# Option to save to file
$saveToFile = Read-Host "Save to secrets.txt file? (yes/no)"
if ($saveToFile -eq "yes") {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "secrets_$timestamp.txt"

    @"
E-Invoice Integrator - Generated Secrets
Generated: $(Get-Date)
=========================================

JWT_SECRET_KEY=$jwtKey
ENCRYPTION_KEY=$encryptionKey
BACKUP_ENCRYPTION_KEY=$backupKey
DATABASE_PASSWORD=$dbPassword
REDIS_PASSWORD=$redisPassword
RABBITMQ_PASSWORD=$rabbitPassword
SMTP_PASSWORD=$smtpPassword
OAUTH_CLIENT_SECRET=$oauthSecret
SSL_CERT_PASSWORD=$sslPassword

=========================================
WARNING: This file contains sensitive information.
Delete after copying to secure storage.
"@ | Out-File -FilePath $filename

    Write-Host "Secrets saved to: $filename" -ForegroundColor Green
    Write-Host "DELETE THIS FILE after copying the values!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Script completed successfully!" -ForegroundColor Green
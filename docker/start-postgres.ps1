# PowerShell script to start PostgreSQL with Docker Compose
# Run this script from the project root directory

Write-Host "Starting PostgreSQL container with Docker Compose..." -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Navigate to project root
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

Write-Host "Project root: $projectRoot" -ForegroundColor Yellow

# Start PostgreSQL and pgAdmin services
Write-Host "Starting PostgreSQL and pgAdmin services..." -ForegroundColor Green
docker-compose up -d postgresql pgadmin

# Wait for PostgreSQL to be healthy
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
$timeout = 60
$elapsed = 0

do {
    $healthStatus = docker-compose ps --format json postgresql | ConvertFrom-Json | Select-Object -ExpandProperty Health
    if ($healthStatus -eq "healthy") {
        Write-Host "PostgreSQL is ready!" -ForegroundColor Green
        break
    }
    
    Start-Sleep -Seconds 2
    $elapsed += 2
    Write-Host "Waiting... ($elapsed/$timeout seconds)" -ForegroundColor Yellow
    
} while ($elapsed -lt $timeout)

if ($elapsed -ge $timeout) {
    Write-Host "Timeout waiting for PostgreSQL to be ready. Check Docker logs:" -ForegroundColor Red
    docker-compose logs postgresql
    exit 1
}

Write-Host "`nPostgreSQL is now running!" -ForegroundColor Green
Write-Host "Database: einvoiceintegrator_dev" -ForegroundColor Cyan
Write-Host "Host: localhost" -ForegroundColor Cyan
Write-Host "Port: 5432" -ForegroundColor Cyan
Write-Host "Username: postgres" -ForegroundColor Cyan
Write-Host "Password: postgres" -ForegroundColor Cyan

Write-Host "`npgAdmin is available at: http://localhost:8080" -ForegroundColor Cyan
Write-Host "pgAdmin Email: admin@einvoice.com" -ForegroundColor Cyan
Write-Host "pgAdmin Password: admin123" -ForegroundColor Cyan

Write-Host "`nTo stop the services, run: docker-compose down" -ForegroundColor Yellow
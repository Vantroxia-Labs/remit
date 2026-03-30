@echo off
REM Batch script to start PostgreSQL with Docker Compose
REM Run this script from the project root directory

echo Starting PostgreSQL container with Docker Compose...

REM Check if Docker is running
docker version >nul 2>&1
if %errorlevel% neq 0 (
    echo Docker is not running. Please start Docker Desktop first.
    pause
    exit /b 1
)

echo Docker is running

REM Navigate to project root (parent directory of docker folder)
cd /d "%~dp0\.."

echo Project root: %cd%

REM Start PostgreSQL and pgAdmin services
echo Starting PostgreSQL and pgAdmin services...
docker-compose up -d postgresql pgadmin

REM Wait a bit for services to start
timeout /t 10 >nul

echo.
echo PostgreSQL is now starting!
echo Database: einvoiceintegrator_dev
echo Host: localhost
echo Port: 5432
echo Username: postgres
echo Password: postgres
echo.
echo pgAdmin is available at: http://localhost:8080
echo pgAdmin Email: admin@einvoice.com
echo pgAdmin Password: admin123
echo.
echo To stop the services, run: docker-compose down
pause
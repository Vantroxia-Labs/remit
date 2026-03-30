# Docker Setup for EInvoice Integrator

This project uses Docker to run PostgreSQL database for development.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Quick Start

### Option 1: Using PowerShell Script (Recommended for Windows)
```powershell
# Run from project root
.\docker\start-postgres.ps1
```

### Option 2: Using Batch Script
```cmd
# Run from project root
docker\start-postgres.bat
```

### Option 3: Manual Docker Compose Commands
```bash
# Start PostgreSQL and pgAdmin
docker-compose up -d postgresql pgadmin

# Check status
docker-compose ps

# View logs
docker-compose logs postgresql

# Stop services
docker-compose down
```

## Services

### PostgreSQL Database
- **Container Name**: `einvoice-postgres`
- **Host**: `localhost`
- **Port**: `5432`
- **Database**: `einvoiceintegrator_dev`
- **Username**: `postgres`
- **Password**: `postgres`

### pgAdmin (Database Management UI)
- **URL**: http://localhost:8080
- **Email**: admin@einvoice.com
- **Password**: admin123

## Application Configuration

The application is already configured to connect to the Docker PostgreSQL instance via the connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=einvoiceintegrator_dev;Username=postgres;Password=postgres;Include Error Detail=true"
  }
}
```

## Usage Workflow

1. **Start Docker Services**:
   ```bash
   docker-compose up -d postgresql pgadmin
   ```

2. **Run the Application**:
   ```bash
   cd src/Presentation/EInvoiceIntegrator.API
   dotnet run
   ```

3. **Access pgAdmin** (optional):
   - Open http://localhost:8080
   - Login with admin@einvoice.com / admin123
   - Add server connection to `localhost:5432` with postgres/postgres credentials

4. **Stop Services** (when done):
   ```bash
   docker-compose down
   ```

## Database Initialization

The database will be automatically initialized with:
- Extensions: `uuid-ossp`, `pgcrypto`
- Timezone set to UTC
- Proper permissions for the postgres user

## Troubleshooting

### PostgreSQL won't start
- Ensure Docker Desktop is running
- Check if port 5432 is already in use: `netstat -an | findstr 5432`
- View logs: `docker-compose logs postgresql`

### Application can't connect to database
- Verify PostgreSQL container is running: `docker-compose ps`
- Check PostgreSQL health: `docker-compose exec postgresql pg_isready -U postgres`
- Ensure connection string matches the Docker configuration

### Reset database
```bash
# Stop services and remove volumes
docker-compose down -v

# Start fresh
docker-compose up -d postgresql pgadmin
```

## Data Persistence

Database data is persisted in Docker volumes:
- `postgres_data`: Database files
- `pgadmin_data`: pgAdmin settings

Data will survive container restarts but will be lost if you use `docker-compose down -v`.
# PostgreSQL + Docker Setup Instructions for EInvoice Integrator

## 🚀 Quick Setup (3 Steps)

### Step 1: Start PostgreSQL with Docker

Choose **ONE** of these options:

#### Option A: Simple PostgreSQL only (Recommended)
```bash
# From the project root directory
docker-compose -f docker-compose.simple.yml up -d
```

#### Option B: PostgreSQL + pgAdmin (Database Management UI)
```bash
# From the project root directory  
docker-compose up -d postgresql
```

#### Option C: Using PowerShell script (Windows)
```powershell
.\docker\start-postgres.ps1
```

### Step 2: Verify PostgreSQL is Running
```bash
# Check container status
docker ps

# You should see a container named 'einvoice-postgres' or 'einvoice-postgres-simple'
```

### Step 3: Run Your Application
```bash
cd src/Presentation/EInvoiceIntegrator.API
dotnet run
```

## ✅ Expected Results

When everything works correctly, you should see:

1. **PostgreSQL Container**: Running and healthy
2. **Application Output**: 
   ```
   [12:21:50 INF] Starting EInvoice Integrator API
   [12:21:50 INF] Migrating database...
   [12:21:50 INF] Now listening on: http://localhost:5242
   [12:21:50 INF] Application started. Press Ctrl+C to shut down.
   ```

## 📋 Database Connection Details

- **Host**: localhost
- **Port**: 5432
- **Database**: einvoiceintegrator_dev
- **Username**: postgres
- **Password**: postgres

## 🔧 Troubleshooting

### PostgreSQL Won't Start

**Problem**: Docker container fails to start or exits immediately.

**Solutions**:
```bash
# Check if port 5432 is already in use
netstat -an | findstr 5432

# If port is in use, kill the process or change the port in docker-compose.yml
# Then restart
docker-compose down
docker-compose up -d postgresql
```

### Docker Image Download is Slow

**Problem**: PostgreSQL image download is taking too long.

**Solutions**:
```bash
# Try pulling the image manually first
docker pull postgres:15-alpine

# Or use a different PostgreSQL version (smaller image)
# Edit docker-compose.yml and change:
# image: postgres:15-alpine
# to:
# image: postgres:13-alpine
```

### Application Can't Connect to Database

**Problem**: Application throws connection errors.

**Solutions**:
1. **Check PostgreSQL is running**:
   ```bash
   docker ps
   docker logs einvoice-postgres
   ```

2. **Test connection manually**:
   ```bash
   # Install postgresql-client if needed, then:
   psql -h localhost -p 5432 -U postgres -d einvoiceintegrator_dev
   ```

3. **Check connection string** in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=einvoiceintegrator_dev;Username=postgres;Password=postgres;Include Error Detail=true"
     }
   }
   ```

### Reset Everything

**Problem**: Something is broken, need to start fresh.

**Solution**:
```bash
# Stop and remove everything (this will delete your data!)
docker-compose down -v

# Remove images (optional)
docker rmi postgres:15-alpine

# Start fresh
docker-compose up -d postgresql
```

## 🎯 Alternative: Using pgAdmin (Database Management)

If you started with Option B, you can access pgAdmin:

1. **Open**: http://localhost:8080
2. **Login**: 
   - Email: admin@einvoice.com
   - Password: admin123
3. **Add Server**:
   - Name: EInvoice DB
   - Host: postgresql (when inside Docker) or localhost (from outside)
   - Port: 5432
   - Username: postgres
   - Password: postgres

## 📝 Usage Workflow

### Daily Development
```bash
# 1. Start PostgreSQL
docker-compose up -d postgresql

# 2. Run your application  
cd src/Presentation/EInvoiceIntegrator.API
dotnet run

# 3. When done, stop PostgreSQL (optional)
docker-compose down
```

### First-time Setup Only
```bash
# 1. Create and start PostgreSQL
docker-compose up -d postgresql

# 2. Application will automatically create database schema on first run
cd src/Presentation/EInvoiceIntegrator.API
dotnet run

# The application will run Entity Framework migrations automatically
```

## 🔍 Verify Everything Works

Run this test to confirm your setup:

```bash
# 1. Check Docker container
docker ps | grep postgres

# 2. Check application startup
cd src/Presentation/EInvoiceIntegrator.API
dotnet run

# 3. Look for these log messages:
# ✅ "Migrating database..."
# ✅ "Application started. Press Ctrl+C to shut down."
# ❌ Any database connection errors
```

## 🚨 Common Issues & Quick Fixes

| Issue | Quick Fix |
|-------|-----------|
| Port 5432 in use | `docker-compose down && docker-compose up -d` |
| Container won't start | `docker logs einvoice-postgres` |
| App can't connect | Check connection string matches Docker settings |
| Database empty | Let the app run - EF migrations will create schema |
| Slow startup | Wait for "Application started" message |

## 📊 What's Created

After successful setup, you'll have:

- ✅ PostgreSQL database server running in Docker
- ✅ Database: `einvoiceintegrator_dev`
- ✅ All necessary Entity Framework tables (created by app migrations)
- ✅ ULID support configured properly
- ✅ Application connecting and running successfully

**You're ready to develop!** 🎉
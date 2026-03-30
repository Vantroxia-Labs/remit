# EInvoice Integrator Background Service - Docker Deployment

This document provides instructions for building and running the EInvoice Integrator Background Service using Docker.

## Overview

The EInvoice Integrator Background Service is a .NET 9.0 application that runs continuously using Quartz scheduler to process invoice files via SFTP and integrate with FIRS (Federal Inland Revenue Service).

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)
- Access to the configured database and SFTP servers

## Files Created

1. **`Dockerfile`** - Multi-stage Docker build configuration
2. **`docker-compose.yml`** - Updated with background service configuration
3. **`.dockerignore`** - Optimizes build context by excluding unnecessary files
4. **`appsettings.Production.json`** - Production configuration for the containerized service

## Quick Start

### 1. Build and Run with Docker Compose (Recommended)

```bash
# Navigate to the project root
cd C:\Users\brave\source\repos\EInvoiceBackend

# Build and start the background service
docker-compose up -d einvoice-background-service

# View logs
docker-compose logs -f einvoice-background-service

# Stop the service
docker-compose down
```

### 2. Build Docker Image Manually

```bash
# Navigate to the project root
cd C:\Users\brave\source\repos\EInvoiceBackend

# Build the Docker image
docker build -f src/Presentation/EInvoiceIntegrator.BackgroundService/Dockerfile -t einvoice-integrator-bg:latest .

# Run the container
docker run -d \
  --name einvoice-background-service \
  -p 8081:8080 \
  -v ./logs:/app/logs \
  -v ./data:/app/data \
  -e ASPNETCORE_ENVIRONMENT=Production \
  einvoice-integrator-bg:latest
```

## Configuration

### Environment Variables

The service can be configured using environment variables. Key configurations include:

```bash
# Database
DB_CONNECTION_STRING=Host=pg-2ec9c3a8-aresdims-2dc9.g.aivencloud.com;Port=22331;Database=EInvoiceUsermgtDb;Username=avnadmin;Password=AVNS_De_tHsKg5She-pUD_IC;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true

# SFTP Settings
SFTP_HOST=172.236.28.121
SFTP_USERNAME=kpmg
SFTP_PASSWORD=Nigeriasns19$

# Processing Settings
PROCESSING_INTERVAL=30
MAX_FILES_PER_BATCH=50
ENABLE_PARALLEL_PROCESSING=true

# Email Settings
SMTP_SERVER=email-smtp.us-east-1.amazonaws.com
SMTP_PORT=587
SMTP_USERNAME=AKIA4ESZHHNERFGJFKMF
SMTP_PASSWORD=BNBtIWD1XdUo7M8UlGWTZvkWHI3atVL8G1V9Sh6NGoqY
```

### Custom Configuration

To use custom configuration:

1. Create a `.env` file in the project root (copy from `.env.example`)
2. Update the values as needed
3. The Docker Compose setup will automatically use these values

## Service Endpoints

Once running, the service exposes several endpoints:

- **Health Check**: `http://localhost:8081/health`
- **Live Check**: `http://localhost:8081/health/live`
- **Ready Check**: `http://localhost:8081/health/ready`
- **Service Status**: `http://localhost:8081/status`
- **Manual Trigger**: `POST http://localhost:8081/trigger`

## Features

### Continuous Operation
- Runs as a long-running service using ASP.NET Core hosting
- Quartz scheduler manages background job execution
- Automatic restart on failure with `restart: unless-stopped`

### Health Monitoring
- Built-in health checks for SFTP connectivity
- Docker health check integration
- Comprehensive logging with Serilog

### Security
- Runs as non-root user (`appuser`)
- Environment variable configuration for sensitive data
- SSL/TLS support for SMTP and database connections

### Volume Mounts
- **`./logs:/app/logs`** - Persistent log storage
- **`./data:/app/data`** - Application data storage
- **`app-temp:/tmp`** - Temporary files

## Monitoring

### View Logs
```bash
# Real-time logs
docker-compose logs -f einvoice-background-service

# Last 100 lines
docker-compose logs --tail=100 einvoice-background-service
```

### Health Checks
```bash
# Check service health
curl http://localhost:8081/health

# Check if service is live
curl http://localhost:8081/health/live

# Check if service is ready
curl http://localhost:8081/health/ready
```

### Service Status
```bash
# Get detailed service status
curl http://localhost:8081/status
```

## Scaling and Production Deployment

### Resource Limits
Add resource limits to docker-compose.yml:

```yaml
deploy:
  resources:
    limits:
      memory: 512M
      cpus: '0.5'
    reservations:
      memory: 256M
      cpus: '0.25'
```

### Multiple Instances
For high availability, you can run multiple instances:

```bash
docker-compose up -d --scale einvoice-background-service=2
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**: If port 8081 is in use, change it in docker-compose.yml
2. **Volume Permissions**: Ensure the host directories have appropriate permissions
3. **Network Connectivity**: Verify access to database and SFTP servers from the container
4. **Configuration Issues**: Check environment variables and appsettings.json

### Debug Mode

To run with debug logging:

```bash
docker-compose run --rm \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e Logging__LogLevel__Default=Debug \
  einvoice-background-service
```

## Maintenance

### Updating the Service

1. Pull latest code changes
2. Rebuild the image:
   ```bash
   docker-compose build einvoice-background-service
   ```
3. Restart the service:
   ```bash
   docker-compose up -d einvoice-background-service
   ```

### Backup

Regular backup of:
- Application logs (`./logs/`)
- Application data (`./data/`)
- Database (separate backup strategy)

## Security Considerations

1. **Secrets Management**: Consider using Docker secrets or external secret management
2. **Network Security**: Use custom networks to isolate containers
3. **Image Security**: Regularly update base images
4. **Access Control**: Limit container capabilities and use read-only filesystems where possible

## Support

For issues or questions:
1. Check the application logs first
2. Verify configuration settings
3. Test connectivity to external services (database, SFTP, SMTP)
4. Review Docker and container logs
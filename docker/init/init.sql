-- PostgreSQL Initialization Script
-- This script runs automatically when the database is first created

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create schemas (if needed)
-- CREATE SCHEMA IF NOT EXISTS einvoice;

-- Set default permissions
-- GRANT ALL PRIVILEGES ON DATABASE einvoiceintegrator_dev TO postgres;

-- You can add initial data or table creation here if needed
-- Note: Entity Framework migrations will handle most schema creation

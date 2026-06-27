#!/bin/bash

# =================================================================
# Bash Script to Generate Secure Keys for Production
# =================================================================
# Run this script to generate cryptographically secure keys
# Usage: ./generate-secrets.sh
# =================================================================

echo "========================================"
echo "E-Invoice Integrator - Secret Generator"
echo "========================================"
echo ""

# Function to generate random base64 string
generate_secure_key() {
    local byte_length=$1
    openssl rand -base64 $byte_length | tr -d '\n'
}

# Function to generate random hex string (for raw UTF-8 keys/IVs)
generate_hex_key() {
    local byte_length=$1
    openssl rand -hex $byte_length | tr -d '\n'
}

# Function to generate random password
generate_secure_password() {
    local length=${1:-24}
    tr -dc 'A-Za-z0-9!@#$%^&*()_+-=[]{}|;:,.<>?' < /dev/urandom | head -c $length
}

echo "GENERATED SECURE KEYS:"
echo "======================"
echo ""

# JWT Secret Key (256-bit)
JWT_KEY=$(generate_secure_key 64)
echo "JWT_SECRET_KEY:"
echo "$JWT_KEY"
echo ""

# Internal Encryption Key (256-bit, exactly 32 UTF-8 characters for AES-256)
ENCRYPTION_KEY=$(generate_hex_key 16)
echo "ENCRYPTION_KEY (32-chars plain text):"
echo "$ENCRYPTION_KEY"
echo ""

# Internal Encryption IV (128-bit, exactly 16 UTF-8 characters for AES-256 block size)
ENCRYPTION_IV=$(generate_hex_key 8)
echo "ENCRYPTION_IV (16-chars plain text):"
echo "$ENCRYPTION_IV"
echo ""

# Backup Encryption Key (exactly 32 UTF-8 characters)
BACKUP_KEY=$(generate_hex_key 16)
echo "BACKUP_ENCRYPTION_KEY (32-chars plain text):"
echo "$BACKUP_KEY"
echo ""

# Payload Encryption Key (base64-encoded, must decode to exactly 32 bytes)
PAYLOAD_ENCRYPTION_KEY=$(generate_secure_key 32)
echo "PAYLOAD_ENCRYPTION_KEY:"
echo "$PAYLOAD_ENCRYPTION_KEY"
echo ""

# Database Password
DB_PASSWORD=$(generate_secure_password 24)
echo "DATABASE_PASSWORD:"
echo "$DB_PASSWORD"
echo ""

# Redis Password
REDIS_PASSWORD=$(generate_secure_password 20)
echo "REDIS_PASSWORD:"
echo "$REDIS_PASSWORD"
echo ""

# RabbitMQ Password
RABBIT_PASSWORD=$(generate_secure_password 20)
echo "RABBITMQ_PASSWORD:"
echo "$RABBIT_PASSWORD"
echo ""

# SMTP Password
SMTP_PASSWORD=$(generate_secure_password 20)
echo "SMTP_PASSWORD:"
echo "$SMTP_PASSWORD"
echo ""

# OAuth Client Secret
OAUTH_SECRET=$(generate_secure_key 32)
echo "OAUTH_CLIENT_SECRET:"
echo "$OAUTH_SECRET"
echo ""

# SSL Certificate Password
SSL_PASSWORD=$(generate_secure_password 16)
echo "SSL_CERT_PASSWORD:"
echo "$SSL_PASSWORD"
echo ""

echo "========================================"
echo "IMPORTANT SECURITY NOTES:"
echo "========================================"
echo "1. Copy these values to your .env.production file"
echo "2. Store them securely in a password manager"
echo "3. Never commit these values to version control"
echo "4. Rotate these keys every 90 days"
echo "5. Use different keys for each environment"
echo ""

# Option to save to file
read -p "Save to secrets.txt file? (yes/no): " save_to_file
if [ "$save_to_file" = "yes" ]; then
    timestamp=$(date +"%Y%m%d_%H%M%S")
    filename="secrets_$timestamp.txt"

    cat > "$filename" << EOF
E-Invoice Integrator - Generated Secrets
Generated: $(date)
=========================================

JWT_SECRET_KEY=$JWT_KEY
ENCRYPTION_KEY=$ENCRYPTION_KEY
ENCRYPTION_IV=$ENCRYPTION_IV
BACKUP_ENCRYPTION_KEY=$BACKUP_KEY
PAYLOAD_ENCRYPTION_KEY=$PAYLOAD_ENCRYPTION_KEY
DATABASE_PASSWORD=$DB_PASSWORD
REDIS_PASSWORD=$REDIS_PASSWORD
RABBITMQ_PASSWORD=$RABBIT_PASSWORD
SMTP_PASSWORD=$SMTP_PASSWORD
OAUTH_CLIENT_SECRET=$OAUTH_SECRET
SSL_CERT_PASSWORD=$SSL_PASSWORD

=========================================
WARNING: This file contains sensitive information.
Delete after copying to secure storage.
EOF

    echo "Secrets saved to: $filename"
    echo "DELETE THIS FILE after copying the values!"

    # Set restrictive permissions
    chmod 600 "$filename"
fi

echo ""
echo "Script completed successfully!"
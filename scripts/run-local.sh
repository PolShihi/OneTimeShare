#!/bin/bash

# Bash script to run the application locally

if [ -z "$1" ] || [ -z "$2" ]; then
    echo "Usage: ./run-local.sh <google_client_id> <google_client_secret>"
    echo "Or set environment variables GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET"
    exit 1
fi

# Set environment variables
export GOOGLE_CLIENT_ID="$1"
export GOOGLE_CLIENT_SECRET="$2"
export ASPNETCORE_ENVIRONMENT="Development"
export APP_BASE_URL="https://localhost:5001"
export STORAGE_ROOT="./storage"
export SQLITE_CONN_STRING="Data Source=./App_Data/app.db"
export MAX_UPLOAD_BYTES="104857600"
export FILE_RETENTION_DAYS="30"
export CLEANUP_INTERVAL_MINUTES="60"
export COOKIE_SECURE="false"
export LOG_LEVEL="Information"

echo "Starting OneTime Share application..."
echo "Environment: Development"
echo "URL: https://localhost:5001"

# Create directories if they don't exist
mkdir -p storage
mkdir -p App_Data

# Navigate to project directory and run
cd src/OneTimeShare.Web
dotnet run
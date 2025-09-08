# PowerShell script to run the application locally
param(
    [string]$GoogleClientId = "",
    [string]$GoogleClientSecret = ""
)

if ([string]::IsNullOrEmpty($GoogleClientId) -or [string]::IsNullOrEmpty($GoogleClientSecret)) {
    Write-Host "Usage: .\run-local.ps1 -GoogleClientId 'your_client_id' -GoogleClientSecret 'your_client_secret'" -ForegroundColor Red
    Write-Host "Or set environment variables GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET" -ForegroundColor Yellow
    exit 1
}

# Set environment variables
$env:GOOGLE_CLIENT_ID = $GoogleClientId
$env:GOOGLE_CLIENT_SECRET = $GoogleClientSecret
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:APP_BASE_URL = "https://localhost:5001"
$env:ASPNETCORE_URLS = "http://localhost:5000;https://localhost:5001"
$env:STORAGE_ROOT = "./storage"
$env:SQLITE_CONN_STRING = "Data Source=./App_Data/app.db"
$env:MAX_UPLOAD_BYTES = "104857600"
$env:FILE_RETENTION_DAYS = "30"
$env:CLEANUP_INTERVAL_MINUTES = "60"
$env:COOKIE_SECURE = "false"
$env:LOG_LEVEL = "Information"

Write-Host "Starting OneTime Share application..." -ForegroundColor Green
Write-Host "Environment: Development" -ForegroundColor Yellow
Write-Host "URL: https://localhost:5001" -ForegroundColor Yellow

# Create directories if they don't exist
if (!(Test-Path "storage")) { New-Item -ItemType Directory -Path "storage" }
if (!(Test-Path "App_Data")) { New-Item -ItemType Directory -Path "App_Data" }

# Navigate to project directory and run
Set-Location "src/OneTimeShare.Web"
dotnet run
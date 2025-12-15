<#
.SYNOPSIS
    Start MedSecure Vision Backend API
    
.DESCRIPTION
    Starts the ASP.NET Core backend API server for MedSecure Vision.
    
.PARAMETER Port
    Port number to run on (default: 5001)
    
.PARAMETER Environment
    Environment: Development or Production (default: Development)
    
.EXAMPLE
    .\start-backend.ps1
    .\start-backend.ps1 -Port 5001 -Environment Production
#>

param(
    [int]$Port = 5001,
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$BackendDir = Join-Path $ProjectRoot "MedSecureVision.Backend"

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║           MedSecure Vision - Backend API Server              ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Port: $Port" -ForegroundColor Yellow
Write-Host "Directory: $BackendDir" -ForegroundColor Yellow
Write-Host ""

# Set environment variables
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:ASPNETCORE_URLS = "https://localhost:$Port;http://localhost:$($Port - 1)"

# Navigate to backend directory
Set-Location $BackendDir

# Start the backend
Write-Host "[INFO] Starting Backend API..." -ForegroundColor Green
Write-Host "[INFO] Swagger UI: https://localhost:$Port/swagger" -ForegroundColor Green
Write-Host "[INFO] Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

dotnet run --no-build







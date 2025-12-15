<#
.SYNOPSIS
    Start All CXA Services
    
.DESCRIPTION
    Starts all CXA services in the correct order:
    1. Backend API (ASP.NET Core)
    2. Face Service (Python)
    3. Client Application (WPF)
    
.PARAMETER SkipClient
    Don't start the client application
    
.PARAMETER Build
    Build before starting
    
.EXAMPLE
    .\start-all.ps1
    .\start-all.ps1 -Build
    .\start-all.ps1 -SkipClient
#>

param(
    [switch]$SkipClient,
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot

Write-Host @"

     ██████╗██╗  ██╗ █████╗ 
    ██╔════╝╚██╗██╔╝██╔══██╗
    ██║      ╚███╔╝ ███████║
    ██║      ██╔██╗ ██╔══██║
    ╚██████╗██╔╝ ██╗██║  ██║
     ╚═════╝╚═╝  ╚═╝╚═╝  ╚═╝
                                                                                 
    Biometric Authentication System
    Service Launcher R1M1

"@ -ForegroundColor Cyan

# Build if requested
if ($Build) {
    Write-Host "[INFO] Building solution..." -ForegroundColor Yellow
    Set-Location $ProjectRoot
    dotnet build MedSecureVision.sln --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "[INFO] Build successful" -ForegroundColor Green
    Write-Host ""
}

# Start Backend API
Write-Host "[1/3] Starting Backend API..." -ForegroundColor Cyan
$backendScript = Join-Path $ScriptRoot "start-backend.ps1"
Start-Process powershell -ArgumentList "-NoExit", "-File", $backendScript

Write-Host "[INFO] Waiting for backend to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Start Face Service
Write-Host "[2/3] Starting Face Service..." -ForegroundColor Cyan
$faceServiceScript = Join-Path $ScriptRoot "start-faceservice.ps1"
Start-Process powershell -ArgumentList "-NoExit", "-File", $faceServiceScript

Write-Host "[INFO] Waiting for face service to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Start Client (if not skipped)
if (-not $SkipClient) {
    Write-Host "[3/3] Starting Client Application..." -ForegroundColor Cyan
    $clientScript = Join-Path $ScriptRoot "start-client.ps1"
    & $clientScript -NoBuild
}

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║                   All Services Started!                       ║
╠══════════════════════════════════════════════════════════════╣
║                                                               ║
║   Backend API:    https://localhost:5001                      ║
║   Swagger UI:     https://localhost:5001/swagger              ║
║   Face Service:   \\.\pipe\CXAFaceService                     ║
║   Client:         Running in separate window                  ║
║                                                               ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green

Write-Host "[INFO] To stop all services, close the PowerShell windows or use stop-all.ps1" -ForegroundColor Yellow







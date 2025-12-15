<#
.SYNOPSIS
    Start CXA WPF Client
    
.DESCRIPTION
    Builds and starts the WPF client application.
    
.PARAMETER NoBuild
    Skip the build step and run the existing executable
    
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)
    
.EXAMPLE
    .\start-client.ps1
    .\start-client.ps1 -NoBuild
    .\start-client.ps1 -Configuration Release
#>

param(
    [switch]$NoBuild,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$ClientDir = Join-Path $ProjectRoot "CXA.Client"

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║           CXA - WPF Client Application                       ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Directory: $ClientDir" -ForegroundColor Yellow
Write-Host ""

# Navigate to client directory
Set-Location $ClientDir

# Build if not skipped
if (-not $NoBuild) {
    Write-Host "[INFO] Building client application..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "[INFO] Build successful" -ForegroundColor Green
}

# Find and run the executable
$exePath = Join-Path $ClientDir "bin\$Configuration\net8.0-windows\CXA.Client.exe"

if (Test-Path $exePath) {
    Write-Host "[INFO] Starting client application..." -ForegroundColor Green
    Write-Host "[INFO] Executable: $exePath" -ForegroundColor Yellow
    Write-Host ""
    
    # Start the application
    Start-Process -FilePath $exePath
    
    Write-Host "[INFO] Client started successfully" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Executable not found at: $exePath" -ForegroundColor Red
    Write-Host "[INFO] Try running without -NoBuild flag" -ForegroundColor Yellow
    exit 1
}







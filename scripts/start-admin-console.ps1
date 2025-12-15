<#
.SYNOPSIS
    Start CXA Admin Console
    
.DESCRIPTION
    Starts the React Admin Console in development mode.
    
.PARAMETER Production
    Serve the production build instead of development server
    
.EXAMPLE
    .\start-admin-console.ps1
    .\start-admin-console.ps1 -Production
#>

param(
    [switch]$Production
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$AdminDir = Join-Path $ProjectRoot "MedSecureVision.AdminConsole"

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║         CXA - Admin Console (React)                          ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Directory: $AdminDir" -ForegroundColor Yellow
Write-Host ""

Set-Location $AdminDir

# Check if node_modules exists
if (-not (Test-Path (Join-Path $AdminDir "node_modules"))) {
    Write-Host "[INFO] Installing npm dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] npm install failed" -ForegroundColor Red
        exit 1
    }
}

if ($Production) {
    # Build and serve production
    Write-Host "[INFO] Building for production..." -ForegroundColor Yellow
    npm run build
    
    Write-Host "[INFO] Starting production server..." -ForegroundColor Green
    Write-Host "[INFO] URL: http://localhost:3000" -ForegroundColor Green
    npx serve -s build -l 3000
} else {
    # Development server
    Write-Host "[INFO] Starting development server..." -ForegroundColor Green
    Write-Host "[INFO] URL: http://localhost:3000" -ForegroundColor Green
    Write-Host "[INFO] Press Ctrl+C to stop" -ForegroundColor Yellow
    npm start
}







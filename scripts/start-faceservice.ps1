<#
.SYNOPSIS
    Start CXA Python Face Service
    
.DESCRIPTION
    Starts the Python face detection and recognition service.
    Creates virtual environment if it doesn't exist.
    
.PARAMETER InstallDeps
    Force reinstall of Python dependencies
    
.EXAMPLE
    .\start-faceservice.ps1
    .\start-faceservice.ps1 -InstallDeps
#>

param(
    [switch]$InstallDeps
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$FaceServiceDir = Join-Path $ProjectRoot "CXA.FaceService"
$VenvDir = Join-Path $FaceServiceDir "venv"

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║         CXA - Python Face Service                            ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Directory: $FaceServiceDir" -ForegroundColor Yellow
Write-Host ""

# Navigate to face service directory
Set-Location $FaceServiceDir

# Check if virtual environment exists
if (-not (Test-Path $VenvDir)) {
    Write-Host "[INFO] Creating Python virtual environment..." -ForegroundColor Yellow
    python -m venv venv
    $InstallDeps = $true
}

# Activate virtual environment
$activateScript = Join-Path $VenvDir "Scripts\Activate.ps1"
if (Test-Path $activateScript) {
    . $activateScript
    Write-Host "[INFO] Virtual environment activated" -ForegroundColor Green
}

# Install dependencies if needed
if ($InstallDeps) {
    Write-Host "[INFO] Installing Python dependencies..." -ForegroundColor Yellow
    pip install --upgrade pip
    pip install -r requirements.txt
    Write-Host "[INFO] Dependencies installed" -ForegroundColor Green
}

# Start the face service
Write-Host ""
Write-Host "[INFO] Starting Face Service..." -ForegroundColor Green
Write-Host "[INFO] Named Pipe: \\.\pipe\CXAFaceService" -ForegroundColor Green
Write-Host "[INFO] Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

python main.py







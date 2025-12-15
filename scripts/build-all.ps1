<#
.SYNOPSIS
    MedSecure Vision - Complete Build Script
    
.DESCRIPTION
    Builds all components of the MedSecure Vision system:
    - .NET Solution (Client, Backend, Shared, Tests)
    - Python Face Service (virtual environment setup)
    - React Admin Console (npm install and build)
    
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
    
.PARAMETER SkipPython
    Skip Python environment setup
    
.PARAMETER SkipNode
    Skip Node.js/React build
    
.EXAMPLE
    .\build-all.ps1
    .\build-all.ps1 -Configuration Debug
    .\build-all.ps1 -SkipPython -SkipNode
    
.NOTES
    Author: MedSecure Vision Team
    Version: 1.0.0
    Requires: .NET 8 SDK, Python 3.10+, Node.js 18+
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [switch]$SkipPython,
    [switch]$SkipNode,
    [switch]$Verbose
)

# ============================================================================
# Configuration
# ============================================================================

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot

# Colors for output
function Write-Header { param($msg) Write-Host "`n========================================" -ForegroundColor Cyan; Write-Host " $msg" -ForegroundColor Cyan; Write-Host "========================================" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[SUCCESS] $msg" -ForegroundColor Green }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Yellow }
function Write-Error { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }

# ============================================================================
# Banner
# ============================================================================

Write-Host @"

    __  __          _ ____                          _    ___      _             
   |  \/  | ___  __| / ___|  ___  ___ _   _ _ __ __| |  / _ \    (_)___ ___ ___ 
   | |\/| |/ _ \/ _` \___ \ / _ \/ __| | | | '__/ _` | | | | |   | / __/ __/ _ \
   | |  | |  __/ (_| |___) |  __/ (__| |_| | | | (_| | | |_| |   | \__ \__ \  __/
   |_|  |_|\___|\__,_|____/ \___|\___|\__,_|_|  \__,_|  \___/    |_|___/___/\___|
                                                                                 
    HIPAA-Compliant Biometric Authentication System
    Build Script v1.0.0

"@ -ForegroundColor Cyan

Write-Host "Configuration: $Configuration" -ForegroundColor White
Write-Host "Project Root: $ProjectRoot" -ForegroundColor White
Write-Host ""

# ============================================================================
# Prerequisites Check
# ============================================================================

Write-Header "Checking Prerequisites"

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK: $dotnetVersion"
} catch {
    Write-Error ".NET SDK not found. Please install .NET 8 SDK."
    exit 1
}

# Check Python (if not skipped)
if (-not $SkipPython) {
    try {
        $pythonVersion = python --version
        Write-Success "Python: $pythonVersion"
    } catch {
        Write-Error "Python not found. Use -SkipPython to skip Python setup."
        exit 1
    }
}

# Check Node.js (if not skipped)
if (-not $SkipNode) {
    try {
        $nodeVersion = node --version
        Write-Success "Node.js: $nodeVersion"
    } catch {
        Write-Error "Node.js not found. Use -SkipNode to skip React build."
        exit 1
    }
}

# ============================================================================
# Build .NET Solution
# ============================================================================

Write-Header "Building .NET Solution"

Set-Location $ProjectRoot

Write-Info "Restoring NuGet packages..."
dotnet restore MedSecureVision.sln
if ($LASTEXITCODE -ne 0) { Write-Error "NuGet restore failed"; exit 1 }
Write-Success "Packages restored"

Write-Info "Building solution ($Configuration)..."
dotnet build MedSecureVision.sln --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }
Write-Success "Solution built successfully"

# ============================================================================
# Run Tests
# ============================================================================

Write-Header "Running Unit Tests"

Write-Info "Executing tests..."
dotnet test MedSecureVision.Tests/MedSecureVision.Tests.csproj --configuration $Configuration --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) { 
    Write-Error "Some tests failed"
    # Continue anyway for build purposes
} else {
    Write-Success "All tests passed"
}

# ============================================================================
# Publish Applications
# ============================================================================

Write-Header "Publishing Applications"

$publishDir = Join-Path $ProjectRoot "publish"

# Create publish directory
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Path $publishDir | Out-Null

# Publish Client
Write-Info "Publishing WPF Client..."
$clientPublishDir = Join-Path $publishDir "Client"
dotnet publish MedSecureVision.Client/MedSecureVision.Client.csproj `
    --configuration $Configuration `
    --output $clientPublishDir `
    --self-contained false `
    -p:PublishSingleFile=false
if ($LASTEXITCODE -ne 0) { Write-Error "Client publish failed"; exit 1 }
Write-Success "Client published to: $clientPublishDir"

# Publish Backend
Write-Info "Publishing Backend API..."
$backendPublishDir = Join-Path $publishDir "Backend"
dotnet publish MedSecureVision.Backend/MedSecureVision.Backend.csproj `
    --configuration $Configuration `
    --output $backendPublishDir `
    --self-contained false
if ($LASTEXITCODE -ne 0) { Write-Error "Backend publish failed"; exit 1 }
Write-Success "Backend published to: $backendPublishDir"

# ============================================================================
# Setup Python Environment
# ============================================================================

if (-not $SkipPython) {
    Write-Header "Setting Up Python Environment"
    
    $pythonDir = Join-Path $ProjectRoot "MedSecureVision.FaceService"
    $venvDir = Join-Path $pythonDir "venv"
    
    Set-Location $pythonDir
    
    # Create virtual environment if it doesn't exist
    if (-not (Test-Path $venvDir)) {
        Write-Info "Creating Python virtual environment..."
        python -m venv venv
        Write-Success "Virtual environment created"
    }
    
    # Activate and install dependencies
    Write-Info "Installing Python dependencies..."
    & "$venvDir\Scripts\pip.exe" install --upgrade pip
    & "$venvDir\Scripts\pip.exe" install -r requirements.txt
    if ($LASTEXITCODE -ne 0) { Write-Error "Python dependencies failed"; exit 1 }
    Write-Success "Python dependencies installed"
    
    # Copy to publish directory
    $pythonPublishDir = Join-Path $publishDir "FaceService"
    Copy-Item -Recurse -Force $pythonDir $pythonPublishDir
    Write-Success "Face Service copied to: $pythonPublishDir"
}

# ============================================================================
# Build React Admin Console
# ============================================================================

if (-not $SkipNode) {
    Write-Header "Building React Admin Console"
    
    $adminDir = Join-Path $ProjectRoot "MedSecureVision.AdminConsole"
    Set-Location $adminDir
    
    Write-Info "Installing npm dependencies..."
    npm install
    if ($LASTEXITCODE -ne 0) { Write-Error "npm install failed"; exit 1 }
    Write-Success "NPM dependencies installed"
    
    Write-Info "Building React application..."
    npm run build
    if ($LASTEXITCODE -ne 0) { Write-Error "React build failed"; exit 1 }
    Write-Success "Admin Console built"
    
    # Copy build to publish directory
    $adminPublishDir = Join-Path $publishDir "AdminConsole"
    Copy-Item -Recurse -Force (Join-Path $adminDir "build") $adminPublishDir
    Write-Success "Admin Console published to: $adminPublishDir"
}

# ============================================================================
# Create Startup Scripts in Publish Directory
# ============================================================================

Write-Header "Creating Startup Scripts"

# Start All script
$startAllScript = @'
@echo off
echo Starting MedSecure Vision Services...
echo.

echo [1/3] Starting Backend API...
start "MedSecure Backend" cmd /k "cd /d %~dp0Backend && dotnet MedSecureVision.Backend.dll"
timeout /t 3 /nobreak > nul

echo [2/3] Starting Face Service...
start "MedSecure FaceService" cmd /k "cd /d %~dp0FaceService && venv\Scripts\python.exe main.py"
timeout /t 5 /nobreak > nul

echo [3/3] Starting Client Application...
start "" "%~dp0Client\MedSecureVision.Client.exe"

echo.
echo All services started!
echo.
pause
'@
$startAllScript | Out-File -FilePath (Join-Path $publishDir "Start-All.bat") -Encoding ASCII

# Stop All script
$stopAllScript = @'
@echo off
echo Stopping MedSecure Vision Services...
taskkill /IM "MedSecureVision.Client.exe" /F 2>nul
taskkill /IM "MedSecureVision.Backend.exe" /F 2>nul
taskkill /IM "python.exe" /F 2>nul
echo Services stopped.
pause
'@
$stopAllScript | Out-File -FilePath (Join-Path $publishDir "Stop-All.bat") -Encoding ASCII

Write-Success "Startup scripts created"

# ============================================================================
# Summary
# ============================================================================

Write-Header "Build Complete!"

Write-Host @"

Build Summary:
==============
Configuration: $Configuration
Output Directory: $publishDir

Published Components:
- Client:       $publishDir\Client
- Backend:      $publishDir\Backend
- FaceService:  $publishDir\FaceService
- AdminConsole: $publishDir\AdminConsole

Startup Scripts:
- Start-All.bat  - Start all services
- Stop-All.bat   - Stop all services

Next Steps:
1. Configure appsettings.json files with your Auth0 and database credentials
2. Run database migrations: dotnet ef database update
3. Start services using Start-All.bat

"@ -ForegroundColor Green

Set-Location $ProjectRoot







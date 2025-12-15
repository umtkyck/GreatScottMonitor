<#
.SYNOPSIS
    Create CXA Installer Package
    
.DESCRIPTION
    Creates a deployment package with all required files for installation.
    
.PARAMETER OutputDir
    Output directory for the installer package
    
.PARAMETER Version
    Version number for the package
    
.EXAMPLE
    .\create-installer.ps1
    .\create-installer.ps1 -Version "1.0.0" -OutputDir "C:\Releases"
#>

param(
    [string]$OutputDir = "..\installer",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║       CXA - Installer Package Creator                        ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

$InstallerDir = Join-Path $ProjectRoot $OutputDir
$PackageDir = Join-Path $InstallerDir "CXA-$Version"

# Clean and create directories
if (Test-Path $PackageDir) {
    Remove-Item -Recurse -Force $PackageDir
}
New-Item -ItemType Directory -Path $PackageDir | Out-Null

Write-Host "[INFO] Building Release configuration..." -ForegroundColor Yellow
Set-Location $ProjectRoot
dotnet publish MedSecureVision.Client/MedSecureVision.Client.csproj -c Release -o "$PackageDir\Client" --self-contained false
dotnet publish MedSecureVision.Backend/MedSecureVision.Backend.csproj -c Release -o "$PackageDir\Backend" --self-contained false

Write-Host "[INFO] Copying Face Service..." -ForegroundColor Yellow
Copy-Item -Recurse "$ProjectRoot\MedSecureVision.FaceService" "$PackageDir\FaceService"
Remove-Item -Recurse -Force "$PackageDir\FaceService\venv" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$PackageDir\FaceService\__pycache__" -ErrorAction SilentlyContinue

Write-Host "[INFO] Copying documentation..." -ForegroundColor Yellow
Copy-Item -Recurse "$ProjectRoot\docs" "$PackageDir\docs"
Copy-Item "$ProjectRoot\README.md" "$PackageDir\README.md"

Write-Host "[INFO] Creating startup scripts..." -ForegroundColor Yellow

# Install script
@"
@echo off
title CXA - Installation
echo.
echo  ========================================================
echo   CXA Installation
echo   Version: $Version
echo  ========================================================
echo.

echo [1/4] Checking .NET Runtime...
dotnet --list-runtimes | findstr "Microsoft.NETCore.App 8" >nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET 8 Runtime not found.
    echo Please install from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo [OK] .NET 8 Runtime found

echo [2/4] Checking Python...
python --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Python not found.
    echo Please install Python 3.10+ from: https://www.python.org/downloads/
    pause
    exit /b 1
)
echo [OK] Python found

echo [3/4] Setting up Python environment...
cd /d %~dp0FaceService
if not exist venv (
    python -m venv venv
)
call venv\Scripts\activate.bat
pip install -r requirements.txt
echo [OK] Python environment ready

echo [4/4] Installation complete!
echo.
echo  ========================================================
echo   INSTALLATION COMPLETE!
echo  ========================================================
echo.
echo   Before first run:
echo   1. Configure Backend\appsettings.json with your database
echo   2. Configure Client\appsettings.json with API URL
echo   3. Run Setup-Database.bat to initialize database
echo.
echo   To start: Run Start-CXA.bat
echo.
pause
"@ | Out-File -FilePath "$PackageDir\Install.bat" -Encoding ASCII

# Start script
@"
@echo off
title CXA
echo Starting CXA...
start "Backend" cmd /k "cd /d %~dp0Backend && dotnet MedSecureVision.Backend.dll"
timeout /t 3 /nobreak > nul
start "FaceService" cmd /k "cd /d %~dp0FaceService && venv\Scripts\python.exe main.py"
timeout /t 5 /nobreak > nul
start "" "%~dp0Client\MedSecureVision.Client.exe"
echo All services started!
"@ | Out-File -FilePath "$PackageDir\Start-CXA.bat" -Encoding ASCII

# Stop script
@"
@echo off
echo Stopping CXA...
taskkill /IM "MedSecureVision.Client.exe" /F 2>nul
taskkill /IM "MedSecureVision.Backend.exe" /F 2>nul
taskkill /IM "python.exe" /F 2>nul
echo Done.
"@ | Out-File -FilePath "$PackageDir\Stop-CXA.bat" -Encoding ASCII

Write-Host "[INFO] Creating ZIP archive..." -ForegroundColor Yellow
$ZipPath = "$InstallerDir\CXA-$Version.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath }
Compress-Archive -Path $PackageDir -DestinationPath $ZipPath

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║                 INSTALLER PACKAGE CREATED!                    ║
╠══════════════════════════════════════════════════════════════╣
║                                                               ║
║   Package: $ZipPath
║   Version: $Version
║                                                               ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green







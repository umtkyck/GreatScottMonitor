@echo off
:: ============================================================================
:: MedSecure Vision - Quick Build Script
:: ============================================================================
:: This script builds the entire MedSecure Vision solution
:: For more options, use scripts\build-all.ps1
:: ============================================================================

title MedSecure Vision - Build

echo.
echo  ========================================================
echo   MedSecure Vision - Build Script
echo  ========================================================
echo.

:: Check for .NET SDK
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK not found. Please install .NET 8 SDK.
    pause
    exit /b 1
)

:: Build solution
echo [INFO] Building .NET Solution...
dotnet build MedSecureVision.sln --configuration Release
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo  ========================================================
echo   BUILD SUCCESSFUL!
echo  ========================================================
echo.
echo  To start all services, run: Start-All.bat
echo.
pause



@echo off
:: ============================================================================
:: MedSecure Vision - Stop All Services
:: ============================================================================
:: Stops all running MedSecure Vision services
:: ============================================================================

title MedSecure Vision - Stopping Services

echo.
echo  ========================================================
echo   MedSecure Vision - Stopping All Services
echo  ========================================================
echo.

echo [1/3] Stopping Client Application...
taskkill /IM "MedSecureVision.Client.exe" /F 2>nul
if %ERRORLEVEL% equ 0 (
    echo        [OK] Client stopped
) else (
    echo        [--] Client was not running
)

echo [2/3] Stopping Backend API...
taskkill /IM "MedSecureVision.Backend.exe" /F 2>nul
:: Also stop dotnet processes that might be running the backend
for /f "tokens=2" %%a in ('tasklist /fi "WINDOWTITLE eq MedSecure - Backend*" /fo csv /nh') do (
    taskkill /PID %%~a /F 2>nul
)
echo        [OK] Backend stopped

echo [3/3] Stopping Face Service...
:: Stop Python processes (be careful with this one)
for /f "tokens=2" %%a in ('tasklist /fi "WINDOWTITLE eq MedSecure - Face*" /fo csv /nh') do (
    taskkill /PID %%~a /F 2>nul
)
echo        [OK] Face Service stopped

echo.
echo  ========================================================
echo   ALL SERVICES STOPPED
echo  ========================================================
echo.
pause



@echo off
:: ============================================================================
:: CXA - Stop All Services
:: ============================================================================
:: Stops all running CXA services
:: ============================================================================

title CXA - Stopping Services

echo.
echo  ========================================================
echo   CXA - Stopping All Services
echo  ========================================================
echo.

echo [1/3] Stopping Client Application...
taskkill /IM "CXA.Client.exe" /F 2>nul
if %ERRORLEVEL% equ 0 (
    echo        [OK] Client stopped
) else (
    echo        [--] Client was not running
)

echo [2/3] Stopping Backend API...
taskkill /IM "CXA.Backend.exe" /F 2>nul
:: Also stop dotnet processes that might be running the backend
for /f "tokens=2" %%a in ('tasklist /fi "WINDOWTITLE eq CXA - Backend*" /fo csv /nh') do (
    taskkill /PID %%~a /F 2>nul
)
echo        [OK] Backend stopped

echo [3/3] Stopping Face Service...
:: Stop Python processes
for /f "tokens=2" %%a in ('tasklist /fi "WINDOWTITLE eq CXA - Face*" /fo csv /nh') do (
    taskkill /PID %%~a /F 2>nul
)
echo        [OK] Face Service stopped

echo.
echo  ========================================================
echo   ALL SERVICES STOPPED
echo  ========================================================
echo.
pause

@echo off
:: ============================================================================
:: CXA - Start All Services
:: ============================================================================
:: Starts all CXA services in the correct order
:: ============================================================================

title CXA - Starting Services

echo.
echo  ========================================================
echo   CXA - Starting All Services
echo  ========================================================
echo.

:: Start Backend API
echo [1/3] Starting Backend API...
start "CXA - Backend API" cmd /k "cd /d %~dp0MedSecureVision.Backend && dotnet run"
timeout /t 3 /nobreak > nul

:: Start Face Service
echo [2/3] Starting Face Service...
cd /d %~dp0MedSecureVision.FaceService
if not exist venv (
    echo [INFO] Creating Python virtual environment...
    python -m venv venv
    call venv\Scripts\activate.bat
    pip install -r requirements.txt
) else (
    call venv\Scripts\activate.bat
)
start "CXA - Face Service" cmd /k "cd /d %~dp0MedSecureVision.FaceService && venv\Scripts\python.exe main.py"
timeout /t 5 /nobreak > nul

:: Start Client
echo [3/3] Starting Client Application...
cd /d %~dp0MedSecureVision.Client
start "" "bin\Debug\net8.0-windows\MedSecureVision.Client.exe"

echo.
echo  ========================================================
echo   ALL SERVICES STARTED!
echo  ========================================================
echo.
echo   Backend API:    https://localhost:5001
echo   Swagger UI:     https://localhost:5001/swagger
echo   Face Service:   \\.\pipe\CXAFaceService
echo   Client:         Running
echo.
echo   To stop all services, run: Stop-All.bat
echo.
pause

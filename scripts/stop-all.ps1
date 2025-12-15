<#
.SYNOPSIS
    Stop All CXA Services
    
.DESCRIPTION
    Stops all running CXA services.
    
.EXAMPLE
    .\stop-all.ps1
#>

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║          Stopping CXA Services                               ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Yellow

# Stop Client
Write-Host "[1/3] Stopping Client Application..." -ForegroundColor Yellow
Get-Process -Name "MedSecureVision.Client" -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host "[OK] Client stopped" -ForegroundColor Green

# Stop Backend
Write-Host "[2/3] Stopping Backend API..." -ForegroundColor Yellow
Get-Process -Name "MedSecureVision.Backend" -ErrorAction SilentlyContinue | Stop-Process -Force
# Also try to stop any dotnet processes running the backend
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*Backend*" -or $_.CommandLine -like "*MedSecureVision.Backend*"
} | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "[OK] Backend stopped" -ForegroundColor Green

# Stop Face Service (Python)
Write-Host "[3/3] Stopping Face Service..." -ForegroundColor Yellow
# Find Python processes running the face service
Get-Process -Name "python" -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*Face*" -or $_.CommandLine -like "*main.py*"
} | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "[OK] Face Service stopped" -ForegroundColor Green

Write-Host ""
Write-Host "[INFO] All services stopped" -ForegroundColor Green







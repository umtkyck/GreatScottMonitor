<#
.SYNOPSIS
    Create Desktop Shortcut for CXA Client
    
.DESCRIPTION
    Creates a desktop shortcut to launch the CXA Biometric Authentication client.
    
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Debug)
    
.EXAMPLE
    .\create-desktop-shortcut.ps1
    .\create-desktop-shortcut.ps1 -Configuration Release
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptRoot
$ClientDir = Join-Path $ProjectRoot "CXA.Client"

Write-Host @"

╔══════════════════════════════════════════════════════════════╗
║        CXA - Create Desktop Shortcut                         ║
╚══════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

# Paths
$exePath = Join-Path $ClientDir "bin\$Configuration\net8.0-windows10.0.19041.0\CXA.Client.exe"
$iconPath = Join-Path $ClientDir "Assets\icon.ico"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "CXA Biometric Auth.lnk"

Write-Host "[INFO] Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "[INFO] Executable: $exePath" -ForegroundColor Yellow
Write-Host "[INFO] Shortcut: $shortcutPath" -ForegroundColor Yellow
Write-Host ""

# Check if executable exists
if (-not (Test-Path $exePath)) {
    Write-Host "[WARNING] Executable not found at: $exePath" -ForegroundColor Yellow
    Write-Host "[INFO] Building application first..." -ForegroundColor Cyan
    
    Push-Location $ClientDir
    dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed. Cannot create shortcut." -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host "[INFO] Build successful" -ForegroundColor Green
    Write-Host ""
}

# Verify executable exists after potential build
if (-not (Test-Path $exePath)) {
    Write-Host "[ERROR] Executable still not found after build: $exePath" -ForegroundColor Red
    exit 1
}

# Create the shortcut using WScript.Shell COM object
try {
    $WScriptShell = New-Object -ComObject WScript.Shell
    $shortcut = $WScriptShell.CreateShortcut($shortcutPath)
    
    # Set shortcut properties
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = Split-Path $exePath -Parent
    $shortcut.Description = "CXA - Biometric Authentication System"
    
    # Set icon if available
    if (Test-Path $iconPath) {
        $shortcut.IconLocation = $iconPath
        Write-Host "[INFO] Using custom icon: $iconPath" -ForegroundColor Green
    } else {
        # Use the executable's embedded icon
        $shortcut.IconLocation = "$exePath,0"
        Write-Host "[INFO] Using embedded icon from executable" -ForegroundColor Yellow
    }
    
    # Save the shortcut
    $shortcut.Save()
    
    Write-Host ""
    Write-Host "[SUCCESS] Desktop shortcut created!" -ForegroundColor Green
    Write-Host "[INFO] Location: $shortcutPath" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now launch CXA from your desktop." -ForegroundColor White
    
} catch {
    Write-Host "[ERROR] Failed to create shortcut: $_" -ForegroundColor Red
    exit 1
}

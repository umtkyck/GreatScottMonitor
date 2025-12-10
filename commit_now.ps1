# MedSecure Vision - Quick Commit Script
# This script will commit all pending changes

# Try to find Git
$gitExe = $null
$possiblePaths = @(
    "C:\Program Files\Git\cmd\git.exe",
    "C:\Program Files\Git\bin\git.exe",
    "C:\Program Files (x86)\Git\cmd\git.exe",
    "$env:LOCALAPPDATA\GitHubDesktop\resources\app\git\cmd\git.exe",
    "$env:ProgramFiles\Git\cmd\git.exe"
)

foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $gitExe = $path
        Write-Host "Found Git at: $path"
        break
    }
}

if (-not $gitExe) {
    Write-Host "ERROR: Git not found. Please install Git or use GitHub Desktop to commit."
    Write-Host "You can download Git from: https://git-scm.com/download/win"
    exit 1
}

# Configure Git if not already configured
& $gitExe config user.name "MedSecure Vision" 2>$null
& $gitExe config user.email "dev@medsecurevision.com" 2>$null

Write-Host "`n=== Committing all changes ===`n"

# Stage all files
Write-Host "Staging all files..."
& $gitExe add .

# Commit with comprehensive message
$commitMessage = @"
feat: Complete MedSecure Vision implementation

Initial implementation of HIPAA-compliant biometric authentication system:

Core Components:
- WPF client with face recognition UI and presence monitoring
- Python face service (MediaPipe BlazeFace + InsightFace ArcFace)
- ASP.NET Core 8 backend API with Auth0 integration
- React admin console for user and policy management

Features:
- Real-time face detection and recognition (sub-500ms authentication)
- Continuous presence monitoring with automatic session locking
- Face enrollment with multi-angle capture and quality validation
- Liveness detection (blink, head movement) for anti-spoofing
- AES-256-GCM encryption for face templates
- HIPAA-compliant audit logging system
- Fallback authentication (PIN, Windows Hello, Smart Card)

Testing:
- Comprehensive unit test suite (xUnit, FluentAssertions, Moq)
- Integration tests for end-to-end workflows
- Tests for encryption, face verification, and audit logging

Documentation:
- User guide for end users
- Administrator guide for system admins
- Developer guide for contributors
- API reference documentation
- HIPAA compliance documentation
- Troubleshooting guide
- Quick reference guide

All code and documentation in English only.
"@

Write-Host "Creating commit..."
& $gitExe commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nCommit successful!" -ForegroundColor Green
    Write-Host "`nCommit details:"
    & $gitExe log -1 --stat
} else {
    Write-Host "`nCommit failed. Check the error above." -ForegroundColor Red
    exit 1
}


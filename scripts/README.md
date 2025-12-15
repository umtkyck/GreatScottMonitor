# MedSecure Vision - Scripts

<div align="center">

![MedSecure Vision](https://img.shields.io/badge/MedSecure-Vision-blue?style=for-the-badge)
![PowerShell](https://img.shields.io/badge/PowerShell-5.1+-blue?style=for-the-badge)
![Batch](https://img.shields.io/badge/Batch-Windows-green?style=for-the-badge)

**Build, Deploy, and Run Scripts for MedSecure Vision**

</div>

---

## 📁 Script Index

| Script | Description | Usage |
|--------|-------------|-------|
| `build-all.ps1` | Complete build script for all components | `.\build-all.ps1 -Configuration Release` |
| `start-all.ps1` | Start all services | `.\start-all.ps1` |
| `stop-all.ps1` | Stop all services | `.\stop-all.ps1` |
| `start-backend.ps1` | Start Backend API only | `.\start-backend.ps1 -Port 5001` |
| `start-faceservice.ps1` | Start Python Face Service only | `.\start-faceservice.ps1` |
| `start-client.ps1` | Start WPF Client only | `.\start-client.ps1` |
| `start-admin-console.ps1` | Start React Admin Console | `.\start-admin-console.ps1` |
| `create-installer.ps1` | Create deployment package | `.\create-installer.ps1 -Version "1.0.0"` |

---

## 🚀 Quick Start

### Development Mode

```powershell
# Build and start all services
cd scripts
.\start-all.ps1 -Build
```

### Production Build

```powershell
# Complete build with all components
.\build-all.ps1 -Configuration Release

# Output will be in: ..\publish\
```

---

## 📋 Script Details

### build-all.ps1

Complete build script that:
- Builds .NET solution (Client, Backend, Shared, Tests)
- Sets up Python virtual environment
- Installs Python dependencies
- Builds React Admin Console
- Creates publish output

**Parameters:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Configuration` | Release | Debug or Release |
| `-SkipPython` | false | Skip Python setup |
| `-SkipNode` | false | Skip React build |

**Example:**
```powershell
# Full build
.\build-all.ps1

# Quick .NET only build
.\build-all.ps1 -SkipPython -SkipNode -Configuration Debug
```

---

### start-all.ps1

Starts all services in the correct order:
1. Backend API (port 5001)
2. Face Service (Named Pipe)
3. WPF Client

**Parameters:**
| Parameter | Description |
|-----------|-------------|
| `-Build` | Build before starting |
| `-SkipClient` | Don't start client |

---

### start-backend.ps1

Starts the ASP.NET Core Backend API.

**Parameters:**
| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Port` | 5001 | HTTP port |
| `-Environment` | Development | Development or Production |

---

### start-faceservice.ps1

Starts the Python Face Service.

**Parameters:**
| Parameter | Description |
|-----------|-------------|
| `-InstallDeps` | Force reinstall dependencies |

---

### create-installer.ps1

Creates a deployment package ZIP file.

**Output:**
```
installer/
├── MedSecureVision-1.0.0/
│   ├── Client/
│   ├── Backend/
│   ├── FaceService/
│   ├── docs/
│   ├── Install.bat
│   ├── Start-MedSecure.bat
│   └── Stop-MedSecure.bat
└── MedSecureVision-1.0.0.zip
```

---

## 🔧 Prerequisites

| Component | Version | Download |
|-----------|---------|----------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Python | 3.10+ | [Download](https://www.python.org/downloads/) |
| Node.js | 18+ | [Download](https://nodejs.org/) |
| Git | Latest | [Download](https://git-scm.com/downloads) |

---

## 📝 Notes

### Execution Policy

If scripts won't run, you may need to change execution policy:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Batch Files

For quick access, use the batch files in the project root:
- `Build.bat` - Quick build
- `Start-All.bat` - Start services
- `Stop-All.bat` - Stop services
- `Run-Tests.bat` - Run unit tests

---

## 🔒 Security

- Never commit `appsettings.Production.json` with real credentials
- Use environment variables for sensitive configuration
- The Face Service runs locally only (Named Pipes)

---

<div align="center">

**MedSecure Vision** - HIPAA-Compliant Biometric Authentication

*© 2024 MedSecure Vision. All rights reserved.*

</div>







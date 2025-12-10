# MedSecure Vision

Smart Biometric Authentication Monitor for Healthcare

## Overview

MedSecure Vision is a HIPAA-compliant biometric authentication system for healthcare environments that provides Apple Face ID-like facial recognition using built-in cameras. The system integrates with cloud authentication services, provides real-time continuous presence monitoring, and automatically locks sessions when users leave their workstations.

## Architecture

- **Windows Client (C# WPF)** - Main UI, session management, Windows integration
- **Python Face Service** - Face detection/recognition via MediaPipe/InsightFace
- **Cloud Backend (ASP.NET Core)** - Auth0 integration, user management, audit logging
- **Admin Web Console** - User management, policy configuration, reporting

## Prerequisites

- Windows 10/11 (64-bit)
- .NET 8 SDK
- Python 3.10+
- PostgreSQL (or Azure SQL)
- Auth0 account

## Getting Started

### Backend Setup

1. Update `MedSecureVision.Backend/appsettings.json` with your Auth0 and database credentials
2. Run database migrations:
   ```bash
   cd MedSecureVision.Backend
   dotnet ef database update
   ```
3. Start the backend:
   ```bash
   dotnet run
   ```

### Python Face Service Setup

1. Install Python dependencies:
   ```bash
   cd MedSecureVision.FaceService
   pip install -r requirements.txt
   ```
2. Download InsightFace models (buffalo_l) on first run
3. Start the service:
   ```bash
   python main.py
   ```

### Client Setup

1. Update `MedSecureVision.Client/appsettings.json` with backend API URL
2. Build and run:
   ```bash
   cd MedSecureVision.Client
   dotnet build
   dotnet run
   ```

## Development

See `docs/` directory for detailed documentation:
- Architecture overview
- Deployment guide
- HIPAA compliance documentation
- API reference

## License

Proprietary - All rights reserved


# MedSecure Vision - Architecture Documentation

## System Overview

MedSecure Vision is a HIPAA-compliant biometric authentication system for healthcare environments. The system provides Apple Face ID-like facial recognition using built-in cameras, with real-time presence monitoring and automatic session locking.

## Architecture Diagram

```
┌─────────────────┐
│  WPF Client     │
│  (C# .NET 8)   │
└────────┬────────┘
         │ Named Pipes
         │
┌────────▼────────┐
│ Python Service  │
│ (MediaPipe/     │
│  InsightFace)   │
└────────┬────────┘
         │ Camera
         │
    ┌────▼────┐
    │ Webcam  │
    └─────────┘

┌─────────────────┐
│  WPF Client     │
└────────┬────────┘
         │ HTTPS/REST
         │
┌────────▼────────┐
│ Backend API     │
│ (ASP.NET Core)  │
└────────┬────────┘
         │
    ┌────▼────┐
    │Database │
    │(PostgreSQL)│
    └─────────┘

┌─────────────────┐
│ Admin Console   │
│ (React)         │
└────────┬────────┘
         │ HTTPS/REST
         │
┌────────▼────────┐
│ Backend API     │
└─────────────────┘
```

## Component Details

### 1. Windows Client (WPF)

**Technology:** C# .NET 8, WPF

**Responsibilities:**
- User interface for authentication and enrollment
- Camera feed capture and display
- Communication with Python face service via named pipes
- Communication with backend API via HTTPS
- Presence monitoring and session locking
- Fallback authentication (PIN, Windows Hello)

**Key Services:**
- `FaceServiceClient` - Named pipe communication with Python service
- `CameraService` - Webcam capture and frame management
- `AuthenticationService` - Face authentication orchestration
- `PresenceMonitorService` - Continuous presence monitoring
- `SessionLockService` - Windows workstation locking
- `CloudAuthService` - Auth0 OAuth 2.0 integration

### 2. Python Face Service

**Technology:** Python 3.10+, MediaPipe, InsightFace, OpenCV

**Responsibilities:**
- Real-time face detection using MediaPipe BlazeFace
- Face recognition using InsightFace ArcFace (512-dim embeddings)
- Liveness detection (blink detection, head movement)
- Quality validation (blur, lighting, coverage)
- Named pipe server for IPC with C# client

**Key Modules:**
- `face_detector.py` - MediaPipe face detection wrapper
- `face_recognizer.py` - InsightFace recognition and embedding extraction
- `liveness_detector.py` - Anti-spoofing and liveness checks
- `quality_validator.py` - Frame quality validation
- `ipc_server.py` - Named pipe server for IPC

### 3. Backend API

**Technology:** ASP.NET Core 8, Entity Framework Core, PostgreSQL

**Responsibilities:**
- Face template storage and verification
- User management
- Audit logging (HIPAA compliant)
- Auth0 integration
- Encryption service for face templates
- Policy configuration

**Key Controllers:**
- `AuthenticationController` - Face verification endpoint
- `EnrollmentController` - Face enrollment workflow
- `AuditLogController` - Audit log retrieval and export
- `AuthController` - OAuth 2.0 callbacks

**Key Services:**
- `FaceVerificationService` - Cosine similarity matching
- `EncryptionService` - AES-256-GCM encryption
- `AuditLogService` - HIPAA-compliant audit logging
- `Auth0Service` - Auth0 Management API integration

### 4. Admin Web Console

**Technology:** React, TypeScript, Chart.js, Auth0 React SDK

**Responsibilities:**
- Dashboard with authentication metrics
- User management (add, remove, suspend, re-enroll)
- Policy configuration
- Audit log viewing and export

**Key Pages:**
- `Dashboard` - Statistics and charts
- `UserManagement` - User CRUD operations
- `PolicyConfiguration` - Security policy settings
- `AuditLog` - Audit log viewer with filters

## Data Flow

### Authentication Flow

1. User sits in front of camera
2. WPF client captures frame from camera
3. Frame sent to Python service via named pipe
4. Python service detects face and extracts embedding
5. Embedding sent to backend API via HTTPS
6. Backend compares embedding with stored templates
7. If match found, session created and user authenticated
8. Presence monitoring begins

### Enrollment Flow

1. User initiates enrollment from admin console or client
2. Guided capture of multiple angles (front, left, right, up, down)
3. Each frame validated for quality (blur, lighting, coverage)
4. Liveness check performed (blink detection, head movement)
5. Embeddings extracted and encrypted
6. Encrypted templates uploaded to backend
7. Enrollment completed and audit log created

### Presence Monitoring Flow

1. Background service checks camera every 200ms
2. Face detection performed on each frame
3. If face detected, embedding extracted and compared with authenticated user
4. If no match or no face for >5 seconds, session locked
5. Lock event logged to audit system

## Security Architecture

### Encryption

- **At Rest:** AES-256-GCM encryption for face templates
- **In Transit:** TLS 1.3 with certificate pinning
- **Key Derivation:** PBKDF2 with 100,000 iterations
- **User-Specific Keys:** Derived from user ID + master key

### Authentication

- **Primary:** Face recognition (biometric)
- **Fallback:** PIN, Windows Hello, Smart Card
- **Cloud:** Auth0 OAuth 2.0 for user management
- **Token Storage:** Windows Credential Manager (DPAPI protected)

### Audit Logging

- All authentication events logged
- HIPAA-compliant retention policies
- Exportable to CSV
- Immutable logs (append-only)

## Performance Considerations

### Face Detection
- MediaPipe BlazeFace: 200-1000 FPS (CPU), sub-millisecond (GPU)
- Target latency: <20ms per frame

### Face Recognition
- InsightFace ArcFace: ~80ms per embedding extraction
- GPU acceleration available via CUDA

### Total Authentication Time
- Target: <500ms (300ms ideal)
- Breakdown:
  - Face detection: 15ms
  - Embedding extraction: 80ms
  - Cloud verification: 200ms
  - Total: ~295ms

### Presence Monitoring
- Check interval: 200ms
- CPU usage: <5% average
- Frame skipping: Every 2nd frame for efficiency

## Deployment Architecture

### Development
- Local PostgreSQL database (Docker)
- Python service runs locally
- Backend API runs locally
- Client runs on Windows workstation

### Production
- Backend API: Azure App Service or AWS ECS
- Database: Azure SQL or AWS RDS (PostgreSQL)
- Secrets: Azure Key Vault or AWS Secrets Manager
- Monitoring: Application Insights or CloudWatch

## HIPAA Compliance

### Technical Safeguards
- Access Control: Biometric + RBAC
- Audit Controls: Complete audit trail
- Integrity: Cryptographic hashing
- Person Authentication: MFA (biometric + PIN/badge)
- Transmission Security: TLS 1.3
- Encryption: AES-256 (FIPS 140-2)

### Administrative Safeguards
- User enrollment requires consent
- Opt-out alternatives provided
- Data retention policies enforced
- Breach notification procedures documented







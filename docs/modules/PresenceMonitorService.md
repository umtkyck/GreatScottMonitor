# PresenceMonitorService Module Documentation

<div align="center">

![MedSecure Vision](https://img.shields.io/badge/MedSecure-Vision-blue?style=for-the-badge)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge)
![HIPAA](https://img.shields.io/badge/HIPAA-Compliant-green?style=for-the-badge)

**Real-Time Continuous User Presence Monitoring**

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Security Architecture](#security-architecture)
- [Class Reference](#class-reference)
- [State Machine](#state-machine)
- [Methods](#methods)
- [Events](#events)
- [Configuration](#configuration)
- [HIPAA Compliance](#hipaa-compliance)
- [Usage Examples](#usage-examples)

---

## Overview

The `PresenceMonitorService` provides continuous real-time monitoring of user presence at healthcare workstations. This critical security component ensures HIPAA compliance by automatically locking sessions when:

- The authenticated user leaves the workstation
- An unauthorized face is detected
- Multiple faces are detected (potential shoulder surfing)
- Camera obstruction or tampering is detected

### Key Features

| Feature | Description |
|---------|-------------|
| **Continuous Monitoring** | 200ms polling interval for real-time presence detection |
| **Face Verification** | Compares detected faces against authenticated user embedding |
| **Auto-Lock** | Automatic session lock after configurable absence threshold |
| **Multi-Face Detection** | Detects and responds to unauthorized observers |
| **Event-Driven** | Publishes presence state changes for UI updates |

---

## Security Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Presence Monitoring Flow                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────┐    ┌──────────────┐    ┌─────────────────────────┐   │
│  │  Camera  │───▶│ Face Service │───▶│  Presence Monitor       │   │
│  │  Frame   │    │  Detection   │    │  Service                │   │
│  └──────────┘    └──────────────┘    └───────────┬─────────────┘   │
│                                                   │                  │
│                         ┌─────────────────────────┼──────────┐      │
│                         ▼                         ▼          ▼      │
│                  ┌─────────────┐          ┌───────────┐ ┌────────┐  │
│                  │   Extract   │          │  Compare  │ │ Count  │  │
│                  │  Embedding  │          │ Embedding │ │ Faces  │  │
│                  └──────┬──────┘          └─────┬─────┘ └───┬────┘  │
│                         │                       │           │       │
│                         └───────────────────────┴───────────┘       │
│                                       │                              │
│                              ┌────────┴────────┐                    │
│                              ▼                 ▼                    │
│                      ┌─────────────┐   ┌─────────────┐              │
│                      │  Authorized │   │Unauthorized │              │
│                      │    User     │   │  / Absent   │              │
│                      └──────┬──────┘   └──────┬──────┘              │
│                             │                 │                     │
│                             ▼                 ▼                     │
│                      ┌─────────────┐   ┌─────────────┐              │
│                      │   Continue  │   │    LOCK     │              │
│                      │  Monitoring │   │   SESSION   │              │
│                      └─────────────┘   └─────────────┘              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Class Reference

### Namespace
```csharp
MedSecureVision.Client.Services
```

### Class Declaration
```csharp
public class PresenceMonitorService : IPresenceMonitorService
```

### Dependencies

| Dependency | Purpose |
|------------|---------|
| `ILogger<PresenceMonitorService>` | Audit logging for compliance |
| `IFaceServiceClient` | Face detection and recognition |
| `ICameraService` | Camera frame capture |
| `ISessionLockService` | Windows session lock integration |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AbsenceDuration` | `TimeSpan?` | Time since user was last detected |

---

## State Machine

```
                    ┌─────────────────────┐
                    │      IDLE           │
                    │  (Not Monitoring)   │
                    └──────────┬──────────┘
                               │ StartMonitoringAsync()
                               ▼
                    ┌─────────────────────┐
           ┌───────│   AUTHENTICATED     │◀──────┐
           │       │   (User Present)    │       │
           │       └──────────┬──────────┘       │
           │                  │                  │
    No Face│          Multiple│Faces      Match │
    Detected                  │          Found  │
           │                  ▼                  │
           │       ┌─────────────────────┐       │
           │       │  MULTIPLE_FACES     │       │
           │       │  (Security Alert)   │───────┤
           │       └──────────┬──────────┘       │
           │                  │                  │
           │                  │ Lock Session     │
           ▼                  ▼                  │
    ┌─────────────────────────────────────┐     │
    │           NO_FACE                    │     │
    │    (Absence Timer Started)           │     │
    └──────────────┬───────────────────────┘     │
                   │                             │
                   │ Timeout Exceeded            │
                   ▼                             │
    ┌─────────────────────────────────────┐     │
    │      UNAUTHORIZED_FACE              │     │
    │    (Different Person Detected)      │─────┘
    └──────────────┬──────────────────────┘
                   │
                   │ Lock Session
                   ▼
    ┌─────────────────────────────────────┐
    │         SESSION_LOCKED              │
    │    (Windows Lock Screen)            │
    └─────────────────────────────────────┘
```

---

## Methods

### SetAuthenticatedUserEmbedding

Sets the face embedding of the authenticated user for presence monitoring.

```csharp
public void SetAuthenticatedUserEmbedding(float[] embedding)
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `embedding` | `float[]` | 512-dimensional face embedding vector |

**Usage:**
```csharp
// After successful authentication
var authResult = await _authService.AuthenticateAsync(frame, face);
if (authResult.Success)
{
    _presenceMonitor.SetAuthenticatedUserEmbedding(authResult.Embedding);
    await _presenceMonitor.StartMonitoringAsync();
}
```

---

### StartMonitoringAsync

Starts continuous presence monitoring with 200ms polling interval.

```csharp
public Task StartMonitoringAsync()
```

**Behavior:**
- Validates that authenticated user embedding is set
- Starts internal timer with 200ms interval
- Begins continuous face detection and verification

**HIPAA Compliance Note:**
> Starting presence monitoring is logged as an audit event

---

### StopMonitoringAsync

Stops presence monitoring.

```csharp
public Task StopMonitoringAsync()
```

**Usage:**
```csharp
// On user logout or application exit
await _presenceMonitor.StopMonitoringAsync();
```

---

## Events

### PresenceChanged

Fired when presence state changes.

```csharp
public event EventHandler<PresenceCheckResult>? PresenceChanged;
```

**PresenceCheckResult Structure:**
```csharp
public class PresenceCheckResult
{
    public PresenceState State { get; set; }
    public TimeSpan? AbsenceDuration { get; set; }
    public float? SimilarityScore { get; set; }
    public int FaceCount { get; set; }
}
```

**PresenceState Enum:**
| State | Description | Action |
|-------|-------------|--------|
| `Authenticated` | Authorized user present | Continue |
| `NoFace` | No face detected | Start absence timer |
| `UnauthorizedFace` | Different face detected | Lock immediately |
| `MultipleFaces` | More than one face | Lock immediately |
| `CameraError` | Camera unavailable | Alert user |

---

## Configuration

### appsettings.json

```json
{
  "PresenceMonitoring": {
    "CheckIntervalMs": 200,
    "AbsenceTimeoutSeconds": 5,
    "SimilarityThreshold": 0.5
  },
  "LockBehavior": {
    "Action": "LockScreen",
    "GracePeriodSeconds": 3
  }
}
```

### Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CheckIntervalMs` | 200 | Monitoring poll interval |
| `AbsenceTimeoutSeconds` | 5 | Time before auto-lock on no face |
| `SimilarityThreshold` | 0.5 | Lower threshold for presence (vs 0.6 for auth) |
| `GracePeriodSeconds` | 3 | Grace period before lock on absence |

---

## HIPAA Compliance

### Technical Safeguards Implemented

| Safeguard | Implementation |
|-----------|----------------|
| **Access Control** | Continuous verification of authorized user |
| **Audit Controls** | All presence events logged with timestamps |
| **Automatic Logoff** | Configurable timeout with auto-lock |
| **Session Integrity** | Immediate lock on unauthorized access attempt |

### Audit Events Generated

```csharp
// Events logged to audit system
"presence_monitoring_started"
"presence_monitoring_stopped"
"user_absent_detected"
"unauthorized_face_detected"
"multiple_faces_detected"
"session_locked"
```

---

## Usage Examples

### Basic Implementation

```csharp
public class AuthenticationViewModel
{
    private readonly IPresenceMonitorService _presenceMonitor;
    
    public AuthenticationViewModel(IPresenceMonitorService presenceMonitor)
    {
        _presenceMonitor = presenceMonitor;
        _presenceMonitor.PresenceChanged += OnPresenceChanged;
    }
    
    private void OnPresenceChanged(object? sender, PresenceCheckResult result)
    {
        switch (result.State)
        {
            case PresenceState.Authenticated:
                StatusMessage = "User verified";
                break;
                
            case PresenceState.NoFace:
                StatusMessage = $"Please return - locking in {5 - result.AbsenceDuration?.TotalSeconds:F0}s";
                break;
                
            case PresenceState.UnauthorizedFace:
                StatusMessage = "Unauthorized access - session locked";
                break;
                
            case PresenceState.MultipleFaces:
                StatusMessage = "Security alert - multiple faces detected";
                break;
        }
    }
}
```

### Integration with Session Lock

```csharp
// Automatic session lock on unauthorized access
private void HandleUnauthorizedFace(float similarity)
{
    _logger.LogWarning($"Unauthorized face detected (similarity: {similarity:F2})");
    _auditService.LogSecurityEvent("unauthorized_face_detected", new
    {
        similarity,
        timestamp = DateTime.UtcNow,
        workstation = Environment.MachineName
    });
    _sessionLockService.LockAsync("Unauthorized face detected");
}
```

---

## Related Documentation

- [Session Lock Service](./SessionLockService.md)
- [Face Service Client](./FaceServiceClient.md)
- [HIPAA Compliance Guide](../HIPAA_COMPLIANCE.md)
- [Security Architecture](../ARCHITECTURE.md)

---

<div align="center">

**MedSecure Vision** - HIPAA-Compliant Biometric Authentication

*Protecting Patient Data Through Continuous Presence Verification*

*© 2024 MedSecure Vision. All rights reserved.*

</div>


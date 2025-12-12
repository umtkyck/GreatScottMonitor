# Authentication Flow Documentation

<div align="center">

![MedSecure Vision](https://img.shields.io/badge/MedSecure-Vision-blue?style=for-the-badge)
![HIPAA](https://img.shields.io/badge/HIPAA-Compliant-green?style=for-the-badge)
![OAuth 2.0](https://img.shields.io/badge/OAuth-2.0-orange?style=for-the-badge)

**End-to-End Biometric Authentication Workflow**

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Authentication States](#authentication-states)
- [Primary Flow](#primary-flow)
- [Fallback Authentication](#fallback-authentication)
- [Session Management](#session-management)
- [Security Considerations](#security-considerations)

---

## Overview

MedSecure Vision implements a multi-layered authentication system that combines:

1. **Biometric Authentication** - Primary facial recognition
2. **Cloud Identity** - OAuth 2.0 via Auth0
3. **Fallback Methods** - PIN, Smart Card, Windows Hello

### Authentication Timeline

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Target: < 500ms Total                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐   │
│  │ Camera  │  │  Face   │  │Embedding│  │ Backend │  │ Session │   │
│  │ Capture │──│Detection│──│Extract  │──│ Verify  │──│ Create  │   │
│  └─────────┘  └─────────┘  └─────────┘  └─────────┘  └─────────┘   │
│     30ms        30ms         100ms        200ms        50ms         │
│                                                                      │
│  ◄────────────────────── 410ms ──────────────────────►              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Authentication States

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Authentication State Machine                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│                         ┌───────────┐                               │
│                         │   IDLE    │                               │
│                         └─────┬─────┘                               │
│                               │ Camera Started                      │
│                               ▼                                     │
│                         ┌───────────┐                               │
│              ┌──────────│ SEARCHING │◀─────────────┐               │
│              │          └─────┬─────┘              │               │
│              │                │ Face Found         │               │
│              │                ▼                    │ Retry         │
│              │          ┌───────────┐              │               │
│              │          │POSITIONING│──────────────┤               │
│              │          └─────┬─────┘              │               │
│              │                │ Face Aligned       │               │
│              │                ▼                    │               │
│   Timeout    │          ┌───────────┐              │               │
│              │          │ VERIFYING │              │               │
│              │          └─────┬─────┘              │               │
│              │         ┌──────┴──────┐             │               │
│              │         │             │             │               │
│              │         ▼             ▼             │               │
│              │   ┌───────────┐ ┌───────────┐      │               │
│              │   │  SUCCESS  │ │  FAILURE  │──────┘               │
│              │   └─────┬─────┘ └─────┬─────┘                      │
│              │         │             │                             │
│              │         │             │ Max Attempts                │
│              │         │             ▼                             │
│              │         │       ┌───────────┐                      │
│              └─────────┼──────▶│ FALLBACK  │                      │
│                        │       └───────────┘                      │
│                        ▼                                           │
│                  ┌───────────┐                                     │
│                  │AUTHENTICATED│                                   │
│                  └─────┬─────┘                                     │
│                        │                                           │
│                        ▼                                           │
│                  ┌───────────┐                                     │
│                  │MONITORING │                                     │
│                  └───────────┘                                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### State Descriptions

| State | Description | UI Indicator |
|-------|-------------|--------------|
| `IDLE` | Application starting | Loading spinner |
| `SEARCHING` | Looking for face | Pulsing green oval |
| `POSITIONING` | Guiding user alignment | Yellow oval + instructions |
| `VERIFYING` | Processing authentication | Yellow oval + "Verifying..." |
| `SUCCESS` | Authentication successful | Green checkmark animation |
| `FAILURE` | Authentication failed | Red oval + shake animation |
| `FALLBACK` | Alternative auth methods | PIN/Card dialog |
| `AUTHENTICATED` | User logged in | Minimize to tray |
| `MONITORING` | Presence tracking active | Tray icon green |

---

## Primary Flow

### Step 1: Camera Initialization

```csharp
// CameraService.cs
public async Task InitializeAsync()
{
    var cameras = GetAvailableCameras();
    if (cameras.Count == 0)
        throw new InvalidOperationException("No cameras available");
    
    _selectedCameraIndex = cameras[0].Index;
    _logger.LogInformation($"Camera initialized: {cameras[0].Name}");
}
```

### Step 2: Face Detection

```csharp
// AuthenticationViewModel.cs
private async void OnDetectionTimerTick(object? sender, EventArgs e)
{
    var frame = await _cameraService.GetCurrentFrameAsync();
    var detectionResult = await _faceServiceClient.DetectFacesAsync(frame);
    
    if (detectionResult.Faces.Count == 1)
    {
        // Single face detected - proceed to verification
        AuthenticationState = "Verifying";
        await VerifyFaceAsync(frame, detectionResult.Faces[0]);
    }
}
```

### Step 3: Embedding Extraction

```csharp
// FaceServiceClient.cs
public async Task<FaceEmbedding> ExtractEmbeddingAsync(
    BitmapSource frame, 
    DetectedFace? face = null)
{
    var message = new IpcMessage
    {
        Command = IpcCommands.ExtractEmbedding,
        FrameData = ConvertFrameToBase64(frame),
        Parameters = face != null 
            ? new { bbox = new[] { face.X, face.Y, face.Width, face.Height } }
            : null
    };
    
    var response = await SendMessageAsync(message);
    return ParseEmbeddingResponse(response);
}
```

### Step 4: Backend Verification

```csharp
// AuthenticationController.cs
[HttpPost("verify")]
public async Task<ActionResult<AuthenticationResponse>> Verify(
    [FromBody] AuthenticationRequest request)
{
    var result = await _faceVerificationService.VerifyFaceAsync(
        request.FaceEmbedding,
        threshold: 0.6f);
    
    // Log authentication attempt
    await _auditLogService.LogEventAsync(new AuditLog
    {
        EventType = "authentication",
        UserId = result.UserId,
        Result = result.Success ? "success" : "failure",
        ConfidenceScore = result.ConfidenceScore
    });
    
    return Ok(new AuthenticationResponse
    {
        Success = result.Success,
        UserId = result.UserId?.ToString(),
        UserName = result.UserName,
        SessionToken = result.SessionToken
    });
}
```

### Step 5: Session Establishment

```csharp
// After successful verification
if (authResult.Success)
{
    // Set user embedding for presence monitoring
    _presenceMonitor.SetAuthenticatedUserEmbedding(embedding.Vector);
    
    // Start continuous monitoring
    await _presenceMonitor.StartMonitoringAsync();
    
    // Update UI
    AuthenticationState = "Success";
    StatusMessage = $"Welcome, {authResult.UserName}!";
    
    // Transition after animation
    await Task.Delay(800);
    MinimizeToTray();
}
```

---

## Fallback Authentication

### Available Methods

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Fallback Authentication Options                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌───────────┐  │
│  │    PIN      │  │ Smart Card  │  │  Windows    │  │  Password │  │
│  │   Code      │  │   (PIV)     │  │   Hello     │  │   + MFA   │  │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘  └─────┬─────┘  │
│         │                │                │               │         │
│   6-8 digits      Certificate      Fingerprint/     Auth0 Login    │
│   5 attempts      PKCS#11          Face/PIN         + TOTP         │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### PIN Authentication

```csharp
// FallbackAuthService.cs
public async Task<bool> AuthenticateWithPinAsync(string pin)
{
    if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
    {
        _logger.LogWarning("PIN authentication locked out");
        return false;
    }
    
    if (_pinAttempts >= MaxPinAttempts)
    {
        _lockoutUntil = DateTime.UtcNow.AddMinutes(30);
        _pinAttempts = 0;
        return false;
    }
    
    var isValid = await VerifyPinWithBackendAsync(pin);
    
    if (!isValid)
        _pinAttempts++;
    else
        _pinAttempts = 0;
    
    return isValid;
}
```

---

## Session Management

### Session Lifecycle

```
┌─────────────────────────────────────────────────────────────────────┐
│                       Session Lifecycle                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  [Login] ──▶ [Session Created] ──▶ [Presence Monitoring Active]     │
│                     │                        │                       │
│                     │                        ▼                       │
│                     │            ┌─────────────────────┐            │
│                     │            │  Continuous Check   │            │
│                     │            │  (every 200ms)      │            │
│                     │            └──────────┬──────────┘            │
│                     │                       │                        │
│                     │         ┌─────────────┼─────────────┐         │
│                     │         ▼             ▼             ▼         │
│                     │    [User OK]    [No Face]    [Wrong Face]    │
│                     │         │             │             │         │
│                     │         │             │             │         │
│                     │         │      5 sec timeout       │         │
│                     │         │             │             │         │
│                     │         ▼             ▼             ▼         │
│                     │    [Continue]   [Lock Session]  [Lock Now]   │
│                     │                       │             │         │
│                     │                       └──────┬──────┘         │
│                     │                              │                 │
│                     ▼                              ▼                 │
│              [Manual Logout]            [Windows Lock Screen]       │
│                     │                              │                 │
│                     └──────────────┬───────────────┘                │
│                                    ▼                                 │
│                          [Session Terminated]                        │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

### Session Lock Triggers

| Trigger | Timeout | Action |
|---------|---------|--------|
| No face detected | 5 seconds | Lock workstation |
| Unauthorized face | Immediate | Lock + alert |
| Multiple faces | Immediate | Lock + alert |
| Camera obstruction | 3 seconds | Lock workstation |
| Manual logout | Immediate | Clean logout |
| Idle timeout | Configurable | Lock workstation |

---

## Security Considerations

### Anti-Spoofing Measures

| Attack | Defense |
|--------|---------|
| Photo attack | Liveness detection (blink) |
| Video replay | Head movement analysis |
| 3D mask | Depth texture analysis |
| Deepfake | Behavioral consistency |

### Rate Limiting

```csharp
// Authentication attempts
- Max 3 failed biometric attempts → fallback required
- Max 5 failed PIN attempts → 30 minute lockout
- Max 10 failed attempts per hour → account disabled

// Presence monitoring
- Check interval: 200ms (5 FPS)
- Absence timeout: 5 seconds
- Lock cooldown: None (immediate re-lock if unauthorized)
```

### Audit Trail

Every authentication event is logged:

```json
{
    "logId": "uuid",
    "eventType": "authentication",
    "timestamp": "2024-01-15T10:30:00Z",
    "userId": "uuid",
    "workstationId": "WORKSTATION-001",
    "ipAddress": "192.168.1.100",
    "result": "success",
    "confidenceScore": 0.89,
    "sessionId": "uuid"
}
```

---

## Related Documentation

- [Face Service Client](./FaceServiceClient.md)
- [Presence Monitor Service](./PresenceMonitorService.md)
- [Fallback Auth Service](./FallbackAuthService.md)
- [HIPAA Compliance](../HIPAA_COMPLIANCE.md)

---

<div align="center">

**MedSecure Vision** - HIPAA-Compliant Biometric Authentication

*Seamless Security for Healthcare Professionals*

*© 2024 MedSecure Vision. All rights reserved.*

</div>



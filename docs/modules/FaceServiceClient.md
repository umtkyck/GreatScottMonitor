# FaceServiceClient Module Documentation

<div align="center">

![CXA](https://img.shields.io/badge/CXA-R1M1-blue?style=for-the-badge)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Production-green?style=for-the-badge)

**Inter-Process Communication Client for Face Recognition Services**

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Class Reference](#class-reference)
- [Methods](#methods)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Error Handling](#error-handling)
- [Performance Considerations](#performance-considerations)

---

## Overview

The `FaceServiceClient` class provides a robust communication interface between the WPF client application and the Python face recognition service. It uses Windows Named Pipes for high-performance inter-process communication (IPC), enabling real-time face detection and recognition capabilities.

### Key Features

| Feature | Description |
|---------|-------------|
| **Named Pipe IPC** | High-performance local communication using Windows Named Pipes |
| **Async Operations** | Fully asynchronous API for non-blocking UI operations |
| **Face Detection** | MediaPipe BlazeFace integration for real-time face detection |
| **Embedding Extraction** | InsightFace ArcFace 512-dimensional face embedding extraction |
| **Cosine Similarity** | Built-in face comparison using cosine similarity algorithm |
| **Error Resilience** | Comprehensive error handling with graceful degradation |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     WPF Client Application                       │
├─────────────────────────────────────────────────────────────────┤
│                      FaceServiceClient                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ DetectFaces │  │  Extract    │  │    Compare Embeddings   │  │
│  │   Async     │  │  Embedding  │  │         Async           │  │
│  └──────┬──────┘  └──────┬──────┘  └───────────┬─────────────┘  │
│         │                │                     │                 │
│         └────────────────┼─────────────────────┘                 │
│                          │                                       │
│                   ┌──────┴──────┐                                │
│                   │ Named Pipe  │                                │
│                   │   Client    │                                │
│                   └──────┬──────┘                                │
└──────────────────────────┼──────────────────────────────────────┘
                           │
              \\.\pipe\CXAFaceService
                           │
┌──────────────────────────┼──────────────────────────────────────┐
│                   Python Face Service                            │
│                   ┌──────┴──────┐                                │
│                   │  IPC Server │                                │
│                   └──────┬──────┘                                │
│         ┌────────────────┼────────────────┐                     │
│         │                │                │                     │
│  ┌──────┴──────┐  ┌──────┴──────┐  ┌──────┴──────┐             │
│  │   MediaPipe │  │  InsightFace │  │   Quality   │             │
│  │  BlazeFace  │  │   ArcFace   │  │  Validator  │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Class Reference

### Namespace
```csharp
CXA.Client.Services
```

### Class Declaration
```csharp
public class FaceServiceClient : IFaceServiceClient
```

### Dependencies

| Dependency | Purpose |
|------------|---------|
| `ILogger<FaceServiceClient>` | Structured logging for diagnostics |
| `IOptions<FaceServiceOptions>` | Configuration injection |

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `BufferSize` | 65536 | Maximum message size for pipe communication |
| `TimeoutMs` | 5000 | Default connection timeout in milliseconds |

---

## Methods

### DetectFacesAsync

Detects faces in the provided frame using MediaPipe BlazeFace via the Python service.

```csharp
public async Task<FaceDetectionResult> DetectFacesAsync(BitmapSource frame)
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `frame` | `BitmapSource` | Bitmap image frame to analyze |

**Returns:** `FaceDetectionResult` containing detected faces with bounding boxes and landmarks

**Example:**
```csharp
var result = await _faceServiceClient.DetectFacesAsync(currentFrame);
if (result.Success && result.Faces.Count > 0)
{
    foreach (var face in result.Faces)
    {
        Console.WriteLine($"Face at ({face.X}, {face.Y}) - Confidence: {face.Confidence:P0}");
    }
}
```

---

### ExtractEmbeddingAsync

Extracts a 512-dimensional face embedding vector from the provided frame.

```csharp
public async Task<FaceEmbedding> ExtractEmbeddingAsync(
    BitmapSource frame, 
    DetectedFace? face = null)
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `frame` | `BitmapSource` | Bitmap image frame containing a face |
| `face` | `DetectedFace?` | Optional detected face with bounding box |

**Returns:** `FaceEmbedding` containing the 512-dimensional vector representation

**Example:**
```csharp
var embedding = await _faceServiceClient.ExtractEmbeddingAsync(frame, detectedFace);
if (embedding.Success)
{
    // Store embedding.Vector for later comparison
    float[] userEmbedding = embedding.Vector;
}
```

---

### CompareEmbeddingsAsync

Compares two face embeddings using cosine similarity.

```csharp
public async Task<FaceComparisonResult> CompareEmbeddingsAsync(
    float[] embedding1, 
    float[] embedding2, 
    float threshold = 0.6f)
```

**Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `embedding1` | `float[]` | - | First face embedding vector (512 dimensions) |
| `embedding2` | `float[]` | - | Second face embedding vector (512 dimensions) |
| `threshold` | `float` | 0.6f | Similarity threshold for match determination |

**Returns:** `FaceComparisonResult` with similarity score and match status

**Similarity Thresholds:**
| Threshold | Use Case | FAR | FRR |
|-----------|----------|-----|-----|
| 0.4 | Low security | Higher | Lower |
| 0.6 | Standard (recommended) | Balanced | Balanced |
| 0.8 | High security | Lower | Higher |

---

### IsConnectedAsync

Checks if the face service is available and responding.

```csharp
public async Task<bool> IsConnectedAsync()
```

**Returns:** `true` if service is connected and responding, `false` otherwise

---

## Configuration

### FaceServiceOptions

```csharp
public class FaceServiceOptions
{
    public string? PipeName { get; set; } = @"\\.\pipe\CXAFaceService";
}
```

### appsettings.json

```json
{
  "FaceService": {
    "PipeName": "\\\\.\\pipe\\CXAFaceService"
  }
}
```

---

## Error Handling

The client implements comprehensive error handling:

```csharp
try
{
    var result = await _faceServiceClient.DetectFacesAsync(frame);
}
catch (TimeoutException)
{
    // Service not responding - start/restart Python service
}
catch (IOException)
{
    // Pipe communication error
}
catch (Exception ex)
{
    // General error - check logs
}
```

### Error Response Structure

```csharp
public class FaceDetectionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }  // Error message if Success is false
    public List<DetectedFace> Faces { get; set; }
}
```

---

## Performance Considerations

| Operation | Typical Latency | Notes |
|-----------|-----------------|-------|
| Face Detection | 10-30ms | Single face, 640x480 frame |
| Embedding Extraction | 50-100ms | With GPU acceleration |
| Comparison | <1ms | Client-side cosine similarity |
| Pipe Round-trip | 5-15ms | Local IPC overhead |

### Optimization Tips

1. **Reuse connections** when possible
2. **Batch operations** for multiple frames
3. **Use GPU** for embedding extraction (CUDA)
4. **Compress frames** to JPEG before transmission

---

## Related Documentation

- [Python Face Service](./FaceService.md)
- [IPC Protocol](./IpcProtocol.md)
- [Authentication Flow](../ARCHITECTURE.md)

---

<div align="center">

**CXA** - HIPAA-Compliant Biometric Authentication

*© 2024 CXA. All rights reserved.*

</div>







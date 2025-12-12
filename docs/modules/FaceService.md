# Python Face Service Module Documentation

<div align="center">

![MedSecure Vision](https://img.shields.io/badge/MedSecure-Vision-blue?style=for-the-badge)
![Python](https://img.shields.io/badge/Python-3.10+-yellow?style=for-the-badge)
![MediaPipe](https://img.shields.io/badge/MediaPipe-BlazeFace-orange?style=for-the-badge)
![InsightFace](https://img.shields.io/badge/InsightFace-ArcFace-red?style=for-the-badge)

**High-Performance Face Detection and Recognition Engine**

</div>

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Components](#components)
- [IPC Protocol](#ipc-protocol)
- [API Reference](#api-reference)
- [Performance](#performance)
- [Deployment](#deployment)

---

## Overview

The Python Face Service is the core biometric processing engine of MedSecure Vision. It provides:

- **Real-time face detection** using MediaPipe BlazeFace
- **Face recognition** using InsightFace ArcFace model
- **Liveness detection** for anti-spoofing
- **Quality validation** for enrollment

### Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| Face Detection | MediaPipe BlazeFace | Real-time face localization |
| Face Recognition | InsightFace ArcFace (buffalo_l) | 512-D embedding extraction |
| Image Processing | OpenCV | Frame manipulation |
| IPC | Windows Named Pipes | Communication with C# client |
| Inference | ONNX Runtime | Model execution (CPU/GPU) |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                      Python Face Service                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                      main.py                                 │    │
│  │                   (Entry Point)                              │    │
│  └──────────────────────────┬──────────────────────────────────┘    │
│                             │                                        │
│           ┌─────────────────┼─────────────────┐                     │
│           ▼                 ▼                 ▼                     │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐       │
│  │  FaceDetector   │ │ FaceRecognizer  │ │   IpcServer     │       │
│  │ (face_detector) │ │(face_recognizer)│ │  (ipc_server)   │       │
│  └────────┬────────┘ └────────┬────────┘ └────────┬────────┘       │
│           │                   │                   │                 │
│           │                   │                   │                 │
│  ┌────────▼────────┐ ┌────────▼────────┐ ┌────────▼────────┐       │
│  │    MediaPipe    │ │   InsightFace   │ │  win32pipe      │       │
│  │   BlazeFace     │ │    ArcFace      │ │  (Named Pipes)  │       │
│  └─────────────────┘ └─────────────────┘ └─────────────────┘       │
│                                                                      │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                   Supporting Modules                         │    │
│  ├─────────────────┬─────────────────┬─────────────────────────┤    │
│  │ liveness_detector│quality_validator│     optimization       │    │
│  │  (Anti-spoof)   │ (Quality check) │  (Performance tuning)  │    │
│  └─────────────────┴─────────────────┴─────────────────────────┘    │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Components

### FaceDetector (`face_detector.py`)

MediaPipe BlazeFace wrapper for real-time face detection.

```python
class FaceDetector:
    def __init__(self, min_confidence: float = 0.7)
    def detect(self, frame: np.ndarray) -> List[Dict]
    def get_largest_face(self, faces: List[Dict]) -> Optional[Dict]
```

**Detection Output:**
```python
{
    'bbox': (x, y, width, height),  # Bounding box in pixels
    'confidence': 0.95,              # Detection confidence
    'landmarks': [                   # 6 facial landmarks
        {'x': 100, 'y': 120, 'type': 0, 'name': 'right_eye'},
        {'x': 180, 'y': 120, 'type': 1, 'name': 'left_eye'},
        {'x': 140, 'y': 160, 'type': 2, 'name': 'nose_tip'},
        {'x': 140, 'y': 200, 'type': 3, 'name': 'mouth_center'},
        {'x': 60, 'y': 140, 'type': 4, 'name': 'right_ear'},
        {'x': 220, 'y': 140, 'type': 5, 'name': 'left_ear'}
    ]
}
```

**Performance Specs:**
| Metric | Value |
|--------|-------|
| Frame Rate | 200-1000 FPS (CPU) |
| Latency | 1-5ms per frame |
| Accuracy | 98.5% mAP |

---

### FaceRecognizer (`face_recognizer.py`)

InsightFace ArcFace wrapper for face embedding extraction.

```python
class FaceRecognizer:
    def __init__(self, model_name: str = 'buffalo_l')
    def get_embedding(self, frame: np.ndarray, bbox: Optional[Tuple] = None) -> Optional[np.ndarray]
    def compare(self, embedding1: np.ndarray, embedding2: np.ndarray, threshold: float = 0.6) -> Tuple[bool, float]
    def align_face(self, frame: np.ndarray, landmarks: List[Dict]) -> Optional[np.ndarray]
```

**Model Specifications:**
| Property | Value |
|----------|-------|
| Model | buffalo_l (ArcFace) |
| Embedding Dimension | 512 |
| Input Size | 112x112 |
| LFW Accuracy | 99.83% |
| Inference | CUDA / CPU |

**Embedding Comparison:**
```python
# Cosine similarity calculation
def compare(self, emb1, emb2, threshold=0.6):
    emb1_norm = emb1 / np.linalg.norm(emb1)
    emb2_norm = emb2 / np.linalg.norm(emb2)
    similarity = np.dot(emb1_norm, emb2_norm)
    return similarity > threshold, float(similarity)
```

---

### IpcServer (`ipc_server.py`)

Windows Named Pipe server for C# client communication.

```python
class IpcServer:
    def __init__(self, pipe_name: str, face_detector, face_recognizer)
    def start(self)
    def stop(self)
```

**Pipe Configuration:**
| Parameter | Value |
|-----------|-------|
| Pipe Name | `\\.\pipe\MedSecureFaceService` |
| Mode | Duplex (bidirectional) |
| Type | Message |
| Buffer Size | 65536 bytes |

---

## IPC Protocol

### Message Format

**Request:**
```json
{
    "command": "DETECT",
    "frame_data": "<base64-encoded-jpeg>",
    "parameters": {
        "bbox": [100, 100, 200, 200]
    }
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "faces": [
            {
                "x": 100,
                "y": 150,
                "width": 200,
                "height": 250,
                "confidence": 0.95,
                "landmarks": [...]
            }
        ]
    },
    "error": null
}
```

### Commands

| Command | Description | Parameters |
|---------|-------------|------------|
| `DETECT` | Detect faces in frame | `frame_data` |
| `EXTRACT_EMBEDDING` | Extract face embedding | `frame_data`, `bbox` (optional) |
| `COMPARE` | Compare frame with embedding | `frame_data`, `embedding`, `threshold` |
| `ENROLL_CAPTURE` | Capture frame for enrollment | `frame_data` |
| `PING` | Health check | None |

---

## API Reference

### DETECT

Detects all faces in the provided frame.

**Request:**
```json
{
    "command": "DETECT",
    "frame_data": "<base64-jpeg>"
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "faces": [
            {
                "x": 120,
                "y": 80,
                "width": 180,
                "height": 220,
                "confidence": 0.97,
                "landmarks": [...]
            }
        ]
    }
}
```

---

### EXTRACT_EMBEDDING

Extracts 512-dimensional face embedding.

**Request:**
```json
{
    "command": "EXTRACT_EMBEDDING",
    "frame_data": "<base64-jpeg>",
    "parameters": {
        "bbox": [120, 80, 180, 220]
    }
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "embedding": [0.012, -0.034, 0.089, ...],
        "confidence": 1.0
    }
}
```

---

### ENROLL_CAPTURE

Captures and validates frame for user enrollment.

**Request:**
```json
{
    "command": "ENROLL_CAPTURE",
    "frame_data": "<base64-jpeg>"
}
```

**Response:**
```json
{
    "success": true,
    "data": {
        "embedding": [0.012, -0.034, 0.089, ...],
        "bbox": [120, 80, 180, 220],
        "confidence": 0.97,
        "quality_score": 0.85,
        "landmarks": [...]
    }
}
```

---

## Performance

### Benchmarks

| Operation | CPU (i7-10700) | GPU (RTX 3060) |
|-----------|----------------|----------------|
| Face Detection | 3ms | 1ms |
| Embedding Extraction | 80ms | 15ms |
| Full Pipeline | 100ms | 20ms |

### Optimization Tips

1. **Enable GPU acceleration:**
   ```python
   providers=['CUDAExecutionProvider', 'CPUExecutionProvider']
   ```

2. **Batch processing** for multiple frames

3. **Frame resizing** for faster detection:
   ```python
   frame = cv2.resize(frame, (640, 480))
   ```

4. **Model warmup** on startup:
   ```python
   # Run inference on dummy frame to warm up
   dummy = np.zeros((480, 640, 3), dtype=np.uint8)
   detector.detect(dummy)
   ```

---

## Deployment

### Requirements

```text
mediapipe>=0.10.0
insightface>=0.7.3
opencv-python>=4.8.0
numpy>=1.24.0
onnxruntime>=1.16.0
onnxruntime-gpu>=1.16.0
pywin32>=306
```

### Installation

```bash
# Create virtual environment
python -m venv venv
venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# Download InsightFace models (automatic on first run)
python main.py
```

### Running as Service

```bash
# Development
python main.py

# Production (with logging)
python main.py > face_service.log 2>&1
```

---

## Related Documentation

- [FaceServiceClient (C#)](./FaceServiceClient.md)
- [IPC Protocol Specification](./IpcProtocol.md)
- [Deployment Guide](../DEPLOYMENT.md)

---

<div align="center">

**MedSecure Vision** - HIPAA-Compliant Biometric Authentication

*Powered by MediaPipe and InsightFace*

*© 2024 MedSecure Vision. All rights reserved.*

</div>



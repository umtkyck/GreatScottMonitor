"""
MediaPipe BlazeFace face detection wrapper
"""

import cv2
import mediapipe as mp
import numpy as np
import logging
from typing import List, Dict, Optional

logger = logging.getLogger(__name__)

class FaceDetector:
    def __init__(self, min_confidence: float = 0.7):
        """
        Initialize MediaPipe face detector
        
        Args:
            min_confidence: Minimum detection confidence threshold (0.0-1.0)
        """
        self.min_confidence = min_confidence
        self.mp_face = mp.solutions.face_detection
        self.detector = self.mp_face.FaceDetection(
            model_selection=0,  # 0=short-range (2m), 1=full-range (5m)
            min_detection_confidence=min_confidence
        )
        logger.info(f"FaceDetector initialized with confidence threshold: {min_confidence}")

    def detect(self, frame: np.ndarray) -> List[Dict]:
        """
        Detect faces in a frame using MediaPipe BlazeFace.
        
        This method processes a single frame and returns all detected faces with their
        bounding boxes, confidence scores, and facial landmarks. The detection is
        optimized for real-time performance (200-1000 FPS on CPU).
        
        Args:
            frame: BGR image frame from OpenCV (numpy array)
            
        Returns:
            List of dictionaries, each containing:
                - 'bbox': Tuple of (x, y, width, height) in pixels
                - 'confidence': Detection confidence score (0.0-1.0)
                - 'landmarks': List of facial landmark points (6 key points)
        """
        if frame is None or frame.size == 0:
            return []

        # Convert BGR to RGB
        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        rgb.flags.writeable = False  # Performance optimization
        
        # Detect faces
        results = self.detector.process(rgb)
        
        faces = []
        if results.detections:
            h, w = frame.shape[:2]
            for detection in results.detections:
                bbox = detection.location_data.relative_bounding_box
                
                # Extract bounding box coordinates
                x = int(bbox.xmin * w)
                y = int(bbox.ymin * h)
                width = int(bbox.width * w)
                height = int(bbox.height * h)
                
                # Extract landmarks (6 key points)
                landmarks = self._extract_landmarks(detection, w, h)
                
                faces.append({
                    'bbox': (x, y, width, height),
                    'confidence': detection.score[0],
                    'landmarks': landmarks
                })
        
        return faces

    def _extract_landmarks(self, detection, width: int, height: int) -> List[Dict]:
        """
        Extract facial landmarks from detection
        
        Args:
            detection: MediaPipe detection result
            width: Frame width
            height: Frame height
            
        Returns:
            List of landmark dictionaries with x, y, type
        """
        landmarks = []
        if hasattr(detection.location_data, 'relative_keypoints'):
            landmark_types = {
                0: 'right_eye',
                1: 'left_eye',
                2: 'nose_tip',
                3: 'mouth_center',
                4: 'right_ear',
                5: 'left_ear'
            }
            
            for idx, landmark in enumerate(detection.location_data.relative_keypoints):
                landmarks.append({
                    'x': int(landmark.x * width),
                    'y': int(landmark.y * height),
                    'type': idx,
                    'name': landmark_types.get(idx, 'unknown')
                })
        
        return landmarks

    def get_largest_face(self, faces: List[Dict]) -> Optional[Dict]:
        """
        Get the largest face from a list of detected faces
        
        Args:
            faces: List of detected faces
            
        Returns:
            Largest face or None if empty
        """
        if not faces:
            return None
        
        return max(faces, key=lambda f: f['bbox'][2] * f['bbox'][3])


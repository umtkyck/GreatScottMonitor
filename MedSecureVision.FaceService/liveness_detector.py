"""
Liveness detection for anti-spoofing
"""

import cv2
import numpy as np
import logging
from typing import List, Dict, Optional
from collections import deque

logger = logging.getLogger(__name__)

class LivenessDetector:
    def __init__(self, blink_threshold: float = 0.3, history_size: int = 10):
        """
        Initialize liveness detector
        
        Args:
            blink_threshold: Eye aspect ratio threshold for blink detection
            history_size: Number of frames to keep in history
        """
        self.blink_threshold = blink_threshold
        self.history_size = history_size
        self.eye_history: deque = deque(maxlen=history_size)
        self.head_pose_history: deque = deque(maxlen=history_size)

    def calculate_eye_aspect_ratio(self, eye_landmarks: List[Dict]) -> float:
        """
        Calculate Eye Aspect Ratio (EAR) for blink detection
        
        Args:
            eye_landmarks: Eye landmark points
            
        Returns:
            EAR value (lower = more closed)
        """
        if len(eye_landmarks) < 4:
            return 1.0

        # Get key points (simplified - assumes 4 points per eye)
        points = [(lm['x'], lm['y']) for lm in eye_landmarks[:4]]
        
        # Calculate distances
        # Vertical distances
        vertical_1 = np.linalg.norm(np.array(points[1]) - np.array(points[5]))
        vertical_2 = np.linalg.norm(np.array(points[2]) - np.array(points[4]))
        
        # Horizontal distance
        horizontal = np.linalg.norm(np.array(points[0]) - np.array(points[3]))
        
        if horizontal == 0:
            return 1.0
        
        # EAR formula
        ear = (vertical_1 + vertical_2) / (2.0 * horizontal)
        return float(ear)

    def detect_blink(self, landmarks: List[Dict]) -> bool:
        """
        Detect if person is blinking
        
        Args:
            landmarks: Face landmarks
            
        Returns:
            True if blink detected
        """
        # Find eye landmarks
        right_eye = [lm for lm in landmarks if lm.get('name') == 'right_eye']
        left_eye = [lm for lm in landmarks if lm.get('name') == 'left_eye']
        
        if not right_eye or not left_eye:
            return False

        # Calculate EAR for both eyes
        right_ear = self.calculate_eye_aspect_ratio(right_eye)
        left_ear = self.calculate_eye_aspect_ratio(left_eye)
        avg_ear = (right_ear + left_ear) / 2.0

        # Add to history
        self.eye_history.append(avg_ear)

        # Blink detected if EAR drops below threshold
        if len(self.eye_history) >= 2:
            if avg_ear < self.blink_threshold and self.eye_history[-2] >= self.blink_threshold:
                return True

        return False

    def detect_head_movement(self, landmarks: List[Dict]) -> Dict:
        """
        Detect head movement/pose changes
        
        Args:
            landmarks: Face landmarks
            
        Returns:
            Dictionary with movement information
        """
        if not landmarks:
            return {"has_movement": False, "angle": 0.0}

        # Calculate head angle from eye positions
        right_eye = next((lm for lm in landmarks if lm.get('name') == 'right_eye'), None)
        left_eye = next((lm for lm in landmarks if lm.get('name') == 'left_eye'), None)
        
        if not right_eye or not left_eye:
            return {"has_movement": False, "angle": 0.0}

        # Calculate angle
        dx = left_eye['x'] - right_eye['x']
        dy = left_eye['y'] - right_eye['y']
        angle = np.degrees(np.arctan2(dy, dx))

        # Add to history
        self.head_pose_history.append(angle)

        # Check for significant movement
        has_movement = False
        if len(self.head_pose_history) >= 3:
            angle_change = abs(self.head_pose_history[-1] - self.head_pose_history[0])
            has_movement = angle_change > 5.0  # 5 degrees threshold

        return {
            "has_movement": has_movement,
            "angle": float(angle),
            "angle_change": float(angle_change) if len(self.head_pose_history) >= 2 else 0.0
        }

    def check_liveness(self, frame: np.ndarray, landmarks: List[Dict], method: str = "blink") -> Dict:
        """
        Perform liveness check
        
        Args:
            frame: Image frame
            landmarks: Face landmarks
            method: Liveness method ('blink', 'movement', or 'both')
            
        Returns:
            Dictionary with liveness result
        """
        result = {
            "is_live": False,
            "method": method,
            "confidence": 0.0,
            "details": {}
        }

        if method == "blink" or method == "both":
            blink_detected = self.detect_blink(landmarks)
            result["details"]["blink_detected"] = blink_detected
            if blink_detected:
                result["is_live"] = True
                result["confidence"] = 0.8

        if method == "movement" or method == "both":
            movement = self.detect_head_movement(landmarks)
            result["details"]["head_movement"] = movement
            if movement["has_movement"]:
                result["is_live"] = True
                result["confidence"] = max(result["confidence"], 0.7)

        if method == "both" and result["details"].get("blink_detected") and result["details"].get("head_movement", {}).get("has_movement"):
            result["confidence"] = 0.95

        return result

    def detect_spoofing(self, frame: np.ndarray) -> Dict:
        """
        Detect photo/video spoofing attempts
        
        Args:
            frame: Image frame
            
        Returns:
            Dictionary with spoofing detection result
        """
        result = {
            "is_spoof": False,
            "confidence": 0.0,
            "reasons": []
        }

        try:
            # Texture analysis - real faces have more texture
            gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
            laplacian = cv2.Laplacian(gray, cv2.CV_64F)
            texture_variance = laplacian.var()
            
            if texture_variance < 50:  # Low texture suggests photo
                result["is_spoof"] = True
                result["confidence"] = 0.6
                result["reasons"].append("low_texture")

            # Reflection detection - screens have reflections
            # This is a simplified check
            hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
            saturation = hsv[:, :, 1].mean()
            
            if saturation < 30:  # Low saturation might indicate screen
                result["confidence"] = max(result["confidence"], 0.4)
                result["reasons"].append("low_saturation")

        except Exception as e:
            logger.error(f"Error in spoofing detection: {e}")

        return result



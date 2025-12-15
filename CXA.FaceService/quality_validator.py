"""
Face quality validation for enrollment
"""

import cv2
import numpy as np
import logging
from typing import Dict, List

logger = logging.getLogger(__name__)

class QualityValidator:
    def __init__(self):
        """Initialize quality validator"""
        self.min_blur_variance = 100.0
        self.min_face_size = 224  # Minimum face size in pixels
        self.min_coverage = 0.5  # 50% of frame
        self.max_coverage = 0.8  # 80% of frame

    def validate_frame(self, frame: np.ndarray, face_bbox: tuple, landmarks: List[Dict]) -> Dict:
        """
        Validate frame quality for enrollment
        
        Args:
            frame: Image frame
            face_bbox: Face bounding box (x, y, width, height)
            landmarks: Face landmarks
            
        Returns:
            Dictionary with validation results
        """
        result = {
            "is_valid": True,
            "score": 0.0,
            "errors": [],
            "warnings": [],
            "details": {}
        }

        # Check blur
        blur_result = self.check_blur(frame, face_bbox)
        result["details"]["blur"] = blur_result
        if not blur_result["is_acceptable"]:
            result["is_valid"] = False
            result["errors"].append("Frame is too blurry")

        # Check lighting
        lighting_result = self.check_lighting(frame, face_bbox)
        result["details"]["lighting"] = lighting_result
        if not lighting_result["is_acceptable"]:
            result["warnings"].append("Lighting may be suboptimal")

        # Check face coverage
        coverage_result = self.check_coverage(frame, face_bbox)
        result["details"]["coverage"] = coverage_result
        if not coverage_result["is_acceptable"]:
            result["is_valid"] = False
            result["errors"].append("Face size is not within acceptable range")

        # Check eye visibility
        eye_result = self.check_eye_visibility(landmarks)
        result["details"]["eye_visibility"] = eye_result
        if not eye_result["both_visible"]:
            result["is_valid"] = False
            result["errors"].append("Both eyes must be visible")

        # Check face resolution
        resolution_result = self.check_resolution(face_bbox)
        result["details"]["resolution"] = resolution_result
        if not resolution_result["meets_minimum"]:
            result["is_valid"] = False
            result["errors"].append(f"Face resolution too low (minimum: {self.min_face_size}x{self.min_face_size})")

        # Calculate overall score
        scores = [
            blur_result.get("score", 0.0),
            lighting_result.get("score", 0.0),
            coverage_result.get("score", 0.0),
            eye_result.get("score", 0.0),
            resolution_result.get("score", 0.0)
        ]
        result["score"] = float(np.mean(scores))

        return result

    def check_blur(self, frame: np.ndarray, face_bbox: tuple) -> Dict:
        """
        Check for blur using Laplacian variance
        
        Args:
            frame: Image frame
            face_bbox: Face bounding box
            
        Returns:
            Blur check result
        """
        x, y, w, h = face_bbox
        face_roi = frame[y:y+h, x:x+w]
        
        if face_roi.size == 0:
            return {
                "is_acceptable": False,
                "variance": 0.0,
                "score": 0.0
            }

        gray = cv2.cvtColor(face_roi, cv2.COLOR_BGR2GRAY)
        laplacian_var = cv2.Laplacian(gray, cv2.CV_64F).var()
        
        is_acceptable = laplacian_var >= self.min_blur_variance
        score = min(laplacian_var / self.min_blur_variance, 1.0)

        return {
            "is_acceptable": is_acceptable,
            "variance": float(laplacian_var),
            "score": float(score)
        }

    def check_lighting(self, frame: np.ndarray, face_bbox: tuple) -> Dict:
        """
        Check lighting uniformity
        
        Args:
            frame: Image frame
            face_bbox: Face bounding box
            
        Returns:
            Lighting check result
        """
        x, y, w, h = face_bbox
        face_roi = frame[y:y+h, x:x+w]
        
        if face_roi.size == 0:
            return {
                "is_acceptable": False,
                "brightness": 0.0,
                "uniformity": 0.0,
                "score": 0.0
            }

        gray = cv2.cvtColor(face_roi, cv2.COLOR_BGR2GRAY)
        
        # Calculate brightness
        brightness = float(gray.mean())
        
        # Calculate uniformity (standard deviation - lower is more uniform)
        std_dev = float(gray.std())
        uniformity = 1.0 - min(std_dev / 50.0, 1.0)  # Normalize
        
        # Acceptable if brightness is reasonable (50-200) and uniformity is good
        is_acceptable = 50 <= brightness <= 200 and uniformity > 0.5
        score = (brightness / 200.0 * 0.5 + uniformity * 0.5)

        return {
            "is_acceptable": is_acceptable,
            "brightness": brightness,
            "uniformity": float(uniformity),
            "score": float(score)
        }

    def check_coverage(self, frame: np.ndarray, face_bbox: tuple) -> Dict:
        """
        Check if face coverage is within acceptable range (50-80% of frame)
        
        Args:
            frame: Image frame
            face_bbox: Face bounding box
            
        Returns:
            Coverage check result
        """
        frame_area = frame.shape[0] * frame.shape[1]
        face_area = face_bbox[2] * face_bbox[3]
        coverage = face_area / frame_area if frame_area > 0 else 0.0

        is_acceptable = self.min_coverage <= coverage <= self.max_coverage
        
        # Score based on how close to ideal (65%)
        ideal_coverage = 0.65
        distance_from_ideal = abs(coverage - ideal_coverage)
        score = 1.0 - min(distance_from_ideal / 0.15, 1.0)

        return {
            "is_acceptable": is_acceptable,
            "coverage": float(coverage),
            "score": float(score)
        }

    def check_eye_visibility(self, landmarks: List[Dict]) -> Dict:
        """
        Check if both eyes are visible
        
        Args:
            landmarks: Face landmarks
            
        Returns:
            Eye visibility check result
        """
        right_eye = any(lm.get('name') == 'right_eye' for lm in landmarks)
        left_eye = any(lm.get('name') == 'left_eye' for lm in landmarks)
        
        both_visible = right_eye and left_eye
        score = 1.0 if both_visible else 0.0

        return {
            "both_visible": both_visible,
            "right_eye_visible": right_eye,
            "left_eye_visible": left_eye,
            "score": score
        }

    def check_resolution(self, face_bbox: tuple) -> Dict:
        """
        Check if face resolution meets minimum requirements
        
        Args:
            face_bbox: Face bounding box
            
        Returns:
            Resolution check result
        """
        width, height = face_bbox[2], face_bbox[3]
        min_dimension = min(width, height)
        
        meets_minimum = min_dimension >= self.min_face_size
        score = min(min_dimension / self.min_face_size, 1.0)

        return {
            "meets_minimum": meets_minimum,
            "width": width,
            "height": height,
            "min_dimension": min_dimension,
            "score": float(score)
        }







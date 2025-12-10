"""
InsightFace ArcFace face recognition wrapper
"""

import cv2
import numpy as np
import logging
from typing import Optional, List, Tuple, Dict
from insightface.app import FaceAnalysis

logger = logging.getLogger(__name__)

class FaceRecognizer:
    def __init__(self, model_name: str = 'buffalo_l'):
        """
        Initialize InsightFace face recognizer
        
        Args:
            model_name: Model name ('buffalo_l' for high accuracy)
        """
        self.model_name = model_name
        logger.info(f"Initializing InsightFace with model: {model_name}")
        
        try:
            self.app = FaceAnalysis(
                name=model_name,
                providers=['CUDAExecutionProvider', 'CPUExecutionProvider']
            )
            self.app.prepare(ctx_id=0, det_size=(640, 640))
            logger.info("FaceRecognizer initialized successfully")
        except Exception as e:
            logger.error(f"Failed to initialize FaceRecognizer: {e}")
            raise

    def get_embedding(self, frame: np.ndarray, bbox: Optional[Tuple[int, int, int, int]] = None) -> Optional[np.ndarray]:
        """
        Extract 512-dimensional face embedding from frame using InsightFace ArcFace.
        
        The embedding is a mathematical representation of facial features that can be
        used for face recognition. Similar faces will have similar embeddings (high
        cosine similarity). This method uses the buffalo_l model which achieves
        99.83% accuracy on the LFW benchmark.
        
        Args:
            frame: BGR image frame from OpenCV (numpy array)
            bbox: Optional bounding box (x, y, width, height) to crop face region.
                  If None, InsightFace will detect and crop automatically.
            
        Returns:
            512-dimensional numpy array (embedding vector) or None if no face detected.
            The embedding is normalized and can be compared using cosine similarity.
        """
        if frame is None or frame.size == 0:
            return None

        try:
            # Crop to bounding box if provided
            if bbox:
                x, y, w, h = bbox
                frame = frame[y:y+h, x:x+w]
                if frame.size == 0:
                    return None

            # Get faces from InsightFace
            faces = self.app.get(frame)
            
            if len(faces) == 0:
                return None
            
            # Return embedding of largest face
            largest = max(faces, key=lambda f: f.bbox[2] * f.bbox[3])
            return largest.embedding  # 512-dimensional vector
            
        except Exception as e:
            logger.error(f"Error extracting embedding: {e}", exc_info=True)
            return None

    def compare(self, embedding1: np.ndarray, embedding2: np.ndarray, threshold: float = 0.6) -> Tuple[bool, float]:
        """
        Compare two face embeddings using cosine similarity.
        
        Cosine similarity measures the angle between two vectors in high-dimensional space.
        Values range from -1 (opposite) to 1 (identical). For face recognition, values
        above 0.6 typically indicate the same person, though this threshold can be
        adjusted based on security requirements.
        
        Args:
            embedding1: First 512-dimensional embedding vector
            embedding2: Second 512-dimensional embedding vector
            threshold: Similarity threshold (default 0.6). Values above this indicate a match.
            
        Returns:
            Tuple of (is_match: bool, similarity_score: float)
            - is_match: True if similarity > threshold
            - similarity_score: Cosine similarity value (0.0-1.0)
        """
        if embedding1 is None or embedding2 is None:
            return False, 0.0
        
        try:
            # Normalize embeddings
            emb1_norm = embedding1 / np.linalg.norm(embedding1)
            emb2_norm = embedding2 / np.linalg.norm(embedding2)
            
            # Cosine similarity
            similarity = np.dot(emb1_norm, emb2_norm)
            
            is_match = similarity > threshold
            return is_match, float(similarity)
            
        except Exception as e:
            logger.error(f"Error comparing embeddings: {e}", exc_info=True)
            return False, 0.0

    def align_face(self, frame: np.ndarray, landmarks: List[Dict]) -> Optional[np.ndarray]:
        """
        Align face using landmarks for better recognition accuracy
        
        Args:
            frame: Original frame
            landmarks: Face landmarks
            
        Returns:
            Aligned face image or None
        """
        if not landmarks or len(landmarks) < 2:
            return None
        
        try:
            # Find eye landmarks
            right_eye = next((lm for lm in landmarks if lm.get('name') == 'right_eye'), None)
            left_eye = next((lm for lm in landmarks if lm.get('name') == 'left_eye'), None)
            
            if not right_eye or not left_eye:
                return None
            
            # Calculate angle between eyes
            eye_center_x = (right_eye['x'] + left_eye['x']) / 2
            eye_center_y = (right_eye['y'] + left_eye['y']) / 2
            eye_dx = left_eye['x'] - right_eye['x']
            eye_dy = left_eye['y'] - right_eye['y']
            angle = np.degrees(np.arctan2(eye_dy, eye_dx))
            
            # Rotate image to align eyes horizontally
            center = (eye_center_x, eye_center_y)
            rotation_matrix = cv2.getRotationMatrix2D(center, angle, 1.0)
            aligned = cv2.warpAffine(frame, rotation_matrix, (frame.shape[1], frame.shape[0]))
            
            return aligned
            
        except Exception as e:
            logger.error(f"Error aligning face: {e}", exc_info=True)
            return None


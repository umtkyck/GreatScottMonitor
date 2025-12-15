"""
Named pipe IPC server for communication with C# client
"""

import json
import base64
import logging
import threading
import time
from typing import Optional
import cv2
import numpy as np

try:
    import win32pipe
    import win32file
    import pywintypes
    WINDOWS_AVAILABLE = True
except ImportError:
    WINDOWS_AVAILABLE = False
    logging.warning("win32pipe not available, using fallback implementation")

logger = logging.getLogger(__name__)

class IpcServer:
    def __init__(self, pipe_name: str, face_detector, face_recognizer):
        """
        Initialize IPC server
        
        Args:
            pipe_name: Named pipe name (e.g., r'\\.\pipe\CXAFaceService')
            face_detector: FaceDetector instance
            face_recognizer: FaceRecognizer instance
        """
        self.pipe_name = pipe_name
        self.face_detector = face_detector
        self.face_recognizer = face_recognizer
        self.running = False
        self.server_thread: Optional[threading.Thread] = None

    def start(self):
        """Start the IPC server"""
        if not WINDOWS_AVAILABLE:
            logger.error("Windows named pipes not available. Install pywin32: pip install pywin32")
            return

        self.running = True
        self.server_thread = threading.Thread(target=self._server_loop, daemon=True)
        self.server_thread.start()
        logger.info(f"IPC server started on pipe: {self.pipe_name}")
        
        # Keep main thread alive
        try:
            while self.running:
                time.sleep(1)
        except KeyboardInterrupt:
            self.stop()

    def stop(self):
        """Stop the IPC server"""
        self.running = False
        if self.server_thread:
            self.server_thread.join(timeout=5)
        logger.info("IPC server stopped")

    def _server_loop(self):
        """Main server loop - accepts connections and processes requests"""
        while self.running:
            try:
                pipe_handle = win32pipe.CreateNamedPipe(
                    self.pipe_name,
                    win32pipe.PIPE_ACCESS_DUPLEX,
                    win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
                    1,  # Max instances
                    65536,  # Out buffer size
                    65536,  # In buffer size
                    0,  # Default timeout
                    None  # Security attributes
                )

                logger.info("Waiting for client connection...")
                win32pipe.ConnectNamedPipe(pipe_handle, None)

                logger.info("Client connected")
                self._handle_client(pipe_handle)

            except pywintypes.error as e:
                if e.winerror == 109:  # ERROR_BROKEN_PIPE
                    logger.debug("Client disconnected")
                elif self.running:
                    logger.error(f"Pipe error: {e}")
                    time.sleep(1)  # Wait before retrying
            except Exception as e:
                if self.running:
                    logger.error(f"Error in server loop: {e}", exc_info=True)
                    time.sleep(1)

    def _handle_client(self, pipe_handle):
        """Handle client requests"""
        try:
            while self.running:
                # Read message
                result, data = win32file.ReadFile(pipe_handle, 65536)
                
                if result != 0:
                    break
                
                if not data:
                    continue

                # Parse JSON message
                try:
                    message = json.loads(data.decode('utf-8'))
                    response = self._process_message(message)
                    
                    # Send response
                    response_json = json.dumps(response).encode('utf-8')
                    win32file.WriteFile(pipe_handle, response_json)
                    win32file.FlushFileBuffers(pipe_handle)
                    
                except json.JSONDecodeError as e:
                    logger.error(f"Invalid JSON message: {e}")
                    response = {
                        "success": False,
                        "error": f"Invalid JSON: {str(e)}"
                    }
                    response_json = json.dumps(response).encode('utf-8')
                    win32file.WriteFile(pipe_handle, response_json)
                    
        except pywintypes.error as e:
            if e.winerror != 109:  # Not a broken pipe error
                logger.error(f"Error handling client: {e}")
        finally:
            try:
                win32file.CloseHandle(pipe_handle)
            except:
                pass

    def _process_message(self, message: dict) -> dict:
        """
        Process incoming IPC message
        
        Args:
            message: Parsed JSON message
            
        Returns:
            Response dictionary
        """
        command = message.get('command', '').upper()
        frame_data = message.get('frame_data')
        parameters = message.get('parameters', {})

        try:
            if command == 'DETECT':
                return self._handle_detect(frame_data)
            elif command == 'EXTRACT_EMBEDDING':
                return self._handle_extract_embedding(frame_data, parameters)
            elif command == 'COMPARE':
                return self._handle_compare(frame_data, parameters)
            elif command == 'ENROLL_CAPTURE':
                return self._handle_enroll_capture(frame_data, parameters)
            else:
                return {
                    "success": False,
                    "error": f"Unknown command: {command}"
                }
        except Exception as e:
            logger.error(f"Error processing command {command}: {e}", exc_info=True)
            return {
                "success": False,
                "error": str(e)
            }

    def _decode_frame(self, frame_data: str) -> Optional[np.ndarray]:
        """Decode base64 encoded frame to OpenCV image"""
        try:
            if not frame_data:
                return None
            
            # Decode base64
            image_bytes = base64.b64decode(frame_data)
            
            # Convert to numpy array
            nparr = np.frombuffer(image_bytes, np.uint8)
            
            # Decode image
            frame = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
            return frame
        except Exception as e:
            logger.error(f"Error decoding frame: {e}")
            return None

    def _handle_detect(self, frame_data: str) -> dict:
        """Handle DETECT command"""
        frame = self._decode_frame(frame_data)
        if frame is None:
            return {"success": False, "error": "Invalid frame data"}

        faces = self.face_detector.detect(frame)
        
        result = {
            "success": True,
            "data": {
                "faces": [
                    {
                        "x": f["bbox"][0],
                        "y": f["bbox"][1],
                        "width": f["bbox"][2],
                        "height": f["bbox"][3],
                        "confidence": float(f["confidence"]),
                        "landmarks": f["landmarks"]
                    }
                    for f in faces
                ]
            }
        }
        return result

    def _handle_extract_embedding(self, frame_data: str, parameters: dict) -> dict:
        """Handle EXTRACT_EMBEDDING command"""
        frame = self._decode_frame(frame_data)
        if frame is None:
            return {"success": False, "error": "Invalid frame data"}

        # Optional bounding box
        bbox = None
        if 'bbox' in parameters:
            bbox = tuple(parameters['bbox'])

        embedding = self.face_recognizer.get_embedding(frame, bbox)
        
        if embedding is None:
            return {"success": False, "error": "No face detected"}

        return {
            "success": True,
            "data": {
                "embedding": embedding.tolist(),
                "confidence": 1.0  # InsightFace doesn't provide confidence for embeddings
            }
        }

    def _handle_compare(self, frame_data: str, parameters: dict) -> dict:
        """Handle COMPARE command - compare frame embedding with provided embedding"""
        frame = self._decode_frame(frame_data)
        if frame is None:
            return {"success": False, "error": "Invalid frame data"}

        if 'embedding' not in parameters:
            return {"success": False, "error": "Missing embedding parameter"}

        # Extract embedding from frame
        frame_embedding = self.face_recognizer.get_embedding(frame)
        if frame_embedding is None:
            return {"success": False, "error": "No face detected in frame"}

        # Get comparison embedding
        compare_embedding = np.array(parameters['embedding'], dtype=np.float32)
        threshold = parameters.get('threshold', 0.6)

        # Compare
        is_match, similarity = self.face_recognizer.compare(frame_embedding, compare_embedding, threshold)

        return {
            "success": True,
            "data": {
                "match": is_match,
                "similarity": float(similarity),
                "threshold": float(threshold)
            }
        }

    def _handle_enroll_capture(self, frame_data: str, parameters: dict) -> dict:
        """Handle ENROLL_CAPTURE command - capture and validate frame for enrollment"""
        frame = self._decode_frame(frame_data)
        if frame is None:
            return {"success": False, "error": "Invalid frame data"}

        # Detect face first
        faces = self.face_detector.detect(frame)
        if not faces:
            return {"success": False, "error": "No face detected"}

        # Get largest face
        largest_face = self.face_detector.get_largest_face(faces)
        if not largest_face:
            return {"success": False, "error": "No valid face detected"}

        # Extract embedding
        bbox = largest_face['bbox']
        embedding = self.face_recognizer.get_embedding(frame, bbox)
        
        if embedding is None:
            return {"success": False, "error": "Failed to extract embedding"}

        # Basic quality checks
        quality_score = self._calculate_quality_score(frame, largest_face)

        return {
            "success": True,
            "data": {
                "embedding": embedding.tolist(),
                "bbox": list(bbox),
                "confidence": float(largest_face['confidence']),
                "quality_score": quality_score,
                "landmarks": largest_face['landmarks']
            }
        }

    def _calculate_quality_score(self, frame: np.ndarray, face: dict) -> float:
        """Calculate quality score for face (0.0-1.0)"""
        try:
            x, y, w, h = face['bbox']
            face_roi = frame[y:y+h, x:x+w]
            
            if face_roi.size == 0:
                return 0.0

            # Blur detection using Laplacian variance
            gray = cv2.cvtColor(face_roi, cv2.COLOR_BGR2GRAY)
            laplacian_var = cv2.Laplacian(gray, cv2.CV_64F).var()
            blur_score = min(laplacian_var / 100.0, 1.0)  # Normalize to 0-1

            # Face size score (prefer larger faces)
            frame_area = frame.shape[0] * frame.shape[1]
            face_area = w * h
            size_score = min(face_area / (frame_area * 0.1), 1.0)  # Prefer 10%+ of frame

            # Confidence score
            conf_score = face['confidence']

            # Combined quality score
            quality = (blur_score * 0.4 + size_score * 0.3 + conf_score * 0.3)
            return float(quality)
            
        except Exception as e:
            logger.error(f"Error calculating quality score: {e}")
            return 0.5  # Default medium quality







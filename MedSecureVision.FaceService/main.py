#!/usr/bin/env python3
"""
MedSecure Vision - Face Service
Main entry point for Python face detection and recognition service
"""

import sys
import logging
from pathlib import Path

# Add current directory to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from ipc_server import IpcServer
from face_detector import FaceDetector
from face_recognizer import FaceRecognizer
import signal

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

class FaceService:
    def __init__(self):
        logger.info("Initializing Face Service...")
        self.face_detector = FaceDetector()
        self.face_recognizer = FaceRecognizer()
        self.ipc_server = IpcServer(
            pipe_name=r'\\.\pipe\MedSecureFaceService',
            face_detector=self.face_detector,
            face_recognizer=self.face_recognizer
        )
        logger.info("Face Service initialized successfully")

    def start(self):
        """Start the face service"""
        logger.info("Starting Face Service...")
        try:
            self.ipc_server.start()
        except KeyboardInterrupt:
            logger.info("Received interrupt signal, shutting down...")
        except Exception as e:
            logger.error(f"Error in face service: {e}", exc_info=True)
        finally:
            self.shutdown()

    def shutdown(self):
        """Cleanup and shutdown"""
        logger.info("Shutting down Face Service...")
        if self.ipc_server:
            self.ipc_server.stop()
        logger.info("Face Service stopped")

def main():
    service = FaceService()
    
    # Handle shutdown signals
    def signal_handler(sig, frame):
        logger.info("Received shutdown signal")
        service.shutdown()
        sys.exit(0)
    
    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)
    
    service.start()

if __name__ == "__main__":
    main()







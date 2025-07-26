"""
Face detection and encoding module for FaceVault.
Uses face_recognition library for detecting and encoding faces in images.
"""

import face_recognition
import cv2
import numpy as np
import pickle
from typing import List, Dict, Any, Tuple, Optional
from pathlib import Path


def detect_faces_in_image(image_path: str) -> Dict[str, Any]:
    """
    Detect all faces in an image using face_recognition library.
    
    Args:
        image_path: Path to the image file
        
    Returns:
        Dictionary containing detection results:
        {
            'success': bool,
            'face_count': int,
            'faces': [{'encoding': bytes, 'box': [x,y,w,h], 'confidence': float}],
            'error': str (if success=False)
        }
    """
    try:
        if not Path(image_path).exists():
            return {
                'success': False,
                'face_count': 0,
                'faces': [],
                'error': f'Image file not found: {image_path}'
            }
        
        # Load the image
        image = face_recognition.load_image_file(image_path)
        
        # Find face locations and encodings
        face_locations = face_recognition.face_locations(image, model='hog')  # hog is faster, cnn is more accurate
        face_encodings = face_recognition.face_encodings(image, face_locations)
        
        faces = []
        for i, (encoding, location) in enumerate(zip(face_encodings, face_locations)):
            # Convert face_recognition format (top, right, bottom, left) to (x, y, w, h)
            top, right, bottom, left = location
            x, y, w, h = left, top, right - left, bottom - top
            
            # Serialize the encoding for storage
            encoding_bytes = pickle.dumps(encoding)
            
            # Calculate confidence (simplified - in real implementation might use more sophisticated method)
            confidence = 0.9  # Placeholder - face_recognition doesn't provide confidence directly
            
            face_data = {
                'encoding': encoding_bytes,
                'box': [x, y, w, h],
                'confidence': confidence,
                'location_index': i
            }
            faces.append(face_data)
        
        return {
            'success': True,
            'face_count': len(faces),
            'faces': faces,
            'error': None
        }
        
    except Exception as e:
        return {
            'success': False,
            'face_count': 0,
            'faces': [],
            'error': str(e)
        }


def encode_face(face_image: np.ndarray) -> bytes:
    """
    Generate face encoding from a face image array.
    
    Args:
        face_image: NumPy array containing the face image
        
    Returns:
        Serialized face encoding as bytes
    """
    if face_image is None or face_image.size == 0:
        raise ValueError("Invalid face image provided")
    
    # Get face encoding
    encodings = face_recognition.face_encodings(face_image)
    
    if len(encodings) == 0:
        raise ValueError("No face found in the provided image")
    
    if len(encodings) > 1:
        print("Warning: Multiple faces found, using the first one")
    
    # Serialize and return the first encoding
    return pickle.dumps(encodings[0])


def compare_faces(known_encodings: List[bytes], unknown_encoding: bytes, tolerance: float = 0.6) -> List[bool]:
    """
    Compare an unknown face encoding against known face encodings.
    
    Args:
        known_encodings: List of serialized known face encodings
        unknown_encoding: Serialized unknown face encoding to compare
        tolerance: How much distance between faces to consider a match (lower = more strict)
        
    Returns:
        List of boolean values indicating matches
    """
    if not known_encodings:
        return []
    
    # Deserialize encodings
    known_face_encodings = [pickle.loads(encoding) for encoding in known_encodings]
    unknown_face_encoding = pickle.loads(unknown_encoding)
    
    # Compare faces
    matches = face_recognition.compare_faces(known_face_encodings, unknown_face_encoding, tolerance=tolerance)
    
    return matches


def face_distance(face_encoding1: bytes, face_encoding2: bytes) -> float:
    """
    Calculate the distance between two face encodings.
    
    Args:
        face_encoding1: First serialized face encoding
        face_encoding2: Second serialized face encoding
        
    Returns:
        Distance between the faces (lower = more similar)
    """
    encoding1 = pickle.loads(face_encoding1)
    encoding2 = pickle.loads(face_encoding2)
    
    distances = face_recognition.face_distance([encoding1], encoding2)
    return distances[0]


def process_image_batch(image_paths: List[str], max_workers: int = 4) -> List[Dict[str, Any]]:
    """
    Process multiple images for face detection with parallel processing.
    
    Args:
        image_paths: List of image file paths
        max_workers: Maximum number of worker threads
        
    Returns:
        List of detection results for each image
    """
    import concurrent.futures
    import time
    
    results = []
    start_time = time.time()
    
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        # Submit all tasks
        future_to_path = {
            executor.submit(detect_faces_in_image, path): path 
            for path in image_paths
        }
        
        # Collect results as they complete
        for future in concurrent.futures.as_completed(future_to_path):
            image_path = future_to_path[future]
            try:
                result = future.result()
                result['image_path'] = image_path
                results.append(result)
            except Exception as e:
                error_result = {
                    'success': False,
                    'face_count': 0,
                    'faces': [],
                    'error': f"Processing error: {str(e)}",
                    'image_path': image_path
                }
                results.append(error_result)
    
    processing_time = time.time() - start_time
    print(f"Processed {len(image_paths)} images in {processing_time:.2f} seconds")
    
    return results


def extract_face_from_image(image_path: str, face_location: Tuple[int, int, int, int]) -> Optional[np.ndarray]:
    """
    Extract a face region from an image given face coordinates.
    
    Args:
        image_path: Path to the image
        face_location: Tuple of (x, y, w, h) coordinates
        
    Returns:
        NumPy array of the extracted face image, or None if extraction fails
    """
    try:
        image = cv2.imread(image_path)
        if image is None:
            raise ValueError(f"Could not load image: {image_path}")
        
        x, y, w, h = face_location
        
        # Validate coordinates
        height, width = image.shape[:2]
        if x < 0 or y < 0 or x + w > width or y + h > height:
            raise ValueError("Face coordinates are outside image boundaries")
        
        # Extract face region
        face_image = image[y:y+h, x:x+w]
        
        # Convert from BGR to RGB (face_recognition expects RGB)
        face_image_rgb = cv2.cvtColor(face_image, cv2.COLOR_BGR2RGB)
        
        return face_image_rgb
        
    except Exception as e:
        print(f"Error extracting face from {image_path}: {e}")
        return None


def validate_face_encoding(encoding_bytes: bytes) -> bool:
    """
    Validate that a serialized face encoding is valid.
    
    Args:
        encoding_bytes: Serialized face encoding
        
    Returns:
        True if valid, False otherwise
    """
    try:
        encoding = pickle.loads(encoding_bytes)
        
        # Face encodings should be 128-dimensional vectors
        return isinstance(encoding, np.ndarray) and encoding.shape == (128,)
        
    except (pickle.UnpicklingError, ValueError, TypeError):
        return False
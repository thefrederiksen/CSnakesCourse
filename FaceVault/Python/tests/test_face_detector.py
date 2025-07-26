"""
Tests for face_detector module.
"""

import pytest
import numpy as np
import tempfile
import os
from unittest.mock import patch, MagicMock
from PIL import Image
import pickle

from ..face_detector import (
    detect_faces_in_image,
    encode_face,
    compare_faces,
    face_distance,
    process_image_batch,
    extract_face_from_image,
    validate_face_encoding
)


class TestFaceDetector:
    
    def setup_method(self):
        """Set up test fixtures."""
        self.temp_dir = tempfile.mkdtemp()
        
        # Create a mock face encoding (128-dimensional vector)
        self.mock_encoding = np.random.rand(128)
        self.mock_encoding_bytes = pickle.dumps(self.mock_encoding)
    
    def create_test_image(self, width=200, height=200, filename="test.jpg"):
        """Create a test image file."""
        image_path = os.path.join(self.temp_dir, filename)
        
        # Create a simple test image
        image = Image.new('RGB', (width, height), color='white')
        image.save(image_path)
        
        return image_path
    
    @patch('face_recognition.load_image_file')
    @patch('face_recognition.face_locations')
    @patch('face_recognition.face_encodings')
    def test_detect_faces_success(self, mock_encodings, mock_locations, mock_load):
        """Test successful face detection."""
        # Setup mocks
        mock_image = np.zeros((200, 200, 3), dtype=np.uint8)
        mock_load.return_value = mock_image
        mock_locations.return_value = [(50, 150, 150, 50)]  # top, right, bottom, left
        mock_encodings.return_value = [self.mock_encoding]
        
        image_path = self.create_test_image()
        
        result = detect_faces_in_image(image_path)
        
        assert result['success'] is True
        assert result['face_count'] == 1
        assert len(result['faces']) == 1
        assert result['error'] is None
        
        face = result['faces'][0]
        assert 'encoding' in face
        assert 'box' in face
        assert 'confidence' in face
        assert face['box'] == [50, 50, 100, 100]  # x, y, w, h format
    
    def test_detect_faces_nonexistent_file(self):
        """Test face detection with nonexistent file."""
        result = detect_faces_in_image("/nonexistent/image.jpg")
        
        assert result['success'] is False
        assert result['face_count'] == 0
        assert len(result['faces']) == 0
        assert 'not found' in result['error'].lower()
    
    @patch('face_recognition.load_image_file')
    def test_detect_faces_processing_error(self, mock_load):
        """Test face detection with processing error."""
        mock_load.side_effect = Exception("Processing error")
        
        image_path = self.create_test_image()
        result = detect_faces_in_image(image_path)
        
        assert result['success'] is False
        assert result['face_count'] == 0
        assert 'Processing error' in result['error']
    
    @patch('face_recognition.face_encodings')
    def test_encode_face_success(self, mock_encodings):
        """Test successful face encoding."""
        mock_encodings.return_value = [self.mock_encoding]
        
        # Create a mock face image
        face_image = np.random.randint(0, 255, (100, 100, 3), dtype=np.uint8)
        
        encoding_bytes = encode_face(face_image)
        
        assert isinstance(encoding_bytes, bytes)
        
        # Verify we can deserialize it
        decoded_encoding = pickle.loads(encoding_bytes)
        np.testing.assert_array_equal(decoded_encoding, self.mock_encoding)
    
    def test_encode_face_invalid_input(self):
        """Test face encoding with invalid input."""
        with pytest.raises(ValueError, match="Invalid face image"):
            encode_face(None)
        
        with pytest.raises(ValueError, match="Invalid face image"):
            encode_face(np.array([]))
    
    @patch('face_recognition.face_encodings')
    def test_encode_face_no_face_found(self, mock_encodings):
        """Test face encoding when no face is found."""
        mock_encodings.return_value = []
        
        face_image = np.random.randint(0, 255, (100, 100, 3), dtype=np.uint8)
        
        with pytest.raises(ValueError, match="No face found"):
            encode_face(face_image)
    
    @patch('face_recognition.compare_faces')
    def test_compare_faces_success(self, mock_compare):
        """Test successful face comparison."""
        mock_compare.return_value = [True, False]
        
        known_encodings = [self.mock_encoding_bytes, self.mock_encoding_bytes]
        unknown_encoding = self.mock_encoding_bytes
        
        matches = compare_faces(known_encodings, unknown_encoding)
        
        assert matches == [True, False]
        mock_compare.assert_called_once()
    
    def test_compare_faces_empty_known(self):
        """Test face comparison with empty known encodings."""
        matches = compare_faces([], self.mock_encoding_bytes)
        
        assert matches == []
    
    @patch('face_recognition.face_distance')
    def test_face_distance_calculation(self, mock_distance):
        """Test face distance calculation."""
        mock_distance.return_value = [0.3]
        
        distance = face_distance(self.mock_encoding_bytes, self.mock_encoding_bytes)
        
        assert distance == 0.3
        mock_distance.assert_called_once()
    
    @patch('face_detector.detect_faces_in_image')
    def test_process_image_batch_success(self, mock_detect):
        """Test batch processing of images."""
        # Mock successful detection for all images
        mock_detect.return_value = {
            'success': True,
            'face_count': 1,
            'faces': [{'encoding': self.mock_encoding_bytes, 'box': [0, 0, 50, 50]}],
            'error': None
        }
        
        image_paths = [
            self.create_test_image(filename="batch1.jpg"),
            self.create_test_image(filename="batch2.jpg")
        ]
        
        results = process_image_batch(image_paths)
        
        assert len(results) == 2
        assert all(result['success'] for result in results)
        assert all('image_path' in result for result in results)
    
    @patch('cv2.imread')
    @patch('cv2.cvtColor')
    def test_extract_face_from_image_success(self, mock_cvtcolor, mock_imread):
        """Test successful face extraction."""
        # Mock image data
        mock_image = np.random.randint(0, 255, (200, 200, 3), dtype=np.uint8)
        mock_imread.return_value = mock_image
        mock_cvtcolor.return_value = mock_image
        
        image_path = self.create_test_image()
        face_location = (50, 50, 100, 100)  # x, y, w, h
        
        face_image = extract_face_from_image(image_path, face_location)
        
        assert face_image is not None
        assert isinstance(face_image, np.ndarray)
        mock_cvtcolor.assert_called_once()
    
    @patch('cv2.imread')
    def test_extract_face_invalid_image(self, mock_imread):
        """Test face extraction with invalid image."""
        mock_imread.return_value = None
        
        image_path = "/nonexistent/image.jpg"
        face_location = (50, 50, 100, 100)
        
        face_image = extract_face_from_image(image_path, face_location)
        
        assert face_image is None
    
    def test_extract_face_invalid_coordinates(self):
        """Test face extraction with invalid coordinates."""
        image_path = self.create_test_image()
        face_location = (-10, -10, 100, 100)  # Invalid coordinates
        
        face_image = extract_face_from_image(image_path, face_location)
        
        assert face_image is None
    
    def test_validate_face_encoding_valid(self):
        """Test validation of valid face encoding."""
        assert validate_face_encoding(self.mock_encoding_bytes) is True
    
    def test_validate_face_encoding_invalid_data(self):
        """Test validation of invalid face encoding data."""
        invalid_data = b"invalid pickle data"
        
        assert validate_face_encoding(invalid_data) is False
    
    def test_validate_face_encoding_wrong_shape(self):
        """Test validation of face encoding with wrong shape."""
        wrong_shape_array = np.random.rand(64)  # Should be 128-dimensional
        wrong_shape_bytes = pickle.dumps(wrong_shape_array)
        
        assert validate_face_encoding(wrong_shape_bytes) is False


class TestFaceDetectorIntegration:
    """Integration tests for face detector functionality."""
    
    def setup_method(self):
        """Set up test fixtures."""
        self.mock_encoding = np.random.rand(128)
        self.mock_encoding_bytes = pickle.dumps(self.mock_encoding)
    
    @patch('face_recognition.load_image_file')
    @patch('face_recognition.face_locations') 
    @patch('face_recognition.face_encodings')
    def test_end_to_end_face_detection_workflow(self, mock_encodings, mock_locations, mock_load):
        """Test complete face detection workflow."""
        # Setup mocks for a successful detection
        mock_image = np.zeros((200, 200, 3), dtype=np.uint8)
        mock_load.return_value = mock_image
        mock_locations.return_value = [(50, 150, 150, 50), (75, 175, 175, 75)]  # Two faces
        mock_encodings.return_value = [self.mock_encoding, self.mock_encoding * 0.8]
        
        with tempfile.NamedTemporaryFile(suffix='.jpg', delete=False) as tmp_file:
            # Create a test image
            image = Image.new('RGB', (200, 200), color='white')
            image.save(tmp_file.name)
            
            try:
                # Detect faces
                result = detect_faces_in_image(tmp_file.name)
                
                # Verify detection results
                assert result['success'] is True
                assert result['face_count'] == 2
                assert len(result['faces']) == 2
                
                # Test face comparison
                face1_encoding = result['faces'][0]['encoding']
                face2_encoding = result['faces'][1]['encoding']
                
                # Validate encodings
                assert validate_face_encoding(face1_encoding)
                assert validate_face_encoding(face2_encoding)
                
                # Test distance calculation
                distance = face_distance(face1_encoding, face2_encoding)
                assert isinstance(distance, float)
                assert distance >= 0
                
            finally:
                os.unlink(tmp_file.name)
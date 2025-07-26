"""
Tests for image_scanner module.
"""

import pytest
import tempfile
import os
from pathlib import Path
from unittest.mock import patch, mock_open
from PIL import Image
import hashlib
from datetime import datetime

from ..image_scanner import (
    scan_directory,
    calculate_file_hash,
    extract_image_metadata,
    get_image_info_batch,
    _extract_date_taken,
    _convert_gps_coordinate
)


class TestImageScanner:
    
    def setup_method(self):
        """Set up test fixtures."""
        self.temp_dir = tempfile.mkdtemp()
        self.test_image_path = None
        
    def teardown_method(self):
        """Clean up test fixtures."""
        if self.test_image_path and os.path.exists(self.test_image_path):
            os.remove(self.test_image_path)
    
    def create_test_image(self, width=100, height=100, filename="test.jpg"):
        """Create a test image file."""
        image_path = os.path.join(self.temp_dir, filename)
        
        # Create a simple test image
        image = Image.new('RGB', (width, height), color='red')
        image.save(image_path)
        
        self.test_image_path = image_path
        return image_path
    
    def test_scan_directory_valid_path(self):
        """Test scanning a valid directory."""
        # Create test images
        self.create_test_image(filename="test1.jpg")
        self.create_test_image(filename="test2.png")
        
        results = scan_directory(self.temp_dir)
        
        assert len(results) == 2
        assert all('file_path' in result for result in results)
        assert any('test1.jpg' in result['file_path'] for result in results)
        assert any('test2.png' in result['file_path'] for result in results)
    
    def test_scan_directory_invalid_path(self):
        """Test scanning an invalid directory."""
        with pytest.raises(ValueError, match="Directory does not exist"):
            scan_directory("/nonexistent/path")
    
    def test_scan_directory_with_extensions_filter(self):
        """Test scanning with specific extensions."""
        self.create_test_image(filename="test.jpg")
        self.create_test_image(filename="test.png")
        
        # Only scan for JPG files
        results = scan_directory(self.temp_dir, ['.jpg'])
        
        assert len(results) == 1
        assert 'test.jpg' in results[0]['file_path']
    
    def test_calculate_file_hash_valid_file(self):
        """Test calculating hash of a valid file."""
        test_content = b"test content for hashing"
        
        with tempfile.NamedTemporaryFile(delete=False) as tmp_file:
            tmp_file.write(test_content)
            tmp_file.flush()
            
            hash_result = calculate_file_hash(tmp_file.name)
            
            # Calculate expected hash
            expected_hash = hashlib.sha256(test_content).hexdigest()
            
            assert hash_result == expected_hash
            
        os.unlink(tmp_file.name)
    
    def test_calculate_file_hash_nonexistent_file(self):
        """Test calculating hash of nonexistent file."""
        with pytest.raises(FileNotFoundError):
            calculate_file_hash("/nonexistent/file.txt")
    
    def test_extract_image_metadata_valid_image(self):
        """Test extracting metadata from a valid image."""
        image_path = self.create_test_image()
        
        metadata = extract_image_metadata(image_path)
        
        assert metadata['file_path'] == image_path
        assert metadata['image_width'] == 100
        assert metadata['image_height'] == 100
        assert isinstance(metadata['file_size'], int)
        assert isinstance(metadata['file_hash'], str)
        assert isinstance(metadata['scan_date'], datetime)
    
    def test_extract_image_metadata_nonexistent_file(self):
        """Test extracting metadata from nonexistent file."""
        with pytest.raises(FileNotFoundError):
            extract_image_metadata("/nonexistent/image.jpg")
    
    def test_get_image_info_batch(self):
        """Test batch processing of images."""
        image1 = self.create_test_image(filename="batch1.jpg")
        image2 = self.create_test_image(filename="batch2.jpg")
        
        results = get_image_info_batch([image1, image2])
        
        assert len(results) == 2
        assert all('file_path' in result for result in results)
        assert not any('error' in result for result in results)
    
    def test_get_image_info_batch_with_errors(self):
        """Test batch processing with some invalid files."""
        valid_image = self.create_test_image()
        invalid_image = "/nonexistent/image.jpg"
        
        results = get_image_info_batch([valid_image, invalid_image])
        
        assert len(results) == 2
        assert 'error' not in results[0]  # Valid image
        assert 'error' in results[1]      # Invalid image
    
    def test_extract_date_taken_valid_exif(self):
        """Test extracting date from valid EXIF data."""
        exif_dict = {
            'DateTime': '2023:01:15 14:30:00'
        }
        
        date_taken = _extract_date_taken(exif_dict)
        
        assert date_taken is not None
        assert date_taken.year == 2023
        assert date_taken.month == 1
        assert date_taken.day == 15
    
    def test_extract_date_taken_invalid_exif(self):
        """Test extracting date from invalid EXIF data."""
        exif_dict = {
            'DateTime': 'invalid_date_format'
        }
        
        date_taken = _extract_date_taken(exif_dict)
        
        assert date_taken is None
    
    def test_convert_gps_coordinate_north(self):
        """Test GPS coordinate conversion for northern hemisphere."""
        coordinate = (40.0, 45.0, 30.0)  # 40°45'30"N
        reference = 'N'
        
        decimal = _convert_gps_coordinate(coordinate, reference)
        
        expected = 40.0 + 45.0/60 + 30.0/3600
        assert abs(decimal - expected) < 0.0001
    
    def test_convert_gps_coordinate_south(self):
        """Test GPS coordinate conversion for southern hemisphere."""
        coordinate = (40.0, 45.0, 30.0)  # 40°45'30"S
        reference = 'S'
        
        decimal = _convert_gps_coordinate(coordinate, reference)
        
        expected = -(40.0 + 45.0/60 + 30.0/3600)
        assert abs(decimal - expected) < 0.0001


class TestImageScannerIntegration:
    """Integration tests for image scanner functionality."""
    
    def test_full_workflow(self):
        """Test the complete image scanning workflow."""
        with tempfile.TemporaryDirectory() as temp_dir:
            # Create subdirectory structure
            subdir = os.path.join(temp_dir, "subdir")
            os.makedirs(subdir)
            
            # Create test images in different locations
            image1 = os.path.join(temp_dir, "image1.jpg")
            image2 = os.path.join(subdir, "image2.png")
            
            for path in [image1, image2]:
                image = Image.new('RGB', (50, 75), color='blue')
                image.save(path)
            
            # Scan the directory
            results = scan_directory(temp_dir)
            
            # Verify results
            assert len(results) == 2
            
            # Check that both images were found
            found_paths = [result['file_path'] for result in results]
            assert any('image1.jpg' in path for path in found_paths)
            assert any('image2.png' in path for path in found_paths)
            
            # Verify metadata completeness
            for result in results:
                assert 'file_hash' in result
                assert 'image_width' in result
                assert 'image_height' in result
                assert result['image_width'] == 50
                assert result['image_height'] == 75
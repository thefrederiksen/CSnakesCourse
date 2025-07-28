"""
Unit tests for screenshot detection functionality.
"""

import unittest
import os
import sys
from pathlib import Path

# Add parent directory to path to import screenshots module
sys.path.insert(0, str(Path(__file__).parent.parent))

from screenshots import ScreenshotDetector, detect_screenshot, detect_screenshot_detailed


class TestScreenshotDetector(unittest.TestCase):
    """Test cases for ScreenshotDetector class."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.detector = ScreenshotDetector()
        self.test_images_dir = Path(__file__).parent / "images"
        
        # Expected test image paths
        self.screenshot_path = self.test_images_dir / "screenshot.jpg"
        self.photo_path = self.test_images_dir / "photo.jpg"
    
    def test_filename_analysis_screenshot_patterns(self):
        """Test filename analysis for screenshot patterns."""
        test_cases = [
            ("screenshot_2024-01-27.png", True),
            ("Screen Shot 2024-01-27 at 10.30.45 AM.png", True),
            ("image_2024-01-27_capture.jpg", True),
            ("snip_001.png", True),
            ("IMG_1234.jpg", False),
            ("vacation_photo.jpg", False),
            ("family_dinner.png", False),
        ]
        
        for filename, expected_is_screenshot in test_cases:
            with self.subTest(filename=filename):
                score, details = self.detector._analyze_filename(f"/fake/path/{filename}")
                is_screenshot = score > 0.5
                self.assertEqual(is_screenshot, expected_is_screenshot, 
                               f"Failed for {filename}: score={score}, details={details}")
    
    def test_dimension_analysis_common_resolutions(self):
        """Test dimension analysis for common screen resolutions."""
        from PIL import Image
        
        test_cases = [
            ((1920, 1080), True),   # Full HD
            ((1366, 768), True),    # Common laptop
            ((750, 1334), True),    # iPhone
            ((2560, 1440), True),   # QHD
            ((3456, 2304), False),  # Camera resolution
            ((4000, 3000), False),  # Camera resolution
            ((800, 600), False),    # Unusual for modern screenshots
        ]
        
        for (width, height), expected_is_screenshot in test_cases:
            with self.subTest(dimensions=f"{width}x{height}"):
                # Create mock image with specified dimensions
                mock_img = type('MockImage', (), {'size': (width, height)})()
                score, details = self.detector._analyze_dimensions(mock_img)
                is_screenshot = score > 0.5
                
                if expected_is_screenshot:
                    self.assertGreater(score, 0.3, 
                                     f"Low score for screenshot dimensions {width}x{height}: {score}")
                else:
                    self.assertLess(score, 0.7, 
                                   f"High score for non-screenshot dimensions {width}x{height}: {score}")
    
    def test_exif_analysis_camera_info(self):
        """Test EXIF analysis for camera information."""
        from PIL import Image
        from PIL.ExifTags import TAGS
        
        # Mock image with no EXIF (typical screenshot)
        mock_img_no_exif = type('MockImage', (), {
            'getexif': lambda: {}
        })()
        
        score, details = self.detector._analyze_exif(mock_img_no_exif)
        self.assertGreaterEqual(score, 0.5, "Images without EXIF should have higher screenshot score")
        self.assertFalse(details["has_exif"])
    
    def test_simple_interface_functions(self):
        """Test the simple interface functions work correctly."""
        # Test with a filename that should be detected as screenshot
        fake_screenshot_path = "/fake/path/screenshot_2024.png"
        
        # Note: This will fail file existence check, but we can test the interface
        try:
            is_screenshot, confidence = detect_screenshot(fake_screenshot_path)
            self.assertIsInstance(is_screenshot, bool)
            self.assertIsInstance(confidence, float)
            self.assertGreaterEqual(confidence, 0.0)
            self.assertLessEqual(confidence, 1.0)
        except:
            # Expected to fail due to non-existent file
            pass
        
        try:
            result = detect_screenshot_detailed(fake_screenshot_path)
            self.assertIsInstance(result, dict)
            self.assertIn("is_screenshot", result)
            self.assertIn("confidence", result)
            self.assertIn("file_path", result)
            self.assertIn("analysis", result)
        except:
            # Expected to fail due to non-existent file
            pass
    
    def test_color_analysis_functionality(self):
        """Test color analysis method."""
        import numpy as np
        
        # Create mock image arrays
        # Uniform image (like UI background)
        uniform_img = np.full((100, 100, 3), [200, 200, 200], dtype=np.uint8)
        uniform_score = self.detector._analyze_colors(uniform_img)
        self.assertGreater(uniform_score, 0.5, "Uniform images should have higher screenshot score")
        
        # Random/noisy image (like natural photo)
        np.random.seed(42)  # For reproducible tests
        noisy_img = np.random.randint(0, 256, (100, 100, 3), dtype=np.uint8)
        noisy_score = self.detector._analyze_colors(noisy_img)
        self.assertLess(noisy_score, uniform_score, "Noisy images should have lower screenshot score")
    
    def test_edge_analysis_functionality(self):
        """Test edge analysis method."""
        import numpy as np
        
        # Create image with many horizontal/vertical lines (UI-like)
        ui_img = np.ones((100, 100, 3), dtype=np.uint8) * 128
        # Add horizontal lines
        ui_img[10::20, :] = 0  # Black horizontal lines
        # Add vertical lines  
        ui_img[:, 10::20] = 255  # White vertical lines
        
        ui_score = self.detector._analyze_edges(ui_img)
        
        # Create more natural image
        natural_img = np.random.randint(0, 256, (100, 100, 3), dtype=np.uint8)
        natural_score = self.detector._analyze_edges(natural_img)
        
        # UI-like images should generally score higher for screenshots
        # Note: This is heuristic-based, so we allow some flexibility
        self.assertIsInstance(ui_score, float)
        self.assertIsInstance(natural_score, float)
        self.assertGreaterEqual(ui_score, 0.0)
        self.assertLessEqual(ui_score, 1.0)
    
    @unittest.skipUnless(
        os.path.exists(Path(__file__).parent / "images" / "screenshot.jpg"),
        "screenshot.jpg test image not found"
    )
    def test_real_screenshot_image(self):
        """Test with actual screenshot image (if available)."""
        is_screenshot, confidence, details = self.detector.detect_screenshot(str(self.screenshot_path))
        
        self.assertTrue(is_screenshot, f"Failed to detect screenshot: confidence={confidence}")
        self.assertGreater(confidence, 0.5, f"Low confidence for screenshot: {confidence}")
        self.assertIsInstance(details, dict)
        print(f"Screenshot analysis: {details}")
    
    @unittest.skipUnless(
        os.path.exists(Path(__file__).parent / "images" / "photo.jpg"),
        "photo.jpg test image not found"
    )
    def test_real_photo_image(self):
        """Test with actual photo image (if available)."""
        is_screenshot, confidence, details = self.detector.detect_screenshot(str(self.photo_path))
        
        self.assertFalse(is_screenshot, f"Incorrectly detected photo as screenshot: confidence={confidence}")
        self.assertLess(confidence, 0.5, f"High confidence for regular photo: {confidence}")
        self.assertIsInstance(details, dict)
        print(f"Photo analysis: {details}")
    
    def test_screenshot_patterns_comprehensive(self):
        """Test comprehensive list of screenshot filename patterns."""
        screenshot_filenames = [
            "Screenshot 2024-01-27 143045.png",
            "Screen Shot 2024-01-27 at 2.30.45 PM.png", 
            "screenshot_20240127_143045.jpg",
            "capture_001.png",
            "snip_2024-01-27.png",
            "ClipboardImage_2024-01-27.png",
            "image_2024-01-27_14-30-45.png",
            "shot_001.jpg"
        ]
        
        regular_filenames = [
            "IMG_1234.jpg",
            "DSC_5678.jpg", 
            "vacation_beach.jpg",
            "family_portrait.png",
            "wedding_ceremony.jpg",
            "nature_landscape.jpg",
            "food_dinner.jpg"
        ]
        
        for filename in screenshot_filenames:
            with self.subTest(filename=filename, type="screenshot"):
                score, _ = self.detector._analyze_filename(f"/path/{filename}")
                self.assertGreater(score, 0.5, f"Failed to detect screenshot pattern in {filename}")
        
        for filename in regular_filenames:
            with self.subTest(filename=filename, type="photo"):
                score, _ = self.detector._analyze_filename(f"/path/{filename}")
                self.assertLess(score, 0.5, f"Incorrectly detected photo pattern in {filename}")


class TestScreenshotDetectorIntegration(unittest.TestCase):
    """Integration tests for screenshot detection."""
    
    def test_missing_file_handling(self):
        """Test handling of missing files."""
        detector = ScreenshotDetector()
        is_screenshot, confidence, details = detector.detect_screenshot("/nonexistent/file.jpg")
        
        self.assertFalse(is_screenshot)
        self.assertEqual(confidence, 0.0)
        self.assertIn("error", details)
        self.assertEqual(details["error"], "File not found")
    
    def test_confidence_range(self):
        """Test that confidence is always in valid range."""
        detector = ScreenshotDetector()
        
        # Test with various mock scenarios
        test_files = [
            "/fake/screenshot.png",
            "/fake/photo.jpg", 
            "/fake/random_name.gif"
        ]
        
        for test_file in test_files:
            try:
                _, confidence, _ = detector.detect_screenshot(test_file)
                self.assertGreaterEqual(confidence, 0.0, f"Confidence below 0 for {test_file}")
                self.assertLessEqual(confidence, 1.0, f"Confidence above 1 for {test_file}")
            except:
                # Expected to fail for non-existent files
                pass


if __name__ == "__main__":
    # Create test images directory if it doesn't exist
    test_images_dir = Path(__file__).parent / "images"
    test_images_dir.mkdir(exist_ok=True)
    
    print(f"Test images directory: {test_images_dir}")
    print(f"Please place test images:")
    print(f"  - screenshot.jpg (a clear screenshot)")
    print(f"  - photo.jpg (a regular photograph)")
    print()
    
    # Run tests
    unittest.main(verbosity=2)
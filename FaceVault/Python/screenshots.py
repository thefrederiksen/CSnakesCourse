"""
Screenshot detection module for FaceVault.

This module provides methods to detect whether an image is a screenshot
based on filename patterns and metadata markers.
"""

import os
from pathlib import Path
from typing import Tuple, Dict, Any

# Try to import PIL for metadata checking
try:
    from PIL import Image, ExifTags
    HAS_PIL = True
except ImportError:
    HAS_PIL = False


# Module-level function for CSnakes integration
def detect_screenshot(file_path: str) -> Tuple[bool, float, Dict[str, Any]]:
    """
    Module-level function to detect screenshots.
    Returns: (is_screenshot, confidence, analysis_details)
    """
    detector = ScreenshotDetector()
    return detector.detect_screenshot(file_path)


def test_detection() -> str:
    """Test function to verify the module works."""
    result = detect_screenshot("screenshot_2024.png")
    return f"Test result: {result[0]}, confidence: {result[1]}"

def check_libraries() -> Dict[str, Any]:
    """Check if required libraries are available."""
    return {
        "has_pil": HAS_PIL,
        "pil_available": "PIL" in globals() or "Image" in globals()
    }


class ScreenshotDetector:
    """Detects screenshots using filename and metadata checks."""
    
    def __init__(self):
        pass
    
    def detect_screenshot(self, file_path: str) -> Tuple[bool, float, Dict[str, Any]]:
        """
        Detect if an image is a screenshot.
        
        Args:
            file_path (str): Path to the image file
            
        Returns:
            Tuple[bool, float, Dict[str, Any]]: (is_screenshot, confidence, analysis_details)
        """
        analysis = {
            'filename_check': False,
            'metadata_check': False,
            'method': None
        }
        
        # Phase 1: Check filename
        filename = os.path.basename(file_path).lower()
        if 'screenshot' in filename:
            analysis['filename_check'] = True
            analysis['method'] = 'filename'
            return True, 1.0, analysis
        
        # Phase 2: Check metadata (if PIL available)
        if HAS_PIL and os.path.exists(file_path):
            has_screenshot_metadata = self._check_metadata(file_path)
            if has_screenshot_metadata:
                analysis['metadata_check'] = True
                analysis['method'] = 'metadata'
                return True, 1.0, analysis
        
        # No screenshot indicators found
        analysis['method'] = 'none'
        return False, 0.0, analysis
    
    def _check_metadata(self, file_path: str) -> bool:
        """Check if metadata contains screenshot markers."""
        try:
            ext = Path(file_path).suffix.lower()
            
            # For JPEG, TIFF, WebP - check EXIF
            if ext in ['.jpg', '.jpeg', '.tiff', '.tif', '.webp']:
                with Image.open(file_path) as img:
                    exif_data = img.getexif()
                    if exif_data:
                        for tag_id, value in exif_data.items():
                            tag_name = ExifTags.TAGS.get(tag_id, str(tag_id))
                            if value and isinstance(value, str):
                                if 'screenshot' in value.lower():
                                    return True
                            # Check iOS UserComment specifically
                            if tag_name == 'UserComment' and value:
                                value_str = str(value).lower()
                                if 'screenshot' in value_str:
                                    return True
            
            # For PNG - check text chunks
            elif ext == '.png':
                with Image.open(file_path) as img:
                    if hasattr(img, 'text'):
                        for key, value in img.text.items():
                            if 'screenshot' in str(value).lower():
                                return True
                    # Also check info dictionary
                    if hasattr(img, 'info'):
                        for key, value in img.info.items():
                            if isinstance(value, str) and 'screenshot' in value.lower():
                                return True
            
            # For HEIC/HEIF - would need special library (not implemented)
            # elif ext in ['.heic', '.heif']:
            #     # Requires pyheif or pillow-heif
            #     pass
            
        except Exception:
            # If any error occurs, just return False
            pass
        
        return False
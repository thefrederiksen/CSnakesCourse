"""
Screenshot detection module for FaceVault.

This module provides methods to detect whether an image is likely a screenshot
based on various heuristics including filename patterns, metadata analysis,
dimensions, and image content analysis.
"""

import os
import re
from pathlib import Path
from typing import Tuple, Dict, Any

# Try to import optional dependencies for image analysis
try:
    from PIL import Image, ExifTags
    import numpy as np
    HAS_IMAGE_LIBS = True
except ImportError:
    HAS_IMAGE_LIBS = False


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
        "has_image_libs": HAS_IMAGE_LIBS,
        "pil_available": "PIL" in globals() or "Image" in globals(),
        "numpy_available": "np" in globals() or "numpy" in globals()
    }


class ScreenshotDetector:
    """Detects screenshots using multiple analysis methods."""
    
    # Common screenshot filename patterns
    SCREENSHOT_PATTERNS = [
        r'screenshot',
        r'screen\s*shot',
        r'screen\s*capture',
        r'capture',
        r'snip',
        r'clip',
        r'image_\d{4}-\d{2}-\d{2}',  # Generic timestamp
        r'img_\d{8}',  # Phone screenshots
        r'shot_\d+',
        r'screen_\d+',
    ]
    
    # Common screen resolutions (width x height)
    COMMON_SCREEN_RESOLUTIONS = {
        (1920, 1080), (1366, 768), (1440, 900), (1536, 864),
        (1600, 900), (1280, 720), (1280, 800), (1680, 1050),
        (2560, 1440), (3840, 2160), (2560, 1600), (1920, 1200),
        (1024, 768), (1152, 864), (1400, 1050), (1600, 1200),
        # Mobile screenshot resolutions
        (1080, 1920), (750, 1334), (828, 1792), (1242, 2208),
        (1125, 2436), (1170, 2532), (768, 1024), (1536, 2048),
    }
    
    def __init__(self):
        self.compiled_patterns = [re.compile(pattern, re.IGNORECASE) 
                                for pattern in self.SCREENSHOT_PATTERNS]
    
    def detect_screenshot(self, file_path: str) -> Tuple[bool, float, Dict[str, Any]]:
        """
        Detect if an image is likely a screenshot.
        
        Args:
            file_path (str): Path to the image file
            
        Returns:
            Tuple[bool, float, Dict[str, Any]]: (is_screenshot, confidence, analysis_details)
        """
        try:
            # Initialize analysis results
            analysis = {
                'filename': {},
                'exif': {},
                'dimensions': {},
                'content': {},
                'total_score': 0.0,
                'max_score': 0.0,
                'confidence': 0.0,
                'error': None
            }
            
            total_score = 0.0
            max_possible_score = 0.0
            
            # 1. Filename analysis (weight: 0.3)
            filename_score, filename_details = self._analyze_filename(file_path)
            analysis['filename'] = filename_details
            total_score += filename_score * 0.3
            max_possible_score += 0.3
            
            # 2. EXIF metadata analysis (weight: 0.2) - only if PIL is available
            if HAS_IMAGE_LIBS and os.path.exists(file_path):
                exif_score, exif_details = self._analyze_exif(file_path)
                analysis['exif'] = exif_details
                total_score += exif_score * 0.2
                max_possible_score += 0.2
                
                # 3. Dimension analysis (weight: 0.2)
                dim_score, dim_details = self._analyze_dimensions(file_path)
                analysis['dimensions'] = dim_details
                total_score += dim_score * 0.2
                max_possible_score += 0.2
                
                # 4. Basic image content analysis (weight: 0.3)
                content_score, content_details = self._analyze_content(file_path)
                analysis['content'] = content_details
                total_score += content_score * 0.3
                max_possible_score += 0.3
            else:
                # If no image libraries or file doesn't exist, use only filename
                max_possible_score = 0.3
                analysis['error'] = "Image analysis libraries not available or file not found"
            
            # Calculate final confidence
            confidence = total_score / max_possible_score if max_possible_score > 0 else 0.0
            is_screenshot = confidence > 0.5
            
            analysis['total_score'] = total_score
            analysis['max_score'] = max_possible_score
            analysis['confidence'] = confidence
            
            return is_screenshot, confidence, analysis
            
        except Exception as e:
            # Fallback to simple filename check if anything fails
            filename = os.path.basename(file_path).lower()
            is_screenshot = any(pattern in filename for pattern in ['screenshot', 'capture', 'snip'])
            return is_screenshot, 0.5 if is_screenshot else 0.1, {
                'error': f"Analysis failed: {str(e)}",
                'fallback_result': is_screenshot,
                'method': 'filename_fallback'
            }
    
    def _analyze_filename(self, file_path: str) -> Tuple[float, Dict[str, Any]]:
        """Analyze filename for screenshot indicators."""
        filename = os.path.basename(file_path).lower()
        matches = []
        
        for pattern in self.compiled_patterns:
            if pattern.search(filename):
                matches.append(pattern.pattern)
        
        # Score based on number and specificity of matches
        score = min(len(matches) * 0.4, 1.0)
        
        return score, {
            'filename': filename,
            'matches': matches,
            'score': score
        }
    
    def _analyze_exif(self, file_path: str) -> Tuple[float, Dict[str, Any]]:
        """Analyze EXIF metadata for screenshot indicators."""
        if not HAS_IMAGE_LIBS:
            return 0.0, {'error': 'PIL not available'}
            
        try:
            with Image.open(file_path) as img:
                exif_data = img.getexif()
                
                # Look for camera-related EXIF data
                has_camera_make = False
                has_camera_model = False
                has_software = False
                camera_make = None
                camera_model = None
                software = None
                
                if exif_data:
                    for tag_id, value in exif_data.items():
                        tag = ExifTags.TAGS.get(tag_id, tag_id)
                        if tag == 'Make':
                            has_camera_make = True
                            camera_make = str(value)
                        elif tag == 'Model':
                            has_camera_model = True
                            camera_model = str(value)
                        elif tag == 'Software':
                            has_software = True
                            software = str(value)
                
                # Screenshots typically lack camera EXIF data
                # Score inversely related to camera metadata presence
                score = 0.0
                if not has_camera_make and not has_camera_model:
                    score += 0.6
                if has_software and any(word in software.lower() for word in ['screenshot', 'snip', 'capture']):
                    score += 0.4
                
                score = min(score, 1.0)
                
                return score, {
                    'has_exif': len(exif_data) > 0,
                    'camera_make': camera_make,
                    'camera_model': camera_model,
                    'software': software,
                    'score': score
                }
                
        except Exception as e:
            return 0.0, {'error': f'EXIF analysis failed: {str(e)}'}
    
    def _analyze_dimensions(self, file_path: str) -> Tuple[float, Dict[str, Any]]:
        """Analyze image dimensions for screenshot indicators."""
        if not HAS_IMAGE_LIBS:
            return 0.0, {'error': 'PIL not available'}
            
        try:
            with Image.open(file_path) as img:
                width, height = img.size
                aspect_ratio = width / height
                
                # Check if dimensions match common screen resolutions
                matches_screen_resolution = (width, height) in self.COMMON_SCREEN_RESOLUTIONS
                
                # Check if aspect ratio matches common display ratios
                common_ratios = [16/9, 16/10, 4/3, 3/2, 21/9]
                matches_common_ratio = None
                for ratio in common_ratios:
                    if abs(aspect_ratio - ratio) < 0.05:  # 5% tolerance
                        matches_common_ratio = ratio
                        break
                
                # Score based on resolution and aspect ratio
                score = 0.0
                if matches_screen_resolution:
                    score += 0.8
                elif matches_common_ratio:
                    score += 0.4
                
                # Very wide or very tall images are less likely to be screenshots
                if aspect_ratio > 3 or aspect_ratio < 0.3:
                    score *= 0.5
                
                return score, {
                    'width': width,
                    'height': height,
                    'aspect_ratio': aspect_ratio,
                    'matches_screen_resolution': matches_screen_resolution,
                    'matches_common_ratio': matches_common_ratio,
                    'score': score
                }
                
        except Exception as e:
            return 0.0, {'error': f'Dimension analysis failed: {str(e)}'}
    
    def _analyze_content(self, file_path: str) -> Tuple[float, Dict[str, Any]]:
        """Basic image content analysis for screenshot characteristics."""
        if not HAS_IMAGE_LIBS:
            return 0.0, {'error': 'PIL and numpy not available'}
            
        try:
            with Image.open(file_path) as img:
                # Convert to RGB if needed
                if img.mode != 'RGB':
                    img = img.convert('RGB')
                
                # Resize for faster analysis
                img.thumbnail((200, 200), Image.Resampling.LANCZOS)
                img_array = np.array(img)
                
                # Basic color analysis
                mean_color = np.mean(img_array, axis=(0, 1))
                color_variance = np.var(img_array, axis=(0, 1))
                
                # Edge detection (simplified)
                gray = np.mean(img_array, axis=2)
                edges_h = np.abs(np.diff(gray, axis=0))
                edges_v = np.abs(np.diff(gray, axis=1))
                edge_density = (np.mean(edges_h) + np.mean(edges_v)) / 2
                
                # Uniformity analysis (screenshots often have uniform areas)
                uniformity = 1.0 / (1.0 + np.std(gray))
                
                # Simple scoring based on characteristics
                color_score = 0.5  # Neutral baseline
                edge_score = min(edge_density / 50.0, 1.0)  # More edges = more likely screenshot
                uniformity_score = uniformity * 0.7  # Some uniformity expected
                
                overall_score = (color_score + edge_score + uniformity_score) / 3
                
                return overall_score, {
                    'color_analysis': float(np.mean(color_variance)),
                    'edge_analysis': float(edge_density),
                    'uniformity_analysis': float(uniformity),
                    'score': overall_score
                }
                
        except Exception as e:
            return 0.0, {'error': f'Content analysis failed: {str(e)}'}


# Test function for development
def run_tests():
    """Run basic tests on the screenshot detector."""
    detector = ScreenshotDetector()
    
    test_files = [
        "screenshot_2024.png",
        "Screen Shot 2024-01-01.jpg",
        "vacation_photo.jpg", 
        "IMG_1234.jpg",
        "capture.png"
    ]
    
    print("Screenshot Detection Test Results:")
    print("-" * 50)
    
    for file_path in test_files:
        is_screenshot, confidence, details = detector.detect_screenshot(file_path)
        print(f"{file_path:25} | {is_screenshot:5} | {confidence:5.2f} | {details.get('error', 'OK')}")
    
    return "Tests completed"


if __name__ == "__main__":
    run_tests()
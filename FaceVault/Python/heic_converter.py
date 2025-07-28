"""
HEIC to JPEG converter module for FaceVault.

This module provides methods to convert HEIC/HEIF files to JPEG format
for better browser and Windows compatibility.
"""

import os
import tempfile
from typing import Optional, Tuple
from pathlib import Path

# Try to import PIL/Pillow with HEIC support
try:
    from PIL import Image
    from pillow_heif import register_heif_opener
    
    # Register HEIF opener with Pillow
    register_heif_opener()
    HAS_HEIC_SUPPORT = True
except ImportError:
    HAS_HEIC_SUPPORT = False


def convert_heic_to_jpeg(heic_path: str, max_size: int = 800, quality: int = 85) -> Optional[bytes]:
    """
    Convert HEIC file to JPEG bytes.
    
    Args:
        heic_path: Path to the HEIC file
        max_size: Maximum dimension for the output image
        quality: JPEG quality (0-100)
        
    Returns:
        JPEG bytes or None if conversion failed
    """
    if not HAS_HEIC_SUPPORT:
        return None
        
    try:
        if not os.path.exists(heic_path):
            return None
            
        # Open HEIC file with Pillow
        with Image.open(heic_path) as img:
            # Convert to RGB if necessary (HEIC might be in different color space)
            if img.mode != 'RGB':
                img = img.convert('RGB')
            
            # Calculate resize dimensions maintaining aspect ratio
            width, height = img.size
            if width > max_size or height > max_size:
                if width > height:
                    new_width = max_size
                    new_height = int((height * max_size) / width)
                else:
                    new_height = max_size
                    new_width = int((width * max_size) / height)
                
                img = img.resize((new_width, new_height), Image.Resampling.LANCZOS)
            
            # Save as JPEG to bytes
            import io
            output = io.BytesIO()
            img.save(output, format='JPEG', quality=quality, optimize=True)
            return output.getvalue()
            
    except Exception as e:
        print(f"Error converting HEIC to JPEG: {e}")
        return None


def check_heic_support() -> dict:
    """Check if HEIC conversion is available."""
    return {
        "has_heic_support": HAS_HEIC_SUPPORT,
        "pillow_available": "PIL" in globals(),
        "pillow_heif_available": HAS_HEIC_SUPPORT
    }


def get_heic_thumbnail(heic_path: str, max_size: int = 250) -> Optional[bytes]:
    """
    Get a thumbnail for HEIC file as JPEG bytes.
    
    Args:
        heic_path: Path to the HEIC file
        max_size: Maximum dimension for thumbnail
        
    Returns:
        JPEG thumbnail bytes or None if failed
    """
    return convert_heic_to_jpeg(heic_path, max_size, quality=80)
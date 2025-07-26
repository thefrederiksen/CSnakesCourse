"""
Image scanning and metadata extraction module for FaceVault.
Handles discovering image files and extracting basic metadata.
"""

import os
import hashlib
from pathlib import Path
from typing import List, Dict, Any, Optional
from datetime import datetime
from PIL import Image
from PIL.ExifTags import TAGS


def scan_directory(directory_path: str, supported_extensions: List[str] = None) -> List[Dict[str, Any]]:
    """
    Recursively scan directory for image files.
    
    Args:
        directory_path: Path to directory to scan
        supported_extensions: List of file extensions to include (default: common image formats)
    
    Returns:
        List of dictionaries containing image metadata
    """
    if supported_extensions is None:
        supported_extensions = ['.jpg', '.jpeg', '.png', '.bmp', '.tiff', '.tif', '.webp']
    
    if not os.path.exists(directory_path):
        raise ValueError(f"Directory does not exist: {directory_path}")
    
    image_files = []
    directory = Path(directory_path)
    
    for file_path in directory.rglob('*'):
        if file_path.is_file() and file_path.suffix.lower() in supported_extensions:
            try:
                metadata = extract_image_metadata(str(file_path))
                image_files.append(metadata)
            except Exception as e:
                # Log error but continue processing other files
                print(f"Error processing {file_path}: {e}")
                continue
    
    return sorted(image_files, key=lambda x: x.get('file_path', ''))


def calculate_file_hash(file_path: str) -> str:
    """
    Calculate SHA-256 hash for duplicate detection.
    
    Args:
        file_path: Path to the file
        
    Returns:
        Hexadecimal hash string
    """
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")
    
    sha256_hash = hashlib.sha256()
    
    with open(file_path, "rb") as f:
        # Read file in chunks to handle large files efficiently
        for chunk in iter(lambda: f.read(4096), b""):
            sha256_hash.update(chunk)
    
    return sha256_hash.hexdigest()


def extract_image_metadata(file_path: str) -> Dict[str, Any]:
    """
    Extract EXIF data, dimensions, and file stats from an image.
    
    Args:
        file_path: Path to the image file
        
    Returns:
        Dictionary containing image metadata
    """
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")
    
    file_stats = os.stat(file_path)
    metadata = {
        'file_path': file_path,
        'file_name': os.path.basename(file_path),
        'file_size': file_stats.st_size,
        'file_hash': calculate_file_hash(file_path),
        'last_modified': datetime.fromtimestamp(file_stats.st_mtime),
        'scan_date': datetime.now(),
        'image_width': None,
        'image_height': None,
        'date_taken': None,
        'camera_make': None,
        'camera_model': None,
        'gps_coordinates': None
    }
    
    try:
        with Image.open(file_path) as img:
            metadata['image_width'] = img.width
            metadata['image_height'] = img.height
            
            # Extract EXIF data if available
            exif_data = img._getexif()
            if exif_data:
                exif_dict = {}
                for tag_id, value in exif_data.items():
                    tag = TAGS.get(tag_id, tag_id)
                    exif_dict[tag] = value
                
                # Extract specific metadata we care about
                metadata['date_taken'] = _extract_date_taken(exif_dict)
                metadata['camera_make'] = exif_dict.get('Make')
                metadata['camera_model'] = exif_dict.get('Model')
                metadata['gps_coordinates'] = _extract_gps_coordinates(exif_dict)
                
    except Exception as e:
        print(f"Warning: Could not extract image metadata from {file_path}: {e}")
    
    return metadata


def _extract_date_taken(exif_dict: Dict[str, Any]) -> Optional[datetime]:
    """Extract date taken from EXIF data."""
    date_fields = ['DateTime', 'DateTimeOriginal', 'DateTimeDigitized']
    
    for field in date_fields:
        if field in exif_dict:
            try:
                return datetime.strptime(exif_dict[field], '%Y:%m:%d %H:%M:%S')
            except (ValueError, TypeError):
                continue
    
    return None


def _extract_gps_coordinates(exif_dict: Dict[str, Any]) -> Optional[Dict[str, float]]:
    """Extract GPS coordinates from EXIF data."""
    gps_info = exif_dict.get('GPSInfo')
    if not gps_info:
        return None
    
    try:
        # This is a simplified GPS extraction - real implementation would be more robust
        if 'GPSLatitude' in gps_info and 'GPSLongitude' in gps_info:
            lat = _convert_gps_coordinate(gps_info['GPSLatitude'], gps_info.get('GPSLatitudeRef', 'N'))
            lon = _convert_gps_coordinate(gps_info['GPSLongitude'], gps_info.get('GPSLongitudeRef', 'E'))
            return {'latitude': lat, 'longitude': lon}
    except (KeyError, ValueError, TypeError):
        pass
    
    return None


def _convert_gps_coordinate(coordinate: tuple, reference: str) -> float:
    """Convert GPS coordinate from DMS format to decimal degrees."""
    degrees, minutes, seconds = coordinate
    decimal = float(degrees) + float(minutes) / 60 + float(seconds) / 3600
    
    if reference in ['S', 'W']:
        decimal = -decimal
    
    return decimal


def get_image_info_batch(file_paths: List[str]) -> List[Dict[str, Any]]:
    """
    Process multiple images efficiently with error handling.
    
    Args:
        file_paths: List of image file paths
        
    Returns:
        List of metadata dictionaries
    """
    results = []
    
    for file_path in file_paths:
        try:
            metadata = extract_image_metadata(file_path)
            results.append(metadata)
        except Exception as e:
            # Create error record instead of failing completely
            error_metadata = {
                'file_path': file_path,
                'error': str(e),
                'scan_date': datetime.now()
            }
            results.append(error_metadata)
    
    return results
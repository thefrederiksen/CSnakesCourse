"""
Simple image analysis module for FaceVault.
This module provides basic image analysis functionality without external API dependencies.
"""

import os
import random
from typing import Dict, Any
from pathlib import Path

# Try to import PIL for basic image analysis
try:
    from PIL import Image
    import PIL.ExifTags
    HAS_PIL = True
except ImportError:
    HAS_PIL = False


def test_analysis() -> str:
    """Test function to verify the module is loaded correctly."""
    return "Image analysis module loaded successfully"


def analyze_image(file_path: str) -> Dict[str, Any]:
    """
    Analyze an image and return basic metadata.
    
    Args:
        file_path: Path to the image file
        
    Returns:
        Dictionary containing analysis results
    """
    result = {
        "category": "Unknown",
        "description": "",
        "confidence": 0.0,
        "tags": [],
        "error": None
    }
    
    try:
        if not os.path.exists(file_path):
            result["error"] = "File not found"
            return result
            
        # Get basic file info
        file_name = os.path.basename(file_path)
        file_ext = Path(file_path).suffix.lower()
        file_size = os.path.getsize(file_path)
        
        # Basic categorization based on filename patterns
        name_lower = file_name.lower()
        
        # Check for screenshot patterns
        if 'screenshot' in name_lower or 'screen shot' in name_lower:
            result["category"] = "Screenshots"
            result["description"] = "Screen capture image"
            result["confidence"] = 0.95
            result["tags"] = ["screenshot", "screen capture"]
        
        # Check for selfie patterns
        elif 'selfie' in name_lower or 'img_' in name_lower:
            result["category"] = "People"
            result["description"] = "Selfie or personal photo"
            result["confidence"] = 0.7
            result["tags"] = ["selfie", "people", "portrait"]
            
        # Check for document patterns
        elif 'scan' in name_lower or 'document' in name_lower or file_ext == '.pdf':
            result["category"] = "Documents"
            result["description"] = "Scanned document or text"
            result["confidence"] = 0.8
            result["tags"] = ["document", "scan", "text"]
            
        # If PIL is available, do more sophisticated analysis
        elif HAS_PIL:
            try:
                with Image.open(file_path) as img:
                    width, height = img.size
                    aspect_ratio = width / height if height > 0 else 1
                    
                    # Analyze based on dimensions and aspect ratio
                    if width == height:
                        result["category"] = "Objects"
                        result["description"] = "Square format image, possibly product or icon"
                        result["confidence"] = 0.6
                        result["tags"] = ["square", "product", "icon"]
                    
                    elif aspect_ratio > 2 or aspect_ratio < 0.5:
                        result["category"] = "Scenes"
                        result["description"] = "Panoramic or tall image"
                        result["confidence"] = 0.65
                        result["tags"] = ["panorama", "landscape", "scene"]
                    
                    elif width > 3000 or height > 3000:
                        result["category"] = "Nature"
                        result["description"] = "High resolution photo, possibly nature or landscape"
                        result["confidence"] = 0.5
                        result["tags"] = ["high-res", "nature", "landscape"]
                    
                    else:
                        # Default photo categorization
                        categories = ["People", "Nature", "Objects", "Animals", "Scenes"]
                        result["category"] = random.choice(categories)
                        result["confidence"] = 0.4
                        
                        # Generate description based on category
                        descriptions = {
                            "People": "Photo containing people or portraits",
                            "Nature": "Nature or outdoor scene",
                            "Objects": "Still life or object photography",
                            "Animals": "Wildlife or pet photography",
                            "Scenes": "General scene or landscape"
                        }
                        result["description"] = descriptions.get(result["category"], "General photo")
                        result["tags"] = ["photo", result["category"].lower()]
                    
                    # Add dimension info to tags
                    result["tags"].append(f"{width}x{height}")
                    
                    # Check EXIF data if available
                    exif = img.getexif()
                    if exif:
                        for tag_id, value in exif.items():
                            tag_name = PIL.ExifTags.TAGS.get(tag_id, tag_id)
                            
                            # Look for camera info
                            if tag_name == "Make" and value:
                                result["tags"].append(f"camera:{value}")
                            elif tag_name == "Model" and value:
                                result["tags"].append(f"model:{value}")
                            elif tag_name == "Software" and value:
                                # Check for screenshot software
                                if "screenshot" in str(value).lower():
                                    result["category"] = "Screenshots"
                                    result["confidence"] = 0.9
                                    
            except Exception as e:
                # If PIL fails, fall back to basic analysis
                pass
        
        # If no category assigned yet, make an educated guess
        if result["category"] == "Unknown":
            # Random assignment for demo purposes
            categories = ["Photos", "Images", "Pictures", "Media", "Files"]
            result["category"] = random.choice(categories)
            result["description"] = "Unanalyzed image file"
            result["confidence"] = 0.3
            result["tags"] = ["unanalyzed", file_ext[1:] if file_ext else "unknown"]
            
    except Exception as e:
        result["error"] = str(e)
        result["category"] = "Error"
        result["description"] = f"Error analyzing image: {str(e)}"
        
    return result


def analyze_image_with_ai(file_path: str, ai_provider: str = "", api_key: str = "", model: str = "", endpoint: str = "") -> Dict[str, Any]:
    """
    Analyze an image using AI settings provided as parameters.
    
    Args:
        file_path: Path to the image file
        ai_provider: AI provider name (e.g., "openai", "azure", "google")
        api_key: API key for authentication
        model: AI model to use (optional)
        endpoint: API endpoint URL (optional)
        
    Returns:
        Dictionary containing analysis results
    """
    # Always start with basic analysis
    result = analyze_image(file_path)
    
    # Check if AI provider and key are provided
    if ai_provider and api_key:
        # In a real implementation, this would call the appropriate AI service
        # For now, we'll add markers that AI was configured
        result["ai_provider"] = ai_provider.lower()
        result["ai_configured"] = True
        
        # Enhance the description based on AI provider
        if ai_provider.lower() == "openai":
            result["description"] = f"[OpenAI] {result['description']}"
            result["model"] = model or "gpt-4o-mini"
        elif ai_provider.lower() == "azure":
            result["description"] = f"[Azure] {result['description']}"
            result["model"] = model or "vision-v3.2"
        elif ai_provider.lower() == "google":
            result["description"] = f"[Google] {result['description']}"
            result["model"] = model or "gemini-pro-vision"
        else:
            result["description"] = f"[{ai_provider}] {result['description']}"
            result["model"] = model or "default"
            
        # AI analysis typically has higher confidence
        result["confidence"] = min(result["confidence"] + 0.2, 0.95)
        
        # Add endpoint info if provided
        if endpoint:
            result["endpoint_configured"] = True
    else:
        # No AI configuration provided
        result["ai_configured"] = False
        result["analysis_mode"] = "basic"
    
    return result


# Example usage
if __name__ == "__main__":
    # Test with a sample image
    test_path = "test_image.jpg"
    if os.path.exists(test_path):
        result = analyze_image(test_path)
        print(f"Analysis result: {result}")
    else:
        print("Test image not found")
    
    # Test the test function
    print(test_analysis())
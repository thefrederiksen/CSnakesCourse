# Test Images for Screenshot Detection

Please place the following test images in this directory:

## Required Test Images

1. **screenshot.jpg** - A clear screenshot image
   - Should be an actual screenshot from Windows, macOS, or mobile device
   - Examples: desktop screenshot, app interface, web browser content
   - Filename patterns like "Screenshot 2024-01-27.jpg" work well

2. **photo.jpg** - A regular photograph
   - Should be a natural photograph taken with a camera
   - Examples: landscape, portrait, food, nature, etc.
   - Should have EXIF data if possible (taken with camera/phone)

## Test Instructions

After placing the images, run the tests:

```bash
cd "C:\Repos\CSnakesCourse\FaceVault\Python\test"
python test_screenshots.py
```

Or run individual tests:

```bash
# Test with a specific image
cd "C:\Repos\CSnakesCourse\FaceVault\Python"
python screenshots.py "test/images/screenshot.jpg"
python screenshots.py "test/images/photo.jpg"
```

## What the Tests Check

1. **Filename Analysis**: Screenshot patterns in filenames
2. **EXIF Metadata**: Presence/absence of camera information
3. **Dimensions**: Common screen resolutions vs camera resolutions
4. **Content Analysis**: Color uniformity, edge patterns, UI elements
5. **Integration**: Overall confidence scoring

## Expected Results

- **screenshot.jpg**: Should be detected as screenshot with confidence > 0.5
- **photo.jpg**: Should NOT be detected as screenshot with confidence < 0.5
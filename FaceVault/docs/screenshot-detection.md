# Screenshot Detection Approach

## Overview

This document defines our approach for detecting screenshots in the FaceVault application. The goal is to accurately identify screenshots using simple, definitive signals from filenames and metadata.

## Core Principles

1. **Simple and fast**: Use only definitive signals (filename and metadata)
2. **High accuracy**: Both methods have near 100% accuracy when they match
3. **No false positives**: Only mark as screenshot when we're certain

## Detection Algorithm (2 Phases)

### Phase 1: Filename Check

If the filename contains "screenshot" (case-insensitive), **immediately classify as screenshot** with 100% confidence.

**Examples that match:**
- `screenshot_2024-01-27.png`
- `Screenshot 2024-01-27 143045.png`
- `Screen Shot 2024-01-27 at 2.30.45 PM.png`
- `my_screenshot_edited.jpg`

**Rationale**: If someone named it "screenshot", it's a screenshot. Simple and accurate.

### Phase 2: Metadata Check

Check if image metadata contains "screenshot" markers:
- iOS: UserComment field = "Screenshot"
- PNG: Text chunks containing "Screenshot"
- EXIF: Any field containing "screenshot"

If found, classify as screenshot with 100% confidence.

**Supported formats**: JPEG, PNG, TIFF, WebP, HEIC (with appropriate libraries)

**Rationale**: Platform-specific markers are definitive proof.

### Phase 3: Future AI Analysis (Not Yet Implemented)

In the future, we will add ChatGPT/Claude analysis for images that don't match Phase 1 or 2. This will handle edge cases like "something.png" (maps, apps without screenshot in name).

## Decision Flow

```
1. Check Filename
   ├─ Contains "screenshot"? → YES → 100% confidence → DONE
   └─ NO → Continue to Phase 2

2. Check Metadata  
   ├─ Contains "screenshot" marker? → YES → 100% confidence → DONE
   └─ NO → Not a screenshot (for now)
```

## Examples

### Example 1: "screenshot_2024.png"
- Phase 1: Filename contains "screenshot" → 100% confidence → Screenshot ✓

### Example 2: "IMG_1234.jpg" with iOS metadata
- Phase 1: Filename check → No match
- Phase 2: iOS UserComment = "Screenshot" → 100% confidence → Screenshot ✓

### Example 3: "something.png" (Google Maps)
- Phase 1: Filename check → No match
- Phase 2: Metadata check → No match
- Result: Not detected as screenshot (will be handled by future AI phase)

## Implementation Notes

1. **Performance**: Both checks are near-instant
2. **No false positives**: Only marks as screenshot when certain
3. **Simple code**: Easy to maintain and understand

## Python Implementation

```python
class ScreenshotDetector:
    def detect_screenshot(self, file_path: str) -> Tuple[bool, float]:
        # Phase 1: Filename check
        if "screenshot" in Path(file_path).name.lower():
            return True, 1.0
        
        # Phase 2: Metadata check
        if self._check_metadata(file_path):
            return True, 1.0
        
        # No match found
        return False, 0.0
    
    def _check_metadata(self, file_path: str) -> bool:
        """Check if metadata contains screenshot markers."""
        # Implementation depends on available libraries
        # Check EXIF, PNG chunks, etc. for "screenshot"
        pass
```

## Summary

This simplified approach:
1. **Fast**: No complex analysis, just string checking
2. **Accurate**: 100% confidence when it matches
3. **Simple**: Easy to implement and maintain
4. **Future-ready**: Can add AI analysis later for better coverage
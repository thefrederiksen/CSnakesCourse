# FaceVault Database Schema Documentation

## Overview
This document describes all tables in the FaceVault database, their purpose, and key columns.

The database uses a modular design where information is separated based on how it's obtained:
- **tbl_images**: File system information (no file opening required)
- **tbl_image_metadata**: Information requiring file opening/parsing
- **tbl_image_analysis**: AI analysis results

All tables use the `tbl_` prefix to clearly distinguish database tables from regular classes.

## Tables

### 1. **tbl_version**
**Purpose**: Tracks the current database schema version for migrations.

| Column | Type | Description |
|--------|------|-------------|
| Version | INTEGER | Current schema version number (starts at 1) |

**Notes**: 
- Single row table
- Updated by each migration script
- Used to determine which migrations to apply

---

### 2. **tbl_images**
**Purpose**: Core table storing file system information (no file opening required).

| Column | Type | Description |
|--------|------|-------------|
| ImageId | INTEGER PK | Auto-incrementing primary key |
| RelativePath | TEXT | Relative path from photo directory (unique) |
| FileName | TEXT | Just the filename |
| FileExtension | TEXT | File extension (.jpg, .png, etc.) |
| FileHash | TEXT | SHA-256 hash for duplicate detection |
| FileSizeBytes | INTEGER | File size in bytes |
| DateCreated | DATETIME | File creation date |
| DateModified | DATETIME | File modification date |
| **File Status** | | |
| IsDeleted | INTEGER | Soft delete flag |
| FileExists | INTEGER | File still exists on disk |
| DateDeleted | DATETIME | When soft deleted |

**Indexes**: RelativePath (unique), FileHash, DateCreated, IsDeleted

**Design**: Contains only information available from file system scanning without opening files.

---

### 3. **tbl_image_metadata**
**Purpose**: Stores metadata that requires opening and parsing the image file.

| Column | Type | Description |
|--------|------|-------------|
| ImageId | INTEGER PK | Same as tbl_images.ImageId (shared primary key) |
| Width | INTEGER | Image width in pixels |
| Height | INTEGER | Image height in pixels |
| DateTaken | DATETIME | EXIF date taken (nullable) |

**Indexes**: None needed (uses same PK as tbl_images)

**Relationships**: 
- One-to-One with tbl_images (CASCADE delete)
- Shares primary key with tbl_images table

**Design**: 
- Uses same primary key as tbl_images table for guaranteed one-to-one relationship
- Small starting set - can be expanded with more EXIF data later
- Use LEFT JOIN to query with tbl_images table

---

### 4. **tbl_image_analysis**
**Purpose**: Stores AI analysis results for images.

| Column | Type | Description |
|--------|------|-------------|
| ImageId | INTEGER PK | Same as tbl_images.ImageId (shared primary key) |
| ImageCategory | TEXT | AI-detected category (photo, screenshot, etc.) |
| PhotoSubcategory | TEXT | Sub-category (landscape, portrait, etc.) |
| AIAnalysisJson | TEXT | Full AI analysis JSON response |
| AIDescription | TEXT | AI-generated description |
| AIAnalyzedAt | DATETIME | When AI analysis was performed |
| AIModelUsed | TEXT | Which AI model was used |
| AIKeywords | TEXT | Comma-separated keywords from AI |

**Indexes**: ImageCategory

**Relationships**: 
- One-to-One with tbl_images (CASCADE delete)
- Shares primary key with tbl_images table

**Design**: 
- Uses same primary key as tbl_images table for guaranteed one-to-one relationship
- Separated from core tbl_images table to allow optional AI analysis
- Use LEFT JOIN to query with tbl_images table

---

### 5. **tbl_app_settings**
**Purpose**: Application configuration and user preferences (singleton).

| Column | Type | Description |
|--------|------|-------------|
| Id | INTEGER PK | Always 1 |
| **Feature Toggles** | | |
| EnableDuplicateDetection | INTEGER | Find duplicates |
| EnableAIImageAnalysis | INTEGER | Enable AI features |
| **Directories** | | |
| PhotoDirectory | TEXT | Main photo folder path |
| AutoScanOnStartup | INTEGER | Scan on app start |
| ScanSubdirectories | INTEGER | Include subfolders |
| **File Types** | | |
| SupportJpeg | INTEGER | Process JPEG files |
| SupportPng | INTEGER | Process PNG files |
| SupportHeic | INTEGER | Process HEIC files |
| SupportGif | INTEGER | Process GIF files |
| SupportBmp | INTEGER | Process BMP files |
| SupportWebp | INTEGER | Process WebP files |
| **Performance** | | |
| BatchSize | INTEGER | Photos per batch |
| MaxConcurrentTasks | INTEGER | Parallel tasks |
| **AI Settings** | | |
| AIProvider | TEXT | OpenAI/Azure/etc. |
| AIApiKey | TEXT | API key |
| AIApiEndpoint | TEXT | API URL |
| AIModel | TEXT | Model name (gpt-4o-mini) |
| AIMaxTokens | INTEGER | Token limit |
| AITemperature | REAL | Generation temperature |
| **Other Settings** | | |
| ThemeName | TEXT | UI theme |
| DateCreated | DATETIME | First run date |
| DateModified | DATETIME | Last settings change |
| LastScanDate | DATETIME | Last photo scan |

**Notes**: 
- Single row table (Id = 1)
- Contains all user preferences and app configuration

---

## Key Relationships

```
tbl_images (PK: ImageId) ---- tbl_image_metadata (PK: ImageId, same value)
   |
   +--- tbl_image_analysis (PK: ImageId, same value)
```

**Query Pattern**: Use LEFT JOIN to get optional metadata/analysis
```sql
SELECT i.*, m.Width, m.Height, a.ImageCategory 
FROM tbl_images i 
LEFT JOIN tbl_image_metadata m ON i.ImageId = m.ImageId 
LEFT JOIN tbl_image_analysis a ON i.ImageId = a.ImageId
```

## Design Decisions

1. **Table Prefix**: All tables use `tbl_` prefix to distinguish from regular classes
   - `tbl_images` vs `Image` class
   - `tbl_image_metadata` vs `ImageMetadata` class
   - Makes Entity Framework models clearly identifiable

2. **Modular Structure**: Information separated by how it's obtained
   - File system scanning (tbl_images)
   - File parsing (tbl_image_metadata) 
   - AI analysis (tbl_image_analysis)

3. **One-to-One Relationships**: tbl_image_metadata and tbl_image_analysis are optional extensions of tbl_images

4. **Cascade Deletes**: Deleting an image removes all related metadata and analysis

5. **Relative Paths**: tbl_images stores relative paths, not absolute paths

6. **Performance**: Focused indexing on commonly queried columns

7. **Extensibility**: Easy to add more fields to tbl_image_metadata or tbl_image_analysis

## Benefits of This Design

1. **Fast Directory Scanning**: Core tbl_images table populated quickly from file system
2. **Optional Processing**: Metadata and analysis can be added incrementally
3. **Robust**: Failures in metadata extraction or AI analysis don't affect core image records
4. **Scalable**: Easy to add new analysis types or metadata fields
5. **Portable**: Relative paths make database portable across different photo directory locations
6. **Clear Separation**: `tbl_` prefix makes database tables easily identifiable in code

## Future Considerations

Future schema versions may add:
- More EXIF fields to tbl_image_metadata (GPS, camera settings, etc.)
- Additional analysis tables (tbl_face_detection, tbl_object_detection, etc.)
- **tbl_people**: Face recognition and person identification
- **tbl_tags**: Hierarchical tagging system
- **tbl_events**: Automatic event grouping
- **tbl_albums**: User-created collections
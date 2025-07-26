# FaceVault - Comprehensive Product Requirements Document (PRD)

## 1. Product Overview

### 1.1 Product Name

**FaceVault** – Smart Photo Face Recognition & Organization

### 1.2 Product Vision

"Your Personal Photo Memory Vault – Never lose a face again"

### 1.3 Educational Purpose & Context

#### 1.3.1 CSnakes Course Lab

FaceVault serves as a **comprehensive demonstration lab** within the CSnakes course curriculum, showcasing hybrid Python+C# development.

#### 1.3.2 Why This Matters

**Python's Advantages:**

- AI/ML: `face_recognition`, `opencv-python`
- Scientific computing: NumPy, clustering
- Rapid prototyping: fewer lines of code
- Rich libraries & faster development

**C# Provides:**

- Professional WinForms UI
- Enterprise-grade structure
- Local database (SQLite)
- CSnakes Runtime for Python interop

#### 1.3.3 Technology Choice Rationale

**Why WinForms:**

- Easy setup and visual impact
- No external servers or tools
- All local; simple for students

**Broader CSnakes Applications:**

- ASP.NET Core APIs
- Console + background services
- Desktop (WPF/Avalonia)
- Cloud or microservices

#### 1.3.4 Learning Objectives

- When to use Python in a .NET app
- Hybrid architecture design
- C#/Python integration patterns
- Real-world application design
- Performance tradeoffs

### 1.4 Mission Statement

FaceVault helps users intelligently organize and rediscover photos using local AI tools. It acts as a real-world learning platform for hybrid software design.

### 1.5 Target Audience

#### 1.5.1 Educational Audience

- CSnakes students
- .NET devs learning AI
- Python devs exploring desktop

#### 1.5.2 End-User Audience

- Families with 1000+ photos
- Photography hobbyists
- Memory archivists
- Small businesses

### 1.6 Key Value Propositions

#### Educational Value

- Shows real hybrid architecture
- Clear example of Python’s superiority for ML
- Immediate impact with real data

#### User Value

- 100% local – private by default
- Auto-tagging and grouping
- Find any face instantly
- Clean, responsive interface
- Collage generation
- “On This Day” memories
- Duplicate and screenshot filtering

---

## 2. Core Requirements

### 2.1 Functional Requirements

#### 2.1.1 Photo Discovery & Scanning

- FR-001: Recursively scan user-selected directories
- FR-002: Supported image extensions:
  - `.jpg`, `.jpeg`
  - `.png`
  - `.bmp`
  - `.tiff`, `.tif`
  - `.webp`
  - `.heic`, `.heif` (if supported by OS or libraries)
  - Legacy support: `.gif` (only if static), `.jp2` (JPEG 2000)
- FR-003: Batch image processing to manage memory
- FR-004: Progress indicators
- FR-005: Skip unreadable/corrupt files
- FR-006: Calculate file hashes for duplicate detection
- FR-041: Detect exact duplicates via hash
- FR-042: Enable user review of duplicates
- FR-043: Detect screenshots using `screenshot_detector`
- FR-044: Allow screenshot exclusion from workflows
- FR-003b: Auto-scan folders on launch

#### 2.1.2 Face Detection & Recognition

- FR-007: Detect faces using `face_recognition`
- FR-008: Generate encodings
- FR-009: Store bounding boxes
- FR-010: Confidence scoring
- FR-011: Multi-face support
- FR-012: Age progression tolerance

#### 2.1.3 Intelligent Grouping

- FR-013: Cluster similar faces with DBSCAN
- FR-014: Create "Unknown Person" groups
- FR-015: Merge clusters above threshold
- FR-016: Handle lighting, angles, glasses
- FR-017: Separate different individuals

#### 2.1.4 User Classification Interface

- FR-018: Present unknown faces
- FR-019: Suggest likely matches
- FR-020: Add new person
- FR-021: Mark non-faces
- FR-022: Batch classify faces
- FR-023: Undo recent classification

#### 2.1.5 Person Management

- FR-024: Rename person
- FR-025: Merge person groups
- FR-026: Delete person
- FR-027: Show count per person
- FR-028: Display face thumbnail

#### 2.1.6 Search & Navigation

- FR-029: Search by name
- FR-030: View all photos of one person
- FR-031: Filter by number of faces
- FR-032: Chronological browsing
- FR-033: Fast person switching

#### 2.1.7 Photo Organization

- FR-034: Move misclassified photos
- FR-035: Mark photos for export
- FR-036: Build albums per person
- FR-037: Export organized folders
- FR-038: Generate photo collages (initial version)

#### 2.1.8 "On This Day" Feature

- FR-039: Match current date to photo timestamps
- FR-040: Display as daily dashboard memory

#### 2.1.9 Collage Generator

- FR-045: Create collage from tagged photos
- FR-046: Optional grayscale mode, aspect ratio config
- FR-047: Allow user to rearrange images
- FR-048: Sub-tag selection for collage inclusion

#### 2.1.10 Tagging System

- FR-049: Tag photos and/or specific faces/objects
- FR-050: Filter workflows based on tags

#### 2.1.11 LLM Integration (optional)

- FR-051: Ask questions in natural language
- FR-052: Select API provider (OpenAI, Claude, etc.)
- FR-053: Custom API endpoint + key
- FR-054: Use vector store for search queries
- FR-055: Agent orchestration using tools (query, summarize)

### 2.2 Non-Functional Requirements

#### Performance

- NFR-001: 1000 photos in <10 min
- NFR-002: UI responsive during background tasks
- NFR-003: Load thumbnails <200ms
- NFR-004: DB queries <1 sec
- NFR-005: Max memory 2GB
- NFR-021: Generate collage <5 sec

#### Privacy

- NFR-006: All local; no cloud needed
- NFR-007: No external data sent
- NFR-008: Encrypted face encoding DB
- NFR-009: Full user control over data
- NFR-010: No telemetry

#### Usability

- NFR-011: App startup <3 sec
- NFR-012: Fewest clicks to classify
- NFR-013: Clear error handling
- NFR-014: Adaptive UI resolution
- NFR-015: Power-user shortcuts

#### Reliability

- NFR-016: Skip broken images
- NFR-017: Database corruption recovery
- NFR-018: Auto-backup classification data
- NFR-019: Resume after crash
- NFR-020: Persistent error logging

---

## 3. Technical Architecture

### 3.1 Tech Stack

#### C# Components

- .NET 9 WinForms (Windows-only)
- SQLite.NET ORM
- CSnakes Runtime bridge to Python
- Background processing with async/task

#### Python Libraries

```python
face_recognition==1.3.0
opencv-python==4.8.0
Pillow==10.0.0
numpy==1.24.0
scikit-learn==1.3.0
pickle
sqlite3
screenshot_detector
photocollage / OpenCV for collage
sentence-transformers (for LLM queries)
```

### 3.2 Database Schema

**Images Table:**

```sql
CREATE TABLE images (
    id INTEGER PRIMARY KEY,
    file_path TEXT UNIQUE NOT NULL,
    file_hash TEXT,
    file_size INTEGER,
    image_width INTEGER,
    image_height INTEGER,
    date_taken DATETIME,
    scan_date DATETIME,
    face_count INTEGER DEFAULT 0,
    last_modified DATETIME
);
```

**People Table:**

```sql
CREATE TABLE people (
    id INTEGER PRIMARY KEY,
    name TEXT,
    created_date DATETIME,
    last_updated DATETIME,
    photo_count INTEGER,
    representative_face_id INTEGER,
    notes TEXT
);
```

**Faces Table:**

```sql
CREATE TABLE faces (
    id INTEGER PRIMARY KEY,
    image_id INTEGER,
    person_id INTEGER,
    bounding_box TEXT,
    face_encoding BLOB,
    confidence REAL,
    is_verified BOOLEAN,
    is_false_positive BOOLEAN,
    detection_date DATETIME
);
```

---

## 4. User Interface Design

### 4.1 Startup Setup Wizard

- Ask user which features to enable:
  - ☑️ On This Day
  - ☑️ Face Recognition
  - ☑️ Screenshot Filtering
  - ☑️ Duplicate Detection
  - ☑️ Collage Generation
  - ☑️ LLM Query (API key entry required)

### 4.2 Main Dashboard

```
[ Photo Folder: C:\Photos   ][📂 Browse] [🔄 Rescan]
📊 Status: 4,120 photos • 213 people • 11 pending review
👥 PEOPLE [ Search ]
[Emma - 43] [Dad - 88] [Anna - 23] [Unknown x12]
🎯 Quick Actions: [Review Faces] [Daily Memories] [Find Duplicates] [Make Collage]
📝 Recent: “Created person Anna”, “5 new images tagged Vacation 2023”
```

### 4.3 Face Review Screen

```
Classify Face [11 of 23 Remaining]
[Photo of Face] → ○ Suggest: Sarah (94%) ○ Suggest: Mom (21%)
[New Person: ____ ]   [Not a face ❌]  [✔ Confirm] [⏭ Next]
```

### 4.4 Person View

```
👤 Peter • 88 Photos
[✏️ Rename] [🗑️ Delete] [Merge] [Add Tag]
[img1] [img2] [img3]...
Timeline: 2024 ▓▓▓  2023 ▓▓▓▓▓  2022 ▓▓
```

### 4.5 Collage Generator

```
Select Tags: [Family] [Vacation] [✔ Collage]
Aspect Ratio: 4:3   Grayscale: ☑️
Images: [img1] [img2] [img3]...
[↔ Swap img1/img2]  [↔ Swap img3/img4]  [Reset Layout]
[Generate Preview] [Save as PNG]
```

---

## 5. User Stories

### 5.1 Discovery

- US-001: Select folder to scan
- US-002: View scan progress
- US-003: Auto-add new images

### 5.2 Recognition

- US-004: Faces auto-grouped
- US-005: Assign names
- US-006: Fix bad matches

### 5.3 Organization

- US-007: Search by person
- US-008: View photo count per person
- US-009: Export folders by person

### 5.4 Quality Management

- US-010: Detect duplicate photos
- US-011: Mark false positives
- US-012: Merge duplicates

### 5.5 Memory Features

- US-013: Show memories from today’s date across years

### 5.6 Collage Tool

- US-014: Tag images and generate printable collage
- US-015: Rearrange or grayscale the layout
- US-016: Filter collages by sub-tags

### 5.7 LLM Interface

- US-017: Ask natural language questions
- US-018: Choose model provider
- US-019: Link own API key or local agent

---

## 6. Success Metrics

### Technical

- Face recognition accuracy > 95%
- Process 100 photos/minute
- Memory usage < 2GB

### UX

- New user onboarding < 15 mins
- Face classification: 50 in under 10 min
- Search time < 3 sec

### Engagement

-
  > 90% satisfaction in beta
- Feature opt-ins tracked (e.g., 80% enable collage)
- Daily memory view > 1x/week average per user

---

This PRD consolidates all current and future product requirements for FaceVault in a complete, implementable document. All features, architecture, and experience goals are defined for development and delivery.


-- FaceVault Database Schema
-- Version: 001
-- Description: Initial database creation with modular table structure
-- Date: 2025-01-29

-- Create Version table for tracking database version
CREATE TABLE IF NOT EXISTS tbl_version (
    Version INTEGER NOT NULL
);

-- Insert initial version
INSERT INTO tbl_version (Version) VALUES (1);

-- Create Images table - Only file system information (no file opening required)
CREATE TABLE IF NOT EXISTS tbl_images (
    ImageId INTEGER PRIMARY KEY AUTOINCREMENT,
    RelativePath TEXT NOT NULL,
    FileName TEXT NOT NULL,
    FileExtension TEXT,
    FileHash TEXT NOT NULL,
    FileSizeBytes INTEGER NOT NULL,
    DateCreated DATETIME NOT NULL,
    DateModified DATETIME NOT NULL,
    
    -- File Status
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    FileExists INTEGER NOT NULL DEFAULT 1,
    DateDeleted DATETIME
);

-- Create indexes for Images table
CREATE UNIQUE INDEX IF NOT EXISTS IX_tbl_images_RelativePath ON tbl_images (RelativePath);
CREATE INDEX IF NOT EXISTS IX_tbl_images_FileHash ON tbl_images (FileHash);
CREATE INDEX IF NOT EXISTS IX_tbl_images_DateCreated ON tbl_images (DateCreated);
CREATE INDEX IF NOT EXISTS IX_tbl_images_IsDeleted ON tbl_images (IsDeleted);

-- Create ImageMetadata table - Information that requires opening the file
-- Uses same primary key as Images table (one-to-one relationship)
CREATE TABLE IF NOT EXISTS tbl_image_metadata (
    ImageId INTEGER PRIMARY KEY,
    Width INTEGER,
    Height INTEGER,
    DateTaken DATETIME,
    
    FOREIGN KEY (ImageId) REFERENCES tbl_images(ImageId) ON DELETE CASCADE
);

-- Create ImageAnalysis table - AI analysis results
-- Uses same primary key as Images table (one-to-one relationship)
CREATE TABLE IF NOT EXISTS tbl_image_analysis (
    ImageId INTEGER PRIMARY KEY,
    ImageCategory TEXT,
    PhotoSubcategory TEXT,
    AIAnalysisJson TEXT,
    AIDescription TEXT,
    AIAnalyzedAt DATETIME,
    AIModelUsed TEXT,
    AIKeywords TEXT,
    
    FOREIGN KEY (ImageId) REFERENCES tbl_images(ImageId) ON DELETE CASCADE
);

-- Create index for ImageAnalysis
CREATE INDEX IF NOT EXISTS IX_tbl_image_analysis_ImageCategory ON tbl_image_analysis (ImageCategory);

-- Create AppSettings table (singleton)
CREATE TABLE IF NOT EXISTS tbl_app_settings (
    Id INTEGER PRIMARY KEY,
    
    -- Feature Toggles
    EnableDuplicateDetection INTEGER,
    EnableAIImageAnalysis INTEGER,
    
    -- Directory Settings
    PhotoDirectory TEXT,
    AutoScanOnStartup INTEGER,
    ScanSubdirectories INTEGER,
    
    -- File Type Support
    SupportJpeg INTEGER,
    SupportPng INTEGER,
    SupportHeic INTEGER,
    SupportGif INTEGER,
    SupportBmp INTEGER,
    SupportWebp INTEGER,
    
    -- Performance Settings
    BatchSize INTEGER,
    MaxConcurrentTasks INTEGER,
    
    -- AI Settings
    AIProvider TEXT,
    AIApiKey TEXT,
    AIApiEndpoint TEXT,
    AIModel TEXT,
    AITemperature REAL,
    
    -- Other Settings
    ThemeName TEXT,
    DateCreated DATETIME NOT NULL,
    DateModified DATETIME NOT NULL,
    LastScanDate DATETIME
);

-- Insert minimal settings record
INSERT INTO tbl_app_settings (
    Id, DateCreated, DateModified
) VALUES (
    1, 
    datetime('now'),
    datetime('now')
);
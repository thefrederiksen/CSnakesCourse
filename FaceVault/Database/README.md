# FaceVault Database

This directory contains the database schema and tools for FaceVault.

## Files

- **`DatabaseVersion_001.sql`** - Initial database schema (version 1)
- **`SCHEMA.md`** - Detailed documentation of all tables and relationships  
- **`GenerateEFModels.bat`** - Windows batch script to generate EF Core models
- **`GenerateEFModels.ps1`** - PowerShell script to generate EF Core models (recommended)
- **`dev_facevault.db`** - Development database (created by running database_manager.py)

## Quick Start

### 1. Create/Update Database
```bash
cd Python
python database_manager.py
```

### 2. Generate EF Core Models

**Method 1: Visual Studio Package Manager Console (Recommended)**
1. Open Visual Studio
2. Open the FaceVault project (`FaceVault.sln` or `FaceVault.csproj`)
3. Tools → NuGet Package Manager → Package Manager Console
4. **Important**: Make sure "FaceVault" is selected in the Default project dropdown
5. **Important**: The console should show `PM> ` - you don't need to navigate to any directory
6. Paste this command and press Enter:

```powershell
Scaffold-DbContext "Data Source=Database\dev_facevault.db" Microsoft.EntityFrameworkCore.Sqlite -OutputDir Models -ContextDir Data -Context FaceVaultDbContext -Force -NoPluralize -UseDatabaseNames
```

**⚠️ Console Location**: The Package Manager Console automatically uses the project root as working directory. You don't need to `cd` anywhere.

**Method 2: Command Line (if you have dotnet-ef installed)**
```bash
# Run from the FaceVault project root directory
dotnet ef dbcontext scaffold "Data Source=Database\dev_facevault.db" Microsoft.EntityFrameworkCore.Sqlite --output-dir Models --context-dir Data --context FaceVaultDbContext --force --no-pluralize --use-database-names
```

## Database Versioning System

- **Version-based migrations**: `DatabaseVersion_001.sql`, `DatabaseVersion_002.sql`, etc.
- **Automatic version tracking**: `tbl_version` table tracks current schema version
- **Incremental updates**: Only runs scripts newer than current version
- **Safe to re-run**: Won't re-apply existing migrations

## Development Workflow

1. **Make schema changes**: Create new `DatabaseVersion_XXX.sql` file
2. **Update database**: Run `python database_manager.py` 
3. **Regenerate models**: Run `GenerateEFModels.ps1`
4. **Update code**: Modify your application to use new schema

## Database Structure

Current version includes these tables:
- `tbl_images` - Core image file information
- `tbl_image_metadata` - Image dimensions, EXIF data
- `tbl_image_analysis` - AI analysis results  
- `tbl_app_settings` - Application configuration
- `tbl_version` - Schema version tracking

See `SCHEMA.md` for detailed table documentation.

## Production vs Development

- **Development**: Use `dev_facevault.db` in this directory
- **Production**: Database created in user's AppData folder by application
- **Same schema**: Both use identical SQL scripts for consistency
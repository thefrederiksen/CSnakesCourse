# Removed Files During Database Schema Migration

This document tracks all files removed to get the project compiling after the database schema change.

## Files Removed

### Pages Removed:
- [ ] DatabaseScan.razor - Database scanning page
- [ ] DatabaseStatus.razor + .cs - Database status information
- [ ] Duplicates.razor + .cs - Duplicate management page  
- [ ] ErrorLog.razor - Error log viewer
- [ ] Index.razor + .cs - Home/Memory page (main dashboard)
- [ ] Logs.razor - System logs viewer
- [ ] PhotoScan.razor + .cs - Photo scanning interface
- [ ] Report.razor + .cs - Library reporting page
- [ ] ScreenshotTest.razor + .cs - Screenshot testing page
- [ ] Settings.razor - Application settings page

### Pages Kept:
- ✅ Error.cshtml + .cs - Error page (ASP.NET Core default)
- ✅ _Host.cshtml - Blazor host page

### Services Removed:
- [ ] Most services that depend on old models
- [ ] Screenshot detection services
- [ ] Photo scanning services  
- [ ] Memory services
- [ ] Report generation services

### Services Kept:
- ✅ Logger.cs - Logging service
- ✅ PathService.cs - Path management
- ✅ DatabaseInitializationService.cs - Database setup

## Restoration Plan

1. **Phase 1**: Get basic project compiling
2. **Phase 2**: Restore core services (Settings, Database)
3. **Phase 3**: Restore basic pages (Settings, Database Status)
4. **Phase 4**: Restore scanning functionality (Photo Scan, Database Scan)
5. **Phase 5**: Restore advanced features (Duplicates, Reports, Memory)

## Notes

- All removed files are backed up in `Backup_RemovedFiles/`
- New EF models are in `Models/` directory
- Database schema is now `tbl_*` prefixed tables
- Need to update all database access code to use new models

## Next Steps

1. Create simple Hello World home page
2. Fix Program.cs registration issues
3. Get project compiling
4. Restore files one by one, fixing as we go
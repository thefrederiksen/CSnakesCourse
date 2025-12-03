# CSnakes Course - Lab 15: Deployment

## Overview

This lab covers deployment scenarios for CSnakes applications, filling gaps in the official documentation.

## Learning Objectives

- Understand how CSnakes downloads and caches Python
- Configure `CSNAKES_REDIST_CACHE` for different deployment scenarios
- Deploy to Windows Services, IIS, Azure, and Docker
- Validate and repair corrupt Python cache
- Handle offline deployment scenarios

## Documentation

| Document | Description |
|----------|-------------|
| [01_Deployment_Overview.md](./docs/01_Deployment_Overview.md) | Complete deployment guide |
| [02_Cache_Validation.md](./docs/02_Cache_Validation.md) | Cache validation and repair |
| 03_Windows_Service_Deployment.md | (TODO) Windows Service details |
| 04_Azure_Deployment.md | (TODO) Azure App Service and Functions |
| 05_Offline_Deployment.md | (TODO) Deploying without internet |

## Key Concepts

### Python Cache Location

Default: `%APPDATA%\CSnakes\python{version}\`

Override with: `CSNAKES_REDIST_CACHE` environment variable

### Critical for Services

Windows Services often lack user profiles. Set `CSNAKES_REDIST_CACHE` to an accessible location like `C:\ProgramData\CSnakes`.

### Cache Validation

CSnakes does NOT validate cache integrity. A corrupt cache causes cryptic errors. Use the validation scripts in the docs to check and repair.

## Quick Reference

### Check if cache exists
```powershell
Test-Path "$env:APPDATA\CSnakes\python3.12.9\python\install\Lib\encodings"
```

### Clear cache (forces re-download)
```powershell
Remove-Item -Recurse -Force "$env:APPDATA\CSnakes\python3.12*"
```

### Set custom cache location
```powershell
[System.Environment]::SetEnvironmentVariable('CSNAKES_REDIST_CACHE', 'C:\ProgramData\CSnakes', 'Machine')
```

## TODO

- [ ] Add sample code demonstrating cache validation
- [ ] Add Windows Service deployment example
- [ ] Add Azure deployment example
- [ ] Add Docker deployment example
- [ ] Test and document Azure Functions compatibility
- [ ] Discuss with Anthony Baloney (CSnakes author) for additional guidance

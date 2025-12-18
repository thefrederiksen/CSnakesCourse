# CSnakes Deployment Guide

## Overview

This document covers deployment scenarios for CSnakes applications, including cache configuration, service account considerations, and platform-specific guidance.

**IMPORTANT**: CSnakes deployment documentation is currently sparse. This guide fills in the gaps based on research and testing.

---

## How CSnakes Handles Python at Runtime

### FromRedistributable() Behavior

When you use `FromRedistributable()`:

1. **First run**: Downloads Python from [python-build-standalone](https://github.com/astral-sh/python-build-standalone) (~50-80MB)
2. **Caches locally**: Stores in user's application data folder
3. **Subsequent runs**: Uses cached version (no download)

### Default Cache Location

| Platform | Default Location |
|----------|-----------------|
| Windows | `%APPDATA%\CSnakes\python{version}\` |
| macOS | `~/Library/Application Support/CSnakes/python{version}/` |
| Linux | `~/.local/share/CSnakes/python{version}/` |

**Example (Windows):**
```
C:\Users\soren\AppData\Roaming\CSnakes\python3.12.9\python\install\
```

---

## CSNAKES_REDIST_CACHE Environment Variable

### Purpose

Override the default cache location. Critical for:
- Windows Services (no user profile)
- Shared/controlled cache locations
- Network or mapped drive storage
- Docker containers

### How to Set

**Windows - System Level (all users):**
```powershell
[System.Environment]::SetEnvironmentVariable('CSNAKES_REDIST_CACHE', 'C:\ProgramData\CSnakes', 'Machine')
```

**Windows - User Level:**
```powershell
[System.Environment]::SetEnvironmentVariable('CSNAKES_REDIST_CACHE', 'D:\MyApp\PythonCache', 'User')
```

**Windows - Process Level (app.config or launchSettings.json):**
```json
{
  "profiles": {
    "MyApp": {
      "environmentVariables": {
        "CSNAKES_REDIST_CACHE": "C:\\ProgramData\\CSnakes"
      }
    }
  }
}
```

**Linux/macOS:**
```bash
export CSNAKES_REDIST_CACHE=/opt/csnakes/cache
```

### Permissions Required

The account running the application needs:
- **Read** access to use cached Python
- **Write** access on first run (to download and extract)

---

## Deployment Scenarios

### 1. Console Application (User Context)

**Simplest scenario** - works out of the box.

```csharp
builder.Services
    .WithPython()
    .WithHome(Environment.CurrentDirectory)
    .FromRedistributable("3.12");
```

- Cache: `%APPDATA%\CSnakes\`
- First run downloads Python
- No special configuration needed

---

### 2. Windows Service

**Problem**: Services often run as LOCAL SYSTEM, NETWORK SERVICE, or custom service accounts that may not have a user profile.

**Default APPDATA for service accounts:**

| Account | APPDATA Path | Issue |
|---------|-------------|-------|
| LOCAL SYSTEM | `C:\Windows\System32\config\systemprofile\AppData\Roaming` | May not exist |
| NETWORK SERVICE | `C:\Windows\ServiceProfiles\NetworkService\AppData\Roaming` | May not exist |
| LOCAL SERVICE | `C:\Windows\ServiceProfiles\LocalService\AppData\Roaming` | May not exist |

**Solution**: Set `CSNAKES_REDIST_CACHE` to an accessible location.

**Option A: System environment variable**
```powershell
[System.Environment]::SetEnvironmentVariable('CSNAKES_REDIST_CACHE', 'C:\ProgramData\CSnakes', 'Machine')
```

**Option B: Service-specific configuration**
In your service installer or configuration, set the environment variable for the service process.

**Option C: Pre-populate cache**
1. Run application as regular user first
2. Copy `%APPDATA%\CSnakes\` to `C:\ProgramData\CSnakes\`
3. Set `CSNAKES_REDIST_CACHE=C:\ProgramData\CSnakes`
4. Grant service account read access

---

### 3. IIS / ASP.NET Application

**Problem**: IIS Application Pool identity (e.g., `IIS APPPOOL\MyApp`) has limited profile access.

**Solution**:
1. Set `CSNAKES_REDIST_CACHE` in system environment variables
2. Or set in `web.config`:
```xml
<configuration>
  <system.webServer>
    <aspNetCore>
      <environmentVariables>
        <environmentVariable name="CSNAKES_REDIST_CACHE" value="C:\ProgramData\CSnakes" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

3. Grant IIS AppPool identity read/write to cache folder

---

### 4. Azure App Service

**Considerations**:
- File system is mostly read-only except for `D:\home` and `D:\local`
- No persistent storage between restarts (unless using mounted storage)
- First cold start will download Python (~50-80MB)

**Options**:

**Option A: Accept cold start delay**
- Python downloads on first request
- Subsequent requests use cached version
- Cache lost on instance recycle

**Option B: Use deployment slots for warming**
- Deploy to staging slot
- Warm up (trigger Python download)
- Swap to production

**Option C: Bundle Python with deployment**
- Include Python in your deployment package
- Use `FromFolder()` instead of `FromRedistributable()`
- No runtime download needed

---

### 5. Azure Functions

**Challenges**:
- Consumption plan: Cold starts, no persistent storage
- Premium plan: Better, but still cold start considerations
- File system restrictions

**Recommendation**:
- Use Premium plan for Python-heavy workloads
- Consider bundling Python with deployment
- May need to use `FromFolder()` with pre-deployed Python

**TODO**: Test and document Azure Functions deployment in detail.

---

### 6. Docker Containers

**Best approach**: Use CSnakes.Stage to pre-build Python environment.

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN dotnet tool install -g CSnakes.Stage
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN setup-python --python 3.12 --venv /app/venv --pip-requirements /src/requirements.txt

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY --from=build /root/.local/share/CSnakes /root/.local/share/CSnakes
COPY --from=build /app/venv /app/venv
```

**Advantages**:
- No runtime download
- Consistent environment
- Smaller final image (if multi-stage)

---

## Cache Management

### Cache Structure

```
{CACHE_ROOT}\
+-- python3.12.9\
|   +-- python\
|       +-- install\
|           +-- DLLs\
|           +-- Lib\
|           +-- python.exe
|           +-- python312.dll
|           +-- ...
+-- python3.11.11\
|   +-- ...
+-- python3.13.2\
    +-- ...
```

### Validating Cache Integrity

See: [Cache Validation Guide](./02_Cache_Validation.md)

A valid cache must contain:
- `Lib/` folder (with `encodings/` subfolder)
- `DLLs/` folder
- `python3.dll`
- `python{version}.dll` (e.g., `python312.dll`)

### Clearing Cache

**Clear specific version:**
```powershell
Remove-Item -Recurse -Force "$env:APPDATA\CSnakes\python3.12.9"
```

**Clear all cached Python versions:**
```powershell
Remove-Item -Recurse -Force "$env:APPDATA\CSnakes"
```

Next run will re-download Python.

### Cache Size

Each Python version: ~100-150MB extracted

Plan disk space accordingly if caching multiple versions.

---

## Troubleshooting

### "ModuleNotFoundError: No module named 'encodings'"

**Cause**: Corrupt or incomplete cache.

**Fix**:
1. Delete the cache folder for that Python version
2. Run application again (will re-download)

### "Python not found" errors

**Cause**: Cache location inaccessible or doesn't exist.

**Fix**:
1. Check `CSNAKES_REDIST_CACHE` is set correctly
2. Verify account has read/write permissions
3. Check disk space

### PYTHONPATH/PYTHONHOME conflicts

**Cause**: System environment variables pointing to different Python.

**Fix**:
1. Remove or clear `PYTHONPATH` and `PYTHONHOME` environment variables
2. CSnakes manages its own paths

---

## Next Steps

- [02_Cache_Validation.md](./02_Cache_Validation.md) - Validating cache integrity
- [03_Windows_Service_Deployment.md](./03_Windows_Service_Deployment.md) - Detailed Windows Service guide
- [04_Azure_Deployment.md](./04_Azure_Deployment.md) - Azure-specific deployment
- [05_Offline_Deployment.md](./05_Offline_Deployment.md) - Deploying without internet access

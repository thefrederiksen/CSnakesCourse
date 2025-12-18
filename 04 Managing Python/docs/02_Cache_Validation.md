# CSnakes Cache Validation

## The Problem

CSnakes does NOT validate that the Python cache is complete or valid. It only checks if the cache folder exists.

If the cache is corrupted (e.g., missing `Lib` folder), you get cryptic errors:
```
Fatal Python error: init_fs_encoding: failed to get the Python codec of the filesystem encoding
ModuleNotFoundError: No module named 'encodings'
```

This error provides no indication that the cache is the problem.

---

## What a Valid Cache Looks Like

### Required Components

| Component | Path | Why Critical |
|-----------|------|--------------|
| Lib folder | `{cache}/Lib/` | Contains Python standard library |
| encodings module | `{cache}/Lib/encodings/` | First module loaded at startup |
| DLLs folder | `{cache}/DLLs/` | Extension modules |
| python3.dll | `{cache}/python3.dll` | Core runtime |
| python{ver}.dll | `{cache}/python312.dll` | Version-specific runtime |

### Complete vs Incomplete Cache

**Complete (valid):**
```
python3.12.9\python\install\
+-- DLLs\                    [OK]
+-- include\                 [OK]
+-- Lib\                     [OK - CRITICAL]
|   +-- encodings\           [OK - CRITICAL]
+-- libs\                    [OK]
+-- Scripts\                 [OK]
+-- tcl\                     [OK]
+-- LICENSE.txt              [OK]
+-- python.exe               [OK]
+-- python3.dll              [OK - CRITICAL]
+-- python312.dll            [OK - CRITICAL]
+-- vcruntime140.dll         [OK]
```

**Incomplete (invalid):**
```
python3.12.9\python\install\
+-- DLLs\                    [OK]
+-- python3.dll              [OK]
+-- python312.dll            [OK]
+-- vcruntime140.dll         [OK]
                             [MISSING: Lib, python.exe, etc.]
```

---

## Manual Cache Validation

### PowerShell Script

```powershell
# Check CSnakes cache for Python 3.12
$version = "3.12.9"
$cacheRoot = $env:CSNAKES_REDIST_CACHE
if (-not $cacheRoot) {
    $cacheRoot = "$env:APPDATA\CSnakes"
}
$cachePath = Join-Path $cacheRoot "python$version\python\install"

Write-Host "Checking cache at: $cachePath"
Write-Host ""

# Required components
$required = @(
    "Lib",
    "Lib\encodings",
    "DLLs",
    "python3.dll"
)

$versionDll = "python" + ($version -replace '\.', '').Substring(0,3) + ".dll"
$required += $versionDll

$valid = $true
foreach ($component in $required) {
    $path = Join-Path $cachePath $component
    if (Test-Path $path) {
        Write-Host "[OK] $component"
    } else {
        Write-Host "[MISSING] $component" -ForegroundColor Red
        $valid = $false
    }
}

Write-Host ""
if ($valid) {
    Write-Host "Cache is VALID" -ForegroundColor Green
} else {
    Write-Host "Cache is INVALID - delete and re-run application" -ForegroundColor Red
    Write-Host "To delete: Remove-Item -Recurse -Force '$cachePath'"
}
```

### Quick Check (One-liner)

```powershell
Test-Path "$env:APPDATA\CSnakes\python3.12.9\python\install\Lib\encodings"
```

Returns `True` if cache is likely valid, `False` if corrupt.

---

## Programmatic Validation (C#)

### Simple Validation

```csharp
public static bool IsCacheValid(string version)
{
    var cacheRoot = Environment.GetEnvironmentVariable("CSNAKES_REDIST_CACHE")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CSnakes");

    // Version format: "3.12" -> folder might be "python3.12.9"
    var versionFolder = Directory.GetDirectories(cacheRoot, $"python{version}*")
        .FirstOrDefault();

    if (versionFolder == null)
        return false;

    var installPath = Path.Combine(versionFolder, "python", "install");

    // Check critical components
    var libPath = Path.Combine(installPath, "Lib");
    var encodingsPath = Path.Combine(installPath, "Lib", "encodings");
    var dllsPath = Path.Combine(installPath, "DLLs");

    return Directory.Exists(libPath)
        && Directory.Exists(encodingsPath)
        && Directory.Exists(dllsPath);
}
```

### Usage Before CSnakes Init

```csharp
const string PythonVersion = "3.12";

if (!IsCacheValid(PythonVersion))
{
    Console.WriteLine("CSnakes Python cache is invalid or missing.");
    Console.WriteLine("The application will download Python on first run.");
    // Optionally: delete corrupt cache to force clean download
}

// Proceed with CSnakes initialization
builder.Services
    .WithPython()
    .WithHome(pythonHome)
    .FromRedistributable(PythonVersion);
```

---

## Repairing Corrupt Cache

### Option 1: Delete and Re-download

```csharp
public static void RepairCache(string version)
{
    var cacheRoot = Environment.GetEnvironmentVariable("CSNAKES_REDIST_CACHE")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CSnakes");

    var versionFolders = Directory.GetDirectories(cacheRoot, $"python{version}*");

    foreach (var folder in versionFolders)
    {
        Console.WriteLine($"Deleting corrupt cache: {folder}");
        Directory.Delete(folder, recursive: true);
    }

    Console.WriteLine("Cache cleared. Python will be re-downloaded on next run.");
}
```

### Option 2: Manual Deletion

```powershell
# Delete Python 3.12 cache
Remove-Item -Recurse -Force "$env:APPDATA\CSnakes\python3.12*"

# Or delete all CSnakes cache
Remove-Item -Recurse -Force "$env:APPDATA\CSnakes"
```

---

## Why Cache Corruption Happens

### Known Causes

1. **Interrupted download** - Network failure during download
2. **Interrupted extraction** - Process killed while extracting tar.zst
3. **Disk full** - Extraction fails partway through
4. **Antivirus** - Files quarantined during extraction
5. **Permission changes** - Folder made read-only after partial extraction
6. **Multiple processes** - Race condition if two apps try to download simultaneously

### Why CSnakes Doesn't Self-Heal

CSnakes only checks:
```csharp
if (Directory.Exists(folder))
    return; // Assumes valid
```

No verification that:
- Required files exist
- Files are correct size
- Checksums match
- Python actually runs

---

## Recommendations

### For Development

1. If you see "encodings" errors, delete the cache folder
2. Check cache validity after updating CSnakes version
3. Keep only one Python version cached if disk space is limited

### For Production

1. **Validate cache at startup** before initializing CSnakes
2. **Log cache status** for troubleshooting
3. **Auto-repair** by deleting corrupt cache (will re-download)
4. **Pre-populate cache** for faster deployment (copy from known-good source)

### For CI/CD

1. **Don't cache between builds** unless you validate integrity
2. **Use CSnakes.Stage** for Docker to pre-create environment
3. **Bundle Python** with deployment for offline scenarios

---

## Future: CSnakes.Extensions.CacheValidator

See PRD in `D:\ReposFred\CSnakes.Extensions\docs\PRD_CacheValidation.md` for a proposed extension that provides:

- `CSnakesCacheValidator.ValidateCache(version)` - Check cache integrity
- `CSnakesCacheValidator.RepairCache(version)` - Delete corrupt cache
- `WithCacheValidation()` - Extension method for CSnakes builder

@echo off
REM Batch script to run FaceVault Python tests on Windows

echo FaceVault Python Test Runner
echo =============================

REM Check if Python is available
python --version >nul 2>&1
if errorlevel 1 (
    echo Error: Python is not installed or not in PATH
    exit /b 1
)

REM Change to the Python directory
cd /d "%~dp0"

REM Run the Python test script with all arguments
python run_tests.py %*

REM Exit with the same error code as the Python script
exit /b %errorlevel%
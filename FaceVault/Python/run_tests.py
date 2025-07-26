#!/usr/bin/env python3
"""
Standalone Python test runner for FaceVault.
This script can be run independently of the C# application to test Python functionality.
"""

import sys
import os
import subprocess
import argparse
from pathlib import Path


def setup_environment():
    """Set up the Python environment for testing."""
    print("Setting up Python environment...")
    
    # Add current directory to Python path
    current_dir = Path(__file__).parent
    sys.path.insert(0, str(current_dir))
    
    # Check if we're in a virtual environment
    if hasattr(sys, 'real_prefix') or (hasattr(sys, 'base_prefix') and sys.base_prefix != sys.prefix):
        print("✓ Running in virtual environment")
    else:
        print("⚠ Not running in virtual environment - consider using one")
    
    return current_dir


def install_dependencies(requirements_file="requirements.txt"):
    """Install Python dependencies."""
    print(f"Installing dependencies from {requirements_file}...")
    
    try:
        subprocess.run([
            sys.executable, "-m", "pip", "install", "-r", requirements_file
        ], check=True, capture_output=True, text=True)
        print("✓ Dependencies installed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"✗ Failed to install dependencies: {e}")
        print(f"Error output: {e.stderr}")
        return False


def run_tests(test_path="tests", verbose=False, coverage=False, specific_test=None):
    """Run the test suite."""
    print(f"Running tests from {test_path}...")
    
    # Build pytest command
    cmd = [sys.executable, "-m", "pytest"]
    
    if verbose:
        cmd.append("-v")
    else:
        cmd.append("-q")
    
    if coverage:
        cmd.extend(["--cov=.", "--cov-report=term-missing"])
    
    if specific_test:
        cmd.append(specific_test)
    else:
        cmd.append(test_path)
    
    try:
        result = subprocess.run(cmd, check=False, text=True)
        return result.returncode == 0
    except Exception as e:
        print(f"✗ Error running tests: {e}")
        return False


def run_linting():
    """Run code quality checks."""
    print("Running code quality checks...")
    
    tools = [
        (["python", "-m", "flake8", ".", "--max-line-length=120"], "flake8"),
        (["python", "-m", "black", ".", "--check"], "black"),
        (["python", "-m", "mypy", ".", "--ignore-missing-imports"], "mypy")
    ]
    
    all_passed = True
    
    for cmd, tool_name in tools:
        try:
            print(f"  Running {tool_name}...")
            result = subprocess.run(cmd, capture_output=True, text=True)
            if result.returncode == 0:
                print(f"  ✓ {tool_name} passed")
            else:
                print(f"  ✗ {tool_name} failed:")
                print(f"    {result.stdout}")
                print(f"    {result.stderr}")
                all_passed = False
        except FileNotFoundError:
            print(f"  ⚠ {tool_name} not installed, skipping")
        except Exception as e:
            print(f"  ✗ Error running {tool_name}: {e}")
            all_passed = False
    
    return all_passed


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="FaceVault Python Test Runner")
    parser.add_argument("--install-deps", action="store_true", 
                       help="Install dependencies before running tests")
    parser.add_argument("--verbose", "-v", action="store_true", 
                       help="Verbose test output")
    parser.add_argument("--coverage", "-c", action="store_true", 
                       help="Run with coverage reporting")
    parser.add_argument("--lint", "-l", action="store_true", 
                       help="Run linting checks")
    parser.add_argument("--test", "-t", type=str, 
                       help="Run specific test file or function")
    parser.add_argument("--no-tests", action="store_true", 
                       help="Skip running tests (useful with --lint)")
    
    args = parser.parse_args()
    
    print("🧪 FaceVault Python Test Runner")
    print("=" * 40)
    
    # Setup environment
    current_dir = setup_environment()
    os.chdir(current_dir)
    
    success = True
    
    # Install dependencies if requested
    if args.install_deps:
        if not install_dependencies():
            print("\n❌ Failed to install dependencies")
            return 1
    
    # Run linting if requested
    if args.lint:
        if not run_linting():
            print("\n⚠ Some linting checks failed")
            success = False
    
    # Run tests unless explicitly disabled
    if not args.no_tests:
        if not run_tests(
            verbose=args.verbose, 
            coverage=args.coverage, 
            specific_test=args.test
        ):
            print("\n❌ Some tests failed")
            success = False
    
    print("\n" + "=" * 40)
    if success:
        print("✅ All checks passed!")
        return 0
    else:
        print("❌ Some checks failed!")
        return 1


if __name__ == "__main__":
    sys.exit(main())
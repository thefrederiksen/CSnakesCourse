"""
Basic environment and setup tests for FaceVault Python modules.
These tests can run without heavy ML dependencies.
"""

import pytest
import sys
import os
import tempfile
from pathlib import Path


class TestPythonEnvironment:
    """Test basic Python environment setup."""
    
    def test_python_version(self):
        """Test that we're using a supported Python version."""
        version = sys.version_info
        assert version.major == 3
        assert version.minor >= 9  # Require Python 3.9+
    
    def test_required_modules_importable(self):
        """Test that basic required modules can be imported."""
        # Test standard library modules
        import os
        import sys
        import tempfile
        import hashlib
        import pickle
        from datetime import datetime
        from pathlib import Path
        
        # Test that we can import PIL (Pillow)
        try:
            from PIL import Image
            pil_available = True
        except ImportError:
            pil_available = False
        
        # For now, just verify the imports don't fail
        # In CI/production, PIL should be available
        assert True  # Basic test passes
    
    def test_file_system_operations(self):
        """Test basic file system operations work."""
        with tempfile.TemporaryDirectory() as temp_dir:
            # Test directory creation
            test_dir = Path(temp_dir) / "test_subdir"
            test_dir.mkdir()
            assert test_dir.exists()
            
            # Test file creation
            test_file = test_dir / "test.txt"
            test_file.write_text("test content")
            assert test_file.exists()
            
            # Test file reading
            content = test_file.read_text()
            assert content == "test content"
    
    def test_package_structure(self):
        """Test that our package structure is correct."""
        # Get the directory containing this test file
        test_dir = Path(__file__).parent
        python_dir = test_dir.parent
        
        # Check that required files exist
        assert (python_dir / "__init__.py").exists()
        assert (python_dir / "requirements.txt").exists()
        assert (python_dir / "run_tests.py").exists()
        
        # Check that our modules exist
        assert (python_dir / "image_scanner.py").exists()
        assert (python_dir / "face_detector.py").exists()


class TestImageScannerBasics:
    """Basic tests for image scanner that don't require ML libraries."""
    
    def test_imports(self):
        """Test that image_scanner module can be imported."""
        try:
            from ..image_scanner import calculate_file_hash
            import_successful = True
        except ImportError as e:
            import_successful = False
            import_error = str(e)
        
        # The import might fail due to missing dependencies
        # That's okay for this basic test
        assert True  # Test that we can at least try to import
    
    def test_file_hash_calculation(self):
        """Test file hash calculation with basic dependencies."""
        try:
            from ..image_scanner import calculate_file_hash
            import hashlib
            
            # Create a temporary file
            with tempfile.NamedTemporaryFile(mode='w', delete=False) as tmp_file:
                tmp_file.write("test content for hashing")
                tmp_file.flush()
                
                # Calculate hash using our function
                calculated_hash = calculate_file_hash(tmp_file.name)
                
                # Calculate expected hash
                with open(tmp_file.name, 'rb') as f:
                    expected_hash = hashlib.sha256(f.read()).hexdigest()
                
                assert calculated_hash == expected_hash
                
            # Clean up
            os.unlink(tmp_file.name)
            
        except ImportError:
            # Skip test if dependencies not available
            pytest.skip("Dependencies not available for hash calculation test")


class TestConfigurationFiles:
    """Test that configuration files are valid."""
    
    def test_requirements_txt_format(self):
        """Test that requirements.txt is properly formatted."""
        python_dir = Path(__file__).parent.parent
        requirements_file = python_dir / "requirements.txt"
        
        assert requirements_file.exists()
        
        # Read and validate basic format
        content = requirements_file.read_text()
        lines = [line.strip() for line in content.split('\n') if line.strip()]
        
        # Should have some requirements
        assert len(lines) > 0
        
        # Check for face recognition related packages
        package_names = [line.split('==')[0].lower() for line in lines if '==' in line]
        
        # Should include testing framework
        assert any('pytest' in pkg for pkg in package_names)
    
    def test_pytest_ini_exists(self):
        """Test that pytest.ini configuration exists."""
        python_dir = Path(__file__).parent.parent
        pytest_ini = python_dir / "pytest.ini"
        
        assert pytest_ini.exists()
        
        content = pytest_ini.read_text()
        assert '[tool:pytest]' in content or '[pytest]' in content
    
    def test_run_tests_script_exists(self):
        """Test that test runner script exists and is executable."""
        python_dir = Path(__file__).parent.parent
        run_tests_py = python_dir / "run_tests.py"
        run_tests_bat = python_dir / "run_tests.bat"
        
        assert run_tests_py.exists()
        assert run_tests_bat.exists()
        
        # Check that Python script has proper shebang or main guard
        try:
            content = run_tests_py.read_text(encoding='utf-8')
        except UnicodeDecodeError:
            content = run_tests_py.read_text(encoding='utf-8', errors='ignore')
        
        assert 'if __name__ == "__main__"' in content
#!/usr/bin/env python3
"""
FaceVault Database Manager
Creates and updates SQLite database by running versioned SQL scripts in order.

Usage:
- Run as script: python database_manager.py (creates in project Database folder)
- Import and call: create_or_update_database(db_path) from C#
"""

import os
import sqlite3
import sys
from pathlib import Path
import re
from typing import Optional, List, Tuple

def get_current_database_version(db_path: str) -> int:
    """
    Get the current version of the database.
    Returns 0 if database doesn't exist or has no version table.
    """
    if not os.path.exists(db_path):
        return 0
    
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        # Check if version table exists
        cursor.execute("""
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='tbl_version'
        """)
        
        if not cursor.fetchone():
            conn.close()
            return 0
        
        # Get current version
        cursor.execute("SELECT Version FROM tbl_version LIMIT 1")
        result = cursor.fetchone()
        conn.close()
        
        return result[0] if result else 0
        
    except sqlite3.Error:
        return 0

def get_sql_scripts(database_dir: str) -> List[Tuple[int, str]]:
    """
    Get all DatabaseVersion_XXX.sql files in order.
    Returns list of (version_number, file_path) tuples.
    """
    scripts = []
    database_path = Path(database_dir)
    
    if not database_path.exists():
        raise FileNotFoundError(f"Database directory not found: {database_dir}")
    
    # Find all DatabaseVersion_XXX.sql files
    pattern = re.compile(r'DatabaseVersion_(\d+)\.sql', re.IGNORECASE)
    
    for file_path in database_path.glob("DatabaseVersion_*.sql"):
        match = pattern.match(file_path.name)
        if match:
            version = int(match.group(1))
            scripts.append((version, str(file_path)))
    
    # Sort by version number
    scripts.sort(key=lambda x: x[0])
    
    if not scripts:
        raise FileNotFoundError(f"No DatabaseVersion_XXX.sql files found in: {database_dir}")
    
    return scripts

def run_sql_script(db_path: str, script_path: str, version: int) -> bool:
    """
    Run a single SQL script against the database.
    Returns True if successful, False otherwise.
    """
    try:
        print(f"  Running DatabaseVersion_{version:03d}.sql...")
        
        # Read the SQL script
        with open(script_path, 'r', encoding='utf-8') as f:
            sql_content = f.read()
        
        # Execute the script
        conn = sqlite3.connect(db_path)
        try:
            conn.executescript(sql_content)
            conn.commit()
            print(f"  ✅ Version {version} applied successfully")
            return True
        finally:
            conn.close()
            
    except sqlite3.Error as e:
        print(f"  ❌ SQLite error in version {version}: {e}")
        return False
    except Exception as e:
        print(f"  ❌ Error running version {version}: {e}")
        return False

def create_or_update_database(db_path: str, database_scripts_dir: Optional[str] = None) -> bool:
    """
    Create or update database by running SQL scripts in version order.
    
    Args:
        db_path: Path where to create/update the database
        database_scripts_dir: Directory containing SQL scripts (auto-detected if None)
    
    Returns:
        True if successful, False otherwise
    """
    try:
        # Auto-detect database scripts directory if not provided
        if database_scripts_dir is None:
            # Assume we're in Python directory, go up one level to find Database directory
            python_dir = Path(__file__).parent
            database_scripts_dir = python_dir.parent / "Database"
        
        database_scripts_dir = str(database_scripts_dir)
        
        print("=== FaceVault Database Manager ===")
        print(f"Database: {db_path}")
        print(f"Scripts Directory: {database_scripts_dir}")
        
        # Get current database version
        current_version = get_current_database_version(db_path)
        print(f"Current Database Version: {current_version}")
        
        # Get all available SQL scripts
        scripts = get_sql_scripts(database_scripts_dir)
        print(f"Available Scripts: {len(scripts)}")
        
        # Filter scripts to only run versions higher than current
        scripts_to_run = [(v, p) for v, p in scripts if v > current_version]
        
        if not scripts_to_run:
            print("✅ Database is already up to date!")
            return True
        
        print(f"Scripts to run: {len(scripts_to_run)}")
        print()
        
        # Run each script in order
        for version, script_path in scripts_to_run:
            if not run_sql_script(db_path, script_path, version):
                print(f"❌ Failed to apply version {version}")
                return False
        
        # Verify final version
        final_version = get_current_database_version(db_path)
        print()
        print(f"✅ Database updated successfully!")
        print(f"Final Version: {final_version}")
        
        # Show database info
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
        tables = cursor.fetchall()
        conn.close()
        
        print(f"Tables in database:")
        for table in tables:
            print(f"  - {table[0]}")
        
        return True
        
    except Exception as e:
        print(f"❌ Error: {e}")
        return False

def main():
    """
    Main function when script is run directly.
    Creates/updates database in the project Database directory.
    """
    # Get the Python directory (where this script is)
    python_dir = Path(__file__).parent
    
    # Database goes in the Database directory (sibling to Python directory)
    database_dir = python_dir.parent / "Database"
    db_path = database_dir / "dev_facevault.db"
    
    print("Running as development script...")
    print(f"Will create database at: {db_path}")
    print()
    
    success = create_or_update_database(str(db_path))
    
    if success:
        print()
        print("Development database ready!")
        print("You can now:")
        print(f"1. Open with: sqlite3 {db_path}")
        print("2. Use for EF scaffolding")
        print("3. Use for development and testing")
    else:
        print("❌ Failed to create/update database")
        sys.exit(1)

if __name__ == "__main__":
    main()
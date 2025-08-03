"""
CSnakes Exception Handling Demo
Demonstrates comprehensive exception handling between Python and C#
"""

import traceback
import sys
from typing import List, Dict, Any
import json
import random
import time


# ===== BASIC EXCEPTION DEMOS =====

def test_divide_by_zero():
    """
    Basic demo: Shows how Python exceptions are caught and re-raised for C#
    """
    try:
        result = 10 / 0
    except ZeroDivisionError as e:
        print(f"Python caught exception: {e}")
        # Re-raise so C# can catch it
        raise


def umtimate_question() -> str:
    """
    Type mismatch demo: Returns int despite type hint of str
    This causes InvalidCastException in C#
    """
    return 42  # Returns int, but C# expects string


def simple_division_error(a: float, b: float) -> float:
    """
    Enhanced division demo with stack trace capture
    """
    try:
        return a / b
    except ZeroDivisionError as e:
        # Capture and print the full stack trace
        tb_str = traceback.format_exc()
        print(f"Python caught error: {e}")
        print(f"Stack trace:\n{tb_str}")
        raise  # Re-raise for C# to catch


# ===== CUSTOM EXCEPTION CLASSES =====

class DataValidationError(Exception):
    """Custom exception for data validation failures"""
    def __init__(self, field_name: str, value: Any, expected_type: str):
        self.field_name = field_name
        self.value = value
        self.expected_type = expected_type
        super().__init__(f"Validation failed for field '{field_name}': expected {expected_type}, got {type(value).__name__}")


class ProcessingError(Exception):
    """Custom exception for processing failures with detailed context"""
    def __init__(self, step: str, details: Dict[str, Any], original_error: Exception = None):
        self.step = step
        self.details = details
        self.original_error = original_error
        
        message = f"Processing failed at step '{step}'"
        if details:
            message += f" - Details: {json.dumps(details)}"
        if original_error:
            message += f" - Original error: {str(original_error)}"
            
        super().__init__(message)


class NetworkTimeoutError(Exception):
    """Custom exception for network timeouts with retry information"""
    def __init__(self, url: str, timeout_seconds: float, retry_count: int):
        self.url = url
        self.timeout_seconds = timeout_seconds
        self.retry_count = retry_count
        super().__init__(f"Network timeout after {timeout_seconds}s and {retry_count} retries for URL: {url}")


# ===== ADVANCED EXCEPTION DEMOS =====

def nested_function_error():
    """
    Demonstrates exception propagation through nested function calls
    Creates a deeper stack trace for better demonstration
    """
    def level_3():
        # This will cause an IndexError
        empty_list = []
        return empty_list[10]
    
    def level_2():
        return level_3() * 2
    
    def level_1():
        return level_2() + 100
    
    try:
        return level_1()
    except IndexError as e:
        # Get detailed stack information
        exc_type, exc_value, exc_traceback = sys.exc_info()
        
        # Extract stack frames
        stack_summary = traceback.extract_tb(exc_traceback)
        
        # Create detailed error info
        error_details = {
            "error_type": exc_type.__name__,
            "error_message": str(exc_value),
            "stack_frames": []
        }
        
        for frame in stack_summary:
            error_details["stack_frames"].append({
                "filename": frame.filename,
                "line_number": frame.lineno,
                "function_name": frame.name,
                "code": frame.line
            })
        
        # Re-raise with additional context
        raise ProcessingError(
            step="nested_function_call",
            details=error_details,
            original_error=e
        )


def validate_user_data_simple(name: str, age_str: str, email: str) -> str:
    """
    Simplified validation demo that works better with CSnakes type conversion
    This demonstrates custom exceptions with simpler parameter types
    """
    # Try to convert age string to int
    try:
        age = int(age_str)
    except ValueError:
        # Use our custom exception for type mismatch
        raise DataValidationError("age", age_str, "int")
    
    # Validate age range
    if age < 0 or age > 150:
        raise ValueError(f"Age must be between 0 and 150, got {age}")
    
    # Validate email
    if "@" not in email:
        raise ValueError(f"Invalid email format: {email}")
    
    return f"Validated user: {name}, age {age}, email {email}"


def simulate_network_operation(url: str, timeout: float = 5.0) -> str:
    """
    Demonstrates custom network exception with retry logic
    """
    max_retries = 3
    retry_count = 0
    
    while retry_count < max_retries:
        try:
            # Simulate network delay
            delay = random.uniform(0.1, 10.0)
            
            if delay > timeout:
                retry_count += 1
                if retry_count >= max_retries:
                    raise NetworkTimeoutError(url, timeout, retry_count)
                print(f"Timeout on attempt {retry_count}, retrying...")
                continue
            
            time.sleep(0.1)  # Simulate actual network operation
            return f"Success: Data from {url}"
            
        except Exception as e:
            # Wrap unexpected errors
            raise ProcessingError(
                step="network_operation",
                details={"url": url, "timeout": timeout, "retry": retry_count},
                original_error=e
            )


def exception_chaining_demo(data: List[int]) -> float:
    """
    Demonstrates exception chaining where one exception causes another
    """
    try:
        # First potential error: empty list
        if not data:
            raise ValueError("Cannot calculate average of empty list")
        
        # Second potential error: sum might fail with non-numeric
        total = sum(data)
        average = total / len(data)
        
        # Third potential error: business logic
        if average < 0:
            raise ValueError("Average cannot be negative for this operation")
        
        # Fourth potential error: division by zero
        result = 100 / (average - 50)
        
        return result
        
    except ValueError as ve:
        # Chain the exception with more context
        raise ProcessingError(
            step="average_calculation",
            details={"input_data": data, "data_length": len(data) if data else 0},
            original_error=ve
        ) from ve
    except ZeroDivisionError as ze:
        # Special handling for division by zero
        raise ProcessingError(
            step="result_calculation",
            details={"average": average, "formula": "100 / (average - 50)"},
            original_error=ze
        ) from ze


def get_exception_info() -> Dict[str, Any]:
    """
    Returns detailed information about the current exception
    Can be called from C# to get Python's view of the error
    """
    exc_type, exc_value, exc_traceback = sys.exc_info()
    
    if exc_type is None:
        return {"error": "No exception currently active"}
    
    # Get the full traceback as a string
    tb_string = ''.join(traceback.format_exception(exc_type, exc_value, exc_traceback))
    
    # Get stack frames
    frames = []
    for frame in traceback.extract_tb(exc_traceback):
        frames.append({
            "file": frame.filename,
            "line": frame.lineno,
            "function": frame.name,
            "code": frame.line
        })
    
    # Get local variables from the most recent frame (be careful with sensitive data)
    locals_dict = {}
    if exc_traceback:
        tb_frame = exc_traceback.tb_frame
        for key, value in tb_frame.f_locals.items():
            try:
                # Only include simple types to avoid serialization issues
                if isinstance(value, (str, int, float, bool, list, dict, type(None))):
                    locals_dict[key] = str(value)1
            except:
                locals_dict[key] = "<unable to serialize>"
    
    return {
        "exception_type": exc_type.__name__,
        "exception_message": str(exc_value),
        "traceback_string": tb_string,
        "stack_frames": frames,
        "local_variables": locals_dict,
        "python_version": sys.version
    }


def demonstrate_all_exceptions():
    """
    Runs all exception demos and returns a summary
    This is useful for testing all exception types at once
    """
    examples = []
    
    # Example 1: Simple division error
    try:
        result = simple_division_error(10, 0)
    except ZeroDivisionError as e:
        examples.append({
            "example": "Division by Zero",
            "error": str(e),
            "type": type(e).__name__
        })
    
    # Example 2: Nested function error
    try:
        result = nested_function_error()
    except ProcessingError as e:
        examples.append({
            "example": "Nested Function Error",
            "error": str(e),
            "type": type(e).__name__,
            "details": e.details
        })
    
    # Example 3: Validation error
    try:
        result = validate_user_data_simple("John", "twenty", "john@example.com")
    except DataValidationError as e:
        examples.append({
            "example": "Data Validation Error",
            "error": str(e),
            "type": type(e).__name__,
            "field": e.field_name,
            "invalid_value": e.value
        })
    
    # Example 4: Network timeout
    try:
        result = simulate_network_operation("https://slow-api.example.com", timeout=0.5)
    except NetworkTimeoutError as e:
        examples.append({
            "example": "Network Timeout",
            "error": str(e),
            "type": type(e).__name__,
            "url": e.url,
            "retries": e.retry_count
        })
    
    return examples
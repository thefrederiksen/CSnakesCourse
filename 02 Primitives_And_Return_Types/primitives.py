# Lab 2: Primitives & Return Types
# This file demonstrates how CSnakes handles primitive type mapping between Python and C#

from typing import Optional
from datetime import datetime

def add_numbers(a: int, b: float) -> float:
    """
    Add an integer and a float, returning a float.
    Demonstrates int -> long and float -> double mapping in C#.
    """
    return a + b

def concatenate_strings(s1: str, s2: str) -> str:
    """
    Concatenate two strings.
    Demonstrates string -> string mapping in C#.
    """
    return s1 + s2

def is_positive(number: float) -> bool:
    """
    Check if a number is positive.
    Demonstrates bool -> bool mapping in C#.
    """
    return number > 0

def multiply_integers(x: int, y: int) -> int:
    """
    Multiply two integers.
    Demonstrates int -> long mapping for both input and output in C#.
    """
    return x * y

def format_greeting(name: str, age: int, height: float, is_student: bool) -> str:
    """
    Create a formatted greeting using all primitive types.
    Demonstrates multiple parameter types in a single function.
    """
    student_status = "student" if is_student else "professional"
    return f"Hello {name}! You are {age} years old, {height:.1f}m tall, and a {student_status}."

def divide_with_default(dividend: int, divisor: int = 2) -> float:
    """
    Divide two numbers with a default divisor.
    Demonstrates default parameter values and how they map to C# nullable types.
    """
    if divisor == 0:
        return float('inf')  # Return infinity for division by zero
    return dividend / divisor

def get_optional_message(include_message: bool = True) -> Optional[str]:
    """
    Return a message or None based on the parameter.
    Demonstrates Python None return values mapping to C# nullable types.
    Uses Optional[str] for compatibility with Python 3.9+.
    """
    if include_message:
        return "This is an optional message!"
    return None

def process_nullable_input(value: Optional[int] = None) -> str:
    """
    Process an input that might be None.
    Demonstrates handling nullable input parameters from C#.
    """
    if value is None:
        return "No value provided (received None)"
    return f"Received value: {value}"

def calculate_percentage(part: float, whole: float) -> float:
    """
    Calculate percentage with proper handling of edge cases.
    Demonstrates float precision and error handling.
    """
    if whole == 0:
        return 0.0
    return (part / whole) * 100.0

def validate_range(value: int, min_val: int = 0, max_val: int = 100) -> bool:
    """
    Check if a value is within a specified range.
    Demonstrates multiple default parameters and boolean logic.
    """
    return min_val <= value <= max_val

def get_current_time() -> datetime:
    return datetime.now()

def get_current_time_as_text() -> str:
    return datetime.now().isoformat()

def reverse_bytes(data: bytes) -> bytes:
    """
    Receives a bytes object, reverses the order, and returns it.
    Demonstrates bytes interop between Python and C#.
    """
    return data[::-1]

def write_to_file(filename: str, file_text: str) -> None:
    """
    Dummy method that simulates writing to a file. Does nothing and returns None.
    Used to demonstrate calling a Python void-returning method from C#.
    """
    pass


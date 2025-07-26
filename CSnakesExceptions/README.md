# Lab 8: CSnakes Exception Handling

This lab demonstrates comprehensive exception handling between Python and C#, showing both basic and advanced patterns for production-ready error management.

## Overview

Exception handling across language boundaries is crucial for robust applications. This lab covers:

### Part 1: Basic Exception Handling
- **Division by Zero** - How Python exceptions propagate to C#
- **Type Mismatches** - When Python returns unexpected types

### Part 2: Advanced Exception Handling
- **Stack Traces** - Accessing full Python stack traces from C#
- **Custom Exceptions** - Creating and handling custom Python exception types
- **Exception Chaining** - Preserving error context through multiple layers
- **Structured Error Data** - Embedding parseable information in exceptions

## Running the Lab

```bash
cd CSnakesExceptions
dotnet run
```

The program runs all demos automatically, showing:
1. Basic division by zero and type mismatch errors
2. Stack trace extraction and analysis
3. Nested function error propagation
4. Custom exception types (DataValidationError, ProcessingError, NetworkTimeoutError)
5. Exception chaining with context preservation
6. Summary of all exception types

## Key Files

- **exception_demo.py** - All Python exception examples and custom exception classes
- **Program.cs** - Comprehensive C# demo showing how to catch and handle Python exceptions

## Exception Flow

```
Python Function Called
        ↓
Exception Raised in Python
        ↓
CSnakes Runtime Catches
        ↓
Wrapped as PythonInvocationException
        ↓
Caught in C# Application
        ↓
Error Details Extracted
```

## Key Concepts

### Python Side

1. **Custom Exception Classes**
```python
class DataValidationError(Exception):
    def __init__(self, field_name: str, value: Any, expected_type: str):
        self.field_name = field_name
        self.value = value
        self.expected_type = expected_type
```

2. **Exception Chaining**
```python
except ValueError as ve:
    raise ProcessingError(...) from ve  # Preserves original exception
```

3. **Stack Trace Capture**
```python
exc_type, exc_value, exc_traceback = sys.exc_info()
stack_summary = traceback.extract_tb(exc_traceback)
```

### C# Side

1. **Catching Python Exceptions**
```csharp
try {
    pythonEnv.ExceptionDemo().SomeMethod();
} catch (PythonInvocationException ex) {
    // All Python exceptions arrive as PythonInvocationException
    Console.WriteLine($"Error: {ex.Message}");
    // Check ex.InnerException for more details
}
```

2. **Extracting Error Details**
- Parse error messages for structured data
- Use regex to extract specific fields
- Check InnerException for additional context

## Best Practices

1. **Always wrap Python calls in try-catch blocks**
2. **Log full stack traces for debugging**
3. **Create user-friendly error messages**
4. **Consider retry logic for transient errors**
5. **Test exception scenarios thoroughly**
6. **Document expected exceptions**
7. **Parse structured error data when available**
8. **Implement error translation layers**

## Sample Output

```
=== CSnakes Exception Handling Demo ===

PART 1: BASIC EXCEPTION HANDLING
================================

--------------------------------------------------
Demo 1: Basic Division by Zero
--------------------------------------------------
✓ Caught PythonInvocationException in C#
  Message: The Python runtime raised a ZeroDivisionError...
  Type: PythonInvocationException

--------------------------------------------------
Demo 2: Type Mismatch (InvalidCastException)
--------------------------------------------------
✓ Caught type mismatch exception
  ✓ Inner exception is InvalidCastException as expected

PART 2: ADVANCED EXCEPTION HANDLING
===================================

--------------------------------------------------
Demo 3: Exception with Stack Trace
--------------------------------------------------
✓ Caught exception with stack trace
Python output (includes stack trace):
  File "exception_demo.py", line 38, in simple_division_error
    return a / b
  ZeroDivisionError: float division by zero

... (continues with all demos)
```

## Production Considerations

- **Security**: Don't expose sensitive data in exceptions
- **Performance**: Exception handling has overhead
- **Logging**: Use structured logging for better searchability
- **User Experience**: Translate technical errors to user-friendly messages

## Extending the Pattern

To add your own exception handling:

1. Define custom exception classes in Python
2. Include relevant context as attributes
3. Catch PythonInvocationException in C#
4. Parse error details from the message
5. Optionally create matching C# exception types

This lab provides a complete foundation for robust error handling in CSnakes applications.
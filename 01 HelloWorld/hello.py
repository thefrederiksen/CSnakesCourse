def hello_world(name: str) -> str:
    """
    Simple greeting function demonstrating basic CSnakes integration.
    
    CSnakes Type Mapping:
    - name: str (Python) → string (C#)
    - return: str (Python) → string (C#)
    
    Called from C#: pythonEnv.Hello().HelloWorld("YourName")
    """
    return f"Hello, {name} - From Python!"
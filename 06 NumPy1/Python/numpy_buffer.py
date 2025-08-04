try:
    from collections.abc import Buffer
except ImportError:
    from typing_extensions import Buffer

import numpy as np

def example_array() -> Buffer:
    """
    Creates a NumPy boolean array and returns it as a buffer.
    
    CSnakes Type Mapping:
    - return: Buffer (Python) → IPyBuffer (C#)
    - Zero-copy memory sharing between Python and C#
    - Accessed via: buffer.AsBoolReadOnlySpan() in C#
    """
    return np.array([True, False, True, False, False], dtype=np.bool_)

def invert_array(arr: Buffer) -> Buffer:
    """
    Inverts a boolean array received from C#.
    
    CSnakes Type Mapping:
    - arr: Buffer (Python) ← IPyBuffer (C#)
    - return: Buffer (Python) → IPyBuffer (C#)
    - Demonstrates bidirectional zero-copy buffer sharing
    """
    a = np.frombuffer(arr, dtype=np.bool_).copy()
    return ~a

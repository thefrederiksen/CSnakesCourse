try:
    from collections.abc import Buffer
except ImportError:
    from typing_extensions import Buffer

import numpy as np

def example_array() -> Buffer:
    return np.array([True, False, True, False, False], dtype=np.bool_)

def invert_array(arr: Buffer) -> Buffer:
    a = np.frombuffer(arr, dtype=np.bool_).copy()
    return ~a

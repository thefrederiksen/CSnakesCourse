try:
    from collections.abc import Buffer
except ImportError:
    from typing_extensions import Buffer

import numpy as np

def example_array_2d() -> Buffer:
    return np.array([[1, 2, 3], [4, 5, 6]], dtype=np.int32)

def example_tensor() -> Buffer:
    arr = np.zeros((2, 3, 4, 5), dtype=np.int32)
    arr[0, 0, 0, 0] = 1
    arr[1, 2, 3, 4] = 3
    return arr

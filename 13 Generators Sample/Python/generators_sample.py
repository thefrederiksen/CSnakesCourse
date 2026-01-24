import time
from typing import Generator

def get_version() -> str:
    """Return the fixed version string for this code"""
    return "1.0.0"

def progress_generator() -> Generator[int, None, None]:
    """
    Yields progress from 1 to 100, one value every 100 ms.
    Usage: for progress in progress_generator(): ...
    """
    for i in range(1, 101):
        time.sleep(0.1)
        yield i


def progress_bar_with_delay() -> Generator[int, None, None]:
    """
    Synchronous generator for progress reporting with delay.

    Note: CSnakes does not support Python async generators (AsyncGenerator).
    Use sync generators and run them in C# Task.Run() for async behavior.
    This is the same pattern used in BlazorTrader production code.
    """
    for i in range(0, 101, 10):
        time.sleep(0.5)  # Simulate work
        yield i



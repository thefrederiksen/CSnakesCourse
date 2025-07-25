import asyncio
import time
from typing import Generator
from typing import AsyncGenerator

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



async def async_progress_bar() -> AsyncGenerator[int, None]:
    for i in range(0, 101, 10):
        await asyncio.sleep(0.5)
        yield i



import time
from typing import Generator

def progress_bar() -> Generator[int, None, None]:
    for i in range(0, 101, 10):
        time.sleep(0.5)  # Simulate a time-consuming step
        yield i  # Send progress to C#


import asyncio
from typing import AsyncGenerator

async def async_progress_bar() -> AsyncGenerator[int, None]:
    for i in range(0, 101, 10):
        await asyncio.sleep(0.5)
        yield i


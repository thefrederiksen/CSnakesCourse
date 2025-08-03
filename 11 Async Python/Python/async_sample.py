import asyncio


def get_version() -> str:
    """Return the fixed version string for this code"""
    return "1.0.0"

async def wait_five_seconds(name: str) -> str:
    await asyncio.sleep(5)
    return f"Done waiting, {name}!"
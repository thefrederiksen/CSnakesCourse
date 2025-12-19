"""
Ideas Tools - Function tools for the Ideas Agent

Manages ideas stored in a JSON file.
"""

import json
import os
import uuid
from datetime import datetime
from agents import function_tool


# Path to ideas storage file
IDEAS_FILE = os.path.join(os.path.dirname(__file__), "ideas.json")


def _load_ideas() -> dict:
    """Load ideas from JSON file."""
    if not os.path.exists(IDEAS_FILE):
        return {"ideas": []}
    with open(IDEAS_FILE, "r", encoding="utf-8") as f:
        return json.load(f)


def _save_ideas(data: dict) -> None:
    """Save ideas to JSON file."""
    with open(IDEAS_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)


@function_tool
def add_idea(title: str, description: str, tags: str) -> str:
    """
    Add a new idea to your collection.

    Args:
        title: Short title for the idea
        description: Detailed description of the idea
        tags: Comma-separated tags (e.g., "app, voice, productivity")

    Returns:
        Confirmation message with the idea ID
    """
    data = _load_ideas()

    idea = {
        "id": str(uuid.uuid4())[:8],
        "title": title,
        "description": description,
        "tags": [t.strip() for t in tags.split(",") if t.strip()],
        "created": datetime.now().isoformat()
    }

    data["ideas"].append(idea)
    _save_ideas(data)

    return f"Added idea '{title}' with ID {idea['id']}"


@function_tool
def list_ideas(filter_text: str = "") -> str:
    """
    List all ideas, optionally filtered.

    Args:
        filter_text: Optional text to filter by tag or title (empty for all)

    Returns:
        Formatted list of ideas
    """
    data = _load_ideas()
    ideas = data["ideas"]

    if filter_text:
        filter_lower = filter_text.lower()
        ideas = [
            i for i in ideas
            if filter_lower in i["title"].lower()
            or filter_lower in i["description"].lower()
            or any(filter_lower in tag.lower() for tag in i["tags"])
        ]

    if not ideas:
        if filter_text:
            return f"No ideas found matching '{filter_text}'"
        return "No ideas yet. Start adding some!"

    result = []
    for idea in ideas:
        tags_str = ", ".join(idea["tags"]) if idea["tags"] else "no tags"
        result.append(f"- [{idea['id']}] {idea['title']} ({tags_str})")

    return "\n".join(result)


@function_tool
def search_ideas(query: str) -> str:
    """
    Search through ideas by keyword.

    Args:
        query: Search query to find in titles and descriptions

    Returns:
        Matching ideas with relevant excerpts
    """
    data = _load_ideas()
    query_lower = query.lower()

    matches = []
    for idea in data["ideas"]:
        score = 0
        if query_lower in idea["title"].lower():
            score += 2
        if query_lower in idea["description"].lower():
            score += 1

        if score > 0:
            matches.append((score, idea))

    matches.sort(key=lambda x: x[0], reverse=True)

    if not matches:
        return f"No ideas found matching '{query}'"

    result = []
    for _, idea in matches[:5]:
        desc_preview = idea["description"][:100] + "..." if len(idea["description"]) > 100 else idea["description"]
        result.append(f"**{idea['title']}** [{idea['id']}]\n{desc_preview}")

    return "\n\n".join(result)


@function_tool
def get_idea_count() -> str:
    """
    Get the total number of ideas.

    Returns:
        Count of ideas
    """
    data = _load_ideas()
    count = len(data["ideas"])
    return f"You have {count} idea{'s' if count != 1 else ''}"

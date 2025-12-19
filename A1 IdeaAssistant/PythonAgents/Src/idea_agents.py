"""
OpenAI Agents SDK - Idea Assistant

Demonstrates:
- Triage/Router agent pattern
- Function tools
- Hosted tools (WebSearchTool)
- Handoffs between agents
- Input guardrails
"""

from agents import Agent, Runner, function_tool, WebSearchTool, GuardrailFunctionOutput, input_guardrail, RunContextWrapper
from ideas_tools import add_idea, list_ideas, search_ideas, get_idea_count


# Input guardrail to validate user messages
@input_guardrail
async def validate_input(ctx: RunContextWrapper, agent: Agent, input: str) -> GuardrailFunctionOutput:
    """Check for off-topic or inappropriate input."""
    # Simple keyword check - production would use a classifier
    off_topic_keywords = ["weather", "recipe", "sports score"]

    # Handle string or list input
    input_text = input if isinstance(input, str) else str(input)
    input_lower = input_text.lower()

    for keyword in off_topic_keywords:
        if keyword in input_lower:
            return GuardrailFunctionOutput(
                output_info={"reason": f"Off-topic: {keyword}"},
                tripwire_triggered=True
            )

    return GuardrailFunctionOutput(
        output_info={"status": "valid"},
        tripwire_triggered=False
    )


# Ideas Agent - manages your idea collection
ideas_agent = Agent(
    name="Ideas Agent",
    instructions="""You help users manage their ideas. You can:
- Add new ideas with title, description, and tags
- List all ideas or filter by tag/keyword
- Search through existing ideas
- Summarize or answer questions about their ideas

Be helpful and encouraging about their ideas.""",
    tools=[add_idea, list_ideas, search_ideas, get_idea_count]
)


# Research Agent - searches the web for inspiration
research_agent = Agent(
    name="Research Agent",
    instructions="""You help users find new ideas and inspiration by searching the web.
When users want to explore topics or find related ideas, search for relevant content.
Summarize findings clearly and suggest how they might inspire new ideas.""",
    tools=[WebSearchTool()]
)


# Triage Agent - routes to the right specialist
triage_agent = Agent(
    name="Triage Agent",
    instructions="""You route user requests to the appropriate specialist:

- For adding, listing, searching, or discussing ideas -> hand off to Ideas Agent
- For web searches, research, or finding inspiration online -> hand off to Research Agent

If the user's intent is unclear, ask a clarifying question.
Never try to handle requests yourself - always hand off to a specialist.""",
    handoffs=[ideas_agent, research_agent],
    input_guardrails=[validate_input]
)


def process_message(user_message: str, api_key: str) -> str:
    """
    Process a user message through the agent system.

    Args:
        user_message: The user's input text
        api_key: OpenAI API key

    Returns:
        The agent's response as a string
    """
    import os
    os.environ["OPENAI_API_KEY"] = api_key

    result = Runner.run_sync(triage_agent, user_message)
    return result.final_output


def process_message_with_history(
    user_message: str,
    conversation_history: list[dict[str, str]],
    api_key: str
) -> tuple[str, str]:
    """
    Process a message with conversation history.

    Args:
        user_message: The user's new message
        conversation_history: List of previous messages [{"role": "user/assistant", "content": "..."}]
        api_key: OpenAI API key

    Returns:
        Tuple of (agent_response, agent_name)
    """
    import os
    os.environ["OPENAI_API_KEY"] = api_key

    # Build messages from history
    messages = []
    for msg in conversation_history:
        messages.append({"role": msg["role"], "content": msg["content"]})
    messages.append({"role": "user", "content": user_message})

    result = Runner.run_sync(triage_agent, messages)

    # Get the name of the agent that responded
    agent_name = result.last_agent.name if result.last_agent else "Triage Agent"

    return (result.final_output, agent_name)

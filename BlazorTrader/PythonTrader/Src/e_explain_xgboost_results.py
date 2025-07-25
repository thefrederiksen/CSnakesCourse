import os
from dotenv import load_dotenv
import openai
from pathlib import Path

def explain_xgboost_results(base_directory: str = ".", openai_api_key: str = None, instructions: str = None) -> str:
    if not openai_api_key:
        raise ValueError("OPENAI_API_KEY must be provided as a parameter.")
    openai.api_key = openai_api_key

    # Read the latest xgboost report
    report_path = Path(base_directory) / "log" / "xgboost_report.txt"
    if not report_path.exists():
        raise FileNotFoundError(f"{report_path} not found.")

    with open(report_path, "r") as f:
        content = f.read().strip()

    # Get the last report (after the last separator)
    if "="*40 in content:
        last_report = content.split("="*40)[-1].strip()
    else:
        last_report = content

    if not instructions:
        instructions = (
            "You are an expert financial machine learning assistant.\n\n"
            "Your response must be written as plain text only. Do not use any markdown, code blocks, bullet points, tables, or any special formatting. "
            "Do not use triple backticks or indentation for code or text. Write your answer as a simple, readable paragraph or list, using only plain text.\n\n"
            "Especially explain how many of the strong buy and must buy we will both correctly and incorrectly take based on this matrix.\n\n"
            "It is not that important that we miss a few, but it is important that when we buy we will get a good return.\n\n"
            "If we enter trades with a two to one risk reward ratio, what is the expected return in percentage?\n\n"
        )

    prompt = f"""
{instructions}

Here is the report:

{last_report}

"Remember: Your entire response must be plain text only, with no markdown, no code blocks, no bullet points, no tables, and no special formatting."
"""

    # Call OpenAI GPT-4o-mini model (changed from o4-mini to gpt-4o-mini and removed temperature parameter)
    response = openai.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        max_completion_tokens=500
    )

    explanation = response.choices[0].message.content.strip()

    # Save explanation to file
    explanation_path = Path(base_directory) / "log" / "xgboost_explanation.txt"
    with open(explanation_path, "w") as f:
        f.write(explanation)

    return explanation

if __name__ == "__main__":
    from dotenv import load_dotenv
    load_dotenv(override=True)
    # Always set base_dir to the script directory (PythonTrader/Src)
    base_dir = os.path.abspath(os.path.dirname(__file__))
    api_key = os.getenv("OPENAI_API_KEY")
    print(explain_xgboost_results(base_dir, api_key)) 
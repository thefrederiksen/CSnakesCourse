import os
import pandas as pd
from pathlib import Path
from typing import Generator
from datetime import datetime
import openai

def lookup_latest_price(base_dir: str, symbol: str) -> tuple[datetime, float]:
    """
    Loads the symbol's CSV file from sp500_data/{SYMBOL}.csv, finds the latest row by date, and returns the latest closing price.
    """
    csv_path = Path(base_dir) / 'sp500_data' / f'{symbol.upper()}.csv'
    if not csv_path.exists():
        raise FileNotFoundError(f"CSV file not found: {csv_path}")
    df = pd.read_csv(csv_path)
    if df.empty:
        raise ValueError(f"CSV file is empty: {csv_path}")
    df['Date'] = pd.to_datetime(df['Date'])
    latest_row = df.sort_values('Date').iloc[-1]
    latest_close = latest_row['Close']
    latest_date = latest_row['Date'].to_pydatetime()
    return latest_date, latest_close

def get_latest_dates_for_all_symbols(base_dir: str) -> Generator[int, None, pd.DataFrame]:
    """
    Iterates over all CSVs in sp500_data, finds the latest date for each symbol, yields progress, and returns a DataFrame with columns Symbol and Date.
    """
    sp500_dir = Path(base_dir) / 'sp500_data'
    csv_files = list(sp500_dir.glob('*.csv'))
    results = []
    total = len(csv_files)
    for idx, csv_path in enumerate(csv_files):
        symbol = csv_path.stem
        try:
            df = pd.read_csv(csv_path)
            if df.empty or 'Date' not in df.columns:
                continue
            df['Date'] = pd.to_datetime(df['Date'])
            latest_date = df['Date'].max()
            results.append({'Symbol': symbol, 'Date': latest_date})
        except Exception as e:
            print(f"[DEBUG] Error processing {csv_path}: {e}")
            continue
        yield int(100 * (idx + 1) / total) if total > 0 else 100
    df_results = pd.DataFrame(results, columns=['Symbol', 'Date'])
    yield 100
    return df_results

def call_openai_model(model: str, system_message: str, prompt: str, api_key: str) -> str:
    """
    Call an OpenAI model with the specified parameters.
    
    Args:
        model: The OpenAI model to use (e.g., 'gpt-4', 'gpt-3.5-turbo')
        system_message: The system message to set the context
        prompt: The user prompt/message
        api_key: The OpenAI API key
        
    Returns:
        The generated text response from the model
        
    Raises:
        Exception: If the API call fails or API key is empty
    """
    try:
        # Check if API key is provided and not empty
        if not api_key or api_key.strip() == "":
            raise Exception("OpenAI API key is required and cannot be empty")
        
        # Create the client
        client = openai.OpenAI(api_key=api_key)
        
        # Create the messages list
        messages = [
            {"role": "system", "content": system_message},
            {"role": "user", "content": prompt}
        ]
        
        # Make the API call - use minimal parameters to avoid compatibility issues
        response = client.chat.completions.create(
            model=model,
            messages=messages,
            max_completion_tokens=2000
        )
        
        # Extract and return the response text
        return response.choices[0].message.content.strip()
        
    except Exception as e:
        raise Exception(f"OpenAI API call failed: {str(e)}")

if __name__ == "__main__":
    base_dir = os.path.abspath(os.path.dirname(__file__))
    symbol = 'AAPL'
    date, price = lookup_latest_price(base_dir, symbol)
    print(f"Latest close for {symbol} on {date.date()}: {price}") 
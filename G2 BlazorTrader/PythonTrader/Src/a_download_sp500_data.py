import yfinance as yf
from datetime import datetime, timedelta
import os
import pandas as pd
import time
from typing import Generator

# Fetch the current S&P 500 symbol list from Wikipedia
def fetch_and_save_sp500_symbols(csv_path: str = 'sp500_symbols.csv') -> None:
    """
    Fetch S&P 500 symbols from Wikipedia and save to CSV.
    Args:
        csv_path (str): Full path where to save the symbols CSV file.
    """
    url = 'https://en.wikipedia.org/wiki/List_of_S%26P_500_companies'
    tables = pd.read_html(url)
    df = tables[0]
    
    # Ensure the directory exists
    os.makedirs(os.path.dirname(csv_path) if os.path.dirname(csv_path) else '.', exist_ok=True)
    
    df[['Symbol']].to_csv(csv_path, index=False)
    print(f"Saved S&P 500 symbols to {csv_path}")

# Load symbols from CSV
def load_sp500_symbols(csv_path: str = 'sp500_symbols.csv') -> list[str]:
    """
    Load S&P 500 symbols from a CSV file.
    Args:
        csv_path (str): Full path to the CSV file containing the symbols.
    Returns:
        list[str]: A list of S&P 500 ticker symbols as strings.
    """

    # Ensure the symbols CSV exists
    if not os.path.exists(csv_path):
        fetch_and_save_sp500_symbols(csv_path)

    df = pd.read_csv(csv_path)
    return df['Symbol'].tolist()

def load_or_download(base_directory: str, years: int = 5) -> Generator[int, None, None]:
    """
    Download S&P 500 historical data for the given number of years.
    Args:
        base_directory (str): Base directory where symbols CSV and data will be stored.
        years (int): Number of years of historical data to download.
    Yields:
        int: Progress from 0 to 500 as download progresses.
    """
    # Use base_directory for symbols CSV path
    symbols_csv_path = os.path.join(base_directory, 'sp500_symbols.csv')
    tickers = load_sp500_symbols(symbols_csv_path)
    total = len(tickers)
    if total == 0:
        yield 500
        return
    end_date = datetime.today()
    start_date = end_date - timedelta(days=years*365)
    
    # Use base_directory for output directory
    output_dir = os.path.join(base_directory, 'sp500_data')
    os.makedirs(output_dir, exist_ok=True)
    
    for idx, symbol in enumerate(tickers):
        csv_path = os.path.join(output_dir, f"{symbol}.csv")
        if os.path.exists(csv_path):
            print(f"Skipping {symbol}: {csv_path} already exists.")
        else:
            print(f"Downloading data for {symbol}...")
            try:
                data = yf.download(symbol, start=start_date.strftime('%Y-%m-%d'), end=end_date.strftime('%Y-%m-%d'), interval='1d')
                if not data.empty:
                    data = data.reset_index()
                    if isinstance(data.columns, pd.MultiIndex):
                        data.columns = [col[0] for col in data.columns]
                    columns_to_keep = ['Date', 'Open', 'High', 'Low', 'Close', 'Volume']
                    data = data[columns_to_keep]
                    data.to_csv(csv_path, index=False)
                    print(f"Saved {symbol} data to {csv_path}")
                else:
                    print(f"No data found for {symbol}.")
            except Exception as e:
                print(f"Error downloading {symbol}: {e}")
        # Yield progress as an integer from 0 to 500
        progress = int((idx + 1) / total * 500)
        yield progress
    print("Download complete.")


def main():
    # Set base_dir to the script directory (PythonTrader/Src), matching C# code
    base_dir = os.path.abspath(os.path.dirname(__file__))
    years = 5  # Number of years of historical data
    
    for progress in load_or_download(base_dir, years=years):
        print(f"Progress: {progress} of 500")


if __name__ == '__main__':
    main()

# Instructions:
# 1. The script will save the S&P 500 symbols to 'sp500_symbols.csv' if it doesn't exist.
# 2. To refresh the list, delete 'sp500_symbols.csv' and rerun the script.
# 3. Change the 'years' variable to download a different range.
# 4. Run this script after installing yfinance and pandas: pip install yfinance pandas 
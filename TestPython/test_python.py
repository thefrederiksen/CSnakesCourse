import yfinance as yf
from datetime import datetime, timedelta
import os
import pandas as pd

# Fetch the current S&P 500 symbol list from Wikipedia
def fetch_and_save_sp500_symbols(csv_path: str = 'sp500_symbols.csv') -> None:
    url = 'https://en.wikipedia.org/wiki/List_of_S%26P_500_companies'
    tables = pd.read_html(url)
    df = tables[0]
    df[['Symbol']].to_csv(csv_path, index=False)
    print(f"Saved S&P 500 symbols to {csv_path}")


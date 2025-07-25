import pandas as pd
from pathlib import Path
import os
from tqdm import tqdm
from typing import Generator

def create_training_data(base_directory: str = ".") -> Generator[int, None, None]:
    INDICATOR_DIR = Path(base_directory) / "sp500_data" / "indicators"
    OUTPUT_FILE = INDICATOR_DIR / "training_data.csv"
    STATS_FILE = INDICATOR_DIR / "stats.csv"

    # Delete stats.csv if it exists to ensure it's always recalculated
    if STATS_FILE.exists():
        try:
            os.remove(STATS_FILE)
        except PermissionError:
            print(f"ERROR: Could not delete {STATS_FILE} because it is most likely open in Excel. Please close the file and try again.")
            yield 0
            return
        except Exception as e:
            print(f"ERROR: Could not delete {STATS_FILE}. Unexpected error: {e}")
            yield 0
            return
    yield 5  # Progress after deleting stats

    # Find all indicator files
    indicator_files = list(INDICATOR_DIR.glob("*_Indicators.csv"))
    total_files = len(indicator_files)
    all_dfs = []
    symbols = []
    for idx, file in enumerate(tqdm(indicator_files, desc="Processing indicator files")):
        df = pd.read_csv(file)
        # Drop the five_day_long_profit column if present
        if 'five_day_long_profit' in df.columns:
            df = df.drop(columns=['five_day_long_profit'])
        # Only keep rows where Date is a Friday
        if 'Date' in df.columns:
            df['Date'] = pd.to_datetime(df['Date'])
            df = df[df['Date'].dt.weekday == 4]  # 4 = Friday
        all_dfs.append(df)
        symbols.append(file.name.replace('_Indicators.csv', ''))
        # Yield progress for each file processed (0-50%)
        yield int(5 + 45 * (idx + 1) / total_files) if total_files > 0 else 5

    # Combine all data
    if all_dfs:
        combined = pd.concat(all_dfs, ignore_index=True)
        # Drop columns that are all NaN (never calculated for any symbol)
        combined = combined.dropna(axis=1, how='all')
        # Only keep rows with no missing values
        combined = combined.dropna(axis=0, how='any')
        # Save to CSV
        try:
            combined.to_csv(OUTPUT_FILE, index=False)
            print(f"Saved combined training data to {OUTPUT_FILE} ({len(combined)} rows, {len(combined.columns)} columns)")
        except Exception as e:
            print(f"WARNING: Could not create {OUTPUT_FILE}. It is most likely open in Excel. Error: {e}")
        yield 60  # Progress after saving combined data

        # --- Create stats.csv ---
        # Reload each symbol's file and count classes
        class_labels = [
            "Must Sell", "Strong Sell", "Sell", "Flat", "Buy", "Strong Buy", "Must Buy"
        ]
        stats_rows = []
        class_totals = {label: 0 for label in class_labels}
        total_rows = 0
        for idx, (file, symbol) in enumerate(tqdm(list(zip(indicator_files, symbols)), desc="Generating stats")):
            df = pd.read_csv(file)
            # Only keep rows with all features present (as in training data)
            if 'five_day_long_profit' in df.columns:
                df = df.drop(columns=['five_day_long_profit'])
            if 'Date' in df.columns:
                df['Date'] = pd.to_datetime(df['Date'])
                df = df[df['Date'].dt.weekday == 4]
            df = df.dropna(axis=1, how='all')
            df = df.dropna(axis=0, how='any')
            row = {'Symbol': symbol}
            for label in class_labels:
                count = (df['five_day_class'] == label).sum() if 'five_day_class' in df.columns else 0
                row[label] = count
                class_totals[label] += count
            row['Total'] = sum(row[label] for label in class_labels)
            total_rows += row['Total']
            stats_rows.append(row)
            # Yield progress for each stats file processed (60-95%)
            yield int(60 + 35 * (idx + 1) / total_files) if total_files > 0 else 60
        # Add total row
        total_row = {'Symbol': 'Total'}
        for label in class_labels:
            total_row[label] = class_totals[label]
        total_row['Total'] = total_rows
        # Add percent row
        percent_row = {'Symbol': 'Percent'}
        for label in class_labels:
            percent_row[label] = f"{(class_totals[label] / total_rows * 100):.2f}%" if total_rows > 0 else '0.00%'
        percent_row['Total'] = '100.00%'
        # Write to CSV
        stats_df = pd.DataFrame(stats_rows + [total_row, percent_row])
        try:
            stats_df.to_csv(STATS_FILE, index=False)
            print(f"Saved stats to {STATS_FILE}")
        except Exception as e:
            print(f"WARNING: Could not create {STATS_FILE}. It is most likely open in Excel. Error: {e}")
        yield 100  # Progress complete
    else:
        print("No indicator files found.")
        yield 100

if __name__ == "__main__":
    # Set base_dir to the script directory (PythonTrader/Src), matching C# code
    base_dir = os.path.abspath(os.path.dirname(__file__))
    for progress in create_training_data(base_dir):
        pass
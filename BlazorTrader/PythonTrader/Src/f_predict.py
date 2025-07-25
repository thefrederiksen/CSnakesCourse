import os
import sys
import pandas as pd
from pathlib import Path
from d_train_xgboost import predict_latest_for_all_symbols

def print_progress_bar(progress, total=100, bar_length=40):
    percent = int(progress)
    filled_length = int(bar_length * percent // total)
    bar = '=' * filled_length + '-' * (bar_length - filled_length)
    sys.stdout.write(f'\r[Progress] |{bar}| {percent}%')
    sys.stdout.flush()
    if percent >= total:
        print()

if __name__ == "__main__":
    # Set base_dir to the script directory (PythonTrader/Src), matching C# code
    base_dir = os.path.abspath(os.path.dirname(__file__))
    print(f"[DEBUG] base_dir: {base_dir}")
    print("\n[DEBUG] Running predict_latest_for_all_symbols...")
    gen = predict_latest_for_all_symbols(base_dir)
    latest_df = None
    for progress in gen:
        if isinstance(progress, int):
            print_progress_bar(progress)
        else:
            latest_df = progress
    if latest_df is not None:
        print("[DEBUG] Latest predictions (head):")
        print(latest_df.head())
        print("[DEBUG] Saved to log/latest_predictions.csv") 
#!/usr/bin/env python
"""
Generate 20 bounded, model-ready technical indicators for every CSV in DATA_DIR.
Each output file is SYMBOL_Indicators.csv (same date rows as the source).

Dependencies
------------
pip install pandas pandas_ta numpy tqdm

Indicator Documentation
-----------------------
Below are the indicators/features calculated, their meaning, and typical range:

Price/Volume Features:
- close_sma20_ratio: (Close/SMA20) - 1, price relative to 20-day SMA, centered at 0, typically -0.1 to 0.1
- gap_over_atr: (Open - prev Close) / ATR, overnight gap normalized by volatility, typically -2 to 2
- ret_z_5: 5-day return Z-score, (Close - 5d mean) / 5d std, typically -2 to 2
- rel_str_5d: 5-day relative strength vs S&P 500, percent change in (Close/SPY Close), clipped to [-0.1, 0.1]
- pctile_20: Price percentile in 20-day high/low range, 0 (low) to 1 (high)

Volume Features:
- vol_zscore_20: Standardized volume, (Volume - 20d mean) / 20d std, typically -2 to 2
- vol_ma_ratio_20: Volume / 20d mean, typically 0.5 to 2
- vol_pctile_20: Percentile of today's volume in last 20 days, 0 to 1

Volatility Feature:
- atr_rank: Cross-sectional percentile rank of normalized ATR for each date (0=least volatile, 1=most volatile)

Technical Indicators (from pandas_ta):
- rsi_14: Relative Strength Index, 0 to 100
- stoch: Stochastic Oscillator %K and %D, 0 to 100
- willr_14: Williams %R, -100 to 0
- mfi_14: Money Flow Index, 0 to 100
- adx_14: Average Directional Index, 0 to 100
- dmp_14: +DI (Directional Indicator), 0 to 100
- dmn_14: -DI (Directional Indicator), 0 to 100
- bb_percent: Percentile of close within Bollinger Bands, 0 to 1
- bb_width: Bollinger Band width (volatility proxy), positive real values
- sma_20: 20-day Simple Moving Average, in price units
- ppo: Percent Price Oscillator, typically -10 to 10
- tsi: True Strength Index, typically -100 to 100
- cmf_20: Chaikin Money Flow, typically -1 to 1
- five_day_long_profit: Percent profit (or loss) if buying at next open and selling at close 5 days later, e.g. buy Monday open, sell Friday close. Typically -0.1 to 0.1
- five_day_class: Categorical label for five_day_long_profit. One of [Must Sell, Strong Sell, Sell, Flat, Buy, Strong Buy, Must Buy]. (last column)

All features are calculated per symbol and date, and are suitable for use as features in machine learning classifiers/regressors.

Note: atr_rank requires cross-sectional computation across all symbols for each date. This is a placeholder and should be computed in a post-processing step after merging all symbols' data by date.
"""

from pathlib import Path
import numpy as np
import pandas as pd
import pandas_ta as ta
from tqdm import tqdm
from typing import Generator
import warnings
import os

# Suppress the pkg_resources deprecation warning from pandas_ta
warnings.filterwarnings("ignore", message="pkg_resources is deprecated as an API")
warnings.filterwarnings("ignore", category=DeprecationWarning, module="pkg_resources")

# --------------------------------------------------------------------------- #
# Configuration
# --------------------------------------------------------------------------- #
def get_data_dir(base_directory: str = ".") -> Path:
    return Path(base_directory) / "sp500_data"

def get_indicator_dir(base_directory: str = ".") -> Path:
    return get_data_dir(base_directory) / "indicators"

def get_output_suffix() -> str:
    return "_Indicators.csv"

def get_benchmark_file() -> str:
    return "SPY.csv"

def get_error_file(base_directory: str = ".") -> Path:
    return get_indicator_dir(base_directory) / "errors.txt"

# --------------------------------------------------------------------------- #
# Helper functions
# --------------------------------------------------------------------------- #
def _add_ta_features(df: pd.DataFrame, symbol: str, error_log: list) -> pd.DataFrame:
    TA_FEATURES = [
        ("rsi",        dict(length=14)),                 # RSI
        ("stoch",      dict(k=14, d=3)),                 # Stoch %K/%D
        ("willr",      dict(length=14)),                 # Williams %R
        ("mfi",        dict(length=14)),                 # Money-Flow Index
        ("adx",        dict(length=14)),                 # ADX, +DI, −DI
        # ("bbands",     dict(length=20)),               # Bollinger bands (handled in custom features)
        ("atr",        dict(length=14)),                 # ATR (used normalised)
        ("sma",        dict(length=20)),                 # SMA 20 (for price/SMA ratio)
        ("ppo",        {}),                              # Percent Price Oscillator
        ("tsi",        dict()),                          # True-Strength Index 25,13 (defaults)
        ("cmf",        dict(length=20)),                 # Chaikin Money Flow
        # ("obv",        {}),                            # On-Balance Volume (removed)
    ]
    ta_df_parts = []

    for name, params in TA_FEATURES:
        try:
            if name == "stoch":
                out = ta.stoch(df["High"], df["Low"], df["Close"], **params)
            elif name == "obv":
                out = ta.obv(df["Close"], df["Volume"], **params)
            elif name in ["willr", "adx", "atr"]:
                out = getattr(ta, name)(df["High"], df["Low"], df["Close"], **params)
            elif name in ["mfi", "cmf"]:
                out = getattr(ta, name)(df["High"], df["Low"], df["Close"], df["Volume"], **params)
            elif name == "bbands":
                out = ta.bbands(df["Close"], **params)
            elif name == "sma":
                out = ta.sma(df["Close"], **params)
                if isinstance(out, pd.Series):
                    out = out.to_frame()
                # Rename the column to SMA_{length}
                length = params.get("length", 20)
                out.columns = [f"SMA_{length}"]
            else:
                out = getattr(ta, name)(df, **params)
            if isinstance(out, pd.Series):
                out = out.to_frame()
            ta_df_parts.append(out)
        except Exception as e:
            # Try to infer column names from pandas_ta, else use generic
            if name == "stoch":
                colnames = ["STOCHk_14_3_3", "STOCHd_14_3_3"]
            elif name == "bbands":
                colnames = ["BBL_20_2.0", "BBM_20_2.0", "BBU_20_2.0", "BBB_20_2.0", "BBP_20_2.0"]
            elif name == "obv":
                colnames = ["OBV"]
            elif name == "willr":
                colnames = ["WILLR_14"]
            elif name == "adx":
                colnames = ["ADX_14", "DMP_14", "DMN_14"]
            elif name == "atr":
                colnames = ["ATR_14"]
            elif name == "mfi":
                colnames = ["MFI_14"]
            elif name == "cmf":
                colnames = ["CMF_20"]
            elif name == "sma":
                length = params.get("length", 20)
                colnames = [f"SMA_{length}"]
            else:
                colnames = [name.upper()]
            nan_df = pd.DataFrame({col: [np.nan]*len(df) for col in colnames}, index=df.index)
            ta_df_parts.append(nan_df)
            error_log.append(f"{symbol}: {name} - {e}")
    return pd.concat(ta_df_parts, axis=1)


def _add_custom_features(df: pd.DataFrame, atr_col: str, spy_close: pd.Series) -> pd.DataFrame:
    out = pd.DataFrame(index=df.index)

    # 1. Normalised ATR (volatility as % of price)
    out["atr_norm_14"] = df[atr_col] / df["Close"]

    # 2. Price percentile inside 20-day high/low range
    high20 = df["High"].rolling(20).max()
    low20  = df["Low"].rolling(20).min()
    out["pctile_20"] = (df["Close"] - low20) / (high20 - low20)

    # 3. Close / SMA20 ratio minus 1 (centred at 0)
    out["close_sma20_ratio"] = df["Close"] / df["SMA_20"] - 1.0

    # 4. Volume Z-Score (Standardized Volume)
    out["vol_zscore_20"] = (df["Volume"] - df["Volume"].rolling(20).mean()) / df["Volume"].rolling(20).std()

    # 5. Volume/Moving Average Ratio
    out["vol_ma_ratio_20"] = df["Volume"] / df["Volume"].rolling(20).mean()

    # 6. Bollinger Band features (percentile and width)
    bb = ta.bbands(df["Close"], length=20)
    if bb is not None and all(col in bb.columns for col in ["BBL_20_2.0", "BBU_20_2.0", "BBB_20_2.0"]):
        out["bb_percent"] = (df["Close"] - bb["BBL_20_2.0"]) / (bb["BBU_20_2.0"] - bb["BBL_20_2.0"])
        out["bb_width"] = bb["BBB_20_2.0"]
    else:
        out["bb_percent"] = np.nan
        out["bb_width"] = np.nan

    # 7. Overnight gap (Open – prev Close) scaled by ATR
    out["gap_over_atr"] = (df["Open"] - df["Close"].shift(1)) / df[atr_col]

    # 8. 5-Day Return Z-Score
    mean5 = df["Close"].rolling(5).mean()
    std5  = df["Close"].rolling(5).std()
    out["ret_z_5"] = (df["Close"] - mean5) / std5

    # 9. Relative-strength vs S&P 500 over 5 trading days
    if spy_close is not None:
        ratio = df["Close"] / spy_close
        out["rel_str_5d"] = ratio.pct_change(5).clip(-0.1, 0.1)   # −10 % … 10 %

    # 10. Volume percentile in rolling 20-day window
    out["vol_pctile_20"] = (
        df["Volume"].rolling(20)
        .apply(lambda x: x.rank(pct=True).iloc[-1], raw=False)
    )

    # 11. ATR cross-sectional rank (percentile) for each date
    out["atr_rank"] = np.nan  # Placeholder; see note below.

    # Remove raw ATR from output
    out = out.drop(columns=["atr_norm_14"])

    # 12. Five-day long profit: buy next open, sell close 5 days later (ensure it's last)
    out["five_day_long_profit"] = (df["Close"].shift(-5) - df["Open"].shift(-1)) / df["Open"].shift(-1)
    cols = [c for c in out.columns if c != "five_day_long_profit"] + ["five_day_long_profit"]
    out = out[cols]

    # 13. five_day_class: categorical label for five_day_long_profit
    def classify_weekly_profit(x):
        if pd.isna(x):
            return None
        if x <= -0.10:
            return "Must Sell"
        elif x <= -0.05:
            return "Strong Sell"
        elif x <= -0.02:
            return "Sell"
        elif x < 0.02:
            return "Flat"
        elif x < 0.05:
            return "Buy"
        elif x < 0.10:
            return "Strong Buy"
        else:
            return "Must Buy"
    out["five_day_class"] = out["five_day_long_profit"].apply(classify_weekly_profit)
    cols = [c for c in out.columns if c != "five_day_class"] + ["five_day_class"]
    out = out[cols]

    return out


def calculate_indicators(price_df: pd.DataFrame, spy_series: pd.Series | None, symbol: str, error_log: list) -> pd.DataFrame:
    ta_df = _add_ta_features(price_df, symbol, error_log)
    atr_col = next((col for col in ta_df.columns if col.startswith("ATRr_") or col.startswith("ATR_")), None)
    custom_df = pd.DataFrame(index=price_df.index)
    if atr_col is not None:
        try:
            custom_df = _add_custom_features(
                pd.concat([price_df, ta_df], axis=1),
                atr_col=atr_col,
                spy_close=spy_series.reindex(price_df.index) if spy_series is not None else None,
            )
        except Exception as e:
            error_log.append(f"{symbol}: custom_features - {e}")
    else:
        error_log.append(f"{symbol}: ATR column not found, custom features skipped")
    # Combine: raw price, TA, custom; only drop rows where five_day_long_profit is NaN
    full = pd.concat([price_df, ta_df, custom_df], axis=1).reset_index()
    if 'five_day_long_profit' in full.columns:
        full = full[full['five_day_long_profit'].notna()]
    return full


# --------------------------------------------------------------------------- #
# Batch runner
# --------------------------------------------------------------------------- #
def process_all_files(base_directory: str = ".") -> Generator[int, None, None]:
    """
    Process all CSV files and yield progress updates.
    Args:
        base_directory (str): Base directory where the sp500_data folder is located.
    Yields:
        int: Progress from 0 to total number of files as processing progresses.
    """
    get_indicator_dir(base_directory).mkdir(parents=True, exist_ok=True)
    # Remove previous errors.txt if it exists
    if get_error_file(base_directory).exists():
        get_error_file(base_directory).unlink()

    csv_path = get_data_dir(base_directory)
    print(f"Processing csv files in: {csv_path}")
    print(f"indicator dir: {get_indicator_dir(base_directory)}")        
    
    errors = []
    spy_series = None
    spy_path = get_data_dir(base_directory) / get_benchmark_file()
    
    if spy_path.exists():
        try:
            spy_series = pd.read_csv(spy_path, parse_dates=["Date"]).set_index("Date")["Close"]
        except Exception as e:
            errors.append((get_benchmark_file(), f"Failed to load benchmark: {e}"))

    # Get all CSV files to process (excluding output files and benchmark)
    csv_files = [
        csv_path for csv_path in sorted(get_data_dir(base_directory).glob("*.csv"))
        if not csv_path.name.endswith(get_output_suffix()) and csv_path.name != get_benchmark_file()
    ]
    
    total_files = len(csv_files)
    processed_count = 0


    
    for csv_path in csv_files:
        symbol = csv_path.stem
        error_log = []
        try:
            df_prices = pd.read_csv(csv_path, parse_dates=["Date"]).set_index("Date")
            # Check for required columns
            required_cols = {"Open", "High", "Low", "Close", "Volume"}
            if not required_cols.issubset(df_prices.columns):
                raise ValueError(f"Missing columns: {required_cols - set(df_prices.columns)}")
            fe_df = calculate_indicators(df_prices, spy_series, symbol, error_log)
            out_path = get_indicator_dir(base_directory) / (symbol + get_output_suffix())
            fe_df.to_csv(out_path, index=False)
            print(f"✅  Saved → {out_path.name}  ({len(fe_df):,} rows)")
        except Exception as e:
            error_msg = f"{symbol}: {e}"
            errors.append((symbol, str(e)))
            error_log.append(error_msg)
            print(f"❌  Error for {symbol}: {e}")
        
        # Write all errors for this symbol to the error file
        if error_log:
            with open(get_error_file(base_directory), "a") as ef:
                for entry in error_log:
                    ef.write(entry + "\n")
        
        processed_count += 1
        yield processed_count  # Yield progress after each file
    
    # Show all errors at the end
    if errors or get_error_file(base_directory).exists():
        print("\n--- Errors encountered ---")
        if errors:
            for symbol, msg in errors:
                print(f"{symbol}: {msg}")
        if get_error_file(base_directory).exists():
            with open(get_error_file(base_directory)) as ef:
                for line in ef:
                    print(line.strip())
    else:
        print("\nNo errors encountered.")

# --------------------------------------------------------------------------- #
if __name__ == "__main__":
    # Set base_dir to the script directory (PythonTrader/Src), matching C# code
    base_dir = os.path.abspath(os.path.dirname(__file__))
    for progress in process_all_files(base_dir):
        pass 
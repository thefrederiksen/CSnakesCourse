"""
Two-Stage Buy Classifier
───────────────────────────────────────────────────────────────
Stage 1 :  Binary  'Buy' (Strong|Must) vs 'No-Buy'
Stage 2 :  (runs only on Stage 1 positives)
          → Must Buy / Strong Buy / No-Buy-FP
"""

import pandas as pd, numpy as np, sys, xgboost, sklearn
from pathlib import Path
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import (accuracy_score, f1_score, fbeta_score, recall_score,
                             precision_recall_fscore_support, average_precision_score,
                             confusion_matrix, classification_report)
from xgboost import XGBClassifier
from sklearn.calibration import CalibratedClassifierCV
import joblib
from datetime import datetime
from typing import Generator, Tuple, Any
import os
from g_llm_knowledge import lookup_latest_price

DATA_PATH  = Path("sp500_data/indicators/training_data.csv")
TARGET_COL = "five_day_class"
DROP_COLS  = [TARGET_COL, "Date", "Open", "High", "Low", "Close"]
BUY_POS    = {"Strong Buy", "Must Buy"}        # "Buy" for Stage 1
POS_MULT   = 4                                 # weight multiplier

# ──────────────────────────────────────────────────────────────
def balanced_weights(y, pos_label=1, mult=1):
    n_pos, n_neg = np.bincount(y)
    w_pos = mult * len(y) / (2 * n_pos)
    w_neg = len(y)      / (2 * n_neg)
    scale = (w_pos*n_pos + w_neg*n_neg) / len(y)
    return np.where(y == pos_label, w_pos/scale, w_neg/scale)

# ──────────────────────────────────────────────────────────────
def train_stage1(X_tr, y_tr, X_val, y_val, sw_tr, sw_val):
    clf = XGBClassifier(
        n_estimators=800, learning_rate=0.05, max_depth=6,
        subsample=0.8, colsample_bytree=0.8,
        objective="binary:logistic", eval_metric="aucpr",
        random_state=42, n_jobs=-1, early_stopping_rounds=40
    )
    clf.fit(X_tr, y_tr,
            sample_weight=sw_tr,
            eval_set=[(X_val, y_val)],
            verbose=False)

    # isotonic calibration
    cal = CalibratedClassifierCV(clf, method="isotonic", cv="prefit")
    cal.fit(X_val, y_val, sample_weight=sw_val)
    return cal

# ──────────────────────────────────────────────────────────────
def best_threshold(proba, y_true):
    best_t, best_f1 = 0, 0
    for t in np.geomspace(0.02, 0.5, 25):
        f1 = f1_score(y_true, proba >= t)
        if f1 > best_f1:
            best_t, best_f1 = t, f1
    for t in np.arange(0.05, 0.081, 0.005):      # fine grid
        f1 = f1_score(y_true, proba >= t)
        if f1 > best_f1:
            best_t, best_f1 = t, f1
    return best_t, best_f1

# ──────────────────────────────────────────────────────────────
def train_stage2(X_s2, y_s2):
    # Encode  →  0 = No-Buy-FP, 1 = Strong, 2 = Must
    lbl_map = {"No-Buy":0, "Strong Buy":1, "Must Buy":2}
    y_enc   = np.vectorize(lbl_map.get)(y_s2)
    classes = np.unique(y_enc)

    # class-balanced sample-weights
    freq  = np.bincount(y_enc)
    w_cl  = len(y_enc) / (len(classes) * np.maximum(freq,1))
    sw2   = w_cl[y_enc]

    X_tr2, X_val2, y_tr2, y_val2, sw_tr2, sw_val2 = train_test_split(
        X_s2, y_enc, sw2, test_size=0.20, stratify=y_enc, random_state=42)

    clf2 = XGBClassifier(
        n_estimators=600, learning_rate=0.05, max_depth=6,
        subsample=0.8, colsample_bytree=0.8,
        objective="multi:softprob", num_class=3,
        eval_metric="mlogloss", random_state=42, early_stopping_rounds=40
    )
    clf2.fit(X_tr2, y_tr2, sample_weight=sw_tr2,
             eval_set=[(X_val2, y_val2)],
             verbose=False)

    return clf2, lbl_map

# ──────────────────────────────────────────────────────────────
def train_models(base_directory: str = ".") -> Generator[int, None, Tuple[bool, str]]:
    try:
        # Remove previous xgboost_report.txt if it exists
        log_dir = Path(base_directory) / 'log'
        report_path = log_dir / 'xgboost_report.txt'
        if report_path.exists():
            report_path.unlink()
        DATA_PATH  = Path(base_directory) / "sp500_data" / "indicators" / "training_data.csv"
        #────────────────  Stage 1  ────────────────
        df  = pd.read_csv(DATA_PATH)
        yield 10  # Progress after loading data
        y1  = df[TARGET_COL].isin(BUY_POS).astype(int)
        X   = df.drop(columns=DROP_COLS, errors="ignore")

        X_tr, X_te, y_tr, y_te = train_test_split(
            X, y1, test_size=0.20, stratify=y1, random_state=42)

        scaler = StandardScaler()
        X_tr_sc = scaler.fit_transform(X_tr)
        X_te_sc = scaler.transform(X_te)

        # hold-out 10 % of train for val
        X_tr_sc, X_val_sc, y_tr, y_val = train_test_split(
            X_tr_sc, y_tr, test_size=0.10, stratify=y_tr, random_state=42)

        sw_tr  = balanced_weights(y_tr, pos_label=1, mult=POS_MULT)
        sw_val = balanced_weights(y_val, pos_label=1, mult=POS_MULT)

        clf1 = train_stage1(X_tr_sc, y_tr, X_val_sc, y_val, sw_tr, sw_val)
        yield 40  # Progress after training stage 1

        p_val = clf1.predict_proba(X_val_sc)[:,1]
        thresh, f1_val = best_threshold(p_val, y_val)
        print(f"\nStage-1 threshold = {thresh:.3f}   (val F1 = {f1_val:.3f}")

        print(f"[DEBUG] Recall on validation: {recall_score(y_val, p_val>=thresh)}")
        # Guardrail
        if recall_score(y_val, p_val>=thresh) < 0.10:
            print("Recall guardrail failed — aborting");
            yield 100
            return False, "Recall guardrail failed."

        #────────────────  Stage 2  ────────────────
        # Route *all* rows through Stage-1 to build Stage-2 dataset
        X_all_sc = scaler.transform(X)
        p_all    = clf1.predict_proba(X_all_sc)[:,1]
        buy_idx  = p_all >= thresh

        strength_label = np.where(
            df[TARGET_COL].isin(BUY_POS),
            df[TARGET_COL],                     # Strong / Must
            "No-Buy"                            # Stage-1 FP
        )
        X_s2 = X_all_sc[buy_idx]
        y_s2 = strength_label[buy_idx]

        clf2, lbl_map = train_stage2(X_s2, y_s2)
        inv_lbl = {v:k for k,v in lbl_map.items()}
        yield 70  # Progress after training stage 2

        #────────────────  End-to-end Test ─────────
        p_test  = clf1.predict_proba(X_te_sc)[:,1]
        buy_te  = p_test >= thresh
        final   = np.full(len(y_te), "No-Buy", dtype=object)

        # Stage 1: Print classification report for Buy/No-Buy
        print("\nStage-1 classification report (test rows):")
        print(classification_report(y_te, buy_te, target_names=["No-Buy", "Buy"], zero_division=0))

        if buy_te.any():
            p_strength = clf2.predict_proba(X_te_sc[buy_te])
            final[buy_te] = [inv_lbl[i] for i in p_strength.argmax(1)]

        # map ground truth for report
        true_lbl = np.where(df.loc[y_te.index, TARGET_COL].isin(BUY_POS),
                            df.loc[y_te.index, TARGET_COL],
                            "No-Buy")

        print("\nStage-2 classification report (test rows):")
        report = classification_report(true_lbl, final,
              labels=["No-Buy","Strong Buy","Must Buy"],
              zero_division=0)
        print(report)
        # Log the report to log/report.txt
        log_dir = Path(base_directory) / 'log'
        log_dir.mkdir(parents=True, exist_ok=True)
        with open(log_dir / 'xgboost_report.txt', 'a') as f:
            f.write(f"\n{'='*40}\n")
            f.write(f"Run at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(report + "\n")

        # Save models and artefacts
        models_dir = Path(base_directory) / 'models'
        models_dir.mkdir(parents=True, exist_ok=True)
        joblib.dump(clf1, models_dir / 'stage1_model.joblib')
        joblib.dump(clf2, models_dir / 'stage2_model.joblib')
        joblib.dump(scaler, models_dir / 'scaler.joblib')
        joblib.dump(list(X.columns), models_dir / 'feature_names.joblib')
        with open(models_dir / 'stage1_threshold.txt', 'w') as f:
            f.write(str(thresh))
        print("\nModels and artefacts saved in 'models': stage1_model.joblib, stage2_model.joblib, scaler.joblib, stage1_threshold.txt, feature_names.joblib")
        yield 90  # Progress after saving models

        # Test: Run recall on last 20 Fridays of AAPL
        print("\n--- Test: Last 20 Fridays of AAPL ---")
        aapl_path = Path(base_directory) / 'sp500_data' / 'indicators' / 'AAPL_Indicators.csv'
        aapl_df = pd.read_csv(aapl_path)
        aapl_fridays = aapl_df[pd.to_datetime(aapl_df['Date']).dt.weekday == 4].copy()
        aapl_fridays = aapl_fridays.tail(20)
        X_aapl = aapl_fridays.drop(columns=[TARGET_COL, 'Date', 'Open', 'High', 'Low', 'Close'], errors='ignore')
        # Align features for prediction
        feature_names = joblib.load(models_dir / 'feature_names.joblib')
        X_aapl = X_aapl.reindex(columns=feature_names, fill_value=0)
        preds = predict_buy_strength(X_aapl, base_directory)
        for date, pred in zip(aapl_fridays['Date'], preds):
            print(f"{date}: {pred}")

        yield 100  # Progress complete
        # Return the full xgboost_report.txt as message
        if report_path.exists():
            with open(report_path, 'r') as f:
                report_content = f.read()
            return True, report_content
        else:
            return True, "Training completed successfully!"
    except Exception as e:
        print(f"[DEBUG] Exception occurred: {e}")
        yield 100
        return False, f"Training failed: {str(e)}"

# ──────────────────────────────────────────────────────────────
def predict_buy_strength(X, base_directory: str = "."):
    """
    Predicts No-Buy, Strong Buy, or Must Buy for each row in X (feature DataFrame).
    Loads scaler, stage 1 model, stage 2 model, threshold, and feature names from disk.
    Returns: numpy array of predicted labels ("No-Buy", "Strong Buy", "Must Buy")
    """
    # Load artefacts
    models_dir = Path(base_directory) / 'models'
    scaler = joblib.load(models_dir / 'scaler.joblib')
    clf1 = joblib.load(models_dir / 'stage1_model.joblib')
    clf2 = joblib.load(models_dir / 'stage2_model.joblib')
    feature_names = joblib.load(models_dir / 'feature_names.joblib')
    with open(models_dir / 'stage1_threshold.txt') as f:
        thresh = float(f.read())
    # Reindex input to match training features
    X = X.reindex(columns=feature_names, fill_value=0)
    # Preprocess
    X_scaled = scaler.transform(X)
    # Stage 1: Buy/No-Buy
    p_buy = clf1.predict_proba(X_scaled)[:,1]
    is_buy = p_buy >= thresh
    # Prepare output
    result = np.full(len(X), "No-Buy", dtype=object)
    # Stage 2: Only for predicted Buy
    if is_buy.any():
        p_strength = clf2.predict_proba(X_scaled[is_buy])
        strength_lbls = np.array(["No-Buy", "Strong Buy", "Must Buy"])[p_strength.argmax(1)]
        result[is_buy] = strength_lbls
    return result

def predict_latest_for_all_symbols(base_directory: str = ".") -> Generator[int, None, list[dict[str, Any]]]:
    """
    For each *_Indicators.csv file, predict the classification for the latest (most recent) row.
    Yields progress as int, then returns a list of dicts: [{Symbol, Date, Prediction}, ...]
    Also saves the DataFrame to log/latest_predictions.csv
    """
    models_dir = Path(base_directory) / 'models'
    indicators_dir = Path(base_directory) / 'sp500_data' / 'indicators'
    log_dir = Path(base_directory) / 'log'
    log_dir.mkdir(parents=True, exist_ok=True)
    feature_names = joblib.load(models_dir / 'feature_names.joblib')
    results = []
    indicator_files = list(indicators_dir.glob("*_Indicators.csv"))
    total = len(indicator_files)
    for idx, file_path in enumerate(indicator_files):
        symbol = file_path.name.replace('_Indicators.csv', '')
        try:
            df = pd.read_csv(file_path)
            if df.empty:
                continue
            # Get the latest row by Date
            df['Date'] = pd.to_datetime(df['Date'])
            latest_row = df.sort_values('Date').iloc[-1:]
            X_latest = latest_row.drop(columns=[TARGET_COL, 'Date', 'Open', 'High', 'Low', 'Close'], errors='ignore')
            X_latest = X_latest.reindex(columns=feature_names, fill_value=0)
            pred = predict_buy_strength(X_latest, base_directory)[0]
            # Get latest price for the symbol
            try:
                latest_date, latest_price = lookup_latest_price(base_directory, symbol)
                results.append({
                    'Symbol': symbol,
                    'Date': str(latest_row['Date'].iloc[0]),
                    'Prediction': pred,
                    'LatestPrice': latest_price
                })
            except Exception as e:
                print(f"[DEBUG] Error getting latest price for {symbol}: {e}")
                results.append({
                    'Symbol': symbol,
                    'Date': str(latest_row['Date'].iloc[0]),
                    'Prediction': pred,
                    'LatestPrice': None
                })
        except Exception as e:
            print(f"[DEBUG] Error processing {file_path}: {e}")
            continue
        yield int(100 * (idx + 1) / total) if total > 0 else 100
    df_results = pd.DataFrame(results, columns=['Symbol', 'Date', 'Prediction', 'LatestPrice'])
    out_path = log_dir / 'latest_predictions.csv'
    df_results.to_csv(out_path, index=False)
    print(f"[DEBUG] Saved latest predictions to {out_path}")
    yield 100
    return df_results.to_dict("records")

# ──────────────────────────────────────────────────────────────
if __name__ == "__main__":
    # Set base_dir to the script directory (PythonTrader/Src), matching C# code
    base_dir = os.path.abspath(os.path.dirname(__file__))
    print(f"[DEBUG] base_dir: {base_dir}")
    for progress in train_models(base_dir):
        pass

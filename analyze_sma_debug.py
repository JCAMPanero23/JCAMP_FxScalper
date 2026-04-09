#!/usr/bin/env python3
"""
Analyze SMA Debug CSV files from cBot and Indicator
Compare SMA values and detect crossovers
"""

import csv
import sys
from pathlib import Path
from datetime import datetime

def analyze_cbot_csv(filepath):
    """Analyze cBot CSV and find key events"""
    print(f"\n{'='*80}")
    print(f"ANALYZING CBOT CSV: {filepath}")
    print(f"{'='*80}\n")

    crossovers = []
    alignment_changes = []
    prev_m1_align = None
    prev_mtf_aligned = None

    with open(filepath, 'r') as f:
        reader = csv.DictReader(f)
        total_bars = 0

        for row in reader:
            total_bars += 1
            timestamp = row['Timestamp']
            bar_idx = row['BarIndex']
            price = float(row['Price_M1'])
            sma_m1 = float(row['SMA_M1']) if row['SMA_M1'] != 'NaN' else None
            m1_align = row['Align_M1']
            mtf_aligned = row['MTF_Aligned']
            m1_crossover = row['M1_Crossover']

            # Detect M1 alignment changes (crossovers)
            if prev_m1_align and prev_m1_align != 'NONE' and m1_align != 'NONE':
                if prev_m1_align != m1_align:
                    crossovers.append({
                        'timestamp': timestamp,
                        'bar': bar_idx,
                        'from': prev_m1_align,
                        'to': m1_align,
                        'price': price,
                        'sma': sma_m1,
                        'csv_crossover': m1_crossover
                    })

            # Detect MTF alignment changes
            if prev_mtf_aligned and prev_mtf_aligned != mtf_aligned:
                alignment_changes.append({
                    'timestamp': timestamp,
                    'bar': bar_idx,
                    'from': prev_mtf_aligned,
                    'to': mtf_aligned
                })

            prev_m1_align = m1_align
            prev_mtf_aligned = mtf_aligned

    print(f"Total bars: {total_bars}")
    print(f"\nM1 Crossovers detected: {len(crossovers)}")

    if crossovers:
        print("\nFirst 10 crossovers:")
        for i, cross in enumerate(crossovers[:10], 1):
            sma_str = f"{cross['sma']:.5f}" if cross['sma'] else "NaN"
            print(f"  {i}. {cross['timestamp']} | Bar {cross['bar']} | "
                  f"{cross['from']} -> {cross['to']} | "
                  f"Price: {cross['price']:.5f} | SMA: {sma_str} | "
                  f"CSV says: {cross['csv_crossover']}")

    print(f"\nMTF Alignment changes: {len(alignment_changes)}")
    if alignment_changes:
        print("\nFirst 5 MTF alignment changes:")
        for i, change in enumerate(alignment_changes[:5], 1):
            print(f"  {i}. {change['timestamp']} | Bar {change['bar']} | {change['from']} -> {change['to']}")

    return {
        'total_bars': total_bars,
        'crossovers': crossovers,
        'alignment_changes': alignment_changes
    }

def compare_sma_values(cbot_csv, indicator_csv):
    """Compare SMA values between cBot and Indicator CSVs"""
    print(f"\n{'='*80}")
    print(f"COMPARING SMA VALUES")
    print(f"{'='*80}\n")

    # Read cBot data
    cbot_data = {}
    with open(cbot_csv, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            timestamp = row['Timestamp']
            cbot_data[timestamp] = {
                'price': float(row['Price_M1']),
                'sma_m1': float(row['SMA_M1']) if row['SMA_M1'] != 'NaN' else None,
                'sma_tf2': float(row['SMA_TF2']) if row['SMA_TF2'] != 'NaN' else None,
                'sma_tf3': float(row['SMA_TF3']) if row['SMA_TF3'] != 'NaN' else None,
                'bar': row['BarIndex']
            }

    # Read Indicator data and compare
    if not Path(indicator_csv).exists():
        print(f"[WARNING]  Indicator CSV not found: {indicator_csv}")
        print("Please re-run the indicator with the fixed code to generate a full CSV.")
        return

    differences = []
    with open(indicator_csv, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            timestamp = row['Timestamp']
            if timestamp not in cbot_data:
                continue

            ind_price = float(row['Price'])
            ind_sma0 = float(row['SMA0_M1']) if row['SMA0_M1'] != 'NaN' else None
            ind_sma1 = float(row['SMA1_TF1']) if row['SMA1_TF1'] != 'NaN' else None
            ind_sma2 = float(row['SMA2_TF2']) if row['SMA2_TF2'] != 'NaN' else None

            cbot = cbot_data[timestamp]

            # Compare prices (should be identical)
            if abs(cbot['price'] - ind_price) > 0.00001:
                differences.append({
                    'timestamp': timestamp,
                    'type': 'PRICE',
                    'cbot': cbot['price'],
                    'indicator': ind_price,
                    'diff': abs(cbot['price'] - ind_price)
                })

            # Compare SMA M1
            if cbot['sma_m1'] and ind_sma0:
                diff = abs(cbot['sma_m1'] - ind_sma0)
                if diff > 0.00001:  # More than 0.00001 difference
                    differences.append({
                        'timestamp': timestamp,
                        'type': 'SMA_M1',
                        'cbot': cbot['sma_m1'],
                        'indicator': ind_sma0,
                        'diff': diff
                    })

            # Compare SMA TF2
            if cbot['sma_tf2'] and ind_sma1:
                diff = abs(cbot['sma_tf2'] - ind_sma1)
                if diff > 0.00001:
                    differences.append({
                        'timestamp': timestamp,
                        'type': 'SMA_TF2',
                        'cbot': cbot['sma_tf2'],
                        'indicator': ind_sma1,
                        'diff': diff
                    })

            # Compare SMA TF3
            if cbot['sma_tf3'] and ind_sma2:
                diff = abs(cbot['sma_tf3'] - ind_sma2)
                if diff > 0.00001:
                    differences.append({
                        'timestamp': timestamp,
                        'type': 'SMA_TF3',
                        'cbot': cbot['sma_tf3'],
                        'indicator': ind_sma2,
                        'diff': diff
                    })

    if not differences:
        print("[OK] All SMA values match perfectly!")
        print("   -> The calculation methods produce identical results.")
    else:
        print(f"[WARNING]  Found {len(differences)} differences in SMA values!")
        print("\nFirst 20 differences:")
        for i, diff in enumerate(differences[:20], 1):
            print(f"  {i}. {diff['timestamp']} | {diff['type']}")
            print(f"     cBot: {diff['cbot']:.5f} | Indicator: {diff['indicator']:.5f} | Diff: {diff['diff']:.5f}")

        # Analyze differences by type
        diff_by_type = {}
        for diff in differences:
            diff_type = diff['type']
            if diff_type not in diff_by_type:
                diff_by_type[diff_type] = []
            diff_by_type[diff_type].append(diff['diff'])

        print("\nDifferences summary by type:")
        for dtype, diffs in diff_by_type.items():
            avg_diff = sum(diffs) / len(diffs)
            max_diff = max(diffs)
            print(f"  {dtype}: {len(diffs)} differences | Avg: {avg_diff:.5f} | Max: {max_diff:.5f}")

def main():
    # Find most recent CSV files
    data_dir = Path("C:/Users/Jcamp_Laptop/Documents/cAlgo/Data")

    cbot_csvs = list(data_dir.glob("cBots/Jcamp_1M_scalping/JCAMP_cBot_SMA_Debug*.csv"))
    indicator_csvs = list(data_dir.glob("Indicators/JCAMP_MTF_MultiSMA_v2/JCAMP_Indicator_SMA_Debug*.csv"))

    if not cbot_csvs:
        print("[ERROR] No cBot CSV files found!")
        return

    # Get most recent files
    cbot_csv = max(cbot_csvs, key=lambda p: p.stat().st_mtime)

    # Analyze cBot CSV
    cbot_analysis = analyze_cbot_csv(cbot_csv)

    # Compare if indicator CSV exists
    if indicator_csvs:
        indicator_csv = max(indicator_csvs, key=lambda p: p.stat().st_mtime)
        compare_sma_values(cbot_csv, indicator_csv)
    else:
        print("\n[WARNING]  No Indicator CSV files found.")
        print("Please rebuild the indicator and run it on the same chart to generate a CSV for comparison.")

if __name__ == "__main__":
    main()

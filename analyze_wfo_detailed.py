#!/usr/bin/env python3
"""
Detailed WFO Analysis - Compare cBot vs Indicator with focus on trade signals
"""

import csv
from pathlib import Path
from collections import defaultdict

def analyze_wfo_signals():
    """Analyze signal generation patterns with WFO settings"""

    # Find most recent CSV files
    data_dir = Path("C:/Users/Jcamp_Laptop/Documents/cAlgo/Data")

    cbot_csvs = list(data_dir.glob("cBots/Jcamp_1M_scalping/JCAMP_cBot_SMA_Debug*.csv"))
    indicator_csvs = list(data_dir.glob("Indicators/JCAMP_MTF_MultiSMA_v2/JCAMP_Indicator_SMA_Debug*.csv"))

    if not cbot_csvs or not indicator_csvs:
        print("[ERROR] Missing CSV files!")
        return

    cbot_csv = max(cbot_csvs, key=lambda p: p.stat().st_mtime)
    indicator_csv = max(indicator_csvs, key=lambda p: p.stat().st_mtime)

    print("="*80)
    print("WFO SETTINGS - DETAILED SIGNAL ANALYSIS")
    print("="*80)
    print(f"\ncBot CSV: {cbot_csv.name}")
    print(f"Indicator CSV: {indicator_csv.name}\n")

    # Analyze cBot signals
    print("-"*80)
    print("CBOT SIGNAL ANALYSIS")
    print("-"*80)

    cbot_signals = {
        'total_bars': 0,
        'buy_signals': [],
        'sell_signals': [],
        'crossovers_no_signal': 0,
        'mtf_aligned_pct': 0,
        'aligned_bars': 0
    }

    prev_align = None

    with open(cbot_csv, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            cbot_signals['total_bars'] += 1

            m1_cross = row['M1_Crossover']
            mtf_aligned = row['MTF_Aligned']
            timestamp = row['Timestamp']

            if mtf_aligned == 'TRUE':
                cbot_signals['aligned_bars'] += 1

            # Track actual trade signals
            if m1_cross == 'BUY':
                cbot_signals['buy_signals'].append({
                    'timestamp': timestamp,
                    'bar': row['BarIndex'],
                    'price': row['Price_M1'],
                    'mtf': mtf_aligned
                })
            elif m1_cross == 'SELL':
                cbot_signals['sell_signals'].append({
                    'timestamp': timestamp,
                    'bar': row['BarIndex'],
                    'price': row['Price_M1'],
                    'mtf': mtf_aligned
                })

            # Count crossovers that didn't generate signals
            m1_align = row['Align_M1']
            if prev_align and prev_align != 'NONE' and m1_align != 'NONE':
                if prev_align != m1_align and m1_cross == 'NONE':
                    cbot_signals['crossovers_no_signal'] += 1

            prev_align = m1_align

    cbot_signals['mtf_aligned_pct'] = (cbot_signals['aligned_bars'] / cbot_signals['total_bars'] * 100)

    print(f"\nTotal bars analyzed: {cbot_signals['total_bars']:,}")
    print(f"MTF Aligned bars: {cbot_signals['aligned_bars']:,} ({cbot_signals['mtf_aligned_pct']:.1f}%)")
    print(f"\nTrade Signals Generated:")
    print(f"  BUY signals: {len(cbot_signals['buy_signals'])}")
    print(f"  SELL signals: {len(cbot_signals['sell_signals'])}")
    print(f"  TOTAL signals: {len(cbot_signals['buy_signals']) + len(cbot_signals['sell_signals'])}")
    print(f"\nCrossovers WITHOUT signals (filtered): {cbot_signals['crossovers_no_signal']}")

    if cbot_signals['buy_signals']:
        print(f"\nFirst 10 BUY signals:")
        for i, sig in enumerate(cbot_signals['buy_signals'][:10], 1):
            print(f"  {i}. {sig['timestamp']} | Bar {sig['bar']} | Price: {sig['price']} | MTF: {sig['mtf']}")

    if cbot_signals['sell_signals']:
        print(f"\nFirst 10 SELL signals:")
        for i, sig in enumerate(cbot_signals['sell_signals'][:10], 1):
            print(f"  {i}. {sig['timestamp']} | Bar {sig['bar']} | Price: {sig['price']} | MTF: {sig['mtf']}")

    # Analyze Indicator signals
    print("\n" + "-"*80)
    print("INDICATOR SIGNAL ANALYSIS")
    print("-"*80)

    indicator_signals = {
        'total_bars': 0,
        'buy_arrows': [],
        'sell_arrows': [],
        'htf_aligned_bars': 0
    }

    with open(indicator_csv, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            indicator_signals['total_bars'] += 1

            signal = row['Signal']
            htf_aligned = row['HTF_Aligned']
            timestamp = row['Timestamp']

            if htf_aligned == 'TRUE':
                indicator_signals['htf_aligned_bars'] += 1

            if signal == 'BUY_ARROW':
                indicator_signals['buy_arrows'].append({
                    'timestamp': timestamp,
                    'bar': row['BarIndex'],
                    'price': row['Price'],
                    'htf': htf_aligned
                })
            elif signal == 'SELL_ARROW':
                indicator_signals['sell_arrows'].append({
                    'timestamp': timestamp,
                    'bar': row['BarIndex'],
                    'price': row['Price'],
                    'htf': htf_aligned
                })

    indicator_signals['htf_aligned_pct'] = (indicator_signals['htf_aligned_bars'] / indicator_signals['total_bars'] * 100)

    print(f"\nTotal bars analyzed: {indicator_signals['total_bars']:,}")
    print(f"HTF Aligned bars: {indicator_signals['htf_aligned_bars']:,} ({indicator_signals['htf_aligned_pct']:.1f}%)")
    print(f"\nSignal Arrows Generated:")
    print(f"  BUY arrows: {len(indicator_signals['buy_arrows'])}")
    print(f"  SELL arrows: {len(indicator_signals['sell_arrows'])}")
    print(f"  TOTAL arrows: {len(indicator_signals['buy_arrows']) + len(indicator_signals['sell_arrows'])}")

    if indicator_signals['buy_arrows']:
        print(f"\nFirst 10 BUY arrows:")
        for i, sig in enumerate(indicator_signals['buy_arrows'][:10], 1):
            print(f"  {i}. {sig['timestamp']} | Bar {sig['bar']} | Price: {sig['price']} | HTF: {sig['htf']}")

    if indicator_signals['sell_arrows']:
        print(f"\nFirst 10 SELL arrows:")
        for i, sig in enumerate(indicator_signals['sell_arrows'][:10], 1):
            print(f"  {i}. {sig['timestamp']} | Bar {sig['bar']} | Price: {sig['price']} | HTF: {sig['htf']}")

    # Compare signals
    print("\n" + "="*80)
    print("COMPARISON: cBot vs Indicator")
    print("="*80)

    # Convert to sets of timestamps for comparison
    cbot_buy_times = {s['timestamp'] for s in cbot_signals['buy_signals']}
    cbot_sell_times = {s['timestamp'] for s in cbot_signals['sell_signals']}
    ind_buy_times = {s['timestamp'] for s in indicator_signals['buy_arrows']}
    ind_sell_times = {s['timestamp'] for s in indicator_signals['sell_arrows']}

    # Find matches and mismatches
    buy_matches = cbot_buy_times & ind_buy_times
    sell_matches = cbot_sell_times & ind_sell_times

    buy_only_cbot = cbot_buy_times - ind_buy_times
    buy_only_indicator = ind_buy_times - cbot_buy_times
    sell_only_cbot = cbot_sell_times - ind_sell_times
    sell_only_indicator = ind_sell_times - cbot_sell_times

    print(f"\nBUY Signals:")
    print(f"  Matching timestamps: {len(buy_matches)}")
    print(f"  Only in cBot: {len(buy_only_cbot)}")
    print(f"  Only in Indicator: {len(buy_only_indicator)}")

    print(f"\nSELL Signals:")
    print(f"  Matching timestamps: {len(sell_matches)}")
    print(f"  Only in cBot: {len(sell_only_cbot)}")
    print(f"  Only in Indicator: {len(sell_only_indicator)}")

    total_matches = len(buy_matches) + len(sell_matches)
    total_cbot = len(cbot_signals['buy_signals']) + len(cbot_signals['sell_signals'])
    total_indicator = len(indicator_signals['buy_arrows']) + len(indicator_signals['sell_arrows'])

    if total_cbot > 0 and total_indicator > 0:
        match_rate = (total_matches / max(total_cbot, total_indicator) * 100)
        print(f"\n[SUMMARY]")
        print(f"  Total matching signals: {total_matches}")
        print(f"  Match rate: {match_rate:.1f}%")

        if match_rate > 95:
            print(f"  [OK] Excellent match! Entry logic is consistent.")
        elif match_rate > 80:
            print(f"  [WARNING] Good match but some differences exist.")
        else:
            print(f"  [WARNING] Significant differences in signal generation!")

    # Show mismatches if any
    if buy_only_cbot:
        print(f"\nBUY signals only in cBot (first 5):")
        for i, ts in enumerate(list(buy_only_cbot)[:5], 1):
            print(f"  {i}. {ts}")

    if buy_only_indicator:
        print(f"\nBUY signals only in Indicator (first 5):")
        for i, ts in enumerate(list(buy_only_indicator)[:5], 1):
            print(f"  {i}. {ts}")

if __name__ == "__main__":
    analyze_wfo_signals()

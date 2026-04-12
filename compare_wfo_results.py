"""
WFO Walk Validation: Compare WFO Recommendations vs Baseline

Usage:
    python compare_wfo_results.py <walk_number>

Example:
    python compare_wfo_results.py 1
"""

import pandas as pd
import sys
from pathlib import Path

def compare_results(walk_num):
    """Compare WFO vs Baseline results for a validation walk"""

    walk_dir = Path(f"data/wfo_test/walk{walk_num}_*/validate")

    # Find walk directory
    walk_dirs = list(Path("data/wfo_test").glob(f"walk{walk_num}_*"))
    if not walk_dirs:
        print(f"[ERROR] Walk {walk_num} directory not found in data/wfo_test/")
        return

    walk_dir = walk_dirs[0] / "validate"

    # Find CSV files
    wfo_csvs = list(walk_dir.glob("*WFO*.csv"))
    baseline_csvs = list(walk_dir.glob("*Baseline*.csv"))

    if not wfo_csvs:
        print(f"[ERROR] WFO validation CSV not found in {walk_dir}")
        print(f"  Expected: TradeLog_*_WFO.csv")
        return

    if not baseline_csvs:
        print(f"[ERROR] Baseline validation CSV not found in {walk_dir}")
        print(f"  Expected: TradeLog_*_Baseline.csv")
        return

    wfo_csv = wfo_csvs[0]
    baseline_csv = baseline_csvs[0]

    print(f"\n{'='*70}")
    print(f"Walk {walk_num} Validation Results Comparison")
    print(f"{'='*70}\n")
    print(f"WFO CSV: {wfo_csv.name}")
    print(f"Baseline CSV: {baseline_csv.name}")
    print()

    # Load data
    wfo_df = pd.read_csv(wfo_csv)
    baseline_df = pd.read_csv(baseline_csv)

    # Calculate metrics
    def calc_metrics(df, label):
        total_trades = len(df)
        wins = (df['WinningTrade'] == True).sum()
        win_rate = (wins / total_trades * 100) if total_trades > 0 else 0
        total_r = df['RMultiple'].sum()
        avg_r = df['RMultiple'].mean()

        wins_r = df[df['RMultiple'] > 0]['RMultiple'].sum()
        losses_r = abs(df[df['RMultiple'] < 0]['RMultiple'].sum())
        pf = (wins_r / losses_r) if losses_r > 0 else float('inf')

        equity = df['RMultiple'].cumsum()
        running_max = equity.expanding().max()
        drawdown = equity - running_max
        max_dd = drawdown.min()

        return {
            'Total Trades': total_trades,
            'Total R': total_r,
            'Win Rate %': win_rate,
            'Avg R': avg_r,
            'Profit Factor': pf,
            'Max DD (R)': max_dd
        }

    wfo_metrics = calc_metrics(wfo_df, 'WFO')
    baseline_metrics = calc_metrics(baseline_df, 'Baseline')

    # Display comparison
    print(f"{'Metric':<20} {'WFO':<15} {'Baseline':<15} {'Improvement':<15} {'Winner':<10}")
    print("-"*75)

    wfo_wins = 0
    for metric in wfo_metrics.keys():
        wfo_val = wfo_metrics[metric]
        base_val = baseline_metrics[metric]

        # Calculate improvement
        if metric == 'Max DD (R)':
            # For DD, lower is better
            improvement = base_val - wfo_val  # Positive = WFO has less DD
            winner = "WFO ✓" if wfo_val > base_val else "Baseline"  # Less negative = better
            wfo_wins += 1 if wfo_val > base_val else 0
        else:
            # For other metrics, higher is better
            improvement = wfo_val - base_val
            winner = "WFO ✓" if wfo_val > base_val else "Baseline"
            wfo_wins += 1 if wfo_val > base_val else 0

        # Format improvement
        if metric in ['Win Rate %']:
            improvement_str = f"{improvement:+.1f}%"
        elif metric == 'Total Trades':
            improvement_str = f"{improvement:+.0f}"
        else:
            improvement_str = f"{improvement:+.2f}"

        # Format values
        if metric in ['Win Rate %']:
            wfo_str = f"{wfo_val:.1f}%"
            base_str = f"{base_val:.1f}%"
        elif metric == 'Total Trades':
            wfo_str = f"{wfo_val:.0f}"
            base_str = f"{base_val:.0f}"
        else:
            wfo_str = f"{wfo_val:.2f}"
            base_str = f"{base_val:.2f}"

        print(f"{metric:<20} {wfo_str:<15} {base_str:<15} {improvement_str:<15} {winner:<10}")

    print()
    print("="*75)

    # Summary
    total_metrics = len(wfo_metrics)
    win_pct = (wfo_wins / total_metrics) * 100

    print(f"\nSUMMARY:")
    print(f"  WFO won {wfo_wins}/{total_metrics} metrics ({win_pct:.0f}%)")

    if wfo_wins >= total_metrics * 0.75:
        print(f"  Result: ✅ WFO VALIDATION SUCCESS")
        print(f"  WFO recommendations beat baseline on this walk!")
    elif wfo_wins >= total_metrics * 0.5:
        print(f"  Result: ⚠️ WFO MIXED RESULTS")
        print(f"  WFO recommendations showed some improvement but not dominant")
    else:
        print(f"  Result: ❌ WFO VALIDATION FAILED")
        print(f"  Baseline beat WFO recommendations - recommendations did not generalize")

    print()

    # Session comparison if available
    if 'IsLondonSession' in wfo_df.columns and 'IsLondonSession' in baseline_df.columns:
        print("="*75)
        print("SESSION BREAKDOWN")
        print("="*75)

        for session_col, session_name in [
            ('IsLondonSession', 'London'),
            ('IsNYSession', 'NY'),
            ('IsAsianSession', 'Asian')
        ]:
            if session_col not in wfo_df.columns:
                continue

            wfo_sess = wfo_df[wfo_df[session_col] == True]
            base_sess = baseline_df[baseline_df[session_col] == True]

            if len(wfo_sess) > 0 or len(base_sess) > 0:
                wfo_r = wfo_sess['RMultiple'].sum() if len(wfo_sess) > 0 else 0
                base_r = base_sess['RMultiple'].sum() if len(base_sess) > 0 else 0

                print(f"\n{session_name} Session:")
                print(f"  WFO: {len(wfo_sess)} trades, {wfo_r:+.2f}R")
                print(f"  Baseline: {len(base_sess)} trades, {base_r:+.2f}R")

                if len(wfo_sess) == 0 and len(base_sess) > 0:
                    print(f"  → WFO DISABLED this session (was {base_r:+.2f}R)")
                elif len(wfo_sess) > 0 and len(base_sess) == 0:
                    print(f"  → WFO ENABLED this session ({wfo_r:+.2f}R)")

        print()

    return wfo_wins >= total_metrics * 0.75  # Return True if WFO won


if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python compare_wfo_results.py <walk_number>")
        print("\nExample:")
        print("  python compare_wfo_results.py 1")
        print("\nAvailable walks:")
        walks = list(Path("data/wfo_test").glob("walk*"))
        if walks:
            for walk in sorted(walks):
                print(f"  - {walk.name}")
        else:
            print("  (No walks found in data/wfo_test/)")
        sys.exit(1)

    walk_num = sys.argv[1]
    success = compare_results(walk_num)
    sys.exit(0 if success else 1)

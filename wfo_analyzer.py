"""
WFO Trade Log Analyzer
======================
Analyzes detailed trade logs from cBot and recommends optimized parameter settings.

Usage:
    python wfo_analyzer.py TradeLog_EURUSD_12345_20260330_120000.csv

Output:
    - Performance analysis report
    - Recommended parameter settings
    - Visualization dashboard (HTML)
    - Optimized settings export (JSON/CSV)
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
from pathlib import Path
import json
import warnings
warnings.filterwarnings('ignore')

class WFOAnalyzer:
    def __init__(self, log_file):
        self.log_file = Path(log_file)
        self.df = None
        self.results = {}

    def load_data(self):
        """Load and preprocess trade log CSV"""
        print(f"\n{'='*70}")
        print(f"Loading trade log: {self.log_file.name}")
        print(f"{'='*70}\n")

        self.df = pd.read_csv(self.log_file)

        # Convert data types
        self.df['EntryDate'] = pd.to_datetime(self.df['EntryDate'])
        self.df['ExitDate'] = pd.to_datetime(self.df['ExitDate'])
        self.df['WinningTrade'] = self.df['WinningTrade'].astype(bool)

        print(f"[OK] Loaded {len(self.df)} trades")
        print(f"  Date range: {self.df['EntryDate'].min().date()} to {self.df['EntryDate'].max().date()}")
        print(f"  Total profit: ${self.df['ProfitCurrency'].sum():.2f}")
        print(f"  Win rate: {self.df['WinningTrade'].mean()*100:.1f}%")
        print()

        return self

    def analyze_sessions(self):
        """Analyze performance by trading session"""
        print(f"\n{'='*70}")
        print("SESSION ANALYSIS")
        print(f"{'='*70}\n")

        session_stats = []

        # London Session
        london = self.df[self.df['IsLondonSession'] == True]
        if len(london) > 0:
            session_stats.append({
                'Session': 'London (08:00-12:00 UTC)',
                'Trades': len(london),
                'Win Rate': f"{london['WinningTrade'].mean()*100:.1f}%",
                'Avg R': f"{london['RMultiple'].mean():.2f}R",
                'Total R': f"{london['RMultiple'].sum():.2f}R",
                'Profit Factor': self._calc_profit_factor(london),
                'Avg Duration': f"{london['DurationMinutes'].mean():.0f}m"
            })

        # NY Overlap Session
        ny = self.df[self.df['IsNYSession'] == True]
        if len(ny) > 0:
            session_stats.append({
                'Session': 'NY Overlap (13:00-17:00 UTC)',
                'Trades': len(ny),
                'Win Rate': f"{ny['WinningTrade'].mean()*100:.1f}%",
                'Avg R': f"{ny['RMultiple'].mean():.2f}R",
                'Total R': f"{ny['RMultiple'].sum():.2f}R",
                'Profit Factor': self._calc_profit_factor(ny),
                'Avg Duration': f"{ny['DurationMinutes'].mean():.0f}m"
            })

        # Asian Session
        asian = self.df[self.df['IsAsianSession'] == True]
        if len(asian) > 0:
            session_stats.append({
                'Session': 'Asian (04:00-08:00, 20:00-04:00 UTC)',
                'Trades': len(asian),
                'Win Rate': f"{asian['WinningTrade'].mean()*100:.1f}%",
                'Avg R': f"{asian['RMultiple'].mean():.2f}R",
                'Total R': f"{asian['RMultiple'].sum():.2f}R",
                'Profit Factor': self._calc_profit_factor(asian),
                'Avg Duration': f"{asian['DurationMinutes'].mean():.0f}m"
            })

        session_df = pd.DataFrame(session_stats)
        print(session_df.to_string(index=False))
        print()

        # Save all session stats
        self.results['session_breakdown'] = session_stats

        # Determine best session
        if len(session_stats) > 0:
            best_session = max(session_stats, key=lambda x: float(x['Total R'].replace('R', '')))
            print(f">>> BEST SESSION: {best_session['Session']}")
            print(f"   Total R: {best_session['Total R']} | Win Rate: {best_session['Win Rate']}")

            self.results['best_session'] = best_session

        return self

    def analyze_hourly(self):
        """Analyze performance by hour of day"""
        print(f"\n{'='*70}")
        print("HOURLY ANALYSIS")
        print(f"{'='*70}\n")

        hourly = self.df.groupby('EntryHour').agg({
            'PositionID': 'count',
            'WinningTrade': 'mean',
            'RMultiple': ['mean', 'sum'],
            'ProfitCurrency': 'sum'
        }).round(2)

        hourly.columns = ['Trades', 'Win Rate', 'Avg R', 'Total R', 'Profit $']
        hourly['Win Rate'] = (hourly['Win Rate'] * 100).round(1).astype(str) + '%'
        hourly['Avg R'] = hourly['Avg R'].astype(str) + 'R'
        hourly['Total R'] = hourly['Total R'].astype(str) + 'R'

        # Filter to only hours with trades
        hourly = hourly[hourly['Trades'] > 0]

        print(hourly.to_string())
        print()

        # Find best hours (min 5 trades, positive R)
        hourly_numeric = self.df.groupby('EntryHour').agg({
            'PositionID': 'count',
            'RMultiple': 'sum'
        })
        hourly_numeric.columns = ['Trades', 'Total R']

        best_hours = hourly_numeric[
            (hourly_numeric['Trades'] >= 5) &
            (hourly_numeric['Total R'] > 0)
        ].sort_values('Total R', ascending=False)

        if len(best_hours) > 0:
            print(f">>> BEST HOURS (min 5 trades):")
            for hour, row in best_hours.head(5).iterrows():
                print(f"   {hour:02d}:00 UTC - {row['Total R']:.2f}R ({int(row['Trades'])} trades)")

            self.results['best_hours'] = best_hours.head(5).index.tolist()

        return self

    def analyze_direction(self):
        """Analyze BUY vs SELL performance"""
        print(f"\n{'='*70}")
        print("DIRECTION ANALYSIS (BUY vs SELL)")
        print(f"{'='*70}\n")

        direction_stats = []

        for direction in ['Buy', 'Sell']:
            trades = self.df[self.df['Direction'] == direction]
            if len(trades) > 0:
                direction_stats.append({
                    'Direction': direction.upper(),
                    'Trades': len(trades),
                    'Win Rate': f"{trades['WinningTrade'].mean()*100:.1f}%",
                    'Avg R': f"{trades['RMultiple'].mean():.2f}R",
                    'Total R': f"{trades['RMultiple'].sum():.2f}R",
                    'Profit Factor': self._calc_profit_factor(trades),
                    'Avg Duration': f"{trades['DurationMinutes'].mean():.0f}m"
                })

        direction_df = pd.DataFrame(direction_stats)
        print(direction_df.to_string(index=False))
        print()

        # Analyze by session AND direction
        print("DIRECTION PERFORMANCE BY SESSION:")
        print()

        for session_col, session_name in [
            ('IsLondonSession', 'London'),
            ('IsNYSession', 'NY Overlap')
        ]:
            session_trades = self.df[self.df[session_col] == True]
            if len(session_trades) > 0:
                print(f"  {session_name}:")
                for direction in ['Buy', 'Sell']:
                    trades = session_trades[session_trades['Direction'] == direction]
                    if len(trades) >= 5:
                        win_rate = trades['WinningTrade'].mean() * 100
                        total_r = trades['RMultiple'].sum()
                        print(f"    {direction.upper()}: {win_rate:.1f}% WR | {total_r:+.2f}R | {len(trades)} trades")
                print()

        return self

    def analyze_adx(self):
        """Analyze ADX settings and FlipDirection effectiveness"""
        print(f"\n{'='*70}")
        print("ADX FILTER ANALYSIS")
        print(f"{'='*70}\n")

        # ADX Mode comparison
        print("ADX MODE PERFORMANCE:")
        print()

        for mode in self.df['ADXMode'].unique():
            trades = self.df[self.df['ADXMode'] == mode]
            win_rate = trades['WinningTrade'].mean() * 100
            total_r = trades['RMultiple'].sum()
            pf = self._calc_profit_factor(trades)

            print(f"  {mode}:")
            print(f"    Win Rate: {win_rate:.1f}% | Total R: {total_r:+.2f}R | PF: {pf:.2f} | Trades: {len(trades)}")

        print()

        # FlipDirection effectiveness
        print("FLIP DIRECTION ANALYSIS:")
        print()

        flip_trades = self.df[self.df['FlipDirectionUsed'] == True]
        normal_trades = self.df[self.df['FlipDirectionUsed'] == False]

        if len(flip_trades) > 0:
            print(f"  Flip Direction Trades: {len(flip_trades)}")
            print(f"    Win Rate: {flip_trades['WinningTrade'].mean()*100:.1f}%")
            print(f"    Avg R: {flip_trades['RMultiple'].mean():.2f}R")
            print(f"    Total R: {flip_trades['RMultiple'].sum():+.2f}R")
            print(f"    Profit Factor: {self._calc_profit_factor(flip_trades):.2f}")

        print()

        if len(normal_trades) > 0:
            print(f"  Normal Direction Trades: {len(normal_trades)}")
            print(f"    Win Rate: {normal_trades['WinningTrade'].mean()*100:.1f}%")
            print(f"    Avg R: {normal_trades['RMultiple'].mean():.2f}R")
            print(f"    Total R: {normal_trades['RMultiple'].sum():+.2f}R")
            print(f"    Profit Factor: {self._calc_profit_factor(normal_trades):.2f}")

        print()

        # ADX Threshold analysis (bucketed)
        print("ADX THRESHOLD RANGES:")
        print()

        self.df['ADX_Bucket'] = pd.cut(
            self.df['ADXValue'],
            bins=[0, 15, 18, 20, 22, 25, 100],
            labels=['<15', '15-18', '18-20', '20-22', '22-25', '>25']
        )

        adx_threshold = self.df.groupby('ADX_Bucket').agg({
            'PositionID': 'count',
            'WinningTrade': 'mean',
            'RMultiple': 'sum'
        })
        adx_threshold.columns = ['Trades', 'Win Rate', 'Total R']
        adx_threshold['Win Rate'] = (adx_threshold['Win Rate'] * 100).round(1)

        print(adx_threshold.to_string())
        print()

        # Find optimal ADX threshold
        optimal_threshold_data = adx_threshold[adx_threshold['Trades'] >= 10]
        if len(optimal_threshold_data) > 0:
            best_threshold = optimal_threshold_data['Total R'].idxmax()
            print(f">>> BEST ADX RANGE: {best_threshold}")
            print(f"   Total R: {optimal_threshold_data.loc[best_threshold, 'Total R']:.2f}R")
            print(f"   Win Rate: {optimal_threshold_data.loc[best_threshold, 'Win Rate']:.1f}%")

            self.results['best_adx_range'] = str(best_threshold)

        return self

    def analyze_day_of_week(self):
        """Analyze performance by day of week"""
        print(f"\n{'='*70}")
        print("DAY OF WEEK ANALYSIS")
        print(f"{'='*70}\n")

        day_order = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']

        day_stats = self.df.groupby('EntryDayOfWeek').agg({
            'PositionID': 'count',
            'WinningTrade': 'mean',
            'RMultiple': ['mean', 'sum']
        }).round(2)

        day_stats.columns = ['Trades', 'Win Rate', 'Avg R', 'Total R']
        day_stats['Win Rate'] = (day_stats['Win Rate'] * 100).round(1).astype(str) + '%'

        # Reorder by weekday
        day_stats = day_stats.reindex([day for day in day_order if day in day_stats.index])

        print(day_stats.to_string())
        print()

        return self

    def generate_recommendations(self):
        """Generate optimized parameter recommendations"""
        print(f"\n{'='*70}")
        print("RECOMMENDED PARAMETER SETTINGS")
        print(f"{'='*70}\n")

        recommendations = {
            'timestamp': datetime.now().isoformat(),
            'data_range': {
                'start': self.df['EntryDate'].min().isoformat(),
                'end': self.df['EntryDate'].max().isoformat(),
                'total_trades': int(len(self.df))
            },
            'parameters': {},
            'overall_performance': {},
            'performance': {}
        }

        # Calculate OVERALL performance (all trades)
        print("\n" + "="*70)
        print("OVERALL BACKTEST PERFORMANCE (All Trades)")
        print("="*70)
        overall_metrics = self._calc_comprehensive_metrics(self.df)
        recommendations['overall_performance'] = overall_metrics

        print(f"  Total Trades: {overall_metrics['total_trades']}")
        print(f"  Win Rate: {overall_metrics['win_rate']:.1f}%")
        print(f"  Profit Factor: {overall_metrics['profit_factor']:.2f}")
        print(f"  Total R: {overall_metrics['total_r']:+.2f}R")
        print(f"  Average R: {overall_metrics['average_r']:+.2f}R")
        print(f"  Max Drawdown: {overall_metrics['max_drawdown']:.2f}R")
        print(f"  Avg Win: {overall_metrics['avg_win']:+.2f}R")
        print(f"  Avg Loss: {overall_metrics['avg_loss']:+.2f}R")
        print(f"  Consecutive Wins: {overall_metrics['consecutive_wins']}")
        print(f"  Consecutive Losses: {overall_metrics['consecutive_losses']}")
        print()

        # Add session breakdown
        if 'session_breakdown' in self.results:
            recommendations['session_breakdown'] = self.results['session_breakdown']

        # Session recommendation
        if 'best_session' in self.results:
            best = self.results['best_session']
            session_name = best['Session']

            if 'London' in session_name:
                recommendations['parameters']['EnableLondonSession'] = True
                recommendations['parameters']['EnableNYSession'] = False
                recommendations['parameters']['EnableAsianSession'] = False
                print(f"[OK] SESSION: London Only (08:00-12:00 UTC)")
                print(f"  Reason: {best['Total R']} total return, {best['Win Rate']} win rate")
            elif 'NY' in session_name:
                recommendations['parameters']['EnableLondonSession'] = False
                recommendations['parameters']['EnableNYSession'] = True
                recommendations['parameters']['EnableAsianSession'] = False
                print(f"[OK] SESSION: NY Overlap Only (13:00-17:00 UTC)")
                print(f"  Reason: {best['Total R']} total return, {best['Win Rate']} win rate")

        # ADX Mode recommendation
        adx_modes = {}
        for mode in self.df['ADXMode'].unique():
            trades = self.df[self.df['ADXMode'] == mode]
            adx_modes[mode] = trades['RMultiple'].sum()

        best_mode = max(adx_modes, key=adx_modes.get)
        recommendations['parameters']['ADXMode'] = best_mode
        print(f"\n[OK] ADX MODE: {best_mode}")
        print(f"  Reason: {adx_modes[best_mode]:+.2f}R total return")

        # ADX Threshold recommendation
        if 'best_adx_range' in self.results:
            adx_range = self.results['best_adx_range']
            # Convert range to threshold value (use midpoint)
            threshold_map = {
                '<15': 12,
                '15-18': 16,
                '18-20': 19,
                '20-22': 21,
                '22-25': 23,
                '>25': 27
            }
            recommended_threshold = threshold_map.get(adx_range, 18)
            recommendations['parameters']['ADXMinThreshold'] = recommended_threshold
            print(f"\n[OK] ADX THRESHOLD: {recommended_threshold}")
            print(f"  Reason: {adx_range} range performed best")

        # ADX Period (analyze if varied in backtest)
        adx_periods = self.df['ADXPeriod'].unique()
        if len(adx_periods) > 1:
            period_performance = {}
            for period in adx_periods:
                trades = self.df[self.df['ADXPeriod'] == period]
                if len(trades) >= 10:
                    period_performance[period] = trades['RMultiple'].sum()

            if period_performance:
                best_period = max(period_performance, key=period_performance.get)
                recommendations['parameters']['ADXPeriod'] = int(best_period)
                print(f"\n[OK] ADX PERIOD: {best_period}")
                print(f"  Reason: {period_performance[best_period]:+.2f}R total return")
        else:
            recommendations['parameters']['ADXPeriod'] = int(adx_periods[0])

        # FlipDirection recommendation
        flip_trades = self.df[self.df['FlipDirectionUsed'] == True]
        if len(flip_trades) > 0:
            flip_r = flip_trades['RMultiple'].sum()
            flip_wr = flip_trades['WinningTrade'].mean() * 100
            print(f"\n[OK] FLIP DIRECTION: Effective")
            print(f"  Stats: {flip_r:+.2f}R total, {flip_wr:.1f}% win rate, {len(flip_trades)} trades")

        # Calculate expected performance with recommended settings
        print(f"\n{'='*70}")
        print("EXPECTED PERFORMANCE (With Recommended Settings)")
        print(f"{'='*70}\n")

        # Filter data to recommended settings
        filtered = self.df.copy()

        # Apply session filter
        if recommendations['parameters'].get('EnableLondonSession'):
            filtered = filtered[filtered['IsLondonSession'] == True]
        elif recommendations['parameters'].get('EnableNYSession'):
            filtered = filtered[filtered['IsNYSession'] == True]

        # Apply ADX mode filter
        filtered = filtered[filtered['ADXMode'] == recommendations['parameters']['ADXMode']]

        # Calculate FILTERED performance (recommended configuration)
        print("\n" + "="*70)
        print("RECOMMENDED CONFIGURATION PERFORMANCE (Filtered Trades)")
        print("="*70)

        if len(filtered) > 0:
            filtered_metrics = self._calc_comprehensive_metrics(filtered)
            recommendations['performance'] = filtered_metrics

            print(f"  Total Trades: {filtered_metrics['total_trades']}")
            print(f"  Win Rate: {filtered_metrics['win_rate']:.1f}%")
            print(f"  Profit Factor: {filtered_metrics['profit_factor']:.2f}")
            print(f"  Total R: {filtered_metrics['total_r']:+.2f}R")
            print(f"  Average R: {filtered_metrics['average_r']:+.2f}R")
            print(f"  Max Drawdown: {filtered_metrics['max_drawdown']:.2f}R")
            print(f"  Avg Win: {filtered_metrics['avg_win']:+.2f}R")
            print(f"  Avg Loss: {filtered_metrics['avg_loss']:+.2f}R")
            print(f"  Consecutive Wins: {filtered_metrics['consecutive_wins']}")
            print(f"  Consecutive Losses: {filtered_metrics['consecutive_losses']}")

        print()

        # Save recommendations
        self.recommendations = recommendations
        return self

    def export_settings(self):
        """Export optimized settings in multiple formats"""
        print(f"\n{'='*70}")
        print("EXPORTING OPTIMIZED SETTINGS")
        print(f"{'='*70}\n")

        output_dir = self.log_file.parent / 'wfo_results'
        output_dir.mkdir(exist_ok=True)

        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')

        # JSON export (full data)
        json_file = output_dir / f'recommended_settings_{timestamp}.json'
        with open(json_file, 'w') as f:
            json.dump(self.recommendations, f, indent=2)
        print(f"[OK] Saved: {json_file.name}")

        # CSV export (simple format for cAlgo)
        csv_data = []
        for param, value in self.recommendations['parameters'].items():
            csv_data.append({'Parameter': param, 'Value': value})

        csv_file = output_dir / f'recommended_settings_{timestamp}.csv'
        pd.DataFrame(csv_data).to_csv(csv_file, index=False)
        print(f"[OK] Saved: {csv_file.name}")

        # Human-readable text file
        txt_file = output_dir / f'recommended_settings_{timestamp}.txt'
        with open(txt_file, 'w') as f:
            f.write("OPTIMIZED PARAMETER SETTINGS\n")
            f.write("=" * 70 + "\n\n")
            f.write(f"Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
            f.write(f"Data Range: {self.recommendations['data_range']['start']} to {self.recommendations['data_range']['end']}\n")
            f.write(f"Total Trades Analyzed: {self.recommendations['data_range']['total_trades']}\n\n")

            f.write("RECOMMENDED PARAMETERS:\n")
            f.write("-" * 70 + "\n")
            for param, value in self.recommendations['parameters'].items():
                f.write(f"  {param}: {value}\n")

            f.write("\nEXPECTED PERFORMANCE:\n")
            f.write("-" * 70 + "\n")
            for metric, value in self.recommendations['performance'].items():
                f.write(f"  {metric}: {value}\n")

        print(f"[OK] Saved: {txt_file.name}")
        print(f"\n>>> Output directory: {output_dir}")
        print()

        return self

    def create_visualizations(self):
        """Create visual analysis dashboard"""
        print(f"\n{'='*70}")
        print("GENERATING VISUALIZATIONS")
        print(f"{'='*70}\n")

        output_dir = self.log_file.parent / 'wfo_results'
        output_dir.mkdir(exist_ok=True)

        fig = plt.figure(figsize=(20, 12))

        # 1. Equity curve
        ax1 = plt.subplot(3, 3, 1)
        equity = self.df['RMultiple'].cumsum()
        plt.plot(equity.values, linewidth=2)
        plt.title('Equity Curve (R Multiple)', fontsize=14, fontweight='bold')
        plt.xlabel('Trade Number')
        plt.ylabel('Cumulative R')
        plt.grid(True, alpha=0.3)

        # 2. Hourly heatmap
        ax2 = plt.subplot(3, 3, 2)
        hourly_pivot = self.df.groupby(['EntryDayOfWeek', 'EntryHour'])['RMultiple'].sum().unstack(fill_value=0)
        day_order = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
        hourly_pivot = hourly_pivot.reindex([day for day in day_order if day in hourly_pivot.index])
        sns.heatmap(hourly_pivot, cmap='RdYlGn', center=0, annot=False, fmt='.1f',
                    cbar_kws={'label': 'Total R'}, ax=ax2)
        plt.title('Performance Heatmap (Day x Hour)', fontsize=14, fontweight='bold')
        plt.ylabel('Day of Week')
        plt.xlabel('Hour (UTC)')

        # 3. Session comparison
        ax3 = plt.subplot(3, 3, 3)
        session_r = [
            self.df[self.df['IsLondonSession'] == True]['RMultiple'].sum(),
            self.df[self.df['IsNYSession'] == True]['RMultiple'].sum(),
            self.df[self.df['IsAsianSession'] == True]['RMultiple'].sum()
        ]
        sessions = ['London\n08:00-12:00', 'NY Overlap\n13:00-17:00', 'Asian\n04:00-08:00\n20:00-04:00']
        colors = ['green' if r > 0 else 'red' for r in session_r]
        plt.bar(sessions, session_r, color=colors, alpha=0.7)
        plt.title('Total R by Session', fontsize=14, fontweight='bold')
        plt.ylabel('Total R Multiple')
        plt.axhline(y=0, color='black', linestyle='-', linewidth=0.5)
        plt.grid(True, alpha=0.3, axis='y')

        # 4. Win rate by hour
        ax4 = plt.subplot(3, 3, 4)
        hourly_wr = self.df.groupby('EntryHour')['WinningTrade'].mean() * 100
        plt.bar(hourly_wr.index, hourly_wr.values, color='steelblue', alpha=0.7)
        plt.title('Win Rate by Hour', fontsize=14, fontweight='bold')
        plt.xlabel('Hour (UTC)')
        plt.ylabel('Win Rate (%)')
        plt.grid(True, alpha=0.3, axis='y')

        # 5. ADX threshold analysis
        ax5 = plt.subplot(3, 3, 5)
        adx_buckets = self.df.groupby('ADX_Bucket')['RMultiple'].sum()
        colors_adx = ['green' if r > 0 else 'red' for r in adx_buckets.values]
        plt.bar(range(len(adx_buckets)), adx_buckets.values, color=colors_adx, alpha=0.7)
        plt.xticks(range(len(adx_buckets)), adx_buckets.index, rotation=45)
        plt.title('Total R by ADX Range', fontsize=14, fontweight='bold')
        plt.ylabel('Total R Multiple')
        plt.axhline(y=0, color='black', linestyle='-', linewidth=0.5)
        plt.grid(True, alpha=0.3, axis='y')

        # 6. Direction comparison
        ax6 = plt.subplot(3, 3, 6)
        buy_r = self.df[self.df['Direction'] == 'Buy']['RMultiple'].sum()
        sell_r = self.df[self.df['Direction'] == 'Sell']['RMultiple'].sum()
        direction_r = [buy_r, sell_r]
        colors_dir = ['green' if r > 0 else 'red' for r in direction_r]
        plt.bar(['BUY', 'SELL'], direction_r, color=colors_dir, alpha=0.7)
        plt.title('Total R by Direction', fontsize=14, fontweight='bold')
        plt.ylabel('Total R Multiple')
        plt.axhline(y=0, color='black', linestyle='-', linewidth=0.5)
        plt.grid(True, alpha=0.3, axis='y')

        # 7. Flip direction effectiveness
        ax7 = plt.subplot(3, 3, 7)
        flip_r = self.df[self.df['FlipDirectionUsed'] == True]['RMultiple'].sum()
        normal_r = self.df[self.df['FlipDirectionUsed'] == False]['RMultiple'].sum()
        flip_data = [normal_r, flip_r]
        colors_flip = ['green' if r > 0 else 'red' for r in flip_data]
        plt.bar(['Normal', 'Flipped'], flip_data, color=colors_flip, alpha=0.7)
        plt.title('FlipDirection Effectiveness', fontsize=14, fontweight='bold')
        plt.ylabel('Total R Multiple')
        plt.axhline(y=0, color='black', linestyle='-', linewidth=0.5)
        plt.grid(True, alpha=0.3, axis='y')

        # 8. Trade duration distribution
        ax8 = plt.subplot(3, 3, 8)
        winners = self.df[self.df['WinningTrade'] == True]['DurationMinutes']
        losers = self.df[self.df['WinningTrade'] == False]['DurationMinutes']
        plt.hist([winners, losers], bins=20, label=['Winners', 'Losers'], alpha=0.7, color=['green', 'red'])
        plt.title('Trade Duration Distribution', fontsize=14, fontweight='bold')
        plt.xlabel('Duration (minutes)')
        plt.ylabel('Frequency')
        plt.legend()
        plt.grid(True, alpha=0.3, axis='y')

        # 9. R-multiple distribution
        ax9 = plt.subplot(3, 3, 9)
        plt.hist(self.df['RMultiple'], bins=30, color='steelblue', alpha=0.7, edgecolor='black')
        plt.axvline(x=0, color='red', linestyle='--', linewidth=2, label='Break-even')
        plt.axvline(x=self.df['RMultiple'].mean(), color='green', linestyle='--', linewidth=2, label=f'Mean: {self.df["RMultiple"].mean():.2f}R')
        plt.title('R-Multiple Distribution', fontsize=14, fontweight='bold')
        plt.xlabel('R Multiple')
        plt.ylabel('Frequency')
        plt.legend()
        plt.grid(True, alpha=0.3, axis='y')

        plt.tight_layout()

        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        chart_file = output_dir / f'analysis_dashboard_{timestamp}.png'
        plt.savefig(chart_file, dpi=150, bbox_inches='tight')
        print(f"[OK] Saved: {chart_file.name}")

        plt.close()

        return self

    def _calc_profit_factor(self, df):
        """Calculate profit factor"""
        wins = df[df['ProfitCurrency'] > 0]['ProfitCurrency'].sum()
        losses = abs(df[df['ProfitCurrency'] < 0]['ProfitCurrency'].sum())

        if losses == 0:
            return float('inf') if wins > 0 else 0

        return round(wins / losses, 2)

    def _calc_comprehensive_metrics(self, df):
        """Calculate comprehensive performance metrics for a dataframe"""
        if len(df) == 0:
            return {}

        wins = df[df['WinningTrade'] == True]
        losses = df[df['WinningTrade'] == False]

        # Calculate cumulative R for drawdown
        cumulative_r = df['RMultiple'].cumsum()
        running_max = cumulative_r.expanding().max()
        drawdown = cumulative_r - running_max
        max_dd = drawdown.min()

        # Calculate consecutive streaks
        streaks = (df['WinningTrade'] != df['WinningTrade'].shift()).cumsum()
        win_streaks = df[df['WinningTrade'] == True].groupby(streaks).size()
        loss_streaks = df[df['WinningTrade'] == False].groupby(streaks).size()

        metrics = {
            'total_trades': int(len(df)),
            'win_rate': round(df['WinningTrade'].mean() * 100, 1),
            'profit_factor': self._calc_profit_factor(df),
            'total_r': round(df['RMultiple'].sum(), 2),
            'average_r': round(df['RMultiple'].mean(), 2),
            'max_drawdown': round(max_dd, 2),
            'avg_win': round(wins['RMultiple'].mean(), 2) if len(wins) > 0 else 0.0,
            'avg_loss': round(losses['RMultiple'].mean(), 2) if len(losses) > 0 else 0.0,
            'win_loss_ratio': round(abs(wins['RMultiple'].mean() / losses['RMultiple'].mean()), 2) if len(losses) > 0 and losses['RMultiple'].mean() != 0 else 0.0,
            'consecutive_wins': int(win_streaks.max()) if len(win_streaks) > 0 else 0,
            'consecutive_losses': int(loss_streaks.max()) if len(loss_streaks) > 0 else 0
        }

        return metrics

    def run_full_analysis(self):
        """Run complete analysis pipeline"""
        self.load_data()
        self.analyze_sessions()
        self.analyze_hourly()
        self.analyze_direction()
        self.analyze_adx()
        self.analyze_day_of_week()
        self.generate_recommendations()
        self.export_settings()
        self.create_visualizations()

        print(f"\n{'='*70}")
        print("ANALYSIS COMPLETE!")
        print(f"{'='*70}\n")
        print("Next steps:")
        print("1. Review the recommended settings in wfo_results/")
        print("2. Apply settings to your cBot in cAlgo")
        print("3. Run out-of-sample backtest to validate")
        print("4. If validated, deploy to demo with monitoring")
        print()


if __name__ == '__main__':
    import sys

    if len(sys.argv) < 2:
        print("Usage: python wfo_analyzer.py <path_to_trade_log.csv>")
        print("\nExample:")
        print("  python wfo_analyzer.py TradeLog_EURUSD_12345_20260330_120000.csv")
        sys.exit(1)

    log_file = sys.argv[1]

    if not Path(log_file).exists():
        print(f"Error: File not found: {log_file}")
        sys.exit(1)

    analyzer = WFOAnalyzer(log_file)
    analyzer.run_full_analysis()

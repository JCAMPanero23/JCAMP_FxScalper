# JCAMP FxScalper v4.1.3

MTF SMA Alignment Strategy with ADX FlipDirection mode and Consecutive Loss Protection.

## Quick Start

```bash
# Edit source
D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs

# Copy to cAlgo and rebuild
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
```

## Current Parameters (v4.1.3)

| Parameter | Value | Notes |
|-----------|-------|-------|
| MTF SMA Period | 275 | Core trend detection |
| Timeframe 2 | M4 | Medium-term alignment |
| Timeframe 3 | M15 | Higher TF confirmation |
| ADX Mode | FlipDirection | Contrarian in ranging |
| ADX Period | 18 | |
| ADX Threshold | 15 | Below = flip direction |
| Minimum RR | 5.0 | High-quality setups only |
| Daily Loss Limit | -3R | Stops trading for the day |
| Consecutive Loss Limit | 9 losses | Strategy degradation warning |
| Monthly DD Limit | 10% | Stop and re-optimize |

**Optimization File:** `Jcamp_1M_scalping, EURUSD m1_v4.1.2.optset`

## Risk Management (3 Layers)

### Layer 1: Daily Loss Limit
- **Trigger**: `-3R` loss in a single day
- **Action**: Stop trading for rest of day
- **Purpose**: Protect against bad trading days
- **Reset**: Automatically at midnight UTC
- **For Optimization**: Keep enabled

### Layer 2: Consecutive Loss Limit
- **Trigger**: `9 consecutive losses` (no wins between)
- **Action**: Stop trading until manual restart
- **Purpose**: Detect strategy degradation / market regime change
- **Reset**: Only on winning trade OR manual bot restart
- **For Optimization**: **DISABLE** (set to false)
- **Backtest Data**: 15 consecutive losses occurred (9 would have warned mid-streak)
- **Response**: Re-optimize parameters OR switch to another currency pair

### Layer 3: Monthly Drawdown Limit
- **Trigger**: `10%` drawdown from month start equity
- **Action**: Stop trading until next month
- **Purpose**: Prevent catastrophic monthly losses
- **Reset**: First day of new month
- **For Optimization**: Keep enabled

## Backtest Results (Nov 2025 - Feb 2026)

| Period | Result | Notes |
|--------|--------|-------|
| Nov-Jan | +36% | FlipDirection 75% win rate |
| Feb | -10% | Capped (was -15%) |
| **Net** | **+26%** | |

## Optimization Guide

### Schedule

| Parameter | Frequency | Range |
|-----------|-----------|-------|
| ADX Threshold | Monthly | 15-25 |
| ADX Period | Monthly | 14-21 |
| SMA Period | Quarterly | 200-300 |
| Timeframe 2 | Quarterly | M2-M5 |
| Timeframe 3 | Quarterly | M10-M30 |

### Target Priority

| Priority | Metric | Target |
|----------|--------|--------|
| 1st | Profit Factor | 1.3 - 2.0 |
| 2nd | Max Drawdown | < 20% |
| 3rd | Net Profit | > 0 |
| 4th | Win Rate | 25-45% |

### Overfitting Warnings
- Profit Factor > 3.0
- Win Rate > 60%
- < 30 trades
- 1-2 trades = most profit

### Trigger
If monthly DD limit (10%) hit → re-optimize immediately.

## Key Files

| File | Purpose |
|------|---------|
| `Jcamp_1M_scalping.cs` | Main bot (edit this) |
| `Jcamp_1M_scalping_MTF.cs` | Clean backup reference |
| `TrendModeRectangleIndicator.cs` | Visual indicator |
| `optimization_sets/` | Saved optimization configs |
| `Backtest/` | Backtest results |
| `archive/` | Old docs and code |

## Version History

### v4.1.3 (Current)
- Consecutive loss limit (default: 9 losses)
- Warns when strategy degradation detected
- Designed for Chandelier-based strategies
- Independent from daily loss count

### v4.1.2
- Monthly drawdown limit (10% from month start)
- Saved 5% in Feb 2026 backtest

### v4.1.1
- ADX FlipDirection mode
- +36% vs +19% without flip

### v4.1.0
- ADX filter + Exhaustion Exit

### v4.0.0
- MTF SMA Alignment entry system
- Clean rewrite (~600 lines)

### v1-v3 (Deprecated)
Zone-based entry system. Files archived in `archive/old_code/`.

# JCAMP FxScalper v4.6.0-WFO

**4-Timeframe MTF System** with TF0 Entry Trigger, ADX FlipDirection mode, Chandelier Trailing SL, and Advanced Risk Protection.

> **Latest Update**: v4.6.0 (2026-04-10) - Separates entry trigger (TF0) from alignment confirmation to eliminate M1 noise

## Quick Start

### cBot (Trading Bot)

```bash
# Edit source
D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs

# Copy to cAlgo and rebuild
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
```

### Indicator (Visual Entry Signals)

```bash
# Edit source
D:\JCAMP_FxScalper\Indicator\JCAMP_MTF_MultiSMA_v2.cs

# Copy to cAlgo and rebuild
cp "D:\JCAMP_FxScalper\Indicator\JCAMP_MTF_MultiSMA_v2.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Indicators\JCAMP_MTF_MultiSMA_v2\JCAMP_MTF_MultiSMA_v2\JCAMP_MTF_MultiSMA_v2.cs"
```

## MTF SMA Indicator (v3 - cBot-aligned)

Visual indicator that shows the same entry signals as the cBot. Use on M1 chart for real-time signal alerts.

### Entry Logic
- **Signal Type**: M1 price crosses M1 SMA when higher TFs (TF1 + TF2) already aligned
- **BUY Arrow**: TF1+TF2 aligned BULL → M1 price crosses ABOVE M1 SMA
- **SELL Arrow**: TF1+TF2 aligned BEAR → M1 price crosses BELOW M1 SMA
- **Bar Close**: Enabled by default (no repaints)

### Recommended Settings (Match cBot)

| Parameter | Value | Notes |
|-----------|-------|-------|
| SMA Period | 275 | Match cBot's MTFSMAPeriod |
| TF1 Timeframe | Minute4 | Match cBot's Timeframe2 |
| TF2 Timeframe | Minute15 | Match cBot's Timeframe3 |
| Require All TFs Aligned | false | 2/2 alignment mode |
| Require Bar Close | true | No repaints |
| Enable Signal Layer | true | Show arrows |

### Features
- 3 MTF SMA lines (M1, M4, M15)
- Real-time alignment status panel (top-left)
- Entry signal arrows (green BUY, red SELL)
- Audio alerts on signal
- No repaint mode (bar-close confirmation)

---

## 🎯 4-Timeframe System (v4.6.0)

### Architecture Overview

| Timeframe | Default | Range | Purpose |
|-----------|---------|-------|---------|
| **M1** | M1 | M1 (chart) | Alignment check |
| **TF0** ✨ | **M4** | **M2-M6** | **Entry trigger** (crossover detection) |
| **TF1** | M10 | M7-M10 | Medium-term alignment |
| **TF2** | M30 | M15/M20/M30 | Higher-term alignment |

### Entry Logic
1. ✅ Check all 4 TFs aligned (M1 + TF0 + TF1 + TF2)
2. ⏳ Wait for **TF0 crossover** (TF0 price crosses TF0 SMA)
3. 🚀 Enter trade (apply ADX flip if needed)

**Key Insight**: TF0 (M2-M6) provides cleaner crossover signals than noisy M1!

---

## Current Parameters (v4.6.0-WFO)

| Parameter | Value | Notes |
|-----------|-------|-------|
| **MTF SMA Period** | 275 | Same SMA period across all TFs |
| **TF0 (Entry Trigger)** | **M4** | **Crossover detection** (optimize M2-M6) |
| **TF1 (Medium Term)** | M10 | Trend filter (optimize M7-M10) |
| **TF2 (Higher Term)** | M30 | Higher TF bias (optimize M15/M20/M30) |
| ADX Mode | FlipDirection | Contrarian in ranging markets |
| ADX Period | 9 | ADX calculation period |
| ADX Threshold | 23 | Below = flip direction (optimize: 15-30) |
| ADX Max Threshold | 40 | Above = block entry (optimize: 35-50) |
| Minimum RR | 4.0 | High-quality setups only |
| Daily Loss Limit | -3R | Stops trading for the day |
| Consecutive Loss Limit | 9 losses | Strategy degradation warning |
| Monthly DD Limit | 10% | Stop and re-optimize |
| **Close on DD Limit** | **true** | **Closes all positions when DD hit** |

**Optimization File**: `Jcamp_1M_scalping_v4.6.0_4TF_System.optset`

**Documentation**:
- Quick Start: `debug/READY_TO_OPTIMIZE_v4.6.0.md`
- Full Guide: `optimization_sets/v4.6.0_OPTIMIZATION_GUIDE.md`
- Architecture: `debug/4TF_SYSTEM_v4.6.0.md`

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
- **Action**: Stop trading until next month + **Close all positions** (v4.4.0+)
- **Purpose**: Prevent catastrophic monthly losses
- **Reset**: First day of new month
- **For Optimization**: Keep enabled
- **Close on DD**: When enabled (default), closes all open positions immediately to prevent further losses

## Backtest Results (Nov 2025 - Feb 2026)

| Period | Result | Notes |
|--------|--------|-------|
| Nov-Jan | +36% | FlipDirection 75% win rate |
| Feb | -10% | Capped (was -15%) |
| **Net** | **+26%** | |

## Optimization Guide (v4.6.0)

### Recommended Approach: 3-Phase Optimization

**Phase 1** (Most Important!): Find Optimal **TF0** (Entry Trigger)
- **Optimize**: TF0 (M2, M3, M4, M5, M6)
- **Fix all others**: TF1=M10, TF2=M30, SMA=275, ADX defaults
- **Goal**: Find which TF0 gives **positive R-multiple**
- **Time**: ~5-10 minutes (only 5 combinations!)

**Phase 2**: Optimize SMA + ADX with Best TF0
- **Use**: Best TF0 from Phase 1
- **Optimize**: SMA Period (200-300), ADX Period (7-21), ADX Threshold (15-30)
- **Goal**: Fine-tune for best Profit Factor
- **Time**: ~30-60 minutes

**Phase 3**: Fine-tune TF1 + TF2 (Optional)
- **Optimize**: TF1 (M7-M10), TF2 (M15/M20/M30)
- **Goal**: Minor improvements
- **Time**: ~10 minutes

### Parameter Ranges (v4.6.0)

| Parameter | Range | Step | Priority |
|-----------|-------|------|----------|
| **TF0 (Entry)** | M2-M6 | - | **Highest** ⭐ |
| TF1 (Medium) | M7-M10 | - | Low |
| TF2 (Higher) | M15, M20, M30 | - | Low |
| SMA Period | 200-300 | 25 | High |
| ADX Period | 7-21 | 1-2 | Medium |
| ADX Threshold | 15-30 | 1-2 | High |
| ADX Max | 35-50 | 5 | Medium |
| Min RR | 3.0-6.0 | 0.5 | Medium |

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
| `Indicator/JCAMP_MTF_MultiSMA_v2.cs` | **MTF SMA Entry Indicator (cBot-aligned)** |
| `TrendModeRectangleIndicator.cs` | Old visual indicator (deprecated) |
| `optimization_sets/` | Saved optimization configs |
| `Backtest/` | Backtest results |
| `archive/` | Old docs and code |

## Version History

### v4.6.0-WFO (Current - 2026-04-10)
- **🎯 NEW: 4-Timeframe MTF System** - Separates entry trigger from alignment confirmation
  - **TF0 (M2-M6)**: Entry trigger - crossover detection (replaces noisy M1)
  - **TF1 (M7-M10)**: Medium-term alignment (renamed from TF2)
  - **TF2 (M15/M20/M30)**: Higher-term alignment (renamed from TF3)
  - **M1**: Chart timeframe - still part of alignment check
  - **Rationale**: M1 too noisy for crossover signals → TF0 provides cleaner entry triggers
  - **Impact**: Reduces false signals from M1 whipsaws while maintaining responsive entries
  - **Parameters**: Added `Timeframe0` (default: M4), renamed `Timeframe2→Timeframe1`, `Timeframe3→Timeframe2`
  - **CSV Logging**: Updated to track all 4 TFs with TF0_Crossover column
  - **Notifications**: Show M1:X TF0:X TF1:X TF2:X format
  - ⚠️ **Breaking Change**: Requires re-optimization (parameter structure changed)

### v4.5.1-WFO (2026-04-10)
- **🚨 CRITICAL BUG FIX**: M1 crossover detection now updates every bar
  - **Problem**: `_previousM1Alignment` only updated when MTF aligned → stale values from hours/days ago
  - **Symptom**: Trades triggered on TF2 (M2) crossovers, NOT M1 crossovers as intended
  - **Example**: 2026-04-10 10:41 trade - M1 was SELL for 3+ bars, but trade executed when M2 crossed
  - **Fix**: Update `_previousM1Alignment` every bar in `OnBar()`, after `ProcessMTFSMAEntry()`
  - **Impact**: Ensures accurate M1 crossover detection using immediate previous bar, not stale data
  - **Note**: Led to discovery that M1 too noisy → v4.6.0 introduces TF0 trigger system

### v4.4.0-WFO
- **CRITICAL BUG FIX**: Chandelier SL safety check - restores SL if position loses it
- **NEW**: Close all positions when Monthly DD limit hit (prevents unprotected trades)
- **FIX**: Use saved SL from state instead of position.StopLoss (may be null)
- WFO analysis integration with equity curve degradation detection
- Custom GetFitness with equity slope analysis
- CSV export toggle for optimization runs

### v4.3.0-WFO
- WFO (Walk-Forward Optimization) logging system
- Advanced filters: Hour/Day/Direction
- Session filtering with Asian/London/NY support
- ADX Range mode

### v4.1.3
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

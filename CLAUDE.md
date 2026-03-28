# JCAMP FxScalper - Claude Context

## File Locations

### Main Bot Code (EDIT THIS LOCATION)
**Repository file (source of truth):**
```
D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs
```
Edit this file only.

**cAlgo location (build location):**
```
C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs
```
cAlgo builds from this file.

**Workflow:**
1. Edit: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs`
2. Copy to cAlgo: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`
3. Rebuild in cTrader
4. Commit changes to git

**Note:** Symlinks don't work across drives (C: and D:), so manual copy is required.

**Backup:** Original cAlgo file saved as `Jcamp_1M_scalping.cs.backup` in the cAlgo directory.

### Backtest Logs Location

The most recent backtest logs are located at:
```
C:\Users\Jcamp_Laptop\Documents\cAlgo\Data\cBots\Jcamp_1M_scalping\ecdeed2d-0789-4f73-a2b9-1fdc36f0aee6-Default\Backtesting\log.txt
```

Also available via local symlink: `D:\JCAMP_FxScalper\Backtest\log.txt` (may be older)

## Key Files

- `Jcamp_1M_scalping.cs` - Main trading bot code
- `TrendModeRectangleIndicator.cs` - Trend mode indicator

## Optimized Parameters (v4.1.2 - Nov 2025 - Feb 2026)

Based on optimization runs with ADX FlipDirection mode:

| Parameter | Value | Notes |
|-----------|-------|-------|
| MTF SMA Period | 275 | Stable across periods |
| Timeframe 2 | M4 | Slower = fewer false signals |
| Timeframe 3 | M15 | Higher TF confirmation |
| SL ATR Multiplier | 2.0 | Tighter initial SL |
| SL Buffer Pips | 4.0 | More room for trades |
| Minimum RR Ratio | 5.0 | Only high-quality setups |
| Chandelier Activation | 0.75 | Let winners run longer |
| Enable Daily Loss Limit | Yes | -3R max, 5 losses max |
| Enable ADX Filter | Yes | FlipDirection mode |
| ADX Mode | FlipDirection | Contrarian in ranging markets |
| ADX Period | 18 | Slightly longer than default |
| ADX Min Threshold | 15 | Lower = more flips |
| Enable Monthly DD Limit | Yes | 10% max drawdown per month |

**Performance (Nov 2025 - Feb 2026):**
| Period | Result | Notes |
|--------|--------|-------|
| Nov-Jan 2026 | +36% | FlipDirection 75% win rate |
| Feb 2026 | -10% | Capped by monthly limit (was -15%) |
| **Net** | **+26%** | vs +21% without monthly limit |

**Recommendation:** Re-optimize monthly using last 2-3 months of data.

## Version History

### v4.1.2 (2026-03-29)
**Feature:** Monthly Drawdown Limit
- Stop trading if equity drops 10% from month start
- Based on NET loss from month start (not peak drawdown)
- Resumes automatically at start of next month
- Saved 5% in Feb 2026 (-10% vs -15% without limit)
- Branch: `feature/adx-exhaustion`

### v4.1.1 (2026-03-29)
**Feature:** ADX FlipDirection Mode
- New ADX mode: FlipDirection (contrarian in ranging markets)
- When ADX < threshold, reverse trade direction (BUY→SELL, SELL→BUY)
- 75% win rate on flipped trades in Nov-Jan 2026 backtest
- +36% with FlipDirection vs +19% without
- Branch: `feature/adx-exhaustion`

### v4.1.0 (2026-03-29)
**Feature:** ADX Filter + Exhaustion Exit Protection
- **ADX Filter:** DirectionalMovementSystem indicator filters ranging markets
  - Configurable period (default: 14) and threshold (default: 20)
  - BlockEntry mode: Entry skipped when ADX < threshold
- **Exhaustion Exit:** RSI divergence detection from v3.0 spec
  - SELL: Bullish divergence (Higher Lows + RSI Lower Lows)
  - BUY: Bearish divergence (Lower Highs + RSI Higher Highs)
  - Swing detection via N-bar method (default: 8 bars)
  - Confirmation bar prevents false signals
  - Activates after chandelier trails N times (default: 2)
  - Disabled by default (EnableExhaustionExit = false)
- Branch: `feature/adx-exhaustion`

### v4.0.0 (2026-03-29)
**Feature:** MTF SMA Alignment Entry System
- New entry strategy: Trade when price > SMA on ALL configured timeframes
- M1 (fixed) + TF2 (configurable) + TF3 (configurable) alignment required
- Entry trigger: M1 SMA crossover while higher TFs already aligned
- Session filter: BEST (13:00-17:00 UTC) and GOOD (08:00-12:00 UTC) periods only
- ATR-based stop loss with configurable multiplier
- Chandelier trailing stop with RR-based activation
- Daily loss limit protection (-3R or 5 losses)
- Clean implementation (~600 lines vs 5500+ in v3.x)
- Optimized defaults: SMA 275, M4+M15, RR 5.0
- Branch: `feature/mtf-sma-alignment` → merged to master

### v3.1.0 (2026-03-27)
**Feature:** Zone Management Fixes + Debug Logging
- Issue #1: Invalidate ARMED zones when price moves > MaxPriceDistancePips away
- Issue #2: Invalidate all active zones when entering danger session
- Issue #3: Enforce MinimumSLPips floor for stop loss
- Issue #5: Block zone creation and arming when position is open
- DebugTradeLogger: Captures first 3 trades per category (16 categories total)
- Outputs detailed and summary logs to D:\JCAMP_FxScalper\DebugLogs\
- Reversal entry system placeholder (not yet implemented)
- Design spec: `Docs/superpowers/specs/2026-03-25-zone-manager-refactor-design.md`
- Implementation plan: `Docs/superpowers/plans/2026-03-27-zone-manager-refactor.md`

### v3.0.0 (2026-03-18)
**Feature:** Exhaustion Exit Protection
- Detects market exhaustion via swing pattern + RSI divergence
- SELL: Exit when 2 consecutive Higher Lows + RSI Lower Lows (bullish divergence)
- BUY: Exit when 2 consecutive Lower Highs + RSI Higher Highs (bearish divergence)
- Activates only after chandelier makes 2+ trailing moves (configurable)
- N-bar swing detection with confirmation/invalidation mechanism
- Defaults: disabled (EnableExhaustionExit=false), conservative settings
- Design spec: `Docs/superpowers/specs/2026-03-18-exhaustion-exit-design.md`
- Implementation plan: `Docs/superpowers/plans/2026-03-18-exhaustion-exit-v3.md`

### v2.0.0 (2026-03-16)
**Features:** Enhanced Entry System + Chandelier Trailing Stop
- FVG (Fair Value Gap) zones with rejection confirmation
- RSI compression-expansion entry filter
- ATR-based stop loss
- Dual SMA trend filter (SMA 50 + SMA 200)
- Chandelier trailing stop with configurable activation (RR-based)
- Three TP modes: KeepOriginal, RemoveTP, TrailingTP
- Design spec: `Docs/superpowers/specs/2026-03-14-chandelier-sl-design.md`

### v1.0.0 (Initial)
**Core Features:**
- M15 SMA 200 trend detection
- Williams Fractal swing detection
- Rectangle entry zones (swing high/low)
- M1 timeframe execution
- Session filtering (London/NY overlap)

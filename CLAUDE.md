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

## Version History

### v4.0.0 (2026-03-28)
**Feature:** MTF SMA Alignment Entry System
- New entry strategy: Trade when price > SMA on ALL configured timeframes
- M1 (fixed) + TF2 (default M3) + TF3 (default M5) alignment required
- Entry trigger: M1 SMA crossover while higher TFs already aligned
- Session filter: BEST (13:00-17:00 UTC) and GOOD (08:00-12:00 UTC) periods only
- Toggle: EnableMTFSMAEntry (default: true) switches between MTF and zone-based entry
- ATR-based stop loss maintained from v2.0
- Keeps all existing risk management, chandelier trailing, and debug logging
- Branch: `feature/mtf-sma-alignment`

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

# JCAMP_FxScalper - Automated MT5 Expert Advisor

## Project Overview

JCAMP_FxScalper is a fully automated MetaTrader 5 Expert Advisor implementing a trend-following scalping strategy with strict risk management for small accounts ($500+).

**Current Status:** Phase 1 - Single Pair (EURUSD) Implementation
**Target Broker:** FP Markets Raw Account ($3 commission/lot/side)
**Minimum Account:** $500 USD

---

## Key Features

### Core Strategy
- **Trend Following:** SMA alignment (21>50>200) with RSI momentum confirmation
- **Scalping Execution:** M5 timeframe entries with H1 structural context
- **Partial Profits:** 80% exit at 2:1 R:R, 20% runner with RSI divergence monitoring
- **Dynamic Risk:** 1-2% risk per trade (user selectable)

### Advanced Enhancements
1. **Multi-Session Trading:** Toggle London, New York, Asian, Tokyo sessions independently
2. **TP Validation Filter:** Aborts trades if Take Profit would cross H1 resistance/support levels
3. **SL Snapping:** Aligns Stop Loss to nearby H1 structural levels for logical invalidation
4. **Commission-Aware Break-Even:** Accounts for ECN commission in BE calculations

### Risk Management
- Max 1 position for EURUSD (Phase 1)
- Daily loss limit protection
- Dynamic lot sizing based on SL distance
- Max spread filter (1.0 pips default)

---

## Installation

### Step 1: Copy Files to MT5
```
1. Locate your MT5 data folder:
   File → Open Data Folder in MT5

2. Copy files:
   JCAMP_FxScalper/MQL5/Experts/JCAMP_FxScalper_v1.mq5
   → [MT5 Data Folder]/MQL5/Experts/

   JCAMP_FxScalper/MQL5/Include/JC_*.mqh
   → [MT5 Data Folder]/MQL5/Include/

   JCAMP_FxScalper/MQL5/Presets/JCAMP_FxScalper_EURUSD.set
   → [MT5 Data Folder]/MQL5/Presets/
```

### Step 2: Compile EA
```
1. Open MetaEditor (F4 in MT5)
2. Navigate to Experts/JCAMP_FxScalper_v1.mq5
3. Compile (F7) - should show 0 errors
```

### Step 3: Load on Chart
```
1. Open EURUSD M5 chart
2. Drag JCAMP_FxScalper_v1 from Navigator → Expert Advisors
3. Load preset: JCAMP_FxScalper_EURUSD.set
4. Enable AutoTrading (checkbox in inputs)
5. Click OK
```

---

## Input Parameters Explained

### Risk Management
- **RiskPercent (1.0-2.0%):** Risk per trade
  - 1.0% = Conservative ($5 on $500 account)
  - 2.0% = Aggressive ($10 on $500) - allows wider SLs or maintains 0.05 lot minimum for partials

- **MaxDailyLoss (3.0%):** Daily loss limit - EA stops trading if reached
- **MaxGlobalPositions (1):** Maximum EURUSD positions (Phase 1 fixed at 1)

### Session Settings
Toggle which sessions to trade:
- **TradeLondon (08:00-16:30 GMT):** Default enabled - highest liquidity
- **TradeNewYork (13:00-21:00 GMT):** Default disabled - test separately
- **TradeAsian (23:00-08:00 GMT):** Default disabled - lower liquidity
- **TradeTokyo (00:00-09:00 GMT):** Default disabled - lower liquidity

**Note:** Sessions can overlap (e.g., London + NY = 08:00-21:00 trading window)

### Indicators
- **SMA1_Period (21):** Fast trend filter
- **SMA2_Period (50):** Medium trend filter
- **SMA3_Period (200):** Slow trend baseline
- **RSI_Period (14):** Momentum oscillator
- **ATR_Period (14):** Volatility measure for SL calculation
- **ATR_Multiplier (1.5):** EURUSD optimized value (wider for GBPUSD in Phase 2)

### Filters
- **MaxSpread (1.0 pips):** Reject entries if spread exceeds this
- **LevelProximity (5 pips):** Price must be within this distance of H1 level to trade
- **EnableTPValidation (true):** Abort trade if TP crosses H1 resistance/support
- **EnableSLSnapping (true):** Adjust SL to nearby H1 structural levels

---

## Strategy Logic Flow

### Entry Conditions (ALL must be true)
1. ✅ New M5 bar closed
2. ✅ Active trading session (any enabled session)
3. ✅ Spread ≤ 1.0 pips
4. ✅ Daily loss limit not exceeded
5. ✅ No existing EURUSD position
6. ✅ Price within 5 pips of H1 support (buy) or resistance (sell)
7. ✅ SMA alignment: 21 > 50 > 200 (bullish) or inverted (bearish)
8. ✅ RSI > 50 (bullish) or < 50 (bearish)
9. ✅ Pattern trigger: Bullish/Bearish engulfing or 3+1 pattern
10. ✅ TP validation passes (no structural levels blocking target)

### Position Management
**Entry:**
- SL = Previous candle low - (1.5 × ATR) for buys
- SL snapped to H1 support if within ATR range
- TP = Entry + (Entry - SL) × 2.0 (recalculated after SL snap)
- Lot size auto-calculated based on SL distance and risk%

**Partial Exit:**
- At 2:1 R:R: Close 80% of position
- Move remaining 20% SL to break-even + commission buffer
- Monitor runner for RSI bearish divergence

**Runner Exit:**
- RSI divergence detected (higher highs in price, lower highs in RSI)
- Close immediately

---

## Important Notes

### Minimum Position Size
- FP Markets minimum: 0.01 lots
- For partial profits (80/20 split), EA requires minimum 0.05 lot initial position:
  - 80% of 0.05 = 0.04 lots (valid)
  - 20% of 0.05 = 0.01 lots (valid)

**Impact on $500 account:**
- With 1% risk ($5), wider SL may prevent some trades
- Solution: Use 2% risk for wider SL scenarios, or accept full exits at 2:1 R:R for positions <0.05 lots

### Break-Even Calculation
EA accounts for FP Markets $3/lot commission:
```
BE Price = Entry + (Commission × 2 / Lot Size / Contract Size × Point)

Example: 0.05 lot EURUSD buy at 1.10000
BE = 1.10000 + ($6 / 0.05 / 100000) = 1.10000 + 0.00012 = 1.10012
```

### Session Times
All sessions defined in GMT. Ensure your broker server time is documented:
- FP Markets typically GMT+2 (summer) or GMT+3 (winter)
- Adjust `GMT_Offset` input if sessions don't match expected times

---

## Phase 1 vs Phase 2 Roadmap

### Phase 1 (Current) - Single Pair Validation
- ✅ EURUSD only
- ✅ All PRD logic implemented
- ✅ Enhanced filters (TP validation, SL snapping)
- ✅ Multi-session flexibility
- 🎯 Goal: Validate strategy profitability and logic correctness

### Phase 2 (Future) - Multi-Pair Expansion
- 🔜 Add AUDUSD, GBPUSD
- 🔜 Global position counter (max 2 across all pairs)
- 🔜 Pair-specific ATR multipliers
- 🔜 Python account dashboard:
  - Real-time equity tracking
  - Per-pair statistics
  - Trade journal with entry/exit reasons
  - Equity curve visualization

---

## Testing Recommendations

### Backtest Setup
1. Strategy Tester → EURUSD → M5
2. Modeling: Every tick (highest accuracy)
3. Date range: 6 months historical (include various market conditions)
4. Initial deposit: $500
5. Load preset: JCAMP_FxScalper_EURUSD.set

### Expected Metrics (Realistic Targets)
- **Profit Factor:** 1.3-1.8
- **Win Rate:** 40-50% (trend-following characteristic)
- **Max Drawdown:** <10% ($50 on $500)
- **Monthly Return:** 5-15% (conservative target)

⚠️ **Warning:** Win rates >70% in backtest suggest over-optimization (curve fitting)

### Forward Testing
1. Start with demo account (FP Markets)
2. Run for 2-4 weeks minimum
3. Monitor:
   - Slippage on partials
   - Session timing accuracy
   - TP validation rejections (are they valid?)
   - SL snapping frequency
4. Compare live vs backtest metrics

---

## WFO Browser UI

A Flask-based web interface for analyzing Walk-Forward Optimization results from cTrader backtests.

### Features

- **Archive Browser:** Browse historical backtest results organized by period and session
- **Analysis Dashboard:**
  - Overall backtest performance (all trades)
  - Session breakdown analysis (London, NY, Asian)
  - Recommended configuration results (filtered trades)
  - Interactive performance charts
  - Current settings vs recommended comparison
- **Full Settings Dropdown:** Collapsible view showing all current bot parameters with recommended values
- **Side-by-Side Comparison:** Compare two backtest periods to identify improvements
- **Settings Manager:** Configure paths, behavior, and bot parameters
- **.cbotset Export:** Generate properly formatted JSON files for cTrader (not XML)

### Quick Start

**Option 1: Windows Batch File (Recommended)**
```bash
# Double-click WFO_Browser.bat in project root
# Browser opens automatically after 2 seconds
```

**Option 2: Python Script**
```bash
python launch_wfo_ui.py
```

The browser will open to http://127.0.0.1:5000

### Using the UI

1. **Import New Analysis:**
   - Click "New Analysis" → Select CSV from cTrader logs
   - Enter period (e.g., "Apr_Jun_2025") and session (e.g., "all_sessions")
   - Analysis runs automatically and archives results

2. **Browse Archive:**
   - View all archived results on home page
   - Click "View Details" to see comprehensive analysis

3. **Export Settings:**
   - On analysis page, click "Export as cBotSet"
   - Downloads .cbotset JSON file ready for cTrader import

4. **Compare Periods:**
   - Click "Compare" → Select two periods/sessions
   - Side-by-side metric comparison

### Architecture

The UI follows a clean service-layer architecture:

- **Service Layer** (`wfo_ui/services/`):
  - `config_service.py` - Configuration management with validation
  - `file_service.py` - Safe file operations and cleanup
  - `analysis_service.py` - WFO analyzer subprocess integration
  - `archive_service.py` - Result archiving, browsing, and latest-file selection
  - `export_service.py` - .cbotset JSON generation (cTrader format)

- **Web Layer** (`wfo_ui/`):
  - `app.py` - Flask routes and request handling
  - `templates/` - Jinja2 HTML templates with collapsible sections
  - `static/` - CSS and JavaScript assets

- **Launcher:**
  - `WFO_Browser.bat` - Windows batch file for easy startup
  - `launch_wfo_ui.py` - Python launcher script

### Key Improvements (v4.4.1-WFO)

- **Export Format Fix:** Generates proper JSON `.cbotset` files (was XML)
  - Structure: `{"Chart": {...}, "Parameters": {...}}`
  - All 70+ cBot parameters with proper defaults
  - Parameter name mapping (MTF_SMA_Period → MTFSMAPeriod)
  - UTF-8 BOM for cTrader compatibility

- **Latest File Selection:** Archive service now uses `sorted(..., reverse=True)` to load the latest timestamped JSON/chart files

- **Full Settings Dropdown:** Shows all current bot settings vs recommended values in collapsible section

- **Backtest Defaults Updated:** Configuration matches actual backtest values (MTF=250, ADXPeriod=16, ADXThreshold=35, MinRR=4.0)

### Configuration

Settings are managed via `wfo_ui/config.json` (auto-created on first run).

**Current Backtest Settings:**
- MTF_SMA_Period: 250
- ADXPeriod: 16
- ADXMinThreshold: 35
- MinimumRR: 4.0
- EnableAsianSession: true

Edit configuration through the Settings page or modify the JSON file directly.

### Documentation

See `CHANGELOG_WFO_UI.md` for detailed version history and fixes.

---

## Troubleshooting

### EA Not Taking Trades
1. Check Experts tab for log messages
2. Common issues:
   - Spread too wide (check MaxSpread input)
   - No active session (verify GMT_Offset, enable at least one session)
   - Price not near H1 level (check LevelProximity)
   - TP validation rejecting trades (review TP crosses resistance/support)
   - Daily loss limit reached

### Compilation Errors
- Ensure all JC_*.mqh files are in MT5/Include/ folder
- Verify #include paths match actual file locations
- Check for typos in file names

### Partial Profits Not Executing
- Verify initial position ≥0.05 lots (check risk% and SL distance)
- Review Strategy Tester limitations (partials may not model accurately)
- In live trading, check broker allows partial closes

---

## Support & Development

**Documentation:**
- Full PRD: `Docs/JCAMP_FxScalper (PRD).md`
- Testing Guide: `Docs/TESTING_GUIDE.md`
- Implementation Notes: `Docs/IMPLEMENTATION_LOG.md`

**Development Status:**
- Version: 1.00
- Last Updated: 2026-03-02
- License: Proprietary (JCAMP Trading)

---

## Disclaimer

This EA is for educational and personal use only. Past performance does not guarantee future results. Trading forex carries significant risk of loss. Only trade with capital you can afford to lose. Always test on demo accounts before live trading.

**No warranty:** This software is provided "as is" without warranty of any kind. The developer assumes no liability for trading losses.

---

## Quick Start Checklist

- [ ] Copy files to MT5 directories
- [ ] Compile EA in MetaEditor (0 errors)
- [ ] Load EURUSD M5 chart
- [ ] Apply EA with EURUSD.set preset
- [ ] Verify session times match broker GMT offset
- [ ] Run backtest (6 months, every tick modeling)
- [ ] Analyze results vs expected metrics
- [ ] Forward test on demo for 2+ weeks
- [ ] Review logs for TP validation/SL snapping behavior
- [ ] Proceed to live with minimum account size if validated

---

**Ready to build consistent, algorithmic profits.** 🎯

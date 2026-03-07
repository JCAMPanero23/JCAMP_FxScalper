# Jcamp 1M Scalping - Backtest Guide

## Quick Start

### 1. Build the cBot
1. Open cTrader Automate
2. Build **Jcamp_1M_scalping.cs**
3. Check for errors (should compile successfully)

### 2. Open Strategy Tester
1. In cTrader, click **"Backtesting"** tab
2. Select **Jcamp_1M_scalping** from dropdown
3. Configure settings below

## Backtest Settings

### Basic Settings
```
cBot: Jcamp_1M_scalping
Symbol: EURUSD (or any forex pair)
Timeframe: M1 (required)
Period: Last 1 month
Initial Deposit: $1000
```

### Visual Mode (IMPORTANT)
```
✅ Enable "Visual mode" checkbox
   - This allows you to SEE rectangles being drawn
   - You'll see mode label and swing zones
   - Essential for verifying strategy logic
```

### Data Quality
```
Use: "Tick data from server"
   - Most accurate backtest results
   - Required for M1 precision
```

## cBot Parameters

### Recommended Starting Values

**Trend Detection:**
- EMA Period: **200** (default)
- Swing Lookback: **30** M15 bars

**Trade Management:**
- Lot Size: **0.01** (conservative for testing)
- Stop Loss: **20** pips (will optimize later for 3:1 RR)
- Take Profit: **40** pips (2:1 ratio for now)
- Max Positions: **1** (one trade at a time)

**Entry Filters:**
- Enable Trading: **true** (to see actual trades)
- Trade on New Swing Only: **true** (prevents overtrading)

**Visualization:**
- Show Rectangles: **true** ✅ (MUST enable for visual feedback)
- Rectangle Width: **50** minutes
- Show Mode Label: **true** ✅ (shows BUY/SELL mode)
- BUY Color: **Green**
- SELL Color: **Red**
- Rectangle Transparency: **80**
- Max Rectangles: **10**

## What You'll See in Visual Mode

### On Chart:
1. **Top-right corner**: "BUY MODE" or "SELL MODE" label
2. **Rectangles**: Transparent filled zones at M15 swing points
   - **Green rectangles**: BUY mode (swing LOW zones)
   - **Red rectangles**: SELL mode (swing HIGH zones)
3. **Trade markers**: Entry/exit points on chart

### In Console Log:
```
=== NEW M15 BAR: 2026-03-07 10:15:00 ===
[TrendDetection] M15 Price: 1.08450 | EMA200: 1.08200 | Mode: BUY
[SwingDetection] BUY Mode - Swing LOW at bar 1234
[SwingZone] BUY Mode | Top: 1.08145 | Bottom: 1.08120 | Height: 2.5 pips
[RectangleDraw] ✅ BUY Mode Rectangle #1
   Start: 2026-03-07 10:15:00 | End: 2026-03-07 11:05:00
✅ BUY EXECUTED | Entry: 1.08135 | SL: 1.08115 | TP: 1.08175
```

## How the Strategy Works (Visual Verification)

### Step 1: Trend Detection
- Watch the **mode label** change when M15 price crosses EMA 200
- **BUY MODE**: Price above EMA (green label)
- **SELL MODE**: Price below EMA (red label)

### Step 2: Swing Detection
- **New M15 bar** triggers swing search (every 15 minutes)
- **Rectangle appears** at swing point (Williams Fractal)
- **SELL Mode**: Rectangle at swing HIGH (red, Close to High)
- **BUY Mode**: Rectangle at swing LOW (green, Close to Low)

### Step 3: Entry
- **Price enters rectangle zone** → Trade executes
- **Entry marker** appears on chart
- **SL and TP** levels visible

### Step 4: Exit
- **Hit TP**: Green checkmark, profit
- **Hit SL**: Red X, loss
- Trade closes, wait for next swing

## Backtest Analysis

### Key Metrics to Check:

**Profitability:**
- Net Profit (positive/negative?)
- Win Rate (% of winning trades)
- Profit Factor (gross profit / gross loss)

**Risk-Reward:**
- Average Win vs Average Loss
- Current: targeting 2:1 (40 pips TP / 20 pips SL)
- Goal: optimize to 3:1 or better

**Trade Quality:**
- Number of trades (too many = overtrading)
- Max drawdown (risk exposure)
- Consecutive losses (strategy robustness)

**Visual Verification:**
- Do trades enter inside rectangles? ✅
- Do rectangles appear at valid swing points? ✅
- Is mode detection accurate (price vs EMA)? ✅

## Common Issues & Solutions

### Issue: No rectangles visible
**Solution:**
- Check "Show Rectangles" = **true**
- Enable "Visual mode" in backtest settings
- Wait for first M15 bar (up to 15 min)
- Verify enough M15 bars (need 200+ for EMA)

### Issue: No trades executing
**Check:**
- "Enable Trading" = **true**
- Price is entering rectangle zones (zoom in to see)
- Console shows swing detection (if not, no valid swings)
- Try longer backtest period (1 month minimum)

### Issue: Too many trades
**Solution:**
- "Trade on New Swing Only" = **true**
- This limits to 1 trade per swing detection
- Prevents re-entering same zone multiple times

### Issue: All trades losing
**Analysis:**
- Current SL/TP might not suit market conditions
- Rectangle height might be too small for SL
- EMA 200 trend might be lagging
- Try different pairs (GBPUSD, USDJPY)

## Optimization Steps (After Initial Test)

### Phase 1: Verify Logic (You are here)
- ✅ Rectangles appear at swing points
- ✅ Trades enter inside rectangles
- ✅ Mode changes with EMA crossover
- ✅ SL and TP are set correctly

### Phase 2: Optimize for 3:1 RR
- Measure average rectangle height (swing zone)
- Set SL based on rectangle height + buffer
- Set TP to 3x SL distance
- Test different swing lookback periods

### Phase 3: Add Filters
- Time of day filter (avoid low liquidity)
- Spread filter (avoid high spread entries)
- ATR filter (avoid low volatility periods)

### Phase 4: Risk Management
- Break-even logic (move SL to entry after X pips)
- Trailing stop (lock in profits)
- Partial take profit (scale out)
- Max daily loss/profit limits

## Example Backtest Workflow

1. **Run initial backtest** (1 month, EURUSD M1)
2. **Watch visual mode** - verify rectangles and entries
3. **Check console logs** - understand what's happening
4. **Review results** - win rate, profit factor, RR
5. **Identify issues** - Why did trades lose? Bad swings?
6. **Adjust parameters** - SL/TP, swing lookback, etc.
7. **Re-run backtest** - Compare results
8. **Iterate** until consistent profitability

## Success Criteria (Foundation)

Before moving to advanced features, verify:
- [ ] Rectangles draw correctly at swing points
- [ ] Mode label changes appropriately
- [ ] Trades enter inside rectangle zones
- [ ] Win rate > 40% (minimum for 3:1 RR to be profitable)
- [ ] No obvious bugs or logic errors
- [ ] Strategy makes sense visually

## Next Steps

Once foundation is verified:
1. Measure actual risk-reward achieved
2. Optimize SL/TP for 3:1 target
3. Add advanced features (see JCAMP_1M_SCALPING_README.md)
4. Test on multiple pairs
5. Forward test on demo account

---

## Quick Checklist

Before running backtest:
- [ ] cBot compiled successfully
- [ ] Backtest timeframe = **M1**
- [ ] Visual mode = **enabled**
- [ ] Show Rectangles = **true**
- [ ] Show Mode Label = **true**
- [ ] Lot Size = **0.01** (small for testing)
- [ ] Period = at least 1 month

During backtest:
- [ ] Watch rectangles appear at swings
- [ ] Watch mode label change
- [ ] Watch trades execute inside rectangles
- [ ] Monitor console logs

After backtest:
- [ ] Review net profit
- [ ] Check win rate and profit factor
- [ ] Analyze trade quality
- [ ] Identify what to optimize next

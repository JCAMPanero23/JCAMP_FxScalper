# Jcamp 1M Scalping cBot - Basic Version

## Overview
Basic trading bot that uses M15 EMA 200 trend detection and swing rectangle entry zones.
This is the foundation version to verify the logic works correctly before adding advanced features.

## Strategy Logic

### Trend Detection (M15 Timeframe)
- **BUY Mode**: M15 price > EMA 200
- **SELL Mode**: M15 price < EMA 200

### Swing Detection (Williams Fractals)
- **SELL Mode**: Find swing HIGH from bullish candle (Close > Open)
- **BUY Mode**: Find swing LOW from bearish candle (Close < Open)
- Lookback: 30 M15 bars (configurable)

### Entry Logic
- **SELL Mode**: Enter SELL when price enters swing HIGH rectangle zone (Close to High)
- **BUY Mode**: Enter BUY when price enters swing LOW rectangle zone (Close to Low)

### Exit Logic (Basic)
- **Stop Loss**: Fixed pips (default 20 pips)
- **Take Profit**: Fixed pips (default 40 pips)
- **Risk:Reward**: 1:2 ratio

## Parameters

### Trend Detection
- **EMA Period**: 200 (default)
- **Swing Lookback Bars**: 30 M15 bars (~7.5 hours)

### Trade Management
- **Lot Size**: 0.01 (default)
- **Stop Loss**: 20 pips (default)
- **Take Profit**: 40 pips (default)
- **Max Positions**: 1 (only one trade at a time)
- **Magic Number**: 100001

### Entry Filters
- **Enable Trading**: true/false
- **Trade on New Swing Only**: If true, only one trade per swing detection

## How It Works

### On Each M15 Bar
1. ✅ Detect trend mode (BUY or SELL) using EMA 200
2. ✅ Find recent swing point (Williams Fractal + candle validation)
3. ✅ Update swing rectangle zone prices
4. ✅ Print detailed logs for verification

### On Each Tick (M1)
1. Check if price is inside swing rectangle zone
2. If inside zone + correct mode + no existing position:
   - Execute trade (BUY or SELL)
3. Trade includes fixed SL and TP

## Testing Instructions

### 1. Backtest Setup
- **Symbol**: EURUSD (or any forex pair)
- **Timeframe**: M1 (required)
- **Date Range**: Last 1 month
- **Initial Deposit**: $1000
- **Data Quality**: Tick data from server

### 2. Initial Parameters (Conservative)
```
Lot Size: 0.01
Stop Loss: 20 pips
Take Profit: 40 pips
Max Positions: 1
Enable Trading: true
Trade on New Swing Only: true
```

### 3. What to Verify

#### Console Logs Should Show:
```
[TrendDetection] M15 Price: 1.08450 | EMA200: 1.08200 | Mode: BUY
[SwingDetection] BUY Mode - Swing LOW at bar 1234 | Low: 1.08120
[SwingZone] BUY Mode | Top: 1.08145 | Bottom: 1.08120 | Height: 0.00025
✅ BUY EXECUTED | Entry: 1.08135 | SL: 1.08115 | TP: 1.08175
```

#### Backtest Results to Check:
- ✅ Trades only in correct mode (BUY when price > EMA, SELL when price < EMA)
- ✅ Entries happen when price enters swing rectangle zone
- ✅ All trades have SL and TP set
- ✅ Max 1 position at a time
- ✅ No excessive trading (respects "Trade on New Swing Only")

### 4. Compare with Indicator
Run the **TrendModeRectangleIndicator** on the same chart to visually verify:
- Rectangles drawn by indicator match the swing zones detected by cBot
- Trade entries happen inside the rectangles
- Mode changes align between indicator and cBot logs

## Current Limitations (Basic Version)

### What This Version DOES NOT Have Yet:
- ❌ Advanced entry filters (time of day, spread check, etc.)
- ❌ Trailing stop
- ❌ Partial profit taking
- ❌ ATR-based dynamic SL/TP
- ❌ Multiple timeframe confirmation
- ❌ News filter
- ❌ Break-even logic
- ❌ Maximum daily loss limit
- ❌ Maximum daily profit target

### Why These Are Not Included (Yet)
This is the **foundation version** to verify:
1. ✅ Trend detection works correctly
2. ✅ Swing detection works correctly
3. ✅ Entry logic triggers at right time
4. ✅ Same calculations as indicator

Once verified, we can add advanced features incrementally.

## Next Steps for Development

### Phase 2 (After Verification)
- [ ] Add ATR-based dynamic SL/TP
- [ ] Add break-even logic
- [ ] Add trailing stop
- [ ] Add time filter (avoid low liquidity hours)

### Phase 3 (Advanced Features)
- [ ] Add partial profit taking
- [ ] Add multi-swing tracking (trade multiple swings)
- [ ] Add confluence filters
- [ ] Add risk management (max daily loss/profit)

### Phase 4 (Optimization)
- [ ] Parameter optimization via backtesting
- [ ] Walk-forward analysis
- [ ] Multi-symbol support

## File Location
`D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs`

## Related Files
- **Indicator**: `TrendModeRectangleIndicator.cs` (for visual verification)
- **Original EA**: `JCAMP_FxScalper.cs` (reference patterns)

## Notes
- This cBot must run on **M1 timeframe** only
- Uses same logic as the indicator (duplicated for independence)
- Lots of Print() statements for debugging and verification
- Conservative default parameters for safe initial testing
- Foundation for building the complete Jcamp scalping strategy

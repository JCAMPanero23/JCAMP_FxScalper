# M1 and M15 Testing Guide

## What Was Fixed

### Indicator (TrendModeRectangleIndicator.cs)
✅ **Improved M1 visibility**
- Added better logging when new M15 bar appears
- Made rectangles interactive for easier visibility
- Enhanced debug output showing rectangle details (height in pips, colors, etc.)

✅ **Better calculation trigger**
- Clearer logic for detecting new M15 bars
- Reduced spam in logs (only print when necessary)

### cBot (Jcamp_1M_scalping.cs)
✅ **Now works on both M1 and M15**
- Accepts M1 timeframe: Uses multi-timeframe M15 data
- Accepts M15 timeframe: Uses chart's own bars directly
- Auto-detects which timeframe it's running on

✅ **Improved logging**
- Matches indicator output format
- Shows new M15 bar notifications
- Better swing detection logs

## How to Test - Side-by-Side Comparison

### Step 1: Rebuild Both Files
1. Open cTrader Automate
2. Rebuild **TrendModeRectangleIndicator**
3. Rebuild **Jcamp_1M_scalping**

### Step 2: Set Up M1 Chart
1. Open **EURUSD M1** chart
2. Apply **TrendModeRectangleIndicator** to M1 chart
3. Watch the log for:
   ```
   === NEW M15 BAR: [timestamp] ===
   [TrendDetection] M15 Price: ... | EMA200: ... | Mode: BUY/SELL
   [SwingDetection] BUY/SELL Mode - Swing HIGH/LOW at bar ...
   [RectangleDraw] ✅ BUY/SELL Mode Rectangle #...
   ```

### Step 3: Set Up M15 Chart (Comparison)
1. Open **EURUSD M15** chart (side-by-side with M1)
2. Apply **TrendModeRectangleIndicator** to M15 chart
3. Both charts should show:
   - Same "BUY MODE" or "SELL MODE" label in top-right
   - Rectangles at the same swing candles
   - M15 chart shows rectangles on actual swing candles
   - M1 chart shows rectangles spanning 50 minutes from swing candles

### Step 4: Run cBot on M15 (Optional - for verification)
1. Open **EURUSD M15** chart
2. Run **Jcamp_1M_scalping** cBot (disable trading or use backtest)
3. Verify logs match the indicator logs
4. Swing zones should match rectangle locations

## What You Should See

### On M1 Chart with Indicator:
- **Top-right corner**: "BUY MODE" (green) or "SELL MODE" (red)
- **Rectangles**: Transparent filled boxes at M15 swing points
  - Width: 50 minutes (50 M1 bars)
  - SELL Mode: Red rectangles from Close to High
  - BUY Mode: Green rectangles from Close to Low
- **Console**: New M15 bar every 15 minutes with rectangle details

### On M15 Chart with Indicator:
- **Same mode label** as M1
- **Rectangles at exact swing candles**
  - Width: 50 minutes (3.33 M15 bars)
  - Should align with Williams Fractal swing points
  - Same colors and transparency as M1

### Expected Console Output (Both Charts):
```
=== NEW M15 BAR: 2026-03-07 10:15:00 ===
[TrendDetection] M15 Price: 1.08450 | EMA200: 1.08200 | Mode: BUY
[SwingDetection] BUY Mode - Swing LOW at bar 1234 | Low: 1.08120 | Time: ...
[RectangleDraw] ✅ BUY Mode Rectangle #1
   Start Time: 2026-03-07 10:15:00 | End Time: 2026-03-07 11:05:00
   Top: 1.08145 | Bottom: 1.08120 | Height: 2.5 pips
   Color: Green | Transparency: 80
```

## Troubleshooting

### Problem: No rectangles appear on M1
**Causes:**
1. Not enough M15 bars yet (need 200+ for EMA)
2. No valid swing points found in last 30 M15 bars
3. Market is ranging (no clear swings)

**Solutions:**
- Wait for next M15 bar (max 15 minutes)
- Check console logs for errors
- Verify EMA calculation: "Waiting for X M15 bars..."
- Try increasing "Swing Lookback Bars" parameter to 50

### Problem: Rectangles on M1 don't match M15
**Check:**
1. Both charts showing same symbol and time period
2. Both indicators using same parameters (EMA period, etc.)
3. Console logs - swing detection should show same bar index
4. Zoom levels - rectangle might be off-screen on M1

### Problem: Mode label not showing
**Check:**
1. "Show Mode Label" parameter is true
2. Chart has enough space at top-right
3. Console shows mode detection working
4. Try zooming out to see top of chart

### Problem: cBot not entering trades
**Remember:**
- This is just for **verification**, not live trading
- Set "Enable Trading" to false if just testing detection
- Use backtest mode to verify without risk
- Check console logs to see if swing zones are being detected

## Verification Checklist

Use this to confirm everything is working:

### Indicator on M1:
- [ ] Mode label shows "BUY MODE" or "SELL MODE"
- [ ] Rectangles appear (may take up to 15 min for first one)
- [ ] Console shows new M15 bar notifications
- [ ] Console shows swing detection and rectangle drawing
- [ ] Rectangles are visible and correctly colored

### Indicator on M15:
- [ ] Mode label matches M1 chart mode
- [ ] Rectangles appear at swing candles
- [ ] Console output matches M1 (same swings detected)
- [ ] Rectangles align with Williams Fractal highs/lows

### cBot on M15 (Optional):
- [ ] Starts without errors
- [ ] Console shows "Chart: M15 (Direct)"
- [ ] Swing zone detection logs appear
- [ ] Zones match indicator rectangles

### Side-by-Side Comparison:
- [ ] Both charts show same mode (BUY or SELL)
- [ ] Rectangles at same time positions
- [ ] Rectangles at same price levels
- [ ] Console logs show same swing bar indices

## Next Steps After Verification

Once you confirm rectangles are working correctly on both M1 and M15:

1. **Analyze swing quality**
   - Are swings clear and tradable?
   - Do rectangle zones make sense for entries?
   - What's the typical rectangle height (SL distance)?

2. **Test cBot on M1 backtest**
   - Use visual mode to see entries vs rectangles
   - Verify trades enter inside rectangle zones
   - Check if 3:1 RR is achievable

3. **Optimize parameters**
   - Adjust swing lookback if needed
   - Tune rectangle width for better entry timing
   - Optimize SL/TP based on rectangle height

4. **Build advanced features**
   - ATR-based dynamic SL/TP
   - Break-even logic
   - Trailing stop
   - Time filters

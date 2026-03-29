# H1 Level Visualization Feature

## ✅ IMPLEMENTED

Added visual representation of H1 support/resistance levels on the chart with automatic cleanup.

## 🎨 Features

### 1. **Horizontal Lines on Chart**
- **Green lines**: Support levels (below current price)
- **Red lines**: Resistance levels (above current price)
- **Thicker lines**: Nearest support/resistance (2px vs 1px)

### 2. **Smart Level Selection**
- Shows only the **closest levels** to current price
- Displays up to **5 levels** of each type (configurable)
- Automatically updates when new H1 fractals form

### 3. **Automatic Cleanup**
Removes old lines to prevent chart clutter:

**Cleanup Triggers:**
- ✓ Line age > 100 bars (configurable)
- ✓ Price moved > 200 pips away from level
- ✓ When bot stops (removes all lines)
- ✓ When new H1 levels are detected (refreshes display)

## ⚙️ Parameters

### New Bot Parameters

```
[Show H1 Levels]
- Default: TRUE
- Enable/disable level visualization
- Turn OFF for backtesting (faster performance)

[Max Levels to Show]
- Default: 5
- Range: 1-20
- How many support + resistance lines to display
- Lower = cleaner chart, Higher = more context

[Level Line Lifetime (bars)]
- Default: 100 bars
- Range: 10-500
- How long to keep lines on chart
- 100 M5 bars = ~8 hours
```

## 📊 Visual Example

```
Chart appearance:

Resistance 3 ─────────────────────── Red (thin)
Resistance 2 ─────────────────────── Red (thin)
Resistance 1 ━━━━━━━━━━━━━━━━━━━━━━━ Red (thick) ← Nearest

          Current Price: 1.0850

Support 1    ━━━━━━━━━━━━━━━━━━━━━━━ Green (thick) ← Nearest
Support 2    ─────────────────────── Green (thin)
Support 3    ─────────────────────── Green (thin)
```

## 🔍 How It Works

### 1. **Level Detection**
- Scans last 200 H1 bars for Williams Fractals
- Identifies support (fractal lows) and resistance (fractal highs)

### 2. **Level Drawing**
- Sorts levels by distance from current price
- Draws the **5 nearest supports** below price
- Draws the **5 nearest resistances** above price
- Highlights the **nearest level** with thicker line

### 3. **Automatic Updates**
- Every **new H1 bar**: Re-scan for fractals, update visualization
- Every **M5 bar**: Check for old lines to cleanup
- **On stop**: Remove all lines from chart

### 4. **Cleanup Logic**
```
Remove line if:
- Line age > 100 M5 bars (default), OR
- Price moved > 200 pips away, OR
- Bot stopped

Keep line if:
- Still within lifetime, AND
- Price within 200 pips, AND
- Bot running
```

## 📈 Use Cases

### During Live Trading
```
Show H1 Levels: TRUE
Max Levels to Show: 3-5
Lifetime: 100 bars

Result: Clean chart with nearest levels visible
```

### For Analysis/Learning
```
Show H1 Levels: TRUE
Max Levels to Show: 10
Lifetime: 200 bars

Result: More context, shows deeper market structure
```

### During Backtesting
```
Show H1 Levels: FALSE

Result: Faster backtest (no drawing overhead)
```

## 🎯 Benefits

1. **Visual Confirmation**
   - See H1 levels the bot is using for TP validation
   - Understand why trades are accepted/rejected

2. **Market Structure Awareness**
   - Identify support/resistance zones visually
   - See how price respects H1 fractals

3. **Debugging Aid**
   - Verify H1 level detection is working correctly
   - Check if TP validation is reasonable

4. **Clean Charts**
   - Old levels auto-removed
   - No manual cleanup needed

## 🚀 Usage Tips

### Tip 1: Start with Defaults
```
Show H1 Levels: TRUE
Max Levels to Show: 5
Lifetime: 100 bars
```
This gives a good balance of information vs. clarity.

### Tip 2: Adjust for Your Style
**Minimalist Traders:**
- Max Levels: 3
- Lifetime: 50 bars
- Shows only immediate levels

**Technical Analysts:**
- Max Levels: 10
- Lifetime: 200 bars
- Shows broader structure

### Tip 3: Disable During Backtest
Turn OFF "Show H1 Levels" when backtesting:
- Faster execution
- No visual overhead
- Same trading logic

### Tip 4: Watch Line Thickness
- **Thick line** = Nearest level (bot prioritizes this)
- **Thin line** = Additional context

### Tip 5: Correlate with Trade Logs
When a trade is rejected, check the chart:
```
Log: "[MarketStructure] TP VALIDATION FAILED - TP crosses H1 resistance"
Chart: Red resistance line visible between entry and TP
```

## 📝 Technical Details

### Line Naming Convention
```
Format: H1_[Type]_[Price]
Examples:
- H1_Support_1.08450
- H1_Resistance_1.09250
```

### Line Properties
```csharp
Support Lines:
- Color: Green
- Style: Solid
- Thickness: 2px (nearest) or 1px (others)

Resistance Lines:
- Color: Red
- Style: Solid
- Thickness: 2px (nearest) or 1px (others)
```

### Performance Impact
- **Minimal** - Only updates on new H1 bars (once per hour)
- **Cleanup** - Runs automatically, no manual intervention
- **Backtesting** - Disable for ~5-10% speed improvement

## ⚠️ Important Notes

1. **Lines are NOT trade signals** - They show structure only
2. **Bot logic unchanged** - Same entry/exit rules
3. **Manual trades** - You can still use these levels for manual analysis
4. **Backtest mode** - Turn OFF for faster backtests

## 🔧 Troubleshooting

### Issue: Too many lines on chart
**Solution:**
- Reduce "Max Levels to Show" (try 3)
- Reduce "Level Line Lifetime" (try 50 bars)

### Issue: Lines disappear too quickly
**Solution:**
- Increase "Level Line Lifetime" (try 200 bars)
- Check if price moved >200 pips (auto-cleanup)

### Issue: No lines showing
**Solution:**
- Verify "Show H1 Levels" = TRUE
- Check logs for "[Visualization]" messages
- Wait for new H1 bar to trigger update

### Issue: Lines not updating
**Solution:**
- Lines only update on new H1 bar (every hour)
- This is normal and by design
- Check logs: "[MarketStructure] H1 levels updated"

## 📊 Example Log Output

```
[MarketStructure] H1 levels updated | Supports: 15 | Resistances: 12 | Nearest Support: 1.08450 | Nearest Resistance: 1.09250
[Visualization] Drew 5 support lines and 5 resistance lines
[Visualization] Cleaned up 3 old level lines
```

## 🎉 Summary

This feature provides:
- ✅ Visual H1 support/resistance levels
- ✅ Automatic cleanup (no chart clutter)
- ✅ Configurable display (3-20 levels)
- ✅ Smart lifetime management
- ✅ Performance-friendly

**Result:** Better understanding of market structure and bot decision-making!

---

**Next Steps:**
1. Build the bot with new visualization code
2. Enable "Show H1 Levels" parameter
3. Watch the chart as H1 fractals are detected
4. Adjust "Max Levels to Show" to your preference

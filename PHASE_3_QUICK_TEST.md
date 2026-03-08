# Phase 3: Quick 5-Minute Test Guide

**Date:** 2026-03-09
**Goal:** Verify Phase 3 FVG detection is working

---

## Step 1: Build the cBot (1 minute)

1. Open cTrader
2. Open Automate → Edit Jcamp_1M_scalping
3. Press **Ctrl+B** to build
4. Verify: **"Build completed successfully - 0 errors, 0 warnings"**

✅ If build succeeds → Continue to Step 2
❌ If build fails → Check error messages

---

## Step 2: Configure Parameters (1 minute)

### NEW Parameters (Phase 3):

**FVG Detection:**
```
Enable FVG Filter: TRUE
FVG Lookback Bars: 50
```

**Score Weights (UPDATED for Phase 3):**
```
Weight: Validity: 0.20   (was 0.25)
Weight: Extremity: 0.25  (was 0.30)
Weight: Fractal: 0.15    (was 0.20)
Weight: Session: 0.20    (unchanged)
Weight: FVG: 0.15        ← NEW!
Weight: Candle: 0.05     (unchanged)
```

**Verify Weights Total:**
```
0.20 + 0.25 + 0.15 + 0.20 + 0.15 + 0.05 = 1.00 ✓
```

### Keep Phase 1C & 2 Settings:
```
Use H1 Levels for TP: TRUE
Use M15 Levels for TP: TRUE
Enable Session Filter: TRUE
Enable Trading: TRUE (if you want trades)
```

---

## Step 3: Run Quick Backtest (2 minutes)

### Backtest Setup:
```
Symbol: EURUSD
Timeframe: M1
From: 2024-01-01
To: 2024-01-15 (2 weeks - quick test)
Initial Deposit: $10,000
```

### Click "Start"

---

## Step 4: Verify Console Output (1 minute)

### Look for NEW Phase 3 messages:

**On Start:**
```
✓ Phase 3 FVG Detection: Enabled=True | Lookback=50 bars | FVG Weight=0.15
✓ Score Weights: Validity=0.20 | Extremity=0.25 | Fractal=0.15 | Session=0.20 | FVG=0.15 | Candle=0.05
✓ Weight Total: 1.00 ✓
```

**FVG Detection:**
```
✓ [FVG] Bullish gap detected at 2024-01-01 10:00 | Zone: 1.09500 - 1.09700
✓ [FVG] Bearish gap detected at 2024-01-01 14:30 | Zone: 1.10200 - 1.10000
✓ [FVG] Scan complete | Active FVGs: 3
```

**FVG Alignment:**
```
✓ [FVGAlign] Swing at Bullish FVG | Price: 1.09600 in zone 1.09500-1.09700 | STRONG
   OR
✓ [FVGAlign] Swing near FVG | Distance: 4.2 pips | MODERATE
   OR
✓ (No message if FVG score is neutral 0.3)
```

**Swing Scoring (now 6 components):**
```
✓ [SwingScore] Bar 45 | Score: 0.82 ✓
   (6 components now: Validity + Extremity + Fractal + Session + FVG + Candle)
```

---

## Step 5: Verify Results (1 minute)

### Check Backtest Statistics:

**Expected Behavior:**
- FVG messages appear on M15 bar close
- Both bullish and bearish FVGs detected
- Some swings score high on FVG alignment (1.0 or 0.7)
- Most swings get neutral/low FVG score (0.3)
- Score breakdown now shows 6 components (not 5)
- Weight total = 1.00

**Success Indicators:**
✅ FVGs detected automatically
✅ Unfilled FVGs tracked
✅ FVG alignment included in scoring
✅ Swings at FVG zones get bonus score
✅ Weights total to 1.0
✅ No errors in console

---

## Troubleshooting

### Issue: Build Failed
**Solution:**
- Check for syntax errors in console
- Verify FairValueGap class defined correctly
- Make sure you're editing Jcamp_1M_scalping.cs

### Issue: No FVG Messages
**Solution:**
- Check if backtest period has M15 bars
- Verify DetectFVGs() is called on M15 bars
- Look for "[FVG]" prefix in console
- May be normal if no gaps in price data (try longer period)

### Issue: FVG Score Always 0.5
**Solution:**
- Normal if Enable FVG Filter = FALSE
- Check if any FVGs were detected (count > 0)
- Swing may not be near any FVG zones

### Issue: Weights Don't Total 1.0
**Solution:**
- Manually adjust weights
- Phase 3 defaults: 0.20 + 0.25 + 0.15 + 0.20 + 0.15 + 0.05 = 1.00
- If changed, recalculate to ensure sum = 1.0

### Issue: Weight Total Warning
**Console shows:** "⚠ WARNING: Should be 1.0!"
**Solution:**
- Check all 6 weight parameters
- Adjust so they sum to exactly 1.00
- Example fix:
  ```
  If Total = 0.95: Increase Extremity by 0.05
  If Total = 1.05: Decrease Validity by 0.05
  ```

---

## Comparison Test

### Test 1: Phase 3 (With FVGs)
```
Settings:
- Enable FVG Filter: TRUE
- Weight: FVG: 0.15

Run backtest → Record results
```

### Test 2: Phase 2 (No FVGs)
```
Settings:
- Enable FVG Filter: FALSE
  (FVG score will be 0.5 always)
- Adjust weights to compensate (e.g., increase Extremity)

Run same backtest → Record results
```

### Compare:
- Phase 3 should show FVG-aligned swings scoring higher
- Phase 3 may have fewer but higher-quality swings
- Phase 3 should show FVG detection messages
- Win rate may improve 3-5%

---

## Success Checklist

- [ ] Code builds without errors
- [ ] Console shows "Phase 3 FVG Detection: Enabled=True"
- [ ] Console shows "[FVG] Bullish gap detected"
- [ ] Console shows "[FVG] Bearish gap detected"
- [ ] Console shows "[FVG] Scan complete | Active FVGs: X"
- [ ] Console shows "[FVGAlign]" messages when applicable
- [ ] Swing scoring shows 6 components (incl. FVG)
- [ ] Weights total to 1.0
- [ ] Swings at FVG zones get higher scores

---

## Weight Tuning Test

### Test Different FVG Weights:

**Test A: Default (Balanced)**
```
Weight: FVG: 0.15
Weight: Extremity: 0.25
Result: Baseline
```

**Test B: Favor FVGs (SMC Focus)**
```
Weight: FVG: 0.25
Weight: Extremity: 0.20
Weight: Fractal: 0.10
Result: More focus on FVG zones
```

**Test C: Ignore FVGs**
```
Weight: FVG: 0.05
Weight: Extremity: 0.35
Result: Focus on extreme swings, ignore FVGs
```

Compare trade frequency and win rate across tests.

---

## FVG Understanding Check

### Example 1: Bullish FVG

```
Candle A: High = 1.09500
Candle B: Big move up (impulse)
Candle C: Low = 1.09700

Gap: 1.09500 to 1.09700 (20 pips)
Type: BULLISH (price gapped up)
Trading: Look for BUY entries when price returns to zone
```

### Example 2: Bearish FVG

```
Candle A: Low = 1.10200
Candle B: Big move down (impulse)
Candle C: High = 1.10000

Gap: 1.10000 to 1.10200 (20 pips)
Type: BEARISH (price gapped down)
Trading: Look for SELL entries when price returns to zone
```

### FVG Fill Detection

**Bullish FVG Filled:**
- Any bar's LOW <= FVG TopPrice
- Example: Price drops back into 1.09500-1.09700 zone

**Bearish FVG Filled:**
- Any bar's HIGH >= FVG BottomPrice
- Example: Price rises back into 1.10000-1.10200 zone

**Only UNFILLED FVGs are used for scoring.**

---

## Understanding FVG Scoring

### Swing at FVG Zone (Score = 1.0)

```
Swing High: 1.09650
FVG Zone: 1.09500 - 1.09700
Check: 1.09650 is between 1.09500 and 1.09700 ✓
FVG Score: 1.0 (STRONG)
Weighted: 1.0 × 0.15 = 0.15 (15% boost)
```

### Swing Near FVG (Score = 0.7)

```
Swing High: 1.09730
FVG Zone: 1.09500 - 1.09700
Distance to zone: 3.0 pips (< 5 pips threshold)
FVG Score: 0.7 (MODERATE)
Weighted: 0.7 × 0.15 = 0.105 (10.5% boost)
```

### Swing Away from FVG (Score = 0.3)

```
Swing High: 1.09850
FVG Zone: 1.09500 - 1.09700
Distance to zone: 15 pips (> 5 pips threshold)
FVG Score: 0.3 (WEAK)
Weighted: 0.3 × 0.15 = 0.045 (4.5% contribution)
```

---

## Advanced Validation

### Check FVG Count Over Time

Run multiple backtests:
```
Period 1: 2024-01-01 to 2024-01-07 (1 week)
Period 2: 2024-02-01 to 2024-02-07 (1 week)
Period 3: 2024-03-01 to 2024-03-07 (1 week)
```

Expected:
- Each period should detect 2-8 FVGs
- More volatile periods = more FVGs
- Quiet periods = fewer FVGs

### Verify FVG Fill Logic

1. Note a detected FVG zone (e.g., 1.09500-1.09700)
2. Watch price action in visual backtest
3. Check if FVG disappears when price fills it
4. Verify filled FVGs don't affect scoring

---

## Next Steps

If all checks pass:
1. ✅ Phase 3 is working correctly
2. ✅ All phases (1A, 1B, 1C, 2, 3) are now complete
3. Run comprehensive backtest (6-12 months)
4. Compare Phase 3 vs Phase 2 performance
5. Tune FVG weight if needed (0.10-0.25 range)
6. Optimize all weights for best performance
7. Consider forward testing or demo account

If issues found:
1. Check troubleshooting section above
2. Review PHASE_3_SUMMARY.md for details
3. Verify all code changes applied correctly
4. Check console logs for error messages
5. Ensure FVG detection is running (count messages)

---

## Final Validation Checklist

### Code Quality
- [ ] Builds without errors
- [ ] No warnings in console
- [ ] All phases initialized correctly

### FVG Detection
- [ ] Bullish FVGs detected
- [ ] Bearish FVGs detected
- [ ] FVG fill detection works
- [ ] Unfilled FVGs tracked correctly

### Scoring
- [ ] 6 components in swing score
- [ ] FVG component included
- [ ] Weights total to 1.0
- [ ] Swings at FVGs score higher

### Integration
- [ ] FVG works with Session scoring
- [ ] FVG works with H1/M15 TP levels
- [ ] No conflicts with previous phases

---

**Estimated Time:** 5 minutes
**Status:** Ready to test
**Files:** Jcamp_1M_scalping.cs (now includes Phase 3)

**ALL PHASES COMPLETE!** 🎉


# Phase 2: Quick 5-Minute Test Guide

**Date:** 2026-03-08
**Goal:** Verify Phase 2 session tracking is working

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

### NEW Parameters (Phase 2):

**Session Management:**
```
Enable Session Filter: TRUE
Show Session Boxes: FALSE (not yet implemented)
```

**Score Weights (NEW):**
```
Weight: Validity: 0.25
Weight: Extremity: 0.30
Weight: Fractal: 0.20
Weight: Session: 0.20  ← NEW!
Weight: Candle: 0.05
```

**Verify Weights Total:**
```
0.25 + 0.30 + 0.20 + 0.20 + 0.05 = 1.00 ✓
```

### Keep Phase 1C Settings:
```
Use H1 Levels for TP: TRUE
Use M15 Levels for TP: TRUE
Enable Trading: TRUE
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

### Look for NEW Phase 2 messages:

**Session Tracking:**
```
✓ [Session] NEW London session started at 2024-01-01 08:00
✓ [Session] Asian session ended | High: 1.10250 | Low: 1.09850
✓ [Session] NEW NewYork session started at 2024-01-01 13:00
```

**Session Alignment:**
```
✓ [SessionAlign] Swing at London session HIGH | Distance: 3.2 pips | STRONG
   OR
✓ Session: 0.500 × 0.20 = 0.100  (not at session level)
```

**Swing Scoring (now 5 components):**
```
✓ [SwingScoring] Bar 45 | Score: 0.78
   Validity:  0.850 × 0.25 = 0.213
   Extremity: 0.920 × 0.30 = 0.276
   Fractal:   0.780 × 0.20 = 0.156
   Session:   1.000 × 0.20 = 0.200  ← NEW!
   Candle:    0.700 × 0.05 = 0.035
```

---

## Step 5: Verify Results (1 minute)

### Check Backtest Statistics:

**Expected Behavior:**
- Session messages appear on M15 bar close
- Sessions detected: Asian (00:00), London (08:00), NY (13:00)
- Some swings score high on session alignment (1.0)
- Most swings get neutral session score (0.5)
- Score breakdown now shows 5 components (not 4)

**Success Indicators:**
✅ Sessions detected automatically
✅ Session high/low tracked
✅ Session alignment included in scoring
✅ Swings at session levels get bonus score
✅ Weights total to 1.0
✅ No errors in console

---

## Troubleshooting

### Issue: Build Failed
**Solution:**
- Check for syntax errors in console
- Verify SessionState class defined correctly
- Make sure you're editing Jcamp_1M_scalping.cs

### Issue: No Session Messages
**Solution:**
- Check if backtest period has M15 bars
- Verify UpdateSessionTracking() is called
- Look for "[Session]" prefix in console

### Issue: Session Score Always 0.5
**Solution:**
- Normal if swing not at session high/low
- Verify Enable Session Filter = TRUE
- Check if session data exists for swing time

### Issue: Weights Don't Total 1.0
**Solution:**
- Manually adjust weights
- Default: 0.25 + 0.30 + 0.20 + 0.20 + 0.05 = 1.00
- If changed, recalculate to ensure sum = 1.0

---

## Comparison Test

### Test 1: Phase 2 (With Sessions)
```
Settings:
- Enable Session Filter: TRUE
- Weight: Session: 0.20

Run backtest → Record results
```

### Test 2: Phase 1C (No Sessions)
```
Settings:
- Enable Session Filter: FALSE
  (Session score will be 0.5 always)

Run same backtest → Record results
```

### Compare:
- Phase 2 should show session-aligned swings scoring higher
- Phase 2 may have fewer but higher-quality swings
- Phase 2 should show session boundary messages

---

## Success Checklist

- [ ] Code builds without errors
- [ ] Console shows "[Session] NEW London session started"
- [ ] Console shows "[SessionAlign] Swing at London session HIGH"
- [ ] Swing scoring shows 5 components (incl. session)
- [ ] Weights total to 1.0
- [ ] Session tracking updates on M15 bars
- [ ] Swings at session levels get higher scores

---

## Weight Tuning Test

### Test Different Session Weights:

**Test A: Default (Balanced)**
```
Weight: Session: 0.20
Weight: Extremity: 0.30
Result: Baseline
```

**Test B: Favor Sessions**
```
Weight: Session: 0.30
Weight: Extremity: 0.20
Result: More focus on session highs/lows
```

**Test C: Ignore Sessions**
```
Weight: Session: 0.05
Weight: Extremity: 0.45
Result: Focus on extreme swings, ignore timing
```

Compare trade frequency and win rate across tests.

---

## Next Steps

If all checks pass:
1. ✅ Phase 2 is working correctly
2. Run longer backtest (3-6 months)
3. Compare Phase 2 vs Phase 1C performance
4. Tune session weight if needed (0.15-0.30 range)
5. Proceed to Phase 3 (FVG Detection)

If issues found:
1. Check troubleshooting section above
2. Review PHASE_2_SUMMARY.md for details
3. Verify all code changes applied correctly
4. Check console logs for error messages

---

**Estimated Time:** 5 minutes
**Status:** Ready to test
**Files:** Jcamp_1M_scalping.cs (now includes Phase 2)

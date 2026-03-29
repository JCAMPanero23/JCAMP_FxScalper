# Phase 1C: Quick 5-Minute Test Guide

**Date:** 2026-03-08
**Goal:** Verify Phase 1C is working correctly

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

### Required Settings:

**TP Management (NEW IN PHASE 1C):**
```
Use H1 Levels for TP: TRUE
Use M15 Levels for TP: TRUE
H1 Level Proximity Pips: 50
```

**Trade Management:**
```
Risk Per Trade %: 1.0
SL Buffer Pips: 2.0
Minimum RR Ratio: 3.0
Max Positions: 1
```

**Entry Filters:**
```
Enable Trading: TRUE
Entry Mode: Breakout
Trade on New Swing Only: TRUE
Max Distance to Arm: 10.0
```

**Trend Detection:**
```
SMA Period: 200
Swing Lookback Bars: 100
Minimum Swing Score: 0.60
```

---

## Step 3: Run Quick Backtest (2 minutes)

### Backtest Setup:
```
Symbol: EURUSD
Timeframe: M1
From: 2024-01-01
To: 2024-01-15 (2 weeks only - quick test)
Initial Deposit: $10,000
```

### Click "Start"

---

## Step 4: Verify Console Output (1 minute)

### Look for these NEW Phase 1C messages:

**On Start:**
```
✓ Phase 1C TP Management: H1 Levels=True | M15 Levels=True | Proximity=50 pips
```

**On Each M15 Bar:**
```
✓ [H1 Levels] Detected X supports and Y resistances
✓ [M15 Levels] Detected X supports and Y resistances
```

**On Trade Entry:**
```
✓ [TP-H1] Using H1 support at 1.09450 | Distance: 65.0 pips | RR: 1:3.25
   OR
✓ [TP-M15] Using M15 support at 1.09350 | Distance: 70.0 pips | RR: 1:3.50
   OR
✓ [TP-Default] Using default 3.0R TP at 1.09400
```

---

## Step 5: Verify Results (1 minute)

### Check Backtest Statistics:

**Expected Behavior:**
- At least 1-2 trades in 2 weeks
- Some TPs should use H1/M15 levels (not all at exactly 3R)
- All TPs should be ≥ 3R (minimum enforced)
- Console shows level detection working

**Success Indicators:**
✅ Bot started without errors
✅ H1/M15 levels detected (counts shown in logs)
✅ Trades executed (if swing setups occurred)
✅ TP placement varies (structure-based, not always fixed)
✅ No TP below 3R (minimum protected)

---

## Troubleshooting

### Issue: Build Failed
**Solution:**
- Check for syntax errors in console
- Verify all using statements at top of file
- Make sure you're editing Jcamp_1M_scalping.cs (not old files)

### Issue: No H1/M15 Level Messages
**Solution:**
- Check if backtest period too short (need data to build)
- Look for "[H1 Levels]" and "[M15 Levels]" in console
- Should appear on start and every M15 bar

### Issue: Always Shows [TP-Default]
**Solution:**
- Normal if no structure levels found within criteria
- Try longer backtest period (1 month+)
- Check if UseH1LevelsForTP and UseM15LevelsForTP are TRUE

### Issue: No Trades Executed
**Solution:**
- Normal if no swing setups in 2-week period
- Try longer backtest (1 month)
- Check "Enable Trading" is TRUE
- Verify Minimum Swing Score not too high (0.60 is good)

---

## Quick Comparison Test

### Test 1: Phase 1C (Structure-based TP)
```
Settings:
- Use H1 Levels for TP: TRUE
- Use M15 Levels for TP: TRUE

Run backtest → Record results
```

### Test 2: Phase 1B (Fixed 3R TP)
```
Settings:
- Use H1 Levels for TP: FALSE
- Use M15 Levels for TP: FALSE

Run same backtest → Record results
```

### Compare:
- Phase 1C should show **variety** in TP distances
- Phase 1B should show **fixed** 3R on all trades
- Phase 1C should have **higher win rate** (5-10%)

---

## Success Checklist

- [ ] Code builds without errors
- [ ] Console shows "Phase 1C TP Management" on start
- [ ] H1 and M15 levels detected (counts shown)
- [ ] Trades execute (if setups present)
- [ ] TPs placed using structure (H1/M15/Default)
- [ ] All TPs maintain minimum 3R
- [ ] Win rate improved vs Phase 1B

---

## Next Steps

If all checks pass:
1. ✅ Phase 1C is working correctly
2. Run longer backtest (3-6 months)
3. Compare performance vs Phase 1B
4. Optimize H1 proximity if needed
5. Proceed to Phase 2 (Session Awareness)

If issues found:
1. Check troubleshooting section above
2. Review PHASE_1C_IMPLEMENTATION.md for details
3. Verify all code changes applied correctly

---

**Estimated Time:** 5 minutes
**Status:** Ready to test
**Files:** Jcamp_1M_scalping.cs (1588 lines)

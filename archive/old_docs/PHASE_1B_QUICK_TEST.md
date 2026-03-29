# Phase 1B Quick Testing Guide

## 🚀 5-Minute Quick Test

### Step 1: Build (30 seconds)
```
1. Open cTrader Automate
2. Load Jcamp_1M_scalping.cs
3. Press Ctrl+B (Build)
4. Check: "Build succeeded" ✅
```

### Step 2: Set Parameters (1 minute)
```
CRITICAL - Change these from defaults:
✅ Enable Trading = TRUE (was FALSE)
✅ Entry Mode = Breakout
✅ Risk Per Trade % = 1.0

Keep defaults:
- SL Buffer Pips = 2.0
- Minimum RR Ratio = 3.0
- Minimum Swing Score = 0.60
- Rectangle Width = 60 minutes
```

### Step 3: Run Backtest (2 minutes)
```
Chart: EURUSD M1
Period: Last 1 month
Visual Mode: ON
Capital: $500
Start backtest
```

### Step 4: Watch Console (2 minutes)
```
Look for these NEW messages:

✅ [BreakoutEntry] SELL trigger detected
✅ [PositionSizing] Risk: 1.0% | SL: X pips | Lot Size: X
✅ [SELL] Entry Setup: ... RR: 1:3.0
✅ SELL EXECUTED SUCCESSFULLY

Or for BUY:
✅ [BreakoutEntry] BUY trigger detected
✅ BUY EXECUTED SUCCESSFULLY
```

### Step 5: Validate (1 minute)
```
Check on chart:
✅ Trades executed AFTER M1 candle closes beyond rectangle
✅ SL visible at rectangle edge
✅ TP visible at ~3x the SL distance
✅ Position size varies (check console logs)
```

---

## ✅ Pass/Fail Criteria

### ✅ PASS if you see:
- Rectangles drawn at swings (Phase 1A working)
- Trades execute on M1 breakout
- SL at rectangle edge + buffer
- TP at 3R from entry
- Position size calculated dynamically
- Console shows "[BreakoutEntry]" messages

### ❌ FAIL if you see:
- No rectangles (Phase 1A broken)
- No trades at all (check Enable Trading = TRUE)
- Trades execute inside rectangle (breakout logic broken)
- Fixed lot size (should vary based on SL distance)
- Compilation errors

---

## 🐛 Quick Troubleshooting

### No trades?
1. Check `Enable Trading = TRUE`
2. Run on M1 timeframe (not M15)
3. Extend backtest period (try 3 months)

### Compilation error?
1. Check for typos in code
2. Ensure all enum definitions exist
3. Restart cTrader

### Trades but wrong behavior?
1. Check console logs for "[BreakoutEntry]"
2. Verify SL/TP calculations in logs
3. Compare with expected logic

---

## 📊 Expected Results (1 month backtest)

```
Trades: 15-30
Win Rate: 40-60%
Profit Factor: 1.5 - 2.5
Max Drawdown: 5-10%
```

If wildly different → debug required.

---

## ⏭️ Next Steps

✅ **If test passes:**
→ Ready for Phase 1C (M1 market structure TP)

❌ **If test fails:**
→ Review PHASE_1B_IMPLEMENTATION.md
→ Check code changes
→ Re-test

---

**Quick Test Time:** 5 minutes
**Full Validation Time:** 15-30 minutes (including backtest analysis)

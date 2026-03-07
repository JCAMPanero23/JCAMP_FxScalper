# Phase 1A Quick Reference Card

## 🚀 Quick Start (5 Minutes)

### 1. Compile (30 seconds)
```
cTrader → Automate → Jcamp_1M_scalping → Build (Ctrl+B)
✅ "Build succeeded" = Ready to test
```

### 2. Load on Chart (1 minute)
```
Chart: EURUSD M1 (MUST BE M1!)
Drag cBot onto chart
Parameters window opens
```

### 3. Verify Parameters (1 minute)
```
✅ Swing Lookback: 100
✅ Rectangle Width: 60
✅ Minimum Score: 0.60
✅ Enable Trading: FALSE
✅ Show Rectangles: TRUE
```

### 4. Run Backtest (5-10 minutes)
```
Backtest button → Set period (1 month)
Visual mode: ON
Start → Watch console + chart
```

### 5. Validate (2 minutes)
```
✅ Console shows swing scores
✅ Rectangles at strong swings
✅ Rectangle width = 60 min
✅ No expired rectangles
```

---

## 📊 What Changed (At a Glance)

| Parameter | OLD | NEW |
|-----------|-----|-----|
| Swing Lookback | 30 bars | **100 bars** |
| Rectangle Width | 50 min | **60 min** |
| Enable Trading | true | **false** |
| Swing Selection | First found | **Highest scored** |
| Quality Filter | None | **Score >= 0.60** |
| Validity Check | None | **No expired rectangles** |

---

## 🎯 Scoring Formula

```
Score = (Validity × 25%) + (Extremity × 35%) +
        (Fractal × 25%) + (Candle × 15%)

Threshold: >= 0.60 to draw rectangle
```

**Components:**
- **Validity (25%):** Rectangle not expired (forward-looking)
- **Extremity (35%):** Highest high or lowest low in period
- **Fractal (25%):** How strong the Williams Fractal is
- **Candle (15%):** Strong body vs weak doji

---

## 👀 Console Output (What to Expect)

### ✅ GOOD Output:
```
[SwingDetection] Found 8 Williams Fractals, scoring...
[SwingScore] Bar 1267 | Score: 0.81 ✓
[SignificantSwing] ✅ Selected Bar 1267 | Score: 0.81
[RectangleDraw] ✅ SELL Mode Rectangle #1
   Start: 10:00 | End: 11:00 | Height: 3.0 pips
```

### ❌ BAD Output (Problems):
```
[SwingDetection] No Williams Fractals found
→ Try different date range

[SwingDetection] No swings scored >= 0.60
→ Lower minimum score to 0.50

[Score] Bar 1100 INVALID (expired rectangle)
→ This is GOOD! Filter working ✅
```

---

## ✅ Pass Criteria Checklist

- [ ] Compiles without errors
- [ ] Console shows "Found X fractals"
- [ ] Console shows swing scores with ✓/✗
- [ ] Selected swing score >= 0.60
- [ ] Rectangles appear on chart
- [ ] Rectangle width = 60 minutes
- [ ] NO expired rectangles (past)
- [ ] Fewer rectangles than before
- [ ] Rectangles at obvious strong swings

**All checked? → Phase 1A PASS ✅**
**Any unchecked? → See troubleshooting**

---

## 🔧 Quick Troubleshooting

### Problem: Won't compile
**Fix:** Check for syntax errors in code

### Problem: No rectangles
**Fix:** Lower minimum score to 0.50

### Problem: Too many rectangles
**Fix:** Raise minimum score to 0.70

### Problem: Rectangle wrong width
**Fix:** Check parameter = 60

### Problem: Rectangles in past
**Fix:** BUG - report validity logic issue

---

## 📈 Expected Results

**Before Phase 1A:**
- ~50-100 rectangles/month
- Mixed quality (weak + strong)
- Possible expired rectangles

**After Phase 1A:**
- ~20-40 rectangles/month
- High quality (only strong >= 0.60)
- ZERO expired rectangles

**Change:** **-50% to -70% rectangles, +100% quality**

---

## 🎓 Key Concepts

**Market Structure Awareness**
- OLD: 30 bars (limited view)
- NEW: 100 bars (broader context)

**Selection Logic**
- OLD: First fractal found
- NEW: Best scoring fractal

**Quality Control**
- OLD: All fractals accepted
- NEW: Only score >= 0.60

**Validity**
- OLD: May draw past rectangles
- NEW: Always forward-looking

---

## 🔮 Next Phases (After 1A)

**Phase 1B:** Entry logic (breakout detection)
**Phase 1C:** M1 market structure TP
**Phase 2:** Session awareness + visual boxes
**Phase 3:** FVG detection

**Estimated total time:** 5-8 hours (all phases)

---

## 📚 Documentation Files

1. **PHASE_1A_IMPLEMENTATION.md** - Full technical spec
2. **PHASE_1A_TESTING_GUIDE.md** - Detailed testing steps
3. **IMPLEMENTATION_SUMMARY.md** - Overview & metrics
4. **PHASE_1A_QUICK_REF.md** - This file (quick access)

---

## 🎯 One-Line Summary

**Phase 1A:** Filters swings by quality score, only draws rectangles for significant (>= 0.60) forward-looking swings, using 100-bar market structure context.

---

## ⚡ Critical Reminders

- ✅ **Run on M1 timeframe** (not M15!)
- ✅ **Trading disabled** (Phase 1A testing only)
- ✅ **Visual mode ON** (essential for validation)
- ✅ **Check console output** (shows scoring)
- ✅ **Verify rectangle width** (60 minutes)

---

**Ready? → Open cTrader and start testing! 🚀**

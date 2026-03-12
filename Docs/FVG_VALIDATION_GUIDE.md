# FVG Detection Validation Guide

**Phase 3: Fair Value Gap (FVG) Detection - UNTESTED**
**Status**: Phase 2 (Session Awareness) ✅ Validated | Phase 3 (FVG) ⚠️ Needs Testing

---

## What is FVG Detection?

**Fair Value Gaps (FVGs)** are price inefficiencies where the market moved too fast, leaving a gap that institutional traders often return to fill.

**The Feature:**
- Detects 3-candle patterns (A-B-C structure)
- **Bullish FVG**: Gap between Candle A's High and Candle C's Low
- **Bearish FVG**: Gap between Candle A's Low and Candle C's High
- Tracks whether gaps have been "filled" (price returned)
- Scores swings higher when they align with unfilled FVG zones
- Weight: **15%** of total swing score

---

## Quick Start: 5-Minute Smoke Test

**Objective**: Verify FVG detection is working at all

### Configuration
```
cBot: Jcamp_1M_scalping.cs
Symbol: EURUSD
Timeframe: M1
Period: 2025-01-15 00:00 to 2025-01-22 00:00 (1 week)
Visual Mode: ON
Enable Trading: FALSE

Parameters:
├─ FVG Detection
│  ├─ Enable FVG Filter: TRUE
│  └─ FVG Lookback Bars: 50
│
└─ Score Weights
   ├─ Weight: Validity: 0.20
   ├─ Weight: Extremity: 0.25
   ├─ Weight: Fractal: 0.15
   ├─ Weight: Session: 0.20
   ├─ Weight: FVG: 0.15
   └─ Weight: Candle: 0.05
```

### What to Look For in Console

**✅ SUCCESS if you see:**
```
Phase 3 FVG Detection: Enabled=True | Lookback=50 bars | FVG Weight=0.15
[FVG] Bullish gap detected at 2025-01-15 10:00 | Zone: 1.09500 - 1.09700
[FVG] Bearish gap detected at 2025-01-15 14:30 | Zone: 1.10200 - 1.10000
[FVG] Scan complete | Active FVGs: 3
Weight Total: 1.00 ✓
```

**❌ FAIL if you see:**
- No FVG messages at all
- "Active FVGs: 0" for entire week
- "Weight Total: 1.00" missing (should be there)
- Any errors or exceptions

### Quick Assessment
- **FVG Count**: Should see 5-20 FVGs detected per week (typical)
- **Too many (>50)**: Detection too sensitive, catching noise
- **Too few (0-2)**: Detection broken or too strict

---

## Test 1: FVG Detection Logic (20 min)

**Objective**: Verify FVG patterns are detected correctly

### Configuration
```
Same as smoke test, but:
Period: 2025-01-01 to 2025-02-01 (1 month)
```

### Validation Checklist

**Console Output:**
- [ ] FVGs detected (count > 0)
- [ ] Both Bullish AND Bearish FVGs appear
- [ ] FVG zones logged with top/bottom prices
- [ ] Top price > Bottom price for all FVGs
- [ ] Gap sizes reasonable (typically 10-50 pips for EURUSD)

**Pattern Verification:**
For a few FVGs, manually verify the pattern:
```
Bullish FVG (BUY zones):
- Candle A (idx-2): Has a high at price H
- Candle B (idx-1): Big bullish move (impulse)
- Candle C (idx-0): Has a low at price L
- Gap: L > H (gap between them)

Bearish FVG (SELL zones):
- Candle A (idx-2): Has a low at price L
- Candle B (idx-1): Big bearish move (impulse)
- Candle C (idx-0): Has a high at price H
- Gap: H < L (gap between them)
```

**What Good FVGs Look Like:**
```
[FVG] Bullish gap detected at 2025-01-15 10:00 | Zone: 1.09500 - 1.09700
✓ Gap size: 20 pips (reasonable)
✓ Top > Bottom (1.09700 > 1.09500)
✓ Unfilled (price hasn't returned to zone yet)
```

**Red Flags:**
```
[FVG] Bullish gap detected | Zone: 1.09500 - 1.09502
❌ Gap size: 0.2 pips (too small - noise)

[FVG] Bullish gap detected | Zone: 1.09500 - 1.12000
❌ Gap size: 250 pips (too large - detection error)

Active FVGs: 0
❌ No FVGs all month (detection broken)
```

### Pass Criteria
- ✅ FVG count: 20-100 for 1 month (reasonable range)
- ✅ Mix of bullish and bearish FVGs
- ✅ Gap sizes: 5-100 pips (typical for EURUSD)
- ✅ All zones have top > bottom
- ✅ No errors in console

---

## Test 2: FVG Alignment Scoring (20 min)

**Objective**: Verify swings at FVG zones get higher scores

### Configuration
```
Same as Test 1
Enable Score Decomposition Logging (if available)
```

### What to Look For

**In Console - FVG Alignment Messages:**
```
[FVGAlign] Swing at Bullish FVG | Price: 1.09600 in zone 1.09500-1.09700 | STRONG
[SwingScoring] FVG: 1.000 × 0.15 = 0.150

[FVGAlign] Swing near Bullish FVG | Distance: 3 pips | MODERATE
[SwingScoring] FVG: 0.700 × 0.15 = 0.105

[SwingScoring] FVG: 0.300 × 0.15 = 0.045 (far from FVG)
```

### Validation Checklist

**Scoring Tiers:**
- [ ] Swings **IN** FVG zone → FVG score = **1.0** (maximum)
- [ ] Swings **NEAR** FVG (within 5 pips) → FVG score = **0.7** (good)
- [ ] Swings **FAR** from FVG → FVG score = **0.3** (neutral)

**Impact on Total Scores:**
```
Example Swing WITHOUT FVG alignment:
Validity: 0.85 × 0.20 = 0.170
Extremity: 0.90 × 0.25 = 0.225
Fractal: 0.75 × 0.15 = 0.113
Session: 0.80 × 0.20 = 0.160
FVG: 0.30 × 0.15 = 0.045
Candle: 0.70 × 0.05 = 0.035
Total: 0.748

Example Swing WITH FVG alignment (in zone):
Validity: 0.85 × 0.20 = 0.170
Extremity: 0.90 × 0.25 = 0.225
Fractal: 0.75 × 0.15 = 0.113
Session: 0.80 × 0.20 = 0.160
FVG: 1.00 × 0.15 = 0.150  ← +0.105 improvement
Candle: 0.70 × 0.05 = 0.035
Total: 0.853 ← Significant boost!
```

### Pass Criteria
- ✅ Swings at FVG zones have higher total scores
- ✅ FVG component varies correctly (0.3 → 0.7 → 1.0)
- ✅ Weight total still equals 1.00
- ✅ FVG-aligned swings are prioritized for trading

---

## Test 3: Performance Comparison (30 min)

**Objective**: Measure FVG impact on trading performance

### Test 3A: With FVG Enabled
```
Symbol: EURUSD
Timeframe: M1
Period: 2025-01-01 to 2025-04-01 (3 months)
Enable Trading: TRUE
Enable FVG Filter: TRUE

Score Weights:
- Validity: 0.20
- Extremity: 0.25
- Fractal: 0.15
- Session: 0.20
- FVG: 0.15
- Candle: 0.05
```

### Test 3B: Without FVG (Baseline)
```
Same settings, but:
Enable FVG Filter: FALSE

Score Weights (redistribute FVG 15%):
- Validity: 0.25 (+0.05)
- Extremity: 0.30 (+0.05)
- Fractal: 0.20 (+0.05)
- Session: 0.20 (same)
- FVG: 0.00 (disabled)
- Candle: 0.05 (same)
```

### Metrics to Compare

| Metric | Baseline (No FVG) | With FVG | Expected |
|--------|------------------|----------|----------|
| Win Rate | ? | ? | +3-5% |
| Total Trades | ? | ? | -10% (fewer but better) |
| Profit Factor | ? | ? | +0.2 |
| Avg RR | ? | ? | +0.5R |
| Avg Swing Score | ? | ? | +0.10 |
| Max Drawdown | ? | ? | Lower |

### Pass Criteria
- ✅ FVG version has **higher win rate** (+3-5%)
- ✅ FVG version has **higher profit factor** (+0.2)
- ✅ FVG version has **higher avg swing score** (+0.10)
- ✅ FVG version produces **fewer trades** (better quality)
- ✅ FVG version has **lower or equal drawdown**

### If FVG Underperforms
Possible causes:
- FVG detection capturing too much noise (too many small gaps)
- FVG fill detection too aggressive (rejecting valid FVGs)
- 5-pip proximity threshold too wide/narrow
- Need to adjust FVG weight (try 0.10 or 0.20)

---

## Common Issues & Troubleshooting

### Issue 1: No FVGs Detected
**Symptom**: "Active FVGs: 0" for entire backtest

**Possible Causes:**
- Enable FVG Filter is FALSE
- FVG detection logic has indexing error
- M15 bars not available (check m15Bars initialization)
- Lookback too small (try increasing to 100)

**Debug Steps:**
1. Check console for "Phase 3 FVG Detection: Enabled=True"
2. Verify FVG Lookback Bars parameter > 0
3. Check for any errors during FVG scan
4. Try different date range (maybe data issue)

---

### Issue 2: Too Many FVGs (>100 per week)
**Symptom**: Excessive FVG count, including tiny gaps

**Possible Causes:**
- No minimum gap size filter
- Counting filled FVGs (should only track unfilled)
- Detection too sensitive

**Solutions:**
- Review FVG zone sizes in console
- Check if filled FVGs being removed properly
- Consider adding minimum gap size parameter (future enhancement)

---

### Issue 3: FVG Score Not Affecting Swings
**Symptom**: Total scores unchanged with/without FVG

**Debug Steps:**
1. Check Weight: FVG is 0.15 (not 0.00)
2. Verify Weight Total = 1.00
3. Look for [FVGAlign] messages in console
4. Check if swings are actually near FVG zones
5. Verify CalculateFVGAlignment() is being called

---

### Issue 4: FVGs Fill Too Quickly
**Symptom**: Most FVGs marked as "filled" immediately

**Possible Causes:**
- Fill detection too sensitive (any touch = filled)
- Should require body close in zone, not just wick touch

**Impact:**
- Too few active FVGs for scoring
- Reduces effectiveness of FVG filter

**Future Enhancement:**
- Add parameter for fill sensitivity (wick vs body)

---

## Success Criteria Summary

### FVG Detection SUCCESS if:
- ✅ FVGs detected (20-100 per month typical)
- ✅ Mix of bullish and bearish FVGs
- ✅ FVG zones are logical (top > bottom, 5-100 pip sizes)
- ✅ Only unfilled FVGs tracked
- ✅ No errors in console

### FVG Scoring SUCCESS if:
- ✅ Swings at FVG zones get higher scores
- ✅ Score tiers work correctly (0.3 / 0.7 / 1.0)
- ✅ Weight total = 1.00
- ✅ FVG component visible in score decomposition

### Overall SUCCESS if:
- ✅ Win rate improvement: +3-5%
- ✅ Profit factor improvement: +0.2
- ✅ Higher quality swings (fewer trades, higher scores)
- ✅ System stable with no crashes
- ✅ Ready for optimization and live consideration

---

## Quick Reference: FVG Parameters

```
Enable FVG Filter: TRUE/FALSE
├─ Default: TRUE
└─ Controls: Whether FVG detection runs

FVG Lookback Bars: 50 (default)
├─ Range: 20-200
├─ Lower: Faster, fewer FVGs, less memory
└─ Higher: Slower, more FVGs, more memory

Weight: FVG: 0.15 (default)
├─ Range: 0.00-1.00
├─ Must sum to 1.0 with other weights
└─ Typical: 0.10-0.20
```

---

## Testing Checklist

- [ ] Task 1: Smoke Test (5 min) - Verify FVG detection works
- [ ] Task 2: Detection Logic (20 min) - Validate patterns are correct
- [ ] Task 3: Alignment Scoring (20 min) - Verify scoring impact
- [ ] Task 4: Performance Test (30 min) - Measure win rate improvement
- [ ] Task 5: Document Results - Create validation report

**Estimated Time**: 1.5 hours total

---

## After Validation

### If Tests PASS:
1. ✅ Mark Phase 3 as VALIDATED
2. Run extended backtest (6-12 months)
3. Test on multiple pairs (GBPUSD, USDJPY)
4. Optimize FVG weight if needed
5. Consider forward testing

### If Tests FAIL:
1. ❌ Document specific failures
2. Review FVG detection code
3. Fix issues and re-test
4. Do NOT proceed until passing

---

**Phase 2**: ✅ VALIDATED (Session Awareness working)
**Phase 3**: ⚠️ PENDING VALIDATION (FVG Detection needs testing)

**Next Step**: Run the 5-minute smoke test to verify FVG detection is functional!

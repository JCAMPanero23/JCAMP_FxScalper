# M1 Scalping System - SL/TP Sizing Analysis & Recommendations

## Analysis Date: March 15, 2026

## Issues Identified from Visual Backtest

### 1. SL Too Large Relative to TP Distance

**Root Cause:**
The SL calculation includes three components:
1. **Breakout slippage** (entry distance from zone boundary)
2. **Full zone width** (~4 pips)
3. **SL buffer** (2.0 pips)

**Example Trade Analysis:**

**Trade 1 (SELL from Optimization #1):**
- Rectangle Zone: 1.07916 - 1.07956 (4.0 pips wide)
- Entry: 1.07914 (2 pips below zone bottom)
- SL: 1.07976 (2 pips above zone top)
- **Total SL: 6.2 pips** (2 pips slippage + 4 pips zone + 2 pips buffer = 8 pips, actual 6.2)

**Trade 2 (SELL from Optimization #1):**
- Rectangle Zone: 1.07338 - 1.07378 (4.0 pips wide)
- Entry: 1.07304 (3.4 pips below zone bottom due to slippage)
- SL: 1.07398 (2 pips above zone top)
- **Total SL: 9.4 pips** (3.4 pips slippage + 4 pips zone + 2 pips buffer)

**Trade 3 (BUY from recent backtest):**
- Rectangle Zone: 1.10363 - 1.10403 (4.0 pips wide)
- Entry: 1.10436 (3.3 pips above zone top due to slippage)
- SL: 1.10343 (2 pips below zone bottom)
- **Total SL: 9.3 pips** (3.3 pips slippage + 4 pips zone + 2 pips buffer)

**Trade 4 (BUY from Optimization #1):**
- Rectangle Zone: 1.07408 - 1.07448 (4.0 pips wide)
- Entry: 1.07454 (0.6 pips above zone top - tight entry)
- SL: 1.07388 (2 pips below zone bottom)
- **Total SL: 6.6 pips** (0.6 pips slippage + 4 pips zone + 2 pips buffer)

**Pattern:**
- Minimum SL: 6.2 pips (tight entry with minimal slippage)
- Maximum SL: 9.4 pips (entry with 3.4 pips breakout slippage)
- Average SL: ~7.9 pips

### 2. TP Too Far Away

**TP Placement Logic:**
- Uses H1/M15 resistance/support levels
- Dynamic RR capping (Max RR: 1:5.0 configurable)
- Falls back to minimum RR (2.5R - 3.5R from optimization)

**Example TP Distances:**
- Trade 1: 17.6 pips (RR 1:2.8) for 6.2 pip risk
- Trade 2: 23.5 pips (RR 1:2.5) for 9.4 pip risk
- Trade 3: 28.8 pips (RR 1:3.1) for 9.3 pip risk
- Trade 4: 33.0 pips (RR 1:5.0) for 6.6 pip risk - **CAPPED at max RR**

**The Issue:**
When SL is large (8-9 pips), the TP must be proportionally far (24-28 pips) to maintain 2.5-3.0 RR ratio. This creates:
- Lower probability of TP being hit
- Longer time in trade
- More exposure to market reversals

### 3. Execution Delay vs Zone Creation

**Timeline Analysis (Trade 3 from recent backtest):**
- **01:03:00** - PRE-Zone created at 1.10363-1.10403 (Score: 0.62)
- **01:03:00** - Zone immediately ARMED (price within zone)
- **01:06:00** - BUY trigger executed (Close: 1.10425 > Top: 1.10403)
- **Delay: 3 minutes**

**This is NORMAL for breakout strategy:**
- Zone creation happens when swing point is detected
- Zone gets ARMED when price is within range
- Entry happens when price BREAKS ABOVE/BELOW zone boundary
- The 3-minute delay is waiting for the breakout confirmation

**Not necessarily a problem**, but if we want faster entries, we'd need to change entry mode from "Breakout" to "Immediate touch" - which would increase false signals.

---

## Recommendations (Priority Order)

### **IMMEDIATE OPTIMIZATION - No Code Changes**

#### Option 1: Reduce SL Buffer (Highest Impact)
**Current:** SLBufferPips = 2.0
**Recommended:** SLBufferPips = 1.0 or 0.5

**Impact:**
- All SLs reduced by 1-1.5 pips
- Example: 9.3 pip SL → 7.8-8.3 pips (15% reduction)
- Maintains same zone-based logic
- Reduces capital risk per trade
- Allows closer TP placement while maintaining same RR

**Test This First:** Re-run April-June backtest with SLBufferPips = 1.0

---

#### Option 2: Lower Max Dynamic RR Cap
**Current:** MaxDynamicRR = 5.0 (allows TP up to 5x risk)
**Recommended:** MaxDynamicRR = 3.0 or 3.5

**Impact:**
- Caps TP distance to 3-3.5x the SL instead of 5x
- Example: 9 pip SL with 3.5R = 31.5 pips TP instead of 45 pips
- Higher probability of TP being hit
- Faster trade closure
- More realistic for M1 scalping

**Test This:** Try MaxDynamicRR = 3.5 with SLBufferPips = 1.0

---

#### Option 3: Increase Minimum RR Ratio
**Current Optimization Range:** MinimumRRRatio = 2.5 - 3.5
**Alternative:** Test MinimumRRRatio = 2.0 - 2.5

**Rationale:**
- Lower RR requirements allow closer TP placement
- With tighter SL (from Option 1), a 2.5RR might still be profitable
- Increases trade frequency (fewer rejections due to RR)

**Caution:** Lower RR requires higher win rate to be profitable

---

### **CODE ENHANCEMENTS - For Future Development**

#### Enhancement 1: Add Max TP Distance Parameter
```csharp
[Parameter("Max TP Distance (pips)", DefaultValue = 25.0, MinValue = 15.0, MaxValue = 40.0, Step = 5.0)]
public double MaxTPDistancePips { get; set; }
```

**Logic:**
- Even if RR allows 35 pips TP, cap it at MaxTPDistancePips
- For M1 scalping, 20-25 pips TP is more realistic
- Prevents over-ambitious TPs on large SLs

---

#### Enhancement 2: Zone-Relative SL Calculation
**Current Logic:**
```
SL = Entry ± (ZoneWidth + SlippageDistance + Buffer)
```

**Proposed Logic:**
```csharp
// Calculate SL based on zone only, ignore slippage
double slDistance = (zoneTop - zoneBottom) + (SLBufferPips * Symbol.PipSize);
double sl = isBuy ? zoneBottom - (SLBufferPips * Symbol.PipSize)
                   : zoneTop + (SLBufferPips * Symbol.PipSize);
```

**Benefits:**
- Consistent SL size regardless of entry slippage
- SL always = zone width + buffer (e.g., 4 + 1 = 5 pips)
- Reduces maximum SL from 9.4 to ~5.5 pips

**Risk:**
- SL might be too close on large slippage entries
- Could increase SL hit rate

---

#### Enhancement 3: Entry Mode Selection
**Add parameter:**
```csharp
public enum EntryTiming
{
    Breakout,      // Current: wait for close above/below zone
    ZoneTouch,     // Immediate: enter when price touches zone
    ZoneRetest     // Confirmation: wait for pullback to zone after initial break
}
```

**Impact:**
- **ZoneTouch**: Reduces slippage, faster entries, but more false signals
- **Breakout** (current): Balanced
- **ZoneRetest**: Fewer entries but higher quality

---

## Testing Plan

### Phase 1: Quick Test (SL Buffer Reduction)
1. Set SLBufferPips = 1.0
2. Keep all other parameters at current best values
3. Backtest Apr-Jun 2024
4. Compare results to baseline

**Expected:**
- SL reduced by ~1 pip per trade
- TP distance reduced proportionally
- Same or better win rate
- Better profit factor

---

### Phase 2: Combined Optimization
1. SLBufferPips: 0.5 - 1.5 (Step: 0.5) → 3 values
2. MaxDynamicRR: 3.0 - 4.0 (Step: 0.5) → 3 values
3. MinimumRRRatio: Keep best from previous optimization

**Total: 9 combinations** (~10 minutes)

---

### Phase 3: Validation
1. Test winning parameters on different periods:
   - Apr-Jun 2024 (optimization period)
   - Jul-Sep 2024 (validation - trending)
   - Oct-Dec 2024 (validation - mixed conditions)

---

## Expected Outcomes

### Conservative Estimate:
- **SL reduction:** 15-20% (9.3 pips → 7.5 pips average)
- **TP distance:** 15-20% closer (28 pips → 23 pips)
- **Win rate:** Slight increase (+2-3%) due to closer TPs
- **Profit factor:** Improve from margin

### Optimistic Scenario:
- **SL reduction:** 30% with zone-relative calculation (9.3 → 6.5 pips)
- **TP accuracy:** +5-8% hit rate
- **Overall profitability:** +15-25% improvement

---

## Current Parameter Set (Baseline)
```
SMAPeriod: 200
SwingLookbackBars: 30
MinimumSwingScore: 0.65
MinFVGSizePips: 1.0-2.0 (varies by optimization)
MinimumRRRatio: 2.5-3.5 (varies by optimization)
SLBufferPips: 2.0 ← CHANGE THIS FIRST
MaxDynamicRR: 5.0 ← CHANGE THIS SECOND
ATRMultiplier: 1.5
EnableChandelierSL: True
TrailModeSelection: CurrentPrice (1)
RetracementMultiplier: 0.5
```

---

## Next Steps

1. ✅ **Immediate:** Test SLBufferPips = 1.0 on Apr-Jun backtest
2. ⏳ **Short-term:** Optimize MaxDynamicRR (3.0-4.0 range)
3. 📋 **Medium-term:** Consider code enhancement for Max TP Distance parameter
4. 🔬 **Long-term:** Evaluate zone-relative SL calculation

---

## Notes
- All measurements based on EURUSD M1 backtests
- Zone widths consistently ~4 pips (influenced by ATR and swing point detection)
- Breakout slippage ranges from 0.6 to 3.4 pips (average ~2 pips)
- Current settings work well in trending markets (Apr-Jun profitable)
- Jan-Mar choppy market remains challenging regardless of parameters

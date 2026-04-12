# SMA Stacking Implementation - Complete ✅

**Date**: 2026-04-12
**Version**: v4.6.0 with SMA Stacking Filter
**Branch**: master

---

## 🎯 What Was Implemented

### 1. cBot: SMA Stacking Filter ✅

**New Parameter**:
```csharp
[Parameter("Enable SMA Stacking", DefaultValue = false)]
public bool EnableSMAStacking { get; set; }
```

**New Function**:
```csharp
private bool CheckSMAStacking(string expectedDirection, out string stackingInfo)
{
    // For BUY: M1_SMA > TF0_SMA > TF1_SMA > TF2_SMA
    // For SELL: M1_SMA < TF0_SMA < TF1_SMA < TF2_SMA
}
```

**Updated Default Timeframes** (Lower = Better Stacking Probability):
| Parameter | Old Default | New Default | Range |
|-----------|-------------|-------------|-------|
| **Timeframe0** | M4 | **M3** | M2-M6 |
| **Timeframe1** | M10 | **M5** | M5-M10 |
| **Timeframe2** | M30 | **M10** | M10-M30 |
| **RequireAllTFsAligned** | true | **false** | 3/4 or 4/4 |

**Rationale**: M1, M3, M5, M10 are closely clustered → SMAs more likely to stack cleanly

**Entry Logic Updated**:
```
1. Check MTF Alignment (3/4 or 4/4)
   └─ Pass → Continue

2. [NEW] Check SMA Stacking (if enabled)
   ├─ Disabled → Skip check, continue
   ├─ Stacked correctly → Continue
   └─ NOT stacked → Block entry, log to CSV

3. Check TF0 Crossover
   └─ Crossover → Enter trade
```

**CSV Logging**:
- Added `SMA_Stacked` column
- Values: TRUE, FALSE, or N/A (if stacking disabled)
- Full SMA values logged for all 4 timeframes

---

### 2. Indicator: 4-Timeframe Visual Display ✅

**New SMA Line Added**:
- **SMA 3 (TF0)** - Entry Trigger Timeframe
- Color: **Cyan**
- Style: **Dotted** (distinguishes from alignment TFs)
- Default: **Minute3** (M3)

**Complete Visual Setup**:
| SMA | Timeframe | Color | Style | Purpose |
|-----|-----------|-------|-------|---------|
| **SMA 0** | M1 (chart) | Blue | Solid | Chart alignment |
| **SMA 1** | TF1 (M5) | Gold | Solid | Medium-term |
| **SMA 2** | TF2 (M10) | Orange-Red | Solid | Higher-term |
| **SMA 3** | **TF0 (M3)** | **Cyan** | **Dotted** | **Entry trigger** ✨ |

**CSV Export Updated**:
- Header: Added `SMA3_TF0` column
- Logging: All 4 SMA values recorded

**Initialization Message**:
```
JCAMP MTF MultiSMA v4 (4TF System v4.6.0)
Period:275 | M1:Minute1 | TF0:Minute3 | TF1:Minute5 | TF2:Minute10
Entry: TF0 Crossover
```

---

## 📊 Ready to Test in cAlgo

### Step 1: Rebuild Both Components

**cBot**:
1. Open cAlgo
2. Go to Automate → cBots → Jcamp_1M_scalping
3. **Rebuild** (should see new parameters)

**Indicator**:
1. Go to Automate → Indicators → JCAMP_MTF_MultiSMA_v2
2. **Rebuild** (should see SMA 3 parameters)

### Step 2: Verify Parameters

**cBot Parameters** (check in cAlgo UI):
```
MTF SMA Alignment:
├─ MTF SMA Period: 275
├─ TF0 - Entry Trigger: Minute3 ✅
├─ TF1 - Medium Term: Minute5 ✅
├─ TF2 - Higher Term: Minute10 ✅
├─ Require All TFs Aligned: FALSE ✅ (3/4 mode)
└─ Enable SMA Stacking: FALSE ✅ (optional filter)
```

**Indicator Parameters**:
```
SMA 0 - Chart TF: Enabled
SMA 1 - TF1: Enabled, Minute5
SMA 2 - TF2: Enabled, Minute10
SMA 3 - TF0: Enabled, Minute3 ✅ (NEW!)
```

### Step 3: Load Indicator on Chart

1. Open EURUSD M1 chart
2. Add indicator: JCAMP_MTF_MultiSMA_v2
3. **Match cBot settings**:
   - SMA Period: 275
   - TF1: Minute5
   - TF2: Minute10
   - TF0: Minute3

4. **You should see 4 SMA lines**:
   - Blue solid (M1)
   - Gold solid (M5)
   - Orange-Red solid (M10)
   - **Cyan dotted (M3)** ← NEW!

---

## 🧪 Testing Strategy

### Test 1: Baseline - Lower Timeframes Only

**Hypothesis**: Lower TFs (M1, M3, M5, M10) improve performance vs original (M1, M4, M10, M30)

**Settings**:
```
TF0: M3
TF1: M5
TF2: M10
RequireAllTFsAligned: FALSE (3/4 alignment)
EnableSMAStacking: FALSE (off)
```

**Run**: Jan-Mar 2026 backtest
**Compare**: vs original v4.6.0 (M4, M10, M30)
**Success**: Total R > 0 (positive)

---

### Test 2: Enhanced - SMA Stacking ON

**Hypothesis**: SMA stacking adds quality filter, improves win rate

**Settings**:
```
TF0: M3
TF1: M5
TF2: M10
RequireAllTFsAligned: FALSE (3/4 alignment)
EnableSMAStacking: TRUE (ON) ← Key difference
```

**Run**: Same period (Jan-Mar 2026)
**Compare**: vs Test 1 baseline
**Success**: Better Profit Factor, lower Max DD

---

### Test 3: Different TF0 Values

**Hypothesis**: Optimal TF0 might not be M3

**Test Matrix**:
| Test | TF0 | TF1 | TF2 | Stacking | Expected |
|------|-----|-----|-----|----------|----------|
| 3A | M2 | M5 | M10 | OFF | Very responsive, noisy? |
| 3B | M3 | M5 | M10 | OFF | Baseline |
| 3C | M4 | M5 | M10 | OFF | Less noise, slower? |
| 3D | M5 | M5 | M10 | OFF | TF0 = TF1 (interesting) |

**Goal**: Find optimal TF0 for positive R

---

### Test 4: Stacking vs 4/4 Alignment

**Question**: Is stacking better than strict 4/4 alignment?

| Test | Alignment | Stacking | Combinations | Trades Expected |
|------|-----------|----------|--------------|-----------------|
| 4A | 3/4 | OFF | More lenient | High |
| 4B | 3/4 | ON | Quantity + Quality | Medium |
| 4C | 4/4 | OFF | Strict | Low |
| 4D | 4/4 | ON | Very strict | Very low |

**Hypothesis**: 4B (3/4 + Stacking) = Best balance

---

## 📈 Visual Verification

### What to Look For on Chart

1. **4 SMA Lines Visible**:
   - Blue (M1): Should be most reactive
   - Cyan dotted (M3 TF0): Entry trigger, slightly smoother than M1
   - Gold (M5 TF1): Smoother trend line
   - Orange-Red (M10 TF2): Smoothest, higher-term bias

2. **SMA Stacking Example (BUY)**:
   ```
   Price is above all SMAs
   Blue M1 SMA (top)
   Cyan M3 SMA (TF0)
   Gold M5 SMA (TF1)
   Orange M10 SMA (TF2, bottom)

   = Perfect uptrend stack!
   ```

3. **SMA Stacking Example (SELL)**:
   ```
   Price is below all SMAs
   Orange M10 SMA (TF2, top)
   Gold M5 SMA (TF1)
   Cyan M3 SMA (TF0)
   Blue M1 SMA (bottom)

   = Perfect downtrend stack!
   ```

4. **Entry Trigger Detection**:
   - Watch for **Cyan (TF0) crossover** events
   - When price crosses Cyan TF0 SMA while other TFs aligned = Entry candidate
   - Check if SMAs are stacked (if stacking enabled)

---

## 📝 CSV Analysis

### cBot CSV Columns

```csv
Timestamp,BarIndex,Price_M1,SMA_M1,Align_M1,SMA_TF0,Align_TF0,SMA_TF1,Align_TF1,
SMA_TF2,Align_TF2,MTF_Aligned,MTF_Direction,TF0_Crossover,SMA_Stacked
```

**Key Columns**:
- `SMA_M1`, `SMA_TF0`, `SMA_TF1`, `SMA_TF2`: All 4 SMA values
- `SMA_Stacked`: TRUE/FALSE/N/A
- `TF0_Crossover`: BUY/SELL/NONE (entry trigger)

### Analyzing Stacking

**Filter to stacked entries**:
```python
import pandas as pd
df = pd.read_csv('backtest.csv')

# Only trades where stacking was TRUE
stacked_entries = df[df['SMA_Stacked'] == 'TRUE']
print(f"Stacked entries: {len(stacked_entries)}")
print(f"Average R: {stacked_entries['Trade_R'].mean()}")
```

**Compare stacked vs non-stacked**:
- Stacked: Should have higher win rate, better PF
- Non-stacked: More trades, but lower quality?

---

## 🔧 Additional Features Still Pending

### Session Box Removal (Not Yet Done)
- User requested removing session box drawings
- Lower priority - doesn't affect testing
- Can implement after core testing

### Enhanced Info Panel (Not Yet Done)
- More comprehensive on-chart info panel
- Show alignment status, stacking status, SMA values
- Would help debugging, but not critical for initial testing
- Can implement based on user feedback after testing

---

## 🎯 Success Criteria

### Minimum Success (Test 1 Baseline)
- ✅ Total R > 0 (positive)
- ✅ Max DD < 20%
- ✅ Profit Factor > 1.2

### Good Success (Test 2 Stacking)
- ✅ Total R improved vs baseline
- ✅ Profit Factor > 1.3
- ✅ Win Rate 30-40%
- ✅ Max DD < 15%

### Excellent Success (Beats v4.5.x)
- ✅ Total R > v4.5.x on same period
- ✅ Profit Factor > 1.5
- ✅ System proves v4.6.0 concept viable

---

## 📁 Files Updated

| File | Purpose | Status |
|------|---------|--------|
| `Jcamp_1M_scalping.cs` | cBot with stacking | ✅ Updated & Copied to cAlgo |
| `Indicator/JCAMP_MTF_MultiSMA_v2.cs` | 4TF indicator | ✅ Updated & Copied to cAlgo |
| `debug/SMA_STACKING_IMPLEMENTATION_COMPLETE.md` | This document | ✅ Created |

**Git Commits**:
- `4fd48b6`: cBot SMA stacking + lower TF defaults
- `22301a4`: Indicator 4th SMA line for TF0

---

## 🚀 Next Actions

1. **Rebuild in cAlgo** - Both cBot and indicator
2. **Load indicator on M1 chart** - Verify 4 SMA lines visible
3. **Run Test 1** - Baseline (stacking OFF, lower TFs)
4. **Run Test 2** - Enhanced (stacking ON)
5. **Compare results** - Which performs better?
6. **Optimize TF0** - If baseline works, try M2, M4, M5
7. **Report findings** - Share results for analysis

---

**Status**: ✅ Implementation complete, ready for testing!
**Last Updated**: 2026-04-12
**Branch**: master (v4.6.0 with SMA stacking)

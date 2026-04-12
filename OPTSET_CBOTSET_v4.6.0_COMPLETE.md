# .optset and .cbotset Files Updated for v4.6.0 - COMPLETE ✅

**Date**: 2026-04-11
**Status**: All optimization and parameter files updated for v4.6.0 4TF system

---

## ✅ Files Created/Updated

### Optimization Sets (.optset)

| File | Purpose | Optimize | Fixed | Combinations |
|------|---------|----------|-------|--------------|
| **v4.6.0_Phase1_TF0_ONLY.optset** | Phase 1: TF0 optimization | TF0 (M2-M6) | TF1, TF2, SMA, ADX, Sessions | 5 |
| **v4.6.0_Phase2_SMA_ADX.optset** | Phase 2: SMA + ADX | SMA, ADX Period/Min/Max, RR | TF0 (from P1), TF1, TF2 | ~4,900 |
| v4.6.0_4TF_System.optset | Original (all-in-one) | TF0, TF1, TF2, SMA, ADX | - | Very large |

**Recommendation**: Use Phase 1 first, then Phase 2 (not the all-in-one).

### Parameter Sets (.cbotset)

| File | Purpose | London | Use For |
|------|---------|--------|---------|
| **Walk1_WFO_Recommended.cbotset** | WFO recommendations from Jan-Mar 2025 | OFF | Apr 2025 validation (WFO) |
| **Walk1_Baseline_AllSessions.cbotset** | Baseline for comparison | ON | Apr 2025 validation (Baseline) |

---

## 🎯 Key Updates for v4.6.0

### 1. Correct Parameter Names ✅

**v4.6.0 4TF System**:
```json
{
  "Timeframe0": "m3",    // Entry trigger (NEW)
  "Timeframe1": "m8",    // Medium term (renamed from Timeframe2)
  "Timeframe2": "m15"    // Higher term (renamed from Timeframe3)
}
```

**Old v4.5.x** (for reference):
```json
{
  "Timeframe2": "m4",    // Old naming
  "Timeframe3": "m15"    // Old naming
}
```

---

### 2. ADX MinThreshold CONSTRAINED ✅

**Critical Fix**: ADX MinThreshold range updated for v4.6.0 safety.

| Version | Old Range | NEW Range | Reason |
|---------|-----------|-----------|--------|
| Phase 1 | N/A | **FIXED at 12** | Don't optimize yet |
| Phase 2 | 15-30 ❌ | **10-18** ✅ | Prevent FlipDirection disasters |

**WARNING**: ADX threshold > 18 causes FlipDirection to trigger on 40-50% of trades with negative avg R!

---

### 3. WFO-Informed Defaults ✅

All files now use WFO recommendations from Jan-Mar 2025 analysis:

```json
{
  "EnableLondonSession": "False",   // WFO: London had -14.91R
  "EnableNYSession": "True",        // WFO: NY had +10.49R
  "EnableAsianSession": "True",     // WFO: Asian had +26.22R
  "ADXMinThreshold": "12",          // WFO: Keep at 12 (NOT 23!)
  "ADXPeriod": "16"                 // WFO: Optimal value
}
```

---

## 📊 WFO Analyzer Now Exports

When you run the WFO analyzer, it now exports **5 files** (up from 3):

### Standard Exports

1. **recommended_settings_YYYYMMDD.json** - Full data
2. **recommended_settings_YYYYMMDD.csv** - cAlgo import format
3. **recommended_settings_YYYYMMDD.txt** - Human-readable

### NEW Exports ✨

4. **wfo_recommended_YYYYMMDD.cbotset** - Ready to load in cAlgo
5. **optimization_strategy_YYYYMMDD.txt** - Optimization roadmap

---

## 📁 File Locations

```
D:\JCAMP_FxScalper\
│
├── optimization_sets/
│   ├── v4.6.0_Phase1_TF0_ONLY.optset           ← Phase 1: TF0 (M2-M6)
│   ├── v4.6.0_Phase2_SMA_ADX.optset            ← Phase 2: SMA + ADX
│   ├── Walk1_WFO_Recommended.cbotset           ← Walk 1 WFO settings
│   ├── Walk1_Baseline_AllSessions.cbotset      ← Walk 1 Baseline
│   └── v4.6.0_4TF_System.optset                ← Original (not recommended)
│
└── data/wfo_test/walk1_jan_mar_to_apr/train/analysis_results/
    ├── wfo_recommended_YYYYMMDD.cbotset        ← Auto-generated from WFO
    ├── optimization_strategy_YYYYMMDD.txt      ← Auto-generated roadmap
    ├── recommended_settings_YYYYMMDD.csv       ← Parameter list
    └── recommended_settings_YYYYMMDD.txt       ← Full details
```

---

## 🚀 How to Use

### Phase 1: TF0 Optimization

**Goal**: Find best entry trigger timeframe (M2-M6)

```
1. Open cAlgo
2. Load optimization: optimization_sets/v4.6.0_Phase1_TF0_ONLY.optset
3. Run optimization on Jan-Mar 2026 (or another period)
4. Find which TF0 gives positive R-multiple
5. Note the best TF0 (e.g., "m4")
```

**Expected**: 5 backtests (one per TF0 value)
**Time**: 5-10 minutes

---

### Phase 2: SMA + ADX Optimization

**Goal**: Optimize SMA period and ADX settings with best TF0

**Prerequisites**: Must complete Phase 1 first!

```
1. Open: optimization_sets/v4.6.0_Phase2_SMA_ADX.optset
2. UPDATE Timeframe0 to best value from Phase 1!
   Example: Change "Timeframe0": "m4" to your best TF0
3. Load in cAlgo
4. Run optimization (use Genetic Algorithm)
5. Target: Profit Factor > 1.3, Max DD < 20%
```

**Expected**: ~4,900 combinations (genetic algorithm finds top 100-200)
**Time**: 30-60 minutes

---

### Walk-Forward Validation

**Goal**: Test if WFO recommendations work on out-of-sample period

#### Run 1: WFO Recommended Settings

```
1. Open cAlgo
2. Load: optimization_sets/Walk1_WFO_Recommended.cbotset
3. Period: Apr 1-30, 2025
4. Symbol: EURUSD, Timeframe: M1
5. Run backtest
6. Save CSV to: data/wfo_test/walk1_jan_mar_to_apr/validate/TradeLog_Apr_2025_WFO.csv
```

#### Run 2: Baseline Settings

```
1. Load: optimization_sets/Walk1_Baseline_AllSessions.cbotset
2. Same period: Apr 1-30, 2025
3. Run backtest
4. Save CSV to: data/wfo_test/walk1_jan_mar_to_apr/validate/TradeLog_Apr_2025_Baseline.csv
```

#### Compare Results

```bash
python compare_wfo_results.py 1
```

**Success**: WFO Total R > Baseline Total R

---

## 📋 Auto-Generated Optimization Strategy

When you run WFO analyzer, it now creates `optimization_strategy_YYYYMMDD.txt` with:

```
OPTIMIZATION STRATEGY RECOMMENDATIONS
======================================================================

v4.6.0 4-TIMEFRAME SYSTEM OPTIMIZATION STRATEGY
----------------------------------------------------------------------

Phase 1: TF0 Entry Trigger Optimization (HIGHEST PRIORITY)
  File: optimization_sets/v4.6.0_Phase1_TF0_ONLY.optset
  Optimize: Timeframe0 (M2, M3, M4, M5, M6)
  Goal: Find which TF0 gives positive R-multiple

Phase 2: SMA + ADX Optimization (After Phase 1)
  File: optimization_sets/v4.6.0_Phase2_SMA_ADX.optset
  WARNING: ADX MinThreshold CONSTRAINED to 10-18 for v4.6.0!

VALIDATION FILES FOR WALK-FORWARD TESTING:
  WFO: optimization_sets/Walk1_WFO_Recommended.cbotset
  Baseline: optimization_sets/Walk1_Baseline_AllSessions.cbotset
```

---

## 🎯 Parameter Details

### Phase 1 TF0 Optimization (.optset)

**Optimized**:
- `Timeframe0`: m2, m3, m4, m5, m6 (5 values)

**Fixed**:
- `MTFSMAPeriod`: 275
- `Timeframe1`: m10
- `Timeframe2`: m30
- `ADXMinThreshold`: 12 (DO NOT change!)
- `ADXPeriod`: 16
- `EnableLondonSession`: False (WFO)
- `EnableNYSession`: True (WFO)
- `EnableAsianSession`: True (WFO)

---

### Phase 2 SMA + ADX Optimization (.optset)

**Optimized**:
- `MTFSMAPeriod`: 200, 225, 250, 275, 300 (5 values)
- `ADXPeriod`: 9, 11, 13, 15, 17, 19, 21 (7 values, step 2)
- `ADXMinThreshold`: 10, 12, 14, 16, 18 (5 values, step 2) ⚠️ **MAX 18!**
- `ADXMaxThreshold`: 35, 40, 45, 50 (4 values, step 5)
- `MinimumRRRatio`: 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0 (7 values)

**Total**: 5 × 7 × 5 × 4 × 7 = 4,900 combinations

**Fixed**:
- `Timeframe0`: **UPDATE THIS to best from Phase 1!**
- `Timeframe1`: m10
- `Timeframe2`: m30
- Sessions: London OFF, NY + Asian ON (WFO)

---

## ⚠️ Important Warnings

### 1. ADX MinThreshold Constraint

```
❌ DON'T: Use ADXMinThreshold > 18
✅ DO: Keep ADXMinThreshold in 10-18 range

Reason: Values > 18 trigger FlipDirection on 40-50% of trades
Result: Negative avg R, equity destruction
Evidence: Jan-Mar 2026 backtest (ADX=23 caused -51.7% loss)
```

### 2. Phase 1 Before Phase 2

```
❌ DON'T: Run Phase 2 with default TF0
✅ DO: Complete Phase 1, then update Phase 2 with best TF0

Reason: TF0 is the HIGHEST PRIORITY parameter
Impact: Can make 20-50% difference in results
```

### 3. Update Timeframe0 in Phase 2

Before running Phase 2 optimization, **MANUALLY UPDATE** the file:

```json
{
  "name": "Timeframe0",
  "value": "m4",     // ← CHANGE THIS to your best TF0 from Phase 1!
  "optimize": false
}
```

---

## 📊 Expected Results

### Phase 1: TF0 Optimization

**Success Criteria**:
- ✅ At least ONE TF0 with positive R-multiple
- ✅ Profit Factor > 1.2
- ✅ Max DD < 20%

**If All TF0 Negative**:
- Try different periods (different market conditions)
- Try different currency pairs (USDJPY, GBPUSD)
- System may not be viable for current conditions

### Phase 2: SMA + ADX

**Success Criteria**:
- ✅ Profit Factor > 1.3
- ✅ Max DD < 20%
- ✅ Win Rate 25-45%
- ✅ Improvement over Phase 1 baseline

---

## 🔄 Version Compatibility

| Bot Version | .optset | .cbotset | Notes |
|-------------|---------|----------|-------|
| **v4.6.0** | ✅ Use new files | ✅ Use new files | TF0, TF1, TF2 naming |
| **v4.5.x** | ❌ Not compatible | ❌ Not compatible | Old TF2, TF3 naming |

**Migration**: If on v4.5.x, upgrade bot to v4.6.0 first before using these files.

---

## 📝 Summary

### What Was Updated

1. ✅ **v4.6.0_Phase1_TF0_ONLY.optset** - TF0 optimization only (5 combinations)
2. ✅ **v4.6.0_Phase2_SMA_ADX.optset** - SMA + ADX with ADX 10-18 constraint
3. ✅ **Walk1_WFO_Recommended.cbotset** - WFO settings (London OFF)
4. ✅ **Walk1_Baseline_AllSessions.cbotset** - Baseline (London ON)
5. ✅ **WFO Analyzer** - Now exports .cbotset + optimization strategy

### What Was Fixed

1. ✅ Parameter names: Timeframe0, Timeframe1, Timeframe2 (v4.6.0)
2. ✅ ADX MinThreshold: Constrained to 10-18 max (safety)
3. ✅ WFO defaults: London OFF, NY + Asian ON
4. ✅ Phase separation: TF0 first, then SMA+ADX

---

**Status**: ✅ ALL FILES UPDATED AND TESTED
**Ready**: ✅ For Phase 1 TF0 optimization
**Ready**: ✅ For Walk-Forward validation testing

**Next Action**: Run Phase 1 optimization or Walk 1 validation!

# Walk-Forward Optimization Testing Plan
## 3-Month Train + 1-Month Validate Methodology

**Goal**: Validate that updated WFO analyzer (v4.6.0) can identify profitable settings for out-of-sample periods

---

## 🎯 Quick Start: 4-Walk Validation Test

Test WFO on 4 walks to prove the methodology works:

### Walk 1: Jan-Mar 2025 → Apr 2025 ⭐ **START HERE**

| Phase | Period | Data Status | Action |
|-------|--------|-------------|--------|
| **TRAIN** | Jan-Mar 2025 | ✅ Have (120 trades) | Run WFO analyzer |
| **VALIDATE** | Apr 2025 | ⚠️ Need backtest | Run with WFO recommendations |

**Expected Recommendations**:
- Sessions: London OFF, NY + Asian ON
- ADX Threshold: 12 (keep current, don't raise to 23!)
- ADX Period: 16
- TF0: Need to test M2-M6

**Success Criteria**:
- Validation month (Apr 2025) with recommendations > Baseline
- Positive R-multiple in Apr 2025
- Profit Factor > 1.2

---

### Walk 2: Feb-Apr 2025 → May 2025

| Phase | Period | Data Status | Action |
|-------|--------|-------------|--------|
| **TRAIN** | Feb-Apr 2025 | ⚠️ Need backtests | Need Feb, Apr 2025 data |
| **VALIDATE** | May 2025 | ⚠️ Need backtest | Run with recommendations |

**Dependencies**: Need to backtest Feb + Apr 2025 first

---

### Walk 3: Jul-Sep 2025 → Oct 2025

| Phase | Period | Data Status | Action |
|-------|--------|-------------|--------|
| **TRAIN** | Jul-Sep 2025 | ✅ Have (96 trades) | Run WFO analyzer |
| **VALIDATE** | Oct 2025 | ⚠️ Need backtest | Run with WFO recommendations |

**Advantage**: Already have training data!

---

### Walk 4: Oct-Dec 2025 → Jan 2026

| Phase | Period | Data Status | Action |
|-------|--------|-------------|--------|
| **TRAIN** | Oct-Dec 2025 | ⚠️ Need backtests | Need Oct-Dec 2025 data |
| **VALIDATE** | Jan 2026 | ✅ Have partial | Use Jan 2026 from existing data |

**Advantage**: Can validate against known Jan 2026 data

---

## 📊 Walk 1 Detailed Plan: Jan-Mar 2025 → Apr 2025

### Step 1: Analyze Training Period (Jan-Mar 2025)

**Use existing baseline backtest**:
```bash
cd D:\JCAMP_FxScalper
python wfo_analyzer.py "data/backtest_archive/v4.6.0_baseline/Jan_Mar_2026_ADX12_ORIGINAL/TradeLog_EURUSD_1092444_20260411_130622.csv"
```

**Expected WFO Recommendations**:
- ✅ Enable Asian session (26.22R)
- ✅ Enable NY session (10.49R)
- ❌ Disable London session (-14.91R)
- ✅ Keep ADX threshold = 12 (not 23!)
- ✅ ADX Period = 16
- ✅ ADX Mode = FlipDirection

**Save recommendations to**: `data/wfo_test/walk1/train_recommendations.csv`

---

### Step 2: Apply Settings to Validation Period (Apr 2025)

**Create new backtest with Walk 1 recommendations**:

| Parameter | Baseline | Walk 1 WFO | Source |
|-----------|----------|------------|--------|
| EnableLondonSession | True | **False** | WFO (session analysis) |
| EnableNYSession | True | **True** | WFO (session analysis) |
| EnableAsianSession | True | **True** | WFO (session analysis) |
| ADXMinThreshold | 12 | **12** | WFO (keep current) |
| ADXPeriod | 16 | **16** | WFO (confirmed) |
| ADXMode | FlipDirection | **FlipDirection** | WFO (confirmed) |
| MTFSMAPeriod | 275 | **275** | Baseline (no change) |
| Timeframe0 | Minute4 | **Minute4** | Baseline (optimize later) |
| Timeframe1 | Minute10 | **Minute10** | Baseline (no change) |
| Timeframe2 | Minute30 | **Minute30** | Baseline (no change) |

**Run backtest**:
- Period: Apr 1-30, 2025
- Symbol: EURUSD
- Timeframe: M1
- Settings: Walk 1 WFO recommendations (above)

**Save to**: `data/wfo_test/walk1/validate_apr2025_wfo.csv`

---

### Step 3: Run Baseline Comparison

**Also run Apr 2025 with ORIGINAL baseline settings**:
- All sessions enabled (London, NY, Asian)
- ADX threshold = 12
- Everything else same

**Save to**: `data/wfo_test/walk1/validate_apr2025_baseline.csv`

---

### Step 4: Compare Results

**Metrics to compare**:

| Metric | Baseline (All Sessions) | WFO (London OFF) | Winner |
|--------|-------------------------|------------------|--------|
| Total R | ? | ? | ? |
| Win Rate % | ? | ? | ? |
| Profit Factor | ? | ? | ? |
| Max DD % | ? | ? | ? |
| Total Trades | ? | ? | ? |

**Success = WFO > Baseline**

---

## 📁 Directory Structure for WFO Testing

```
D:\JCAMP_FxScalper\data\wfo_test\
│
├── walk1_jan_mar_to_apr\
│   ├── train\
│   │   ├── TradeLog_Jan_Mar_2025.csv (existing)
│   │   └── analysis_results\
│   │       ├── recommended_settings_YYYYMMDD.csv
│   │       └── analysis_dashboard_YYYYMMDD.png
│   │
│   └── validate\
│       ├── TradeLog_Apr_2025_WFO.csv (new - with recommendations)
│       ├── TradeLog_Apr_2025_Baseline.csv (new - original settings)
│       └── comparison_results.md
│
├── walk2_feb_apr_to_may\
│   └── (same structure)
│
├── walk3_jul_sep_to_oct\
│   └── (same structure)
│
└── walk4_oct_dec_to_jan\
    └── (same structure)
```

---

## 🚀 Execution Plan

### Week 1: Walk 1 Setup & Execution

**Day 1-2: Analyze Training Period**
- [ ] Run WFO analyzer on Jan-Mar 2025 (already have data)
- [ ] Review recommendations
- [ ] Document baseline vs recommended settings
- [ ] Create `data/wfo_test/walk1_jan_mar_to_apr/` structure

**Day 3: Run Validation Backtests**
- [ ] Backtest Apr 2025 with WFO recommendations (London OFF)
- [ ] Backtest Apr 2025 with baseline settings (all sessions ON)
- [ ] Save both CSVs

**Day 4-5: Analyze Results**
- [ ] Compare WFO vs Baseline on Apr 2025
- [ ] Calculate improvement (if any)
- [ ] Document findings
- [ ] **Decision point**: If WFO > Baseline, continue to Walk 2

---

### Week 2: Walk 3 (Jul-Sep → Oct)

**Why Walk 3 before Walk 2?** We already have Jul-Sep 2025 training data!

**Day 6-7: Analyze Training Period**
- [ ] Run WFO analyzer on Jul-Sep 2025 (already have data)
- [ ] Get recommendations
- [ ] Compare to Walk 1 recommendations (are they consistent?)

**Day 8: Run Validation Backtests**
- [ ] Backtest Oct 2025 with WFO recommendations
- [ ] Backtest Oct 2025 with baseline settings
- [ ] Save both CSVs

**Day 9-10: Analyze & Compare**
- [ ] Compare WFO vs Baseline for Oct 2025
- [ ] Compare Walk 1 vs Walk 3 recommendations
- [ ] Look for consistency in recommendations
- [ ] **Decision point**: If 2/2 walks show WFO > Baseline, methodology is validated!

---

### Week 3-4: Fill Missing Data (Walk 2, Walk 4)

**Only if Walk 1 and Walk 3 showed WFO works**

- [ ] Backtest Feb 2025, Apr 2025, May 2025 for Walk 2
- [ ] Backtest Oct-Dec 2025 for Walk 4
- [ ] Run WFO analysis and validation for each

---

## 📊 WFO Validation Scorecard

Track success rate across all walks:

| Walk | Train Period | Validate Period | WFO Total R | Baseline Total R | Winner | Status |
|------|-------------|-----------------|-------------|------------------|--------|--------|
| 1 | Jan-Mar 2025 | Apr 2025 | ? | ? | ? | ⏳ Pending |
| 3 | Jul-Sep 2025 | Oct 2025 | ? | ? | ? | ⏳ Pending |
| 2 | Feb-Apr 2025 | May 2025 | ? | ? | ? | 📅 Later |
| 4 | Oct-Dec 2025 | Jan 2026 | ? | ? | ? | 📅 Later |

**Success Criteria**:
- WFO wins ≥ 75% of walks (3 out of 4)
- WFO average R > Baseline average R
- At least 2 walks show profitable validation periods

**If WFO fails** (wins < 50%):
- Indicates recommendations are not generalizing
- May need to adjust WFO methodology
- Or system itself may not be robust

---

## 🎯 Expected Outcomes

### If WFO Works (Success)
- Walk 1 (Apr 2025): WFO with London OFF > Baseline
- Walk 3 (Oct 2025): WFO with London OFF > Baseline
- Recommendations are consistent across walks
- **Conclusion**: WFO analyzer works, can be trusted for future optimization

### If WFO Fails
- WFO recommendations don't beat baseline in out-of-sample
- Recommendations vary wildly between walks
- **Conclusion**: Need to improve WFO methodology or fix system

---

## 🛠️ Tools & Scripts Needed

### 1. Batch Backtest Runner

Create `run_validation_backtest.bat`:
```batch
@echo off
REM Run validation backtest for WFO walk

set WALK=%1
set MONTH=%2
set SETTINGS=%3

echo Running validation backtest for Walk %WALK% - %MONTH%
echo Using settings: %SETTINGS%

REM (User manually runs in cAlgo - this is just a template)
echo.
echo BACKTEST SETTINGS:
type "data\wfo_test\walk%WALK%\train\analysis_results\recommended_settings*.csv"
echo.
echo Press any key when backtest is complete and CSV is saved...
pause
```

### 2. WFO Comparison Script

Create `compare_wfo_results.py`:
```python
"""Compare WFO recommendations vs Baseline for validation period"""

import pandas as pd
import sys

def compare_results(walk_num):
    # Load WFO and Baseline CSVs
    wfo_csv = f"data/wfo_test/walk{walk_num}/validate/TradeLog_WFO.csv"
    baseline_csv = f"data/wfo_test/walk{walk_num}/validate/TradeLog_Baseline.csv"

    wfo_df = pd.read_csv(wfo_csv)
    baseline_df = pd.read_csv(baseline_csv)

    # Compare metrics
    print(f"\n{'='*70}")
    print(f"Walk {walk_num} Validation Results Comparison")
    print(f"{'='*70}\n")

    metrics = {
        'Total Trades': (len(wfo_df), len(baseline_df)),
        'Total R': (wfo_df['RMultiple'].sum(), baseline_df['RMultiple'].sum()),
        'Win Rate %': (wfo_df['WinningTrade'].mean()*100, baseline_df['WinningTrade'].mean()*100),
        'Avg R': (wfo_df['RMultiple'].mean(), baseline_df['RMultiple'].mean())
    }

    print(f"{'Metric':<20} {'WFO':<15} {'Baseline':<15} {'Winner':<10}")
    print("-"*60)

    for metric, (wfo_val, base_val) in metrics.items():
        winner = "WFO ✓" if wfo_val > base_val else "Baseline"
        print(f"{metric:<20} {wfo_val:<15.2f} {base_val:<15.2f} {winner:<10}")

    print()

if __name__ == '__main__':
    walk_num = sys.argv[1] if len(sys.argv) > 1 else '1'
    compare_results(walk_num)
```

---

## 📝 Quick Reference: Walk 1 Execution Checklist

### Pre-Execution
- [ ] Create `data/wfo_test/walk1_jan_mar_to_apr/` directories
- [ ] Copy Jan-Mar 2025 baseline CSV to `walk1/train/`

### Training Phase
- [ ] Run: `python wfo_analyzer.py "data/wfo_test/walk1_jan_mar_to_apr/train/TradeLog*.csv"`
- [ ] Review analysis results in `train/analysis_results/`
- [ ] Note recommended settings (should be: London OFF, ADX=12)

### Validation Phase - WFO Settings
- [ ] Open cAlgo
- [ ] Load Apr 2025 data (Apr 1-30, 2025)
- [ ] Apply WFO settings:
  - EnableLondonSession = **FALSE**
  - EnableNYSession = TRUE
  - EnableAsianSession = TRUE
  - ADXMinThreshold = 12
  - (keep all other settings same as Jan-Mar baseline)
- [ ] Run backtest
- [ ] Save CSV to `walk1/validate/TradeLog_Apr_2025_WFO.csv`

### Validation Phase - Baseline Settings
- [ ] Keep same Apr 2025 period
- [ ] Apply baseline settings (all sessions ON)
- [ ] Run backtest
- [ ] Save CSV to `walk1/validate/TradeLog_Apr_2025_Baseline.csv`

### Analysis
- [ ] Run: `python compare_wfo_results.py 1`
- [ ] Document winner and improvement %
- [ ] Decide: Continue to Walk 3 if WFO wins

---

## 🎯 Success Metrics

### Individual Walk Success
- ✅ WFO Total R > Baseline Total R
- ✅ Positive R in validation month
- ✅ Profit Factor > 1.2

### Overall WFO Validation Success
- ✅ WFO wins ≥ 75% of walks (3/4)
- ✅ Average improvement > 10%
- ✅ Recommendations consistent across walks
- ✅ No catastrophic failures (WFO causing >-20% while baseline positive)

### If All Success Criteria Met
- **WFO methodology VALIDATED** ✓
- Can trust WFO analyzer for future optimizations
- Apply WFO recommendations to live trading with confidence

---

**Status**: Ready to execute Walk 1
**Estimated Time**: 1 week for Walk 1, 2 weeks for Walk 1+3
**Next Action**: Set up `data/wfo_test/walk1_jan_mar_to_apr/` and run WFO analyzer on Jan-Mar 2025

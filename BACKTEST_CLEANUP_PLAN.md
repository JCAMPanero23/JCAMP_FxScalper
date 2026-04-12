# Backtest Cleanup & Fresh Start Plan

**Date**: 2026-04-11
**Action**: Archive old data, start fresh v4.6.0 optimization

---

## ✅ What We Just Did

1. Created organized archive structure in `data/backtest_archive/`
2. Moved failed WFO experiment (ADX=23) to `archive_failed_wfo/`
3. Copied v4.6.0 baseline (ADX=12) to `v4.6.0_baseline/`

---

## 📊 Current Backtest Inventory

### Keep (Reference & Learning)

| Period | Location | Status | Keep? | Why |
|--------|----------|--------|-------|-----|
| **Jan-Mar 2026** (ADX=12) | `v4.6.0_baseline/` | ✅ Good | **YES** | Baseline for v4.6.0, shows +27.4% return |
| **Jan-Mar 2026** (ADX=23) | `archive_failed_wfo/` | ❌ Failed | **YES** | Learning material, documents WFO failure |
| **Dec-Feb 2026** | `Dec_Feb_2026/` | ? Unknown | **REVIEW** | Check if v4.6.0 or older version |
| **Jul-Sep 2025** | `Jul_Sep_2025/` | ? Unknown | **REVIEW** | Check if v4.6.0 or older version |
| **Apr-Jun 2025** | `Apr_Jun_2025/` | ? Unknown | **REVIEW** | Check if v4.6.0 or older version |
| **Jul 2025** | `Jul_2025/` | ? Unknown | **REVIEW** | Check if v4.6.0 or older version |
| **Jun-Aug 2025** | `Jun_Aug_2025/` | ? Unknown | **REVIEW** | Check if v4.6.0 or older version |

---

## 🔍 Review Existing Backtests

Check which backtests are v4.6.0 vs older:

```bash
# Check for v4.6.0 (has Timeframe0 and Timeframe1 columns)
cd "D:\JCAMP_FxScalper\data\backtest_archive"

for dir in Dec_Feb_2026 Jul_Sep_2025 Apr_Jun_2025 Jul_2025 Jun_Aug_2025; do
    echo "Checking $dir..."

    # Find CSV files
    csv_file=$(find "$dir" -name "TradeLog*.csv" -type f | head -1)

    if [ -n "$csv_file" ]; then
        # Check for v4.6.0 markers (Timeframe0, Timeframe1)
        if head -1 "$csv_file" | grep -q "Timeframe0"; then
            echo "  ✓ v4.6.0 (4TF system) - KEEP for baseline"
        else
            echo "  ✗ v4.5.x or earlier (3TF system) - ARCHIVE"
        fi
    else
        echo "  ? No CSV found"
    fi
done
```

---

## 📁 Recommended Actions

### Action 1: Review & Categorize (Do First)

Run the check above to identify which backtests are v4.6.0.

**Move to appropriate folders**:
- v4.6.0 backtests → `v4.6.0_baseline/`
- v4.5.x and earlier → `archive_v4.5.x_and_earlier/` (create if needed)

### Action 2: Keep Baseline Data

**KEEP in `v4.6.0_baseline/`**:
- ✅ Jan-Mar 2026 (ADX=12) - Your best baseline (+27.4%)
- ✅ Any other v4.6.0 backtests with clean settings
- ✅ Different time periods (for out-of-sample validation)

**KEEP in `archive_failed_wfo/`**:
- ✅ Jan-Mar 2026 (ADX=23) - Documents what went wrong

### Action 3: Archive Old Versions

**MOVE to `archive_v4.5.x_and_earlier/`** (if they exist):
- Any backtests without Timeframe0/Timeframe1
- Old 3TF system backtests
- Pre-v4.6.0 WFO data

**Reason**: Different architecture, not comparable to v4.6.0

### Action 4: Delete Only Junk

**DELETE** (safe to remove):
```bash
# Delete partial/corrupt files
find . -name "TradeLog*.csv" -size 0 -delete

# Delete temporary test runs (<30 trades)
# (Manual check - don't auto-delete)

# Delete duplicate files
# (Manual check - don't auto-delete)
```

---

## 🚀 Fresh Start: New v4.6.0 Optimization

### Phase 1: TF0 Optimization (Highest Priority)

**Goal**: Find optimal entry trigger timeframe

**Settings**:
```
Fixed Parameters:
- TF1 (Timeframe1): M10
- TF2 (Timeframe2): M30
- SMA Period: 275
- ADX Threshold: 12
- ADX Period: 16
- Sessions: London=OFF, NY=ON, Asian=ON
- ADX Mode: FlipDirection

Optimize:
- TF0 (Timeframe0): Test M2, M3, M4, M5, M6 (5 runs)

Target Metric:
- Positive R-multiple (>0)
- Profit Factor > 1.2
- Max DD < 20%
```

**Backtest Periods** (run each TF0 on each period):
1. Jan-Mar 2026 (you already have baseline)
2. Apr-Jun 2025
3. Jul-Sep 2025
4. Oct-Dec 2025 (if available)

**Total Runs**: 5 TF0 values × 4 periods = 20 backtests

### Phase 2: SMA + ADX Optimization

**After** finding best TF0 from Phase 1:

```
Fixed:
- TF0: [Best from Phase 1]
- TF1: M10
- TF2: M30
- Sessions: London=OFF, NY=ON, Asian=ON

Optimize:
- SMA Period: 200, 225, 250, 275, 300 (5 values)
- ADX Threshold: 10, 12, 14, 16, 18 (5 values)
- ADX Period: 9, 12, 16, 21 (4 values)

Total combinations: 5 × 5 × 4 = 100 runs
```

**Use WFO analyzer** after each period to validate.

### Phase 3: TF1 + TF2 Fine-tuning (Optional)

Only if Phase 1 + 2 results are promising.

---

## 📋 Step-by-Step Execution Plan

### Week 1: Organization & Phase 1 Setup

**Day 1-2**: Archive & organize
- [ ] Run version check on all existing backtests
- [ ] Move v4.5.x backtests to archive
- [ ] Keep v4.6.0 baselines organized
- [ ] Delete only obvious junk (0-byte files, etc.)

**Day 3-4**: Phase 1 TF0 optimization setup
- [ ] Create optimization set: `v4.6.0_Phase1_TF0_Optimization.optset`
- [ ] Configure TF0: M2, M3, M4, M5, M6
- [ ] Set fixed parameters (TF1=M10, TF2=M30, SMA=275, ADX=12)

**Day 5-7**: Run Phase 1 backtests
- [ ] Run on Jan-Mar 2026
- [ ] Run on Apr-Jun 2025
- [ ] Run on Jul-Sep 2025
- [ ] Run on Oct-Dec 2025 (if available)
- [ ] Save results to `v4.6.0_optimization/Phase1_TF0/`

### Week 2: Phase 1 Analysis

**Day 8-9**: Analyze Phase 1 results
- [ ] Run WFO analyzer on each TF0 result
- [ ] Compare Total R across TF0 values
- [ ] Check for consistency across periods
- [ ] Identify best TF0 (positive R, PF > 1.2)

**Day 10-11**: Validate best TF0
- [ ] Run out-of-sample test on NEW period (not used in optimization)
- [ ] Confirm best TF0 maintains performance
- [ ] Document findings

**Day 12-14**: Phase 2 setup
- [ ] Create Phase 2 optimization set
- [ ] Use best TF0 from Phase 1
- [ ] Configure SMA + ADX parameter grid

### Week 3-4: Phase 2 Optimization

(Only proceed if Phase 1 found profitable TF0)

---

## 🎯 Success Criteria

### Phase 1 Success
- ✅ At least ONE TF0 with positive R across 3+ periods
- ✅ Profit Factor > 1.2
- ✅ Max DD < 20%
- ✅ Win rate 25-45%

### If Phase 1 Fails
- All TF0 values negative R across all periods
- **STOP** - System may not be viable for current market conditions
- Consider:
  - Different currency pairs (USDJPY, GBPUSD)
  - Different timeframe ranges (try M1-M5 instead of M2-M6)
  - Fundamental system redesign

---

## 💾 Data Retention Policy

### Keep Forever
- ✅ v4.6.0 baseline backtests
- ✅ Failed experiments (learning material)
- ✅ Final optimization results

### Archive After 6 Months
- Old version backtests (v4.5.x and earlier)
- Intermediate optimization runs

### Delete After Review
- Corrupt/partial CSVs
- Test runs (<30 trades)
- Obvious duplicates

---

## 📝 Summary

**DON'T DELETE EVERYTHING!** Instead:

1. ✅ **Archive old data** - It's valuable for learning and comparison
2. ✅ **Organize by version** - Separate v4.6.0 from v4.5.x
3. ✅ **Keep baselines** - Jan-Mar 2026 (ADX=12) is your reference point
4. ✅ **Keep failures** - Jan-Mar 2026 (ADX=23) shows what NOT to do
5. ✅ **Start fresh optimization** - Phase 1: TF0 (M2-M6)

**Next Action**: Review existing backtests with version check, then start Phase 1 TF0 optimization.

---

**Status**: Ready to execute
**Estimated Time**: 2-4 weeks for complete Phase 1-2 optimization
**Priority**: Phase 1 TF0 optimization (find entry trigger that works)

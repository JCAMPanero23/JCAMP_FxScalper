# Entry Optimization Guide - JCAMP FxScalper

**Date:** 2025-03-15
**Purpose:** Optimize entry quality without code changes

---

## 📁 Preset Files Overview

Three pre-configured parameter sets are provided for different trading styles:

### 1. `entry_quality_high.cbotset` - CONSERVATIVE
**Goal:** Maximum win rate, fewer trades
**Best for:** Low drawdown, high accuracy

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Min Swing Score | 0.70 | Only best swing points |
| Min FVG Size | 2.0 pips | Only significant gaps |
| Min RR Ratio | 3.5 | Higher reward requirement |
| Max Dynamic RR | 4.5 | Cap extended TPs |
| ATR Multiplier | 1.75 | Strong impulse required |
| Min PRE-Zone Score | 0.55 | Higher quality zones |
| Max Distance to Arm | 8 pips | Tighter entry |
| Session Weight | 0.30 | Session timing important |

**Expected:** 30-40% fewer trades, 5-10% higher win rate

---

### 2. `entry_balanced.cbotset` - MODERATE (RECOMMENDED START)
**Goal:** Balance between quality and frequency
**Best for:** General use, baseline testing

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Min Swing Score | 0.65 | Moderate filtering |
| Min FVG Size | 1.5 pips | Standard gap size |
| Min RR Ratio | 3.0 | Standard risk/reward |
| Max Dynamic RR | 5.0 | Full dynamic range |
| ATR Multiplier | 1.5 | Moderate impulse |
| Min PRE-Zone Score | 0.50 | Standard threshold |
| Max Distance to Arm | 10 pips | Moderate flexibility |
| Session Weight | 0.25 | Balanced importance |

**Expected:** Balanced trade frequency and quality

---

### 3. `entry_frequency_high.cbotset` - AGGRESSIVE
**Goal:** Maximum trades, lower filtering
**Best for:** High volume testing, active trading

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Min Swing Score | 0.60 | Lower threshold |
| Min FVG Size | 1.0 pips | Smaller gaps accepted |
| Min RR Ratio | 2.5 | Lower reward requirement |
| Max Dynamic RR | 5.0 | Full dynamic range |
| ATR Multiplier | 1.25 | Easier impulse threshold |
| Min PRE-Zone Score | 0.45 | Lower zone quality |
| Max Distance to Arm | 12 pips | More flexible entry |
| Session Weight | 0.20 | Less time-dependent |

**Expected:** 40-60% more trades, potentially lower win rate

---

## 🎯 How to Use These Presets

### Step 1: Baseline Test
1. Load `entry_balanced.cbotset` in cAlgo
2. Run backtest on your standard period (e.g., 3 months)
3. Record metrics:
   - Total trades
   - Win rate %
   - Average RR
   - Max drawdown
   - Profit factor

### Step 2: Compare Extremes
4. Test `entry_quality_high.cbotset`
5. Test `entry_frequency_high.cbotset`
6. Compare all three results

### Step 3: Fine-Tune Winner
7. Take the best-performing preset
8. Use the optimization matrix below for fine-tuning

---

## 🔬 Manual Optimization Matrix

Use cAlgo's built-in optimizer with these parameter ranges:

### Priority 1: Core Quality Filters (Optimize First)

| Parameter | Start | Range | Step | Goal |
|-----------|-------|-------|------|------|
| **Min Swing Score** | 0.65 | 0.60 → 0.75 | 0.05 | Higher = fewer/better trades |
| **Min FVG Size** | 1.5 | 1.0 → 2.0 | 0.5 | Higher = cleaner gaps |
| **Min RR Ratio** | 3.0 | 2.5 → 4.0 | 0.5 | Higher = better reward |

**Test combinations:** 3 × 3 × 4 = **36 combinations**

---

### Priority 2: Impulse Strength (Optimize Second)

| Parameter | Start | Range | Step | Goal |
|-----------|-------|-------|------|------|
| **ATR Multiplier** | 1.5 | 1.25 → 2.0 | 0.25 | Higher = stronger moves only |
| **Min PRE-Zone Score** | 0.50 | 0.45 → 0.60 | 0.05 | Higher = better zones |

**Test combinations:** 4 × 4 = **16 combinations**

---

### Priority 3: Fine Tuning (Optimize Third)

| Parameter | Start | Range | Step | Goal |
|-----------|-------|-------|------|------|
| **Max Distance to Arm** | 10 | 8 → 12 | 2 | Lower = tighter entry |
| **Session Weight** | 0.25 | 0.20 → 0.35 | 0.05 | Higher = session more important |
| **Max Dynamic RR** | 5.0 | 4.0 → 5.0 | 0.5 | Cap TP extension |

---

## 📊 Optimization Workflow

### Phase 1: Baseline (1 test)
```
Load: entry_balanced.cbotset
Period: Last 3 months
Record: All metrics
```

### Phase 2: Preset Comparison (3 tests)
```
Test all 3 presets
Compare: Win rate, trades, profit factor
Choose: Best performer
```

### Phase 3: Priority 1 Optimization (36 tests)
```
Optimize: MinSwingScore, MinFVGSize, MinRRRatio
Keep: Best 3 combinations
```

### Phase 4: Priority 2 Optimization (16 tests per winner)
```
For each top 3 from Phase 3:
  Optimize: ATRMultiplier, MinPreZoneScore
Keep: Best overall
```

### Phase 5: Priority 3 Fine-Tuning (Optional)
```
Take winner from Phase 4
Fine-tune: MaxDistanceToArm, SessionWeight
Final validation: Walk-forward test
```

---

## ⚠️ Optimization Best Practices

### 1. Avoid Over-Optimization
- Don't chase 100% perfect backtest results
- Test on different time periods (walk-forward)
- Validate on out-of-sample data

### 2. Track the Right Metrics
**Primary:**
- Profit Factor (target: > 1.5)
- Win Rate % (target: > 60%)
- Max Drawdown (target: < 15%)

**Secondary:**
- Average RR (target: > 3.0)
- Total Trades (minimum: 30+ for statistical validity)
- Recovery Factor (Net Profit / Max DD)

### 3. Balance Trade-offs
| If you want... | Adjust... | Direction |
|----------------|-----------|-----------|
| Fewer trades | Min Swing Score | ↑ Increase |
| Better entries | Min FVG Size | ↑ Increase |
| Higher win rate | Min RR Ratio | ↑ Increase |
| More opportunities | ATR Multiplier | ↓ Decrease |
| Time-based filter | Session Weight | ↑ Increase |

---

## 🎓 Understanding the Parameters

### Min Swing Score (0.60 - 0.75)
- **What it does:** Filters swing point quality based on multiple factors
- **Higher:** Only trades near best swing extremes
- **Lower:** More swing points qualify
- **Impact:** HIGH - directly affects entry frequency

### Min FVG Size (1.0 - 2.0 pips)
- **What it does:** Minimum gap size to consider valid
- **Higher:** Only significant imbalance gaps
- **Lower:** Accepts smaller gaps (more noise)
- **Impact:** HIGH - affects entry quality

### ATR Multiplier (1.25 - 2.0)
- **What it does:** How strong displacement must be
- **Higher:** Only enters on explosive moves
- **Lower:** Accepts weaker impulses
- **Impact:** MEDIUM - affects setup confirmation

### Min PRE-Zone Score (0.45 - 0.60)
- **What it does:** Quality threshold for PRE-zones
- **Higher:** Stricter zone validation
- **Lower:** More zones qualify
- **Impact:** MEDIUM - affects zone quality

### Session Weight (0.20 - 0.35)
- **What it does:** How much session timing matters in scoring
- **Higher:** London/NY sessions heavily favored
- **Lower:** Time-of-day less important
- **Impact:** LOW-MEDIUM - time-based filtering

---

## 📈 Expected Results by Preset

### Conservative (Quality High)
```
Trades per month:     8-15
Win rate:            65-75%
Avg RR:              3.5-4.0
Max Drawdown:        8-12%
Profit Factor:       2.0-2.5+
```

### Balanced (Recommended)
```
Trades per month:    15-25
Win rate:            60-70%
Avg RR:              3.0-3.5
Max Drawdown:        10-15%
Profit Factor:       1.5-2.0
```

### Aggressive (Frequency High)
```
Trades per month:    25-40
Win rate:            55-65%
Avg RR:              2.5-3.0
Max Drawdown:        12-18%
Profit Factor:       1.3-1.8
```

---

## 🔄 Next Steps

1. **Test Presets:** Start with all 3 preset files
2. **Compare Results:** Use metrics table above
3. **Choose Direction:** Quality vs Frequency
4. **Optimize:** Follow Priority 1 → 2 → 3 workflow
5. **Validate:** Walk-forward test on new data
6. **Deploy:** Use best parameters for live/forward testing

---

## 📝 Notes

- All presets keep chandelier settings constant (5 pips increment, 0.5 retracement, 5 pips buffer)
- Visualizations are disabled for faster backtesting
- Risk per trade fixed at 1%
- Max positions = 1 (one trade at a time)

**Created:** 2025-03-15
**For:** Entry optimization without code changes
**Next Review:** After optimization results

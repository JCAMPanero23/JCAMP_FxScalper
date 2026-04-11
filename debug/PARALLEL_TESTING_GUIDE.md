# Parallel Testing Guide: v4.5.x vs v4.6.0

**Purpose**: Run identical backtests on both versions to confirm which performs better

---

## Quick Comparison Setup

### Option 1: Use Git Worktrees (Recommended)

Keep both versions accessible simultaneously without switching branches:

```bash
# Create worktree for v4.5.x
git worktree add ../JCAMP_FxScalper_v4.5.x v4.5.x

# Now you have:
# D:\JCAMP_FxScalper\ (master = v4.6.0)
# D:\JCAMP_FxScalper_v4.5.x\ (v4.5.x branch)
```

**Copy to cAlgo from each version**:

```bash
# Test v4.5.x
cp "D:\JCAMP_FxScalper_v4.5.x\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
# Run backtest → Save as "TradeLog_EURUSD_Jan_Mar_2026_v4.5.x.csv"

# Test v4.6.0
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
# Run backtest → Save as "TradeLog_EURUSD_Jan_Mar_2026_v4.6.0.csv"
```

### Option 2: Branch Switching

Switch branches and copy each time:

```bash
# Test v4.5.x
git checkout v4.5.x
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
# Run backtest → Save CSV

# Test v4.6.0
git checkout master
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
# Run backtest → Save CSV
```

---

## Test Matrix

Run these **exact same backtests** on both versions:

| Period | Symbol | Timeframe | Start Date | End Date | Parameters |
|--------|--------|-----------|------------|----------|------------|
| Jan-Mar 2026 | EURUSD | M1 | 2026-01-01 | 2026-03-31 | Use defaults or matched set |
| Apr-Jun 2025 | EURUSD | M1 | 2025-04-01 | 2025-06-30 | Same parameters |
| Nov 2025 | EURUSD | M1 | 2025-11-01 | 2025-11-30 | Same parameters |
| Dec 2025 | EURUSD | M1 | 2025-12-01 | 2025-12-31 | Same parameters |
| Feb 2026 | EURUSD | M1 | 2026-02-01 | 2026-02-28 | Same parameters |

**Critical**: Use **identical parameters** (or default for each version)

---

## Parameter Mapping

v4.5.x and v4.6.0 have different parameter names:

| Concept | v4.5.x Name | v4.6.0 Name | Suggested Value |
|---------|-------------|-------------|-----------------|
| Entry TF | Timeframe2 | Timeframe0 | M4 (both) |
| Medium TF | Timeframe3 | Timeframe1 | M15 → M10 |
| Higher TF | N/A | Timeframe2 | M30 |
| SMA Period | MTFSMAPeriod | MTFSMAPeriod | 275 |
| ADX Period | ADXPeriod | ADXPeriod | 16 |
| ADX Threshold | ADXMinThreshold | ADXMinThreshold | 12 |
| Min RR | MinimumRRRatio | MinimumRRRatio | 4.0 |

### Matched Parameter Sets

**v4.5.x**:
```
MTFSMAPeriod: 275
Timeframe2: m4
Timeframe3: m15
RequireAllTFsAligned: true
ADXMode: FlipDirection
ADXPeriod: 16
ADXMinThreshold: 12
MinimumRRRatio: 4.0
EnableLondonSession: true  (or false for WFO)
```

**v4.6.0**:
```
MTFSMAPeriod: 275
Timeframe0: m4
Timeframe1: m10  (closest to v4.5.x's m15)
Timeframe2: m30  (higher tier)
RequireAllTFsAligned: true
ADXMode: FlipDirection
ADXPeriod: 16
ADXMinThreshold: 12
MinimumRRRatio: 4.0
EnableLondonSession: true  (or false for WFO)
```

---

## Results Recording

Create comparison table:

```markdown
# v4.5.x vs v4.6.0 Backtest Results

| Period | Version | Total R | Win Rate | Profit Factor | Max DD | Trades |
|--------|---------|---------|----------|---------------|--------|--------|
| Jan-Mar 2026 | v4.5.x | ? | ? | ? | ? | ? |
| Jan-Mar 2026 | v4.6.0 | -X.X | ? | ? | ? | ? |
| Apr-Jun 2025 | v4.5.x | ? | ? | ? | ? | ? |
| Apr-Jun 2025 | v4.6.0 | -X.X | ? | ? | ? | ? |
| Nov 2025 | v4.5.x | ? | ? | ? | ? | ? |
| Nov 2025 | v4.6.0 | ? | ? | ? | ? | ? |
```

**Decision Criteria**:
- If v4.5.x wins 4+ out of 5 periods → v4.5.x is superior
- If v4.6.0 wins 4+ out of 5 periods → Re-evaluate v4.6.0
- If tied → Look at total R across all periods

---

## WFO Analysis on Both Versions

After determining the winner, run WFO optimization:

### v4.5.x WFO
```bash
git checkout v4.5.x

# Run backtest with WFO CSV export enabled
# Import to WFO browser
# Analyze recommendations
```

### v4.6.0 WFO (if it proves viable)
```bash
git checkout master

# Run backtest with WFO CSV export enabled
# Import to WFO browser (already updated for v4.6.0)
# Analyze recommendations
```

---

## What To Look For

### If v4.5.x Wins:
- **Total R**: Should be consistently positive
- **Win Rate**: 25-45% (high RR strategy)
- **Profit Factor**: > 1.3
- **Trades**: Reasonable sample size (30+)

### If v4.6.0 Wins:
- Question initial negative results
- Check if parameter mapping was correct
- Verify same periods were tested
- Re-run validations to confirm

### If Both Are Negative:
- Market regime changed
- Parameters need optimization
- Strategy may need fundamental changes

---

## Expected Outcome (Based on Current Data)

**Hypothesis**: v4.5.x will outperform v4.6.0 across all test periods

**Rationale**:
1. User reported negative R on v4.6.0 validations
2. v4.5.x historically showed positive R
3. v4.6.0's 4TF alignment may be too restrictive
4. TF0 entry delay may miss optimal entries

**If hypothesis confirmed**:
- Continue development on v4.5.x branch
- Archive v4.6.0 as experimental
- Apply WFO optimization to v4.5.x
- Document lessons learned from v4.6.0

---

## Next Actions After Testing

### If v4.5.x Confirmed Superior:

1. **Make v4.5.x the default branch**:
   ```bash
   git checkout v4.5.x
   git checkout -b main  # New default branch
   git push origin main
   ```

2. **Update all documentation** to reference v4.5.x

3. **Run WFO optimization** on v4.5.x with focus on:
   - Timeframe2 (M2-M6 range)
   - SMA Period (200-300)
   - ADX settings (Period 7-21, Threshold 10-18)

4. **Archive v4.6.0** with findings for future reference

### If Results Are Mixed:

1. Analyze why some periods favor v4.6.0
2. Identify market conditions where each excels
3. Consider hybrid approach or conditional logic

### If v4.6.0 Surprisingly Wins:

1. Re-examine initial negative results
2. Check if WFO recommendations were applied correctly
3. Test more periods to confirm
4. Investigate what changed

---

## File Naming Convention

Save backtests with clear version tags:

```
TradeLog_EURUSD_Jan_Mar_2026_v4.5.x.csv
TradeLog_EURUSD_Jan_Mar_2026_v4.6.0.csv
TradeLog_EURUSD_Apr_Jun_2025_v4.5.x.csv
TradeLog_EURUSD_Apr_Jun_2025_v4.6.0.csv
```

Organize in directories:

```
Backtest/
├─ v4.5.x/
│  ├─ TradeLog_EURUSD_Jan_Mar_2026.csv
│  ├─ TradeLog_EURUSD_Apr_Jun_2025.csv
│  └─ ...
└─ v4.6.0/
   ├─ TradeLog_EURUSD_Jan_Mar_2026.csv
   ├─ TradeLog_EURUSD_Apr_Jun_2025.csv
   └─ ...
```

---

**Last Updated**: 2026-04-12
**Status**: Parallel testing enabled, awaiting comprehensive comparison

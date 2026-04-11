# Re-Optimization Guide - JCAMP FxScalper v4.4.0-WFO

**Last Updated:** 2026-04-04
**Version:** v4.4.0-WFO (MTF SMA Alignment + ADX FlipDirection)

---

## When to Re-Optimize

Re-optimization is triggered when:
1. **Monthly DD Limit Hit** (10%) - Bot stops trading, re-optimize immediately
2. **Consecutive Loss Limit** (9 losses) - Strategy degradation detected
3. **Quarterly Review** - Scheduled parameter refresh

---

## Quick Start: Re-Optimization Workflow

### Step 1: Load Optimization File
```
File: Jcamp_1M_scalping, EURUSD m1.optset
Location: C:\Users\Jcamp_Laptop\Downloads\ (or optimization_sets folder)
```

### Step 2: Set Backtest Period
- **Optimization Period:** Last 3 months of data
- **Walk-Forward Validation:** Next 1 month (out-of-sample)

### Step 3: Run Optimization
1. Open cTrader → Automate → Your Bot → Optimize
2. Import the `.optset` file
3. Start optimization (genetic algorithm recommended)

---

## Optimization Priority Order

### Priority 1: Core Parameters (Monthly)

| Parameter | Current | Range | Step | Impact |
|-----------|---------|-------|------|--------|
| **ADX Min Threshold** | 23 | 15-35 | 5 | HIGH - Entry filtering |
| **ADX Period** | 16 | 7-28 | 1 | HIGH - Trend detection |
| **MTF SMA Period** | 250 | 50-300 | 25 | HIGH - Trend alignment |

### Priority 2: Timeframe Alignment (Quarterly)

| Parameter | Current | Options | Impact |
|-----------|---------|---------|--------|
| **Timeframe 2** | m4 | m2, m3, m4, m5 | MEDIUM - MTF confirmation |
| **Timeframe 3** | m15 | m6, m10, m15, m20, m30 | MEDIUM - Higher TF filter |

### Priority 3: Entry Quality (Quarterly)

| Parameter | Current | Range | Step | Impact |
|-----------|---------|-------|------|--------|
| **ATR Period** | 16 | 10-20 | 2 | LOW - SL sizing |
| **SL ATR Multiplier** | 2.0 | 1.0-2.5 | 0.25 | MEDIUM - SL distance |
| **Minimum RR Ratio** | 4.0 | 2.0-5.0 | 0.5 | HIGH - Trade quality |

---

## Target Metrics (Fitness Criteria)

| Priority | Metric | Target | Warning |
|----------|--------|--------|---------|
| 1st | **Profit Factor** | 1.3 - 2.0 | > 3.0 = overfitting |
| 2nd | **Max Drawdown** | < 20% | > 25% = reject |
| 3rd | **Net Profit** | > 0 | Negative = reject |
| 4th | **Win Rate** | 25-45% | > 60% = overfitting |
| 5th | **Trade Count** | 30+ | < 30 = insufficient data |

### Overfitting Red Flags
- Profit Factor > 3.0
- Win Rate > 60%
- Less than 30 trades
- 1-2 trades account for most profit
- Perfect equity curve (no drawdowns)

---

## Current Optimization Ranges (.optset)

Based on `Jcamp_1M_scalping, EURUSD m1.optset`:

### Enabled for Optimization

| Parameter | Min | Max | Step |
|-----------|-----|-----|------|
| MTFSMAPeriod | 50 | 300 | 25 |
| Timeframe2 | m2 | m5 | enum |
| Timeframe3 | m6 | m30 | enum |
| ATRPeriod | 10 | 20 | 2 |
| SLATRMultiplier | 1.0 | 2.5 | 0.25 |
| ADXPeriod | 7 | 28 | 1 |
| ADXMinThreshold | 15 | 35 | 5 |
| ADXMaxThreshold | 40 | 50 | 5 |

### Fixed During Optimization

| Parameter | Value | Reason |
|-----------|-------|--------|
| EnableTrading | true | Required for trades |
| RiskPercent | 1.0 | Consistent position sizing |
| MinimumRRRatio | 4.0 | Quality filter |
| EnableSessionFilter | true | Time-based filtering |
| EnableChandelierSL | true | Trailing SL system |
| EnableDailyLossLimit | **false** | Disable for optimization |
| EnableConsecutiveLossLimit | **false** | Disable for optimization |
| EnableMonthlyDrawdownLimit | true | Keep for realistic DD |

---

## Session Filter Combinations

Test these session combinations separately:

| Preset | London | NY | Asian | Best For |
|--------|--------|-----|-------|----------|
| All Sessions | true | true | true | Maximum trades |
| London+NY | true | true | false | High volatility |
| NY Only | false | true | false | US session focus |
| London Only | true | false | false | EU session focus |

---

## ADX Mode Strategy

| Mode | When to Use |
|------|-------------|
| **FlipDirection** (1) | Current default - contrarian in ranging markets |
| **BlockEntry** (0) | Conservative - skip trades when ADX < threshold |

**FlipDirection Logic:**
- ADX < Threshold → Reverse signal direction (contrarian)
- ADX >= Threshold → Follow signal direction (trend)

---

## Walk-Forward Optimization (WFO)

### Recommended WFO Schedule

| Optimization Period | Validation Period | Purpose |
|--------------------|-------------------|---------|
| 3 months | 1 month | Standard WFO |
| 6 months | 2 months | Stable parameters |

### WFO Process
1. Optimize on Period 1 (e.g., Jan-Mar)
2. Validate on Period 2 (e.g., Apr)
3. If validation fails (DD > 15%), re-optimize
4. If validation passes, use for next period

---

## Quick Reference: Parameter Impact

### Increase for Fewer/Better Trades
- ADX Min Threshold ↑
- MTF SMA Period ↑
- Minimum RR Ratio ↑
- SL ATR Multiplier ↑

### Decrease for More Trades
- ADX Min Threshold ↓
- Minimum RR Ratio ↓

### Session Timing
- Enable fewer sessions = fewer but higher-quality trades
- NY Only typically has best volatility for scalping

---

## Files Reference

| File | Purpose | Location |
|------|---------|----------|
| `Jcamp_1M_scalping, EURUSD m1.optset` | Optimization ranges | Downloads or optimization_sets |
| `*.cbotset` | Runtime settings | optimization_sets folder |
| `TradeLog_*.csv` | Trade history for analysis | Documents/cAlgo/Trade_Logs |

---

## Troubleshooting

### Import Issues (Cloud vs Local)
If `.cbotset` won't import, check for missing parameters:
- `ClosePositionsOnMonthlyDD` (added v4.4.0)
- `EnableCSVExport` (added v4.4.0)

Add missing parameters manually or update Cloud instance to latest version.

### Optimization Takes Too Long
1. Reduce parameter ranges
2. Use genetic algorithm instead of grid search
3. Reduce backtest period to 2 months
4. Disable unused parameters from optimization

---

**Remember:** Re-optimization is about finding robust parameters, not perfect backtest results. Prioritize consistency over peak performance.

# WFO Quick Reference Card

## 🚀 Quick Start (3 Steps)

### 1. Add Logging to cBot
```csharp
// Follow TradeLogger_Addition.cs instructions
// Key changes: Add fields, OnStart init, log entry/exit
```

### 2. Run Backtest
```
cAlgo → Backtest → Set date range → Start
Log file: C:\Users\Jcamp_Laptop\Documents\cAlgo\Trade_Logs\TradeLog_*.csv
```

### 3. Analyze & Get Recommendations
```bash
cd D:\JCAMP_FxScalper
python wfo_analyzer.py "path\to\TradeLog.csv"
```

**Output**: Optimized settings in `wfo_results/` folder

---

## 📊 What Gets Analyzed

| Dimension | What It Shows |
|-----------|---------------|
| **Session** | London vs NY vs Asian performance |
| **Hourly** | Which hours of day are profitable |
| **Direction** | BUY vs SELL win rates and R |
| **ADX Mode** | BlockEntry vs FlipDirection effectiveness |
| **ADX Threshold** | Optimal ADX value ranges |
| **Flip Direction** | Performance when ADX flips direction |
| **Day of Week** | Monday-Friday patterns |

---

## ✅ WFO Cycle Checklist

### Training Phase (3 months)
- [ ] Set date range (e.g., Jan-Mar)
- [ ] Run backtest with current params
- [ ] Verify trade log created
- [ ] Run Python analyzer
- [ ] Review recommendations

### Testing Phase (1 month)
- [ ] Set date range (e.g., Apr)
- [ ] Apply recommended params from training
- [ ] Run backtest (DON'T change params!)
- [ ] Calculate degradation
- [ ] Decide: Deploy or re-optimize

### Validation
- [ ] Training Profit Factor: _____
- [ ] Test Profit Factor: _____
- [ ] Degradation: _____ % (target: < 30%)
- [ ] Min trades: Training ≥30, Test ≥15
- [ ] Overfitting check: PF < 3.0, WR < 60%

---

## 🎯 Decision Matrix

### Session Selection

| Condition | Decision |
|-----------|----------|
| London +R, NY +R | Enable both |
| London +R, NY -R | London only |
| London -R, NY +R | NY only |
| London -R, NY -R | Review entry logic |

### ADX Mode Selection

| Condition | Decision |
|-----------|----------|
| FlipDirection Total R > BlockEntry | Use FlipDirection |
| FlipDirection trades < 20 | Insufficient data, use BlockEntry |
| Both negative | ADX filter ineffective, consider disabling |

### ADX Threshold Selection

| Best Range | Recommended Threshold |
|------------|----------------------|
| <15 | 12-14 |
| 15-18 | 16-17 |
| 18-20 | 19 |
| 20-22 | 21 |
| 22-25 | 23-24 |
| >25 | 26-28 |

---

## ⚠️ Red Flags

| Issue | Cause | Solution |
|-------|-------|----------|
| PF > 3.0 in training | Overfitting | Reject, use simpler settings |
| Win Rate > 60% | Curve fitting | Reject, extend date range |
| Training PF 2x Test PF | Look-ahead bias | Reject parameters |
| < 30 trades in training | Sample too small | Extend training period |
| Test R negative | Regime change | Re-optimize or pause trading |

---

## 📈 Key Metrics Target Ranges

| Metric | Realistic Range | Notes |
|--------|----------------|-------|
| Profit Factor | 1.3 - 2.0 | Higher = suspicious |
| Win Rate | 25% - 45% | Low WR OK with 5.0 RR |
| Degradation | 70% - 100% | Test PF / Training PF |
| Trades/Month | 20 - 50 | Depends on session filter |
| Avg Trade Duration | 30 - 90 min | M1 scalping typical |

---

## 🔧 Common Commands

### Install Python Dependencies
```bash
pip install -r requirements.txt
```

### Run Analyzer
```bash
python wfo_analyzer.py "C:\Path\To\TradeLog.csv"
```

### Find Latest Log File
```bash
dir "C:\Users\Jcamp_Laptop\Documents\cAlgo\Trade_Logs" /O-D /B
```

### Copy cBot to cAlgo
```bash
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
```

---

## 🎓 Interpretation Guide

### Session Analysis Output
```
London (08:00-12:00 UTC): 65 trades | 48.2% WR | +42.5R | PF 1.85
NY Overlap (13:00-17:00): 43 trades | 32.6% WR | -8.3R | PF 0.87
```

**Interpretation**: London is profitable (positive R, PF > 1.0), NY is not. Disable NY session.

### Hourly Analysis Output
```
Hour  Trades  Win Rate  Total R
08    18      55.6%     +12.5R
09    22      45.5%     +10.2R
10    15      40.0%     +8.3R
```

**Interpretation**: Hours 8-10 UTC are best. Consider time-based filter.

### Direction Analysis Output
```
BUY: 58 trades | 42.5% WR | +28.3R | PF 1.68
SELL: 50 trades | 38.0% WR | +14.2R | PF 1.42
```

**Interpretation**: Both profitable, BUY slightly better. No need to disable either.

### ADX Analysis Output
```
BlockEntry: 45 trades | 38.9% WR | +18.5R | PF 1.52
FlipDirection: 63 trades | 46.0% WR | +24.0R | PF 1.71

Flip Direction Trades: 28 | 57.1% WR | +18.8R | PF 2.15
```

**Interpretation**: FlipDirection mode superior. Flipped trades have high win rate = effective contrarian.

---

## 💡 Pro Tips

### Tip 1: Session-Specific Optimization
Run separate analyses for London and NY sessions, optimize ADX independently.

### Tip 2: Combine WFO with Risk Limits
- Daily loss limit (-3R): Emergency brake
- Consecutive loss limit (9): Triggers re-optimization
- Monthly DD limit (10%): Forces parameter review

### Tip 3: Track Parameter Stability
If WFO recommends ADX=18 in Cycle 1, 19 in Cycle 2, 18 in Cycle 3 → Stable!
If it swings 14→25→16 → Unstable, use conservative middle ground.

### Tip 4: Out-of-Sample Is King
In-sample (training) can lie. Only out-of-sample (test) validates robustness.

### Tip 5: Less Is More
Fewer sessions + stricter filters = fewer trades but higher quality.

---

## 📁 File Locations Reference

| File | Location |
|------|----------|
| cBot Source | `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` |
| cAlgo cBot | `C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs` |
| Trade Logs | `C:\Users\Jcamp_Laptop\Documents\cAlgo\Trade_Logs\` |
| WFO Results | `D:\JCAMP_FxScalper\wfo_results\` |
| Python Analyzer | `D:\JCAMP_FxScalper\wfo_analyzer.py` |

---

## 📞 Troubleshooting Quick Fixes

| Problem | Solution |
|---------|----------|
| No CSV file created | Check `EnableTrading = true`, verify trades executed |
| Python not found | Install Python 3.9+, add to PATH |
| Module import error | `pip install pandas numpy matplotlib seaborn` |
| No trades in log | Check session filter, ADX filter blocking entries |
| Analyzer crashes | Check CSV has data, verify column headers match |

---

## 🎯 Realistic Expectations

**What WFO Will Do:**
- ✓ Find optimal sessions for your strategy
- ✓ Identify best times of day
- ✓ Optimize ADX settings per market regime
- ✓ Reduce drawdown by avoiding bad periods
- ✓ Increase parameter confidence

**What WFO Won't Do:**
- ✗ Guarantee profits (markets are unpredictable)
- ✗ Eliminate losing months
- ✗ Find "perfect" parameters (they don't exist)
- ✗ Work without sufficient data (min 30 trades)

---

**Remember**: WFO is about robustness, not perfection. Parameters that work across multiple out-of-sample periods are gold.

Good luck! 🚀

# JCAMP_FxScalper Testing Guide

## Table of Contents
1. [Pre-Test Checklist](#pre-test-checklist)
2. [Backtest Configuration](#backtest-configuration)
3. [Forward Test Setup](#forward-test-setup)
4. [Validation Criteria](#validation-criteria)
5. [Common Issues & Troubleshooting](#common-issues--troubleshooting)
6. [Results Analysis](#results-analysis)

---

## Pre-Test Checklist

### Installation Verification
- [ ] All include files (JC_*.mqh) copied to `[MT5 Data]/MQL5/Include/`
- [ ] Main EA (JCAMP_FxScalper_v1.mq5) in `[MT5 Data]/MQL5/Experts/`
- [ ] Preset file (.set) in `[MT5 Data]/MQL5/Presets/`
- [ ] EA compiled successfully (0 errors in MetaEditor)
- [ ] Terminal connected to FP Markets demo account (for forward test)

### Broker Verification (Critical for Accuracy)
- [ ] Broker: FP Markets Raw Account
- [ ] EURUSD spread typically 0.2-0.5 pips (verify in Market Watch)
- [ ] Commission: $3 per lot per side (check contract specifications)
- [ ] Server GMT offset documented (for session timing accuracy)
- [ ] Minimum lot: 0.01 (verify in symbol properties)

---

## Backtest Configuration

### Strategy Tester Settings

**Step 1: Open Strategy Tester**
- Press Ctrl+R in MT5 or View → Strategy Tester

**Step 2: Basic Settings**
```
Expert Advisor: JCAMP_FxScalper_v1.ex5
Symbol: EURUSD
Period: M5
Date: Custom Period (see recommended ranges below)
Execution: Every tick based on real ticks
Deposit: 500 USD
Leverage: 1:500
Optimization: Disabled (for initial validation)
```

**Step 3: Load Preset**
- Click "Load" in Strategy Tester inputs tab
- Select `JCAMP_FxScalper_EURUSD.set`
- Verify all inputs match expected values (see Input Parameters section)

**Step 4: Visual Mode (Optional)**
- Enable for first run to watch EA behavior
- Disable for faster full-period testing

### Recommended Test Periods

**Initial Validation (3 months):**
- Date range: 2025-10-01 to 2026-01-01
- Purpose: Quick validation of logic correctness
- Expected: 20-40 trades (London session only, conservative filtering)

**Full Validation (6 months):**
- Date range: 2025-07-01 to 2026-01-01
- Purpose: Include both trending and ranging markets
- Expected: 50-100 trades

**Extended Test (12 months):**
- Date range: 2025-01-01 to 2026-01-01
- Purpose: Validate across all market conditions (only after initial validation passes)
- Expected: 100-200 trades

### Modeling Quality
- **Required:** Every tick based on real ticks (highest accuracy)
- **Avoid:** 1-minute OHLC (insufficient for M5 scalping)
- **Check:** After test completes, verify "Modeling Quality" = 90%+ in report

---

## Input Parameters Reference

### Default Values (EURUSD.set Preset)

**Risk Management:**
```
RiskPercent = 1.0          // Conservative start (1% per trade)
MaxDailyLoss = 3.0         // Stop trading if -3% daily loss
MaxGlobalPositions = 1     // Phase 1: Single EURUSD position
```

**Session Settings:**
```
TradeLondon = true         // 08:00-16:30 GMT (highest liquidity)
TradeNewYork = false       // Disabled by default (test separately)
TradeAsian = false         // Disabled by default
TradeTokyo = false         // Disabled by default
```

**Indicators:**
```
SMA1_Period = 21           // Fast trend
SMA2_Period = 50           // Medium trend
SMA3_Period = 200          // Slow baseline
RSI_Period = 14            // Momentum
ATR_Period = 14            // Volatility
ATR_Multiplier = 1.5       // EURUSD optimized
```

**Filters:**
```
MaxSpread = 1.0            // Max 1.0 pips spread
LevelProximity = 5         // Price within 5 pips of H1 level
EnableTPValidation = true  // Abort if TP crosses H1 levels
EnableSLSnapping = true    // Snap SL to nearby H1 structure
```

### Test Variations

**Test 1: Conservative (Default)**
- RiskPercent = 1.0
- EnableTPValidation = true
- EnableSLSnapping = true
- TradeLondon = true only

**Test 2: Aggressive Risk**
- RiskPercent = 2.0 (all else default)
- Purpose: Test wider SL acceptance, larger positions

**Test 3: No TP Validation**
- EnableTPValidation = false
- Purpose: Measure impact of structural level filtering
- Compare: Trade frequency, Win%, Profit Factor vs Test 1

**Test 4: No SL Snapping**
- EnableSLSnapping = false
- Purpose: Pure ATR-based SL (no structural adjustment)
- Compare: Average SL distance, Win% vs Test 1

**Test 5: Multi-Session**
- TradeLondon = true
- TradeNewYork = true
- Purpose: Extended trading window (08:00-21:00 GMT)
- Warning: May include lower-liquidity overlap periods

---

## Forward Test Setup

### Demo Account Configuration

**Step 1: Open FP Markets Demo Account**
1. Register at FP Markets website
2. Select: MT5 → Raw Account (ECN pricing)
3. Deposit: $500 (matches backtest)
4. Leverage: 1:500

**Step 2: Verify Broker Settings**
```
Account Type: Raw (commission-based, not markup)
EURUSD Commission: $3 per lot per side
Typical Spread: 0.2-0.5 pips (check Market Watch)
Server GMT Offset: Document in testing log
```

**Step 3: Load EA on Chart**
1. Open EURUSD M5 chart
2. Drag JCAMP_FxScalper_v1 from Navigator
3. Load preset: JCAMP_FxScalper_EURUSD.set
4. Enable AutoTrading checkbox
5. Enable "Allow live trading" in inputs
6. Verify "Allow DLL imports" is checked (if using external libraries)

**Step 4: Monitoring Setup**
- Enable Experts tab logging (Tools → Options → Expert Advisors → Enable Journal)
- Take screenshot of input parameters
- Document start date/time and account balance

### Forward Test Duration
**Minimum:** 2 weeks (40-60 trading sessions)
**Recommended:** 4 weeks (80-120 sessions)
**Purpose:** Validate slippage, partial executions, real-time session timing

### Daily Monitoring Checklist
- [ ] Check Experts tab for errors or warnings
- [ ] Verify trades executed during expected sessions
- [ ] Review partial profit executions (if any positions reached 2:1 R:R)
- [ ] Monitor spread rejections (should be rare with FP Markets)
- [ ] Check TP validation rejections (log review)
- [ ] Verify SL adjustments due to snapping (log review)

---

## Validation Criteria

### Backtest Success Metrics

**Profitability:**
- ✅ Net Profit: Positive over 6-month period
- ✅ Profit Factor: ≥1.5 (for every $1 risked, earn $1.50+)
- ✅ Expected Payoff: Positive (average profit per trade)

**Risk Management:**
- ✅ Max Drawdown: <10% of initial deposit ($50 on $500)
- ✅ Max Consecutive Losses: <6 trades
- ✅ Recovery Factor: ≥2.0 (Net Profit / Max DD)

**Win Rate:**
- ✅ Win %: 40-50% (trend-following characteristic)
- ⚠️ Warning: >70% suggests over-optimization (curve fitting)
- ⚠️ Concern: <35% indicates excessive filtering or bad entry logic

**Trade Characteristics:**
- ✅ Total Trades: 50-100 (6-month period, London only)
- ✅ Average Trade Duration: <4 hours (validates scalping approach)
- ✅ Average Win / Average Loss: ≥2.0 (confirms 2:1 R:R target)

### Forward Test Comparison

**Acceptable Deviation:**
- Profit Factor: Within 20% of backtest (e.g., BT=1.6 → FT=1.28-1.92 acceptable)
- Win Rate: Within 10% of backtest (e.g., BT=45% → FT=35-55% acceptable)
- Max DD: Within 5% of backtest (e.g., BT=8% → FT≤13% acceptable)

**Red Flags (Do NOT proceed to live):**
- Forward test Win% <30%
- Forward test Profit Factor <1.2
- Forward test Max DD >15%
- Frequent partial profit execution failures
- Session timing misalignment (trades outside expected hours)

---

## Common Issues & Troubleshooting

### Issue 1: No Trades Executed

**Possible Causes:**
1. **Spread too wide**
   - Check: Market Watch → EURUSD spread
   - Solution: Increase MaxSpread input if FP Markets typically >1.0 pips
   - Expected: Rare with ECN account (0.2-0.5 typical)

2. **No active session**
   - Check: Experts tab for "[Utils] No active session" messages
   - Verify: Broker server time vs GMT offset
   - Solution: Adjust session times or enable correct sessions

3. **Price not near H1 level**
   - Check: Experts tab for "Price not within proximity of H1 level" rejections
   - Review: Open H1 chart, verify fractal levels being detected
   - Solution: Increase LevelProximity input (try 10 pips) if levels too strict

4. **TP validation rejecting all trades**
   - Check: Count of "TP crosses resistance/support - trade aborted" in logs
   - Review: If >80% of signals rejected, disable EnableTPValidation
   - Solution: Test with validation OFF to isolate issue

5. **Trend filters too strict**
   - Check: SMA alignment messages in logs
   - Review: EURUSD may be ranging (no 21>50>200 alignment)
   - Solution: Test on different date range with clearer trends

### Issue 2: Compilation Errors

**Error: "Cannot open include file JC_Utils.mqh"**
- Cause: Include file not in correct folder
- Solution: Copy all JC_*.mqh to `[MT5 Data]/MQL5/Include/`

**Error: "Undeclared identifier"**
- Cause: Function called before definition or missing include
- Solution: Check #include order in main EA file

**Error: "Invalid array access"**
- Cause: Indicator buffer not properly copied
- Solution: Verify CopyBuffer() return value >0 before accessing array

### Issue 3: Partial Profits Not Executing

**Symptom:** Position closed 100% at 2:1 R:R instead of 80/20 split

**Possible Causes:**
1. **Initial lot size <0.05**
   - Check: Experts tab for "Position too small for partials - full exit at TP"
   - Cause: $500 with 1% risk + wide SL = 0.01-0.04 lot position
   - Solution: Increase RiskPercent to 2.0% or accept full exits on small positions

2. **Broker rejects partial close**
   - Check: Experts tab for error code (e.g., "Modify rejected - error 10015")
   - Cause: Some brokers don't allow position splits
   - Solution: Contact FP Markets support (should support partials on Raw account)

3. **Backtest limitation**
   - Check: Only occurs in Strategy Tester, not live demo
   - Cause: MT5 backtester may not model partials accurately
   - Solution: Validate via forward test only

### Issue 4: Unexpected Stop Outs

**Symptom:** SL hit more frequently than expected (Win% <30%)

**Possible Causes:**
1. **SL too tight (snapping issue)**
   - Check: Review SL distances in logs (Entry - SL in pips)
   - Compare: Snapped SL vs calculated ATR SL
   - Solution: Disable EnableSLSnapping to test pure ATR-based SL

2. **ATR multiplier too low**
   - Check: EURUSD ATR(14) on M5 - if >0.0010 during test period, 1.5x may be tight
   - Solution: Test with ATR_Multiplier = 2.0 (wider stops)

3. **Slippage (forward test only)**
   - Check: Entry price in log vs actual executed price
   - Cause: Market order slippage during volatile periods
   - Solution: Expected on live/demo - factor into expectations (2-3 pips slippage normal)

### Issue 5: Session Timing Misalignment

**Symptom:** Trades executing outside expected London hours (08:00-16:30 GMT)

**Cause:** Broker server time ≠ GMT

**Solution:**
1. Determine broker server GMT offset:
   ```
   Current server time: 14:00
   Current GMT time: 12:00
   Offset: +2 hours (GMT+2)
   ```

2. Adjust session times in code OR document offset:
   - If broker is GMT+2, London 08:00 GMT = 10:00 server time
   - Verify trades occur at expected server time

3. Future enhancement: Add `GMT_Offset` input parameter

---

## Results Analysis

### Backtest Report Review

**Key Metrics to Extract:**
```
Total Net Profit: $XXX (target: >$50 on $500 = 10%+ over 6 months)
Profit Factor: X.XX (target: ≥1.5)
Expected Payoff: $XX (target: positive)
Absolute Drawdown: $XX (target: <$50)
Maximal Drawdown: X.XX% (target: <10%)

Total Trades: XXX (target: 50-100 for 6 months)
Win Rate: XX% (target: 40-50%)
Average Win: $XX
Average Loss: $XX
Largest Win: $XX
Largest Loss: $XX (should respect risk% limit)

Average Trade Length: X hours (target: <4 hours)
Longest Trade: X hours
```

### Log File Analysis

**Step 1: Export Experts Tab Logs**
1. Right-click Experts tab → Save As
2. Save to Docs/ folder as `backtest_YYYYMMDD_results.log`

**Step 2: Search for Key Events**

**Trade Entry Rejections:**
```
Search: "rejected" OR "aborted" OR "no signal"
Count: Each rejection reason
Analysis: If >50% rejections due to one filter, consider adjustment
```

**TP Validation Impact:**
```
Search: "TP crosses resistance" OR "TP crosses support"
Count: Total TP validation rejections
Compare: Total signals vs executed trades
Decision: If validation rejects >30% of trades, test with disabled
```

**SL Snapping Frequency:**
```
Search: "SL snapped to" OR "SL adjusted"
Count: How many trades had SL adjusted
Analysis: Average SL adjustment distance (in pips)
Verify: TP recalculated correctly after snap
```

**Partial Profit Executions:**
```
Search: "Partial close 80%" OR "Runner position"
Count: Total partials executed
Verify: 20% runner tickets created
Check: Break-even SL set correctly (Entry + commission buffer)
```

**RSI Divergence Exits:**
```
Search: "RSI divergence detected" OR "Runner closed"
Count: How many runners closed due to divergence
Analysis: Was divergence detection too sensitive? (closed winners prematurely)
```

### Equity Curve Analysis

**Visual Inspection:**
1. Strategy Tester → Graph tab
2. Look for:
   - ✅ Smooth upward curve (consistent profitability)
   - ✅ Shallow drawdown periods (good risk management)
   - ⚠️ Flat periods (ranging markets - normal)
   - ❌ Sharp vertical drops (risk management failure - investigate)

**Drawdown Periods:**
- Identify longest drawdown (bar count)
- Verify daily loss limit triggered correctly during drawdowns
- Expected: 3-5 consecutive losers max (within PRD limits)

### Trade Distribution Analysis

**By Hour (GMT):**
```
Group trades by entry hour
Expected: Concentration during London session (08:00-16:30 GMT)
Red flag: Trades outside enabled sessions
```

**By Day of Week:**
```
Expected: Relatively even distribution Mon-Fri
Red flag: All trades on one day (possible overfitting to specific event)
```

**By Win/Loss Streak:**
```
Longest winning streak: X trades
Longest losing streak: X trades (target: <6)
Analysis: Losing streak >6 suggests insufficient trade filtering
```

---

## Pre-Live Checklist

Before deploying to live account, ALL must be true:

### Backtest Validation
- [ ] 6-month backtest Profit Factor ≥1.5
- [ ] Max Drawdown <10%
- [ ] Win Rate 40-50% (not >70%)
- [ ] Average trade duration <4 hours
- [ ] No critical errors in log files

### Forward Test Validation (Minimum 2 weeks)
- [ ] Demo account Profit Factor ≥1.2
- [ ] Demo Win Rate within 10% of backtest
- [ ] Demo Max DD within 5% of backtest
- [ ] Partials executed correctly (if lot ≥0.05)
- [ ] Break-even SL set correctly (includes commission)
- [ ] RSI divergence logic working (runners closed appropriately)
- [ ] No session timing issues (trades during expected hours)
- [ ] No spread rejection errors (except during news)

### Code Verification
- [ ] All log entries reviewed for unexpected behavior
- [ ] TP validation impact analyzed (enabled vs disabled comparison)
- [ ] SL snapping impact analyzed (adjusted vs non-adjusted trades)
- [ ] Lot size calculations verified (no <0.01 or >account risk)
- [ ] Daily loss limit tested (manually trigger 3% loss, verify EA stops)

### Risk Assessment
- [ ] Comfortable with expected 10-20% Max DD
- [ ] Live account ≥$500 (minimum for strategy)
- [ ] Can sustain 6 consecutive losses (~3% account loss with 1% risk)
- [ ] FP Markets commission confirmed ($3/lot/side)
- [ ] Understand partial profit limitations (minimum 0.05 lot)

### Operational Readiness
- [ ] VPS setup (if required for 24/7 operation)
- [ ] Monitoring plan (daily log review schedule)
- [ ] Stop conditions defined (when to pause EA manually)
- [ ] Phase 2 trigger criteria (e.g., account growth to $1000 → add pairs)

---

## Testing Timeline Recommendation

**Week 1: Initial Backtest Validation**
- Day 1: Compile EA, run 3-month backtest (conservative settings)
- Day 2: Analyze results, fix any critical issues
- Day 3: Run 6-month backtest (default settings)
- Day 4: Test variations (TP validation ON/OFF, SL snapping ON/OFF)
- Day 5: Review all logs, document findings

**Week 2-3: Forward Test on Demo**
- Start demo account with $500 deposit
- Monitor daily, document all trades in spreadsheet
- Track: Entry reason, exit reason, TP validation rejections, SL adjustments

**Week 4: Analysis & Decision**
- Compare demo vs backtest metrics
- Final log review
- Go/No-Go decision for live deployment

**Total Time to Live:** 4 weeks minimum (can extend if demo shows issues)

---

## Support Resources

**Log Analysis:**
- All logs stored in `[MT5 Data]/MQL5/Logs/`
- Experts tab entries include EA name prefix `[JCAMP_FxScalper]`

**Performance Tracking:**
- Create Excel/Google Sheets with columns:
  - Date, Entry Time, Direction, Entry Price, SL, TP, Lot, Exit Price, P&L, Exit Reason, Notes

**Community (Future):**
- GitHub Issues: Report bugs or request features
- Trading Journal: Document weekly performance reviews

---

## Appendix: Sample Test Results Template

```
=== JCAMP_FxScalper Backtest Results ===
Date: YYYY-MM-DD
Period: YYYY-MM-DD to YYYY-MM-DD
Preset: JCAMP_FxScalper_EURUSD.set

--- Settings ---
RiskPercent: 1.0%
EnableTPValidation: true
EnableSLSnapping: true
Sessions: London only

--- Performance Metrics ---
Net Profit: $XXX (XX%)
Profit Factor: X.XX
Expected Payoff: $XX
Max Drawdown: $XX (X.X%)
Recovery Factor: X.XX

--- Trade Statistics ---
Total Trades: XXX
Wins: XX (XX%)
Losses: XX (XX%)
Average Win: $XX
Average Loss: $XX
Largest Win: $XX
Largest Loss: $XX

--- Risk Analysis ---
Max Consecutive Wins: X
Max Consecutive Losses: X
Average Trade Length: X hours
Longest Trade: X hours

--- Filter Analysis (from logs) ---
Total Signals Detected: XXX
TP Validation Rejections: XX (XX%)
Spread Rejections: XX (XX%)
Level Proximity Rejections: XX (XX%)
Daily Loss Limit Triggers: X days

--- Partial Profit Analysis ---
Trades with Partials: XX (out of XX winners)
Average Runner Duration: X hours
Runner Wins: XX
Runner Losses (divergence): XX

--- Decision ---
[ ] PASS - Proceed to forward test
[ ] FAIL - Requires optimization
[ ] INCONCLUSIVE - Extend test period

--- Notes ---
[Insert observations, concerns, or optimization ideas]
```

---

**End of Testing Guide**

# JCAMP_FxScalper - Debug Guide

## Enhanced Logging Active ✅

I've added comprehensive debug logging to help identify why the EA isn't taking trades.

---

## What Was Added

### Main EA (JCAMP_FxScalper_v1.mq5)
✅ **Detailed bar analysis headers** - Clear section separators
✅ **GMT time stamps** - Show exact time of each bar
✅ **Current price display** - Track price movement
✅ **H1 level statistics** - Count of detected support/resistance levels
✅ **Session debug info** - Shows which sessions are enabled
✅ **Spread monitoring** - Exact spread value in pips
✅ **Daily P&L stats** - Track account equity changes
✅ **Position counts** - Current vs max allowed
✅ **Indicator values** - SMA21, SMA50, SMA200, RSI, ATR displayed every bar
✅ **Trend analysis** - Shows if bullish/bearish trend exists
✅ **Momentum analysis** - Shows if RSI confirms direction
✅ **Filter pass/fail** - Each filter shows PASSED or FAILED with reason

### Entry Logic (JC_EntryLogic.mqh)
✅ **Pattern detection details** - Shows why engulfing patterns fail
✅ **Complex pattern breakdown** - Shows each candle in 5-bar sequence
✅ **Signal confirmation** - Clear "DETECTED" vs "conditions not met"

---

## How to Run Debug Backtest

### Step 1: Recompile with Debug Version
```
1. Open MetaEditor (F4)
2. Open JCAMP_FxScalper_v1.mq5
3. Press F7 to compile
4. Expected: "0 error(s), 0 warning(s)"
```

### Step 2: Run Short Backtest (Test Period)
```
Strategy Tester Settings:
- Symbol: EURUSD
- Period: M5
- Dates: 2025-12-01 to 2025-12-07 (1 week only for detailed logs)
- Modeling: Every tick based on real ticks
- Deposit: 500
- Leverage: 1:500
- Visual Mode: DISABLED (logs show better without visual)

Load Preset: JCAMP_FxScalper_EURUSD.set

Click START
```

**Why 1 week only?**
- Detailed logs will be VERY verbose
- Easier to analyze shorter period first
- Identify which filter is blocking trades

### Step 3: Analyze Journal Logs

After backtest completes:

1. Click **"Journal"** tab in Strategy Tester
2. Look for the debug sections (see examples below)
3. Right-click Journal → **"Save As"** → Save to `Docs\backtest_debug_YYYYMMDD.log`

---

## What to Look For in Logs

### Example Log Output - Successful Analysis

```
========================================
*** NEW M5 BAR - STARTING ANALYSIS ***
Time: 2025-12-01 08:05:00 GMT | Price: 1.10523
========================================
[DEBUG] H1 Levels | Supports: 15 | Resistances: 12 | Nearest S: 1.10450 | Nearest R: 1.10600
[DEBUG] Checking sessions at GMT time: 2025-12-01 08:05:00 GMT
[DEBUG] London=ENABLED | NY=disabled | Asian=disabled | Tokyo=disabled
[FILTER PASSED] Active sessions: London
[DEBUG] Current spread: 0.4 pips (max allowed: 1.0)
[FILTER PASSED] Spread acceptable
[DEBUG] Daily P&L: $0.00 (0.00%) | Start: $500.00 | Current: $500.00
[FILTER PASSED] Daily loss limit OK
[DEBUG] Current positions: 0 | Max allowed: 1
[FILTER PASSED] Can open new position
--- INDICATOR VALUES ---
SMA21: 1.10520 | SMA50: 1.10480 | SMA200: 1.10400
RSI(14): 55.23 | ATR(14): 0.00085
Bullish trend? YES (SMA21>SMA50>SMA200)
Bearish trend? NO (SMA200>SMA50>SMA21)
Bullish momentum? YES (RSI>50)
Bearish momentum? NO (RSI<50)
--- CHECKING BULLISH SIGNAL ---
[EntryLogic] Bullish trend confirmed | SMA21: 1.10520 > SMA50: 1.10480 > SMA200: 1.10400
[EntryLogic] Bullish momentum confirmed | RSI: 55.2 > 50
[DEBUG] Bullish Engulfing check | Prev: BEARISH | Curr: BULLISH | Engulfs: YES
[EntryLogic] Bullish Engulfing DETECTED | Prev[O:1.10530 C:1.10515] Curr[O:1.10518 C:1.10535]
[EntryLogic] *** COMPLETE BULLISH SIGNAL CONFIRMED ***
[SIGNAL DETECTED] Bullish entry conditions met!
[MarketStructure] Price near H1 level | Price: 1.10523 | Level: 1.10450 | Distance: 7.3 pips | Max: 5 pips
[FILTER FAILED] Price not near H1 support - SKIPPING BUY
[NO SIGNAL] Bearish conditions not met
========================================
*** M5 BAR ANALYSIS COMPLETE ***
========================================
```

### Key Diagnostic Questions

#### 1. Are bars being analyzed at all?
**Look for:** `*** NEW M5 BAR - STARTING ANALYSIS ***`

**If missing:** New bar detection may not be working
- Check if backtest period has any M5 data
- Verify symbol is EURUSD

#### 2. Is the session filter passing?
**Look for:** `[FILTER PASSED] Active sessions: London`

**If shows:** `[FILTER FAILED] No active session - SKIPPING ANALYSIS`
**Problem:** GMT time doesn't match London session (08:00-16:30)
**Fix:**
- Check broker server time vs GMT
- Verify at least ONE session is enabled in inputs
- Check if test period includes London hours

#### 3. Is spread acceptable?
**Look for:** `[DEBUG] Current spread: 0.4 pips (max allowed: 1.0)`

**If shows:** `[FILTER FAILED] Spread too wide - SKIPPING ANALYSIS`
**Problem:** Spread >1.0 pips during backtest
**Fix:**
- This is unusual for EURUSD (normally 0.2-0.5 pips)
- Check Strategy Tester spread setting
- May need to increase MaxSpread input to 2.0 for backtest

#### 4. Are H1 levels being detected?
**Look for:** `[DEBUG] H1 Levels | Supports: 15 | Resistances: 12`

**If shows:** `Supports: 0 | Resistances: 0`
**Problem:** No fractal levels detected on H1
**Possible causes:**
- Not enough H1 data in backtest period
- Fractal detection logic issue
- Try longer backtest period (more H1 bars)

#### 5. Is trend alignment happening?
**Look for:** `Bullish trend? YES (SMA21>SMA50>SMA200)`

**If always shows:** `Bullish trend? NO` AND `Bearish trend? NO`
**Problem:** SMAs not aligned in either direction (ranging market)
**Solution:** This is NORMAL in ranging markets - EA designed for trending markets only
**Action:** Try different date range with clearer EURUSD trend

#### 6. Are patterns being detected?
**Look for:**
- `[EntryLogic] Bullish Engulfing DETECTED`
- `[EntryLogic] Complex bullish pattern DETECTED`

**If shows:** `[DEBUG] Bullish Engulfing check | Prev: BULLISH | Curr: BEARISH | Engulfs: no`
**Problem:** Candle patterns not forming
**This is NORMAL:** Patterns are rare - may only occur 5-10 times per week
**Action:** Review multiple days of logs to find pattern occurrences

#### 7. Is price proximity filter blocking trades?
**Look for:** `[FILTER FAILED] Price not near H1 support - SKIPPING BUY`

**If this appears AFTER signal detected:**
**Problem:** Price not within 5 pips of H1 support/resistance
**This is BY DESIGN:** Prevents trades away from structural levels
**If over-filtering:**
- Increase `LevelProximity` from 5 to 10 pips in inputs
- Or disable temporarily to test: Set to 100 pips

#### 8. Is TP validation rejecting trades?
**Look for:** `[MarketStructure] TP VALIDATION FAILED (BUY) | TP: X crosses resistance: Y`

**If appears frequently:**
**Problem:** Many H1 resistance levels blocking targets
**Solution:**
- This is a FEATURE (prevents blocked targets)
- Try disabling: `EnableTPValidation = false` in inputs
- Compare performance with ON vs OFF

---

## Common Scenarios & Solutions

### Scenario 1: "No active session" every bar
```
[FILTER FAILED] No active session - SKIPPING ANALYSIS
```

**Diagnosis:** Session filter rejecting all times

**Check:**
1. Is at least ONE session enabled? (TradeLondon = true)
2. What is GMT time in logs? Does it match London hours (08:00-16:30)?
3. Broker server time offset?

**Quick Fix:** Enable ALL sessions temporarily to test:
```
TradeLondon = true
TradeNewYork = true
TradeAsian = true
TradeTokyo = true
```

### Scenario 2: "Bullish trend? NO" and "Bearish trend? NO" always
```
SMA21: 1.10500 | SMA50: 1.10505 | SMA200: 1.10490
Bullish trend? NO (SMA21>SMA50>SMA200)
Bearish trend? NO (SMA200>SMA50>SMA21)
```

**Diagnosis:** Ranging market - SMAs tangled

**This is NORMAL:** EA designed for trending markets only

**Solution:** Test different date ranges:
- Look for strong EURUSD trends in 2025
- Trending periods: Often during major ECB/Fed events
- Try: Sept-Oct 2025 (ECB tightening cycle)

### Scenario 3: Trend passes, momentum passes, but no pattern
```
Bullish trend? YES (SMA21>SMA50>SMA200)
Bullish momentum? YES (RSI>50)
[DEBUG] Bullish Engulfing check | Prev: BEARISH | Curr: BULLISH | Engulfs: no
[DEBUG] Complex bullish pattern check | [4]:BULL [3]:BULL [2]:BEAR [1]:BEAR [0]:BULL>open[1]
[NO SIGNAL] Bullish conditions not met
```

**Diagnosis:** Trend and momentum OK, but no trigger pattern

**This is EXPECTED:** Patterns are specific and rare

**Interpretation:**
- EA is working correctly
- Waiting for proper entry trigger
- May need to analyze 50-100 bars to find one pattern

**Action:** Let backtest run full week - patterns will appear eventually

### Scenario 4: Signal detected but price not near level
```
[SIGNAL DETECTED] Bullish entry conditions met!
[MarketStructure] Price NOT near H1 level | Distance: 15.3 pips | Required: <5 pips
[FILTER FAILED] Price not near H1 support - SKIPPING BUY
```

**Diagnosis:** All entry conditions met, but proximity filter blocks

**This is BY DESIGN:** Prevents trades away from key levels

**Testing options:**
1. **Increase proximity:** Set `LevelProximity = 10` (instead of 5)
2. **Temporary disable:** Set `LevelProximity = 100` to effectively disable
3. **Analyze:** How often does this reject valid trades?

### Scenario 5: Everything passes, then TP validation fails
```
[SIGNAL DETECTED] Bullish entry conditions met!
[FILTER PASSED] Price near H1 support - PROCESSING BUY
[BUY] Initial SL calculation | ...
[BUY] TP calculation | Entry: 1.10500 | SL: 1.10450 | TP: 1.10600
[MarketStructure] TP VALIDATION FAILED (BUY) | TP: 1.10600 crosses resistance: 1.10580 | TRADE ABORTED
```

**Diagnosis:** TP validation preventing trade

**This is BY DESIGN:** Prevents targets that can't be reached

**Testing:**
1. **Disable to measure impact:** Set `EnableTPValidation = false`
2. **Run two backtests:**
   - Test A: TP validation ON
   - Test B: TP validation OFF
3. **Compare:** Trade frequency, Win%, Profit Factor

**Expected:** More trades with validation OFF, but possibly lower Win%

---

## Debug Checklist - Run Through These

Run a 1-week backtest and check each item:

- [ ] **Bars analyzed:** Count how many `*** NEW M5 BAR` entries (should be ~2400 bars/week on M5)
- [ ] **Session passes:** At least SOME bars show `[FILTER PASSED] Active sessions`
- [ ] **Spread acceptable:** Most bars show spread <1.0 pips
- [ ] **H1 levels detected:** Shows `Supports: >0 | Resistances: >0`
- [ ] **Trend exists:** At least SOME bars show `Bullish trend? YES` OR `Bearish trend? YES`
- [ ] **Patterns checked:** Shows `Bullish Engulfing check` or `Complex pattern check`
- [ ] **Signals detected:** At least ONE `[SIGNAL DETECTED]` in 1 week (if trending)
- [ ] **Proximity checked:** Shows distance calculation in pips
- [ ] **TP validation checked:** Shows whether TP crosses levels

---

## Next Steps Based on Findings

### If NO bars are analyzed:
→ Problem with new bar detection or backtest setup
→ Check: Symbol correct? M5 data available? Backtest running?

### If all bars fail session filter:
→ Session time mismatch
→ Solution: Enable all sessions temporarily OR check GMT offset

### If all bars fail spread filter:
→ Unrealistic spread in backtest
→ Solution: Increase MaxSpread to 2.0 or 3.0

### If trend never aligns:
→ Ranging market period selected
→ Solution: Try different date range (Sept-Oct 2025 for EURUSD trend)

### If patterns never detected:
→ NORMAL if no trend exists
→ If trend exists but no patterns: May need to run longer backtest (2-4 weeks)

### If signals detected but proximity blocks all:
→ Proximity filter too strict
→ Solution: Increase LevelProximity from 5 to 10 or 15 pips

### If TP validation blocks all signals:
→ Many resistance/support levels in the way
→ Solution: Test with `EnableTPValidation = false` to compare

---

## Sample Debug Command

After running backtest:

1. Save Journal tab to file
2. Search for these key phrases:

```
Search for: "FILTER FAILED"
Count: How many times each filter fails

Search for: "SIGNAL DETECTED"
Count: How many signals found

Search for: "Bullish trend? YES"
Count: How many bars have bullish trend

Search for: "Engulfing DETECTED"
Count: How many engulfing patterns found
```

---

## Quick Test Settings for Maximum Trades

**If you want to see IF the EA can trade at all, try these relaxed settings:**

```
=== Relaxed Test Settings ===
TradeLondon = true
TradeNewYork = true
TradeAsian = true
TradeTokyo = true
MaxSpread = 5.0 (very relaxed)
LevelProximity = 50 (effectively disabled)
EnableTPValidation = false (disabled)
EnableSLSnapping = false (disabled)

Test Period: 2025-09-01 to 2025-10-01 (strong EURUSD trend)
```

**Expected:** Should get SOME trades if:
- Trend exists (SMA alignment)
- RSI confirms direction
- Patterns form (engulfing or complex)

**If STILL no trades with relaxed settings:**
→ Problem with indicator buffers or pattern detection logic
→ Review logs for ERROR messages

---

## Status: Enhanced Logging Active ✅

Recompile and run backtest to see detailed analysis of every M5 bar.

**Files updated:**
- JCAMP_FxScalper_v1.mq5 (detailed OnTick logging)
- JC_EntryLogic.mqh (pattern detection debug)

**Next:** Run 1-week backtest, analyze logs, report findings!

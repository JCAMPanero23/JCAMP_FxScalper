# Visual Chart Investigation Guide

**Date:** 2026-03-25
**Purpose:** Manually analyze losing trades to understand WHY the system has 20% win rate

---

## Setup in cTrader

### 1. Enable Visual Mode Backtest
- Run backtest with **Visual Mode ON**
- Set speed to **slow** (100-500x) to watch trades develop
- Period: Use a short period first (1-2 weeks) to see patterns

### 2. Key Indicators to Display
- SMA 200 (trend direction)
- Rectangle zones (entry areas)
- Pending orders (green/red lines)

---

## What to Look For

### A. Zone Quality Issues

**Check each zone that triggers a trade:**

| Question | Good Sign | Bad Sign |
|----------|-----------|----------|
| Is zone at clear S/R level? | Price bounced here before | Random price level |
| Is fractal significant? | Clear swing high/low | Tiny pullback |
| Zone size vs price action? | Zone contains the swing | Zone too small/large |

**Note patterns:**
- Are zones forming at weak price levels?
- Do fractals form during consolidation (choppy price)?
- Are zones too close to each other?

### B. Entry Timing Issues

**Watch the entry moment:**

| Question | Good Sign | Bad Sign |
|----------|-----------|----------|
| Price action at entry? | Strong rejection candle | Weak/uncertain price |
| Momentum direction? | Moving toward TP | Already reversing |
| Distance from zone? | Enters inside zone | Enters far from zone |

**Note patterns:**
- Does price blast through zones without stopping?
- Are entries happening at the worst possible moment?
- Is there a delay issue (pending order fills late)?

### C. Stop Loss Issues

**Watch losing trades:**

| Question | Good Sign | Bad Sign |
|----------|-----------|----------|
| SL placement? | Behind structure | In the middle of noise |
| SL distance? | Appropriate for volatility | Too tight/too wide |
| How SL gets hit? | Clean breakout | Wick hunt then reversal |

**Note patterns:**
- Are SLs being hit by wicks then price reverses?
- Is SL too tight for the timeframe volatility?
- Does price return to profit zone after hitting SL?

### D. Take Profit Issues

**Watch winning AND losing trades:**

| Question | Good Sign | Bad Sign |
|----------|-----------|----------|
| TP placement? | At logical level | Random distance |
| Does price reach TP? | Yes, frequently | Reverses just before |
| Chandelier behavior? | Locks in profits | Exits too early/late |

**Note patterns:**
- Does Chandelier SL exit at good prices?
- Are we leaving money on the table?
- Do winners run or get cut short?

---

## Recording Observations

### Create a Trade Log

For each trade you observe, note:

```
Trade #: ___
Direction: BUY / SELL
Zone Type: Fractal / PRE-Zone / VALID
Entry Price: ___
SL: ___
TP: ___
Result: WIN / LOSS
Pips: ___

Observations:
- Zone quality (1-5): ___
- Entry timing (1-5): ___
- SL placement (1-5): ___

What went wrong/right:
_______________________

Pattern noticed:
_______________________
```

---

## Key Patterns to Identify

### 1. Fake Breakout Pattern
- Price enters zone
- Triggers entry
- Immediately reverses
- Hits SL
- Then continues in original direction

**If common:** Zone levels are weak, need better S/R identification

### 2. Stop Hunt Pattern
- Price approaches zone
- Spikes through SL
- Immediately reverses back
- Would have been profitable

**If common:** SL too tight, need ATR-based buffer

### 3. Late Entry Pattern
- Strong move starts
- Zone forms after move is extended
- Entry happens at exhaustion point
- Price reverses

**If common:** Timing issue, zones form too late

### 4. Trend Exhaustion Pattern
- Multiple losing trades in same direction
- Trend is ending but system keeps trying
- Counter-trend moves are strong

**If common:** Need trend strength filter or reversal detection

---

## Questions to Answer

After visual investigation, answer:

1. **Are zones at quality price levels?**
   - Yes → Problem is elsewhere
   - No → Need better zone identification

2. **Is entry timing good?**
   - Yes → Problem is SL/TP
   - No → Need entry confirmation filter

3. **Are SLs reasonable?**
   - Yes → Market conditions issue
   - No → Need SL optimization

4. **Is the trend filter working?**
   - Yes → Individual trade quality issue
   - No → Trend detection needs work

5. **What's the most common losing pattern?**
   - Document top 3 patterns
   - Prioritize fixes based on frequency

---

## Next Session Action Items

After visual investigation:

1. Document top 3 losing patterns observed
2. Take screenshots of representative examples
3. Propose specific fixes for each pattern
4. Prioritize by impact (which pattern causes most losses)

---

## Quick Reference: Zone Colors

| Color | State | Meaning |
|-------|-------|---------|
| Yellow | PRE | Displacement + FVG detected, waiting for fractal |
| Blue | VALID | Fractal confirmed the zone |
| Green | ARMED | Price near zone, ready for entry |
| Red | Trade | Entry triggered |

---

**Remember:** The goal is to understand WHY trades lose, not just HOW MANY lose. One insight about a common pattern is worth more than running 100 backtests.

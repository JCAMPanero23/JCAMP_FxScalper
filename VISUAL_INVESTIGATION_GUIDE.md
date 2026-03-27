# Visual Chart Investigation Guide

**Date:** 2026-03-27
**Version:** v3.1.0
**Purpose:** Manually analyze losing trades to understand WHY the system has 20% win rate

---

## What's New in v3.1.0

### Debug Trade Logger
The bot now automatically tracks trade performance by:
- **Zone Type:** PRE-Zone vs Swing (fractal-only)
- **Entry System:** Standard vs Reversal (placeholder)
- **Direction:** BUY vs SELL
- **Result:** Win vs Loss

### New Zone Management Rules
1. **Price Distance Invalidation:** Zones invalidate if price moves >15 pips away without entry
2. **Danger Session Invalidation:** Zones invalidate when entering danger sessions (04:00-08:00 UTC, 20:00-00:00 UTC)
3. **Minimum SL:** Stop loss never smaller than 5 pips (prevents tiny SLs from small FVG zones)
4. **No Zone Creation With Open Trade:** Prevents new zones forming while position is open

### Debug Log Files
After backtesting, check these files:
```
C:\Users\Jcamp_Laptop\Documents\cAlgo\Data\cBots\Jcamp_1M_scalping\DebugLogs\
├── trades_detailed_YYYYMMDD_HHMMSS.txt   # Full trade details with replay timestamps
└── trades_summary_YYYYMMDD_HHMMSS.txt    # Performance summary by category
```

---

## Setup in cTrader

### 1. Enable Visual Mode Backtest
- Run backtest with **Visual Mode ON**
- Set speed to **slow** (100-500x) to watch trades develop
- Period: Use a short period first (1-2 weeks) to see patterns

### 2. Key Parameters to Set
| Parameter | Recommended | Description |
|-----------|-------------|-------------|
| Enable Debug Logging | true | Generates trade analysis files |
| Max Price Distance (pips) | 15.0 | Zone invalidates if price too far |
| Minimum SL (pips) | 5.0 | Floor for stop loss distance |
| Enable PRE-Zone System | true | Test PRE-Zone vs Swing performance |

### 3. Key Indicators to Display
- SMA 200 (trend direction)
- Rectangle zones (entry areas)
- Pending orders (green/red lines)

---

## Using Debug Trade Logs

### Step 1: Run a Backtest
Run a normal backtest (non-visual) first to generate debug logs.

### Step 2: Check the Summary File
Open `trades_summary_YYYYMMDD_HHMMSS.txt` to see:
```
=== ZONE TYPE PERFORMANCE ===

PRE-ZONE:
  Standard BUY:  Wins: 5   Losses: 22  Win Rate: 18.5%
  Standard SELL: Wins: 3   Losses: 18  Win Rate: 14.3%
  TOTAL:         Wins: 8   Losses: 40  Win Rate: 16.7%

SWING ZONE:
  Standard BUY:  Wins: 7   Losses: 23  Win Rate: 23.3%
  Standard SELL: Wins: 5   Losses: 20  Win Rate: 20.0%
  TOTAL:         Wins: 12  Losses: 43  Win Rate: 21.8%

=== COMPARISON ===
PRE-Zone Win Rate:   16.7%
Swing Zone Win Rate: 21.8%
```

### Step 3: Get Replay Timestamps
The summary file includes timestamps for visual replay:
```
=== REPLAY TIMESTAMPS ===
(Copy these times to Visual Mode backtest)

PRE-ZONE Standard BUY Win:
  1. 2024-10-15 14:32
  2. 2024-10-18 09:15
  3. 2024-10-22 13:45

PRE-ZONE Standard BUY Loss:
  1. 2024-10-12 11:45
  2. 2024-10-14 16:20
  3. 2024-10-16 10:30
```

### Step 4: Visual Replay
1. Open cTrader backtester
2. Enable Visual Mode
3. Set start date to just before a timestamp from the log
4. Run at slow speed (100-500x)
5. Watch the specific trade develop

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
Zone Type: PRE-Zone / Swing
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

### 5. Zone Invalidation Pattern (NEW)
- Zone created during good session
- Price moves away without triggering
- Zone should have invalidated but didn't
- Late entry at bad price

**If common:** Check MaxPriceDistancePips setting

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

5. **Which zone type performs better?**
   - PRE-Zone better → Focus on displacement detection
   - Swing better → Simplify to fractal-only system

6. **What's the most common losing pattern?**
   - Document top 3 patterns
   - Prioritize fixes based on frequency

---

## Next Session Action Items

After visual investigation:

1. Document top 3 losing patterns observed
2. Take screenshots of representative examples
3. Propose specific fixes for each pattern
4. Prioritize by impact (which pattern causes most losses)
5. Compare PRE-Zone vs Swing performance from debug logs

---

## Quick Reference: Zone Colors

| Color | State | Meaning |
|-------|-------|---------|
| Yellow | PRE | Displacement + FVG detected, waiting for fractal |
| Blue | VALID | Fractal confirmed the zone |
| Green | ARMED | Price near zone, ready for entry |
| Red | Trade | Entry triggered |
| Gray | Invalidated | Zone cancelled (price too far, danger session, etc.) |

---

## Quick Reference: Zone Invalidation Rules

| Rule | Trigger | Parameter |
|------|---------|-----------|
| Price Too Far | Price moves away without entry | MaxPriceDistancePips (default: 15) |
| Danger Session | Market enters dead zone | 04:00-08:00 UTC or 20:00-00:00 UTC |
| Wrong Direction | Candle body closes against zone | Automatic |
| Expiry | Zone too old | ZoneExpiryMinutes (default: 240) |

---

**Remember:** The goal is to understand WHY trades lose, not just HOW MANY lose. Use the debug logs to identify patterns, then visual replay to understand the mechanics. One insight about a common pattern is worth more than running 100 backtests.

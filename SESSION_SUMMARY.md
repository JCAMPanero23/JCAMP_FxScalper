# PRE-ZONE INVESTIGATION SESSION SUMMARY

**Date:** 2026-03-25
**Branch:** enhance-entry-system
**Commit:** 2c52300

---

## FINDINGS

### PRE-Zone Triple Confirmation Hypothesis: FAILED

We tested whether requiring Displacement + FVG + Fractal would improve zone quality.

**Results:**

| Test | Trades | Win Rate | Net P/L |
|------|--------|----------|---------|
| TestA (no PRE-Zone) | 224 | 20.5% | -$1,531 |
| TestG (PRE-Zone ON) | 211 | 19.4% | -$2,229 |

**Conclusion:** PRE-Zone doesn't improve trade quality. Win rate is essentially the same (~20%).

---

## BUGS FIXED

### 1. Trend Validation (Line 2904-2912)
PRE-Zone now rejects displacements that go against SMA trend.

### 2. Fractal Confirmation Required (Line 3051)
PRE-zones can no longer arm directly - must wait for fractal confirmation.

---

## ROOT CAUSE ANALYSIS

The **zone locations themselves are the problem**, not the confirmation method.

Both systems (fractal-only and PRE-Zone) create zones at similar price levels because:
- PRE-Zone still relies on fractals for final confirmation
- Fractals form at weak swing points (score 0.6 threshold)
- These are not real support/resistance levels

---

## NEXT SESSION: VISUAL INVESTIGATION

**File:** `VISUAL_INVESTIGATION_GUIDE.md`

### Goal
Manually watch losing trades in Visual Mode to understand WHY they lose.

### Key Questions
1. Are zones at quality S/R levels?
2. Is entry timing good?
3. Are SLs being stop-hunted?
4. What's the most common losing pattern?

### How to Run
1. Open cTrader backtester
2. Enable Visual Mode
3. Run on short period (1-2 weeks)
4. Slow speed (100-500x)
5. Watch and document losing patterns

---

## POSSIBLE ISSUES TO LOOK FOR

1. **Fake Breakouts** - Price enters zone, triggers, immediately reverses
2. **Stop Hunts** - SL hit by wick, then price continues original direction
3. **Late Entries** - Zone forms after move is exhausted
4. **Weak Fractals** - Zones at random price levels, not real S/R

---

## FILES

| File | Purpose |
|------|---------|
| `Jcamp_1M_scalping.cs` | Bot code with PRE-Zone fixes |
| `VISUAL_INVESTIGATION_GUIDE.md` | Guide for manual chart analysis |
| `Backtest/V3 baseline/Period 2/Round2/` | Test results |

---

## HYPOTHESIS FOR NEXT SESSION

**If visual investigation shows zones at weak levels:**
→ Increase MinimumSwingScore (0.6 → 0.7-0.8)
→ Add H1/H4 confluence requirement
→ Require zone at session high/low

**If visual investigation shows SL issues:**
→ Widen SL buffer
→ Use ATR-based dynamic SL
→ Add break-even logic earlier

**If visual investigation shows entry timing issues:**
→ Add momentum confirmation
→ Wait for rejection candle
→ Use limit orders instead of stops

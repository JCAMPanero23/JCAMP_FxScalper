# PRE-ZONE INVESTIGATION SESSION

**Date:** 2026-03-24
**Branch:** enhance-entry-system

---

## BUG FIXED

### Critical Issue: PRE-Zone Trend Mismatch

**Problem Found:**
PRE-Zone system was creating zones based solely on displacement direction without validating against the SMA trend. This caused:
- Counter-trend zones (e.g., BUY zone when SMA trend is SELL)
- `SyncZoneToLegacyVariables()` would override `currentMode` with zone direction
- System would take trades AGAINST the overall trend

**Fix Applied:** Added trend validation in `CreatePreZone()` at line 2904-2912:
```csharp
// CRITICAL FIX: Validate displacement matches SMA trend
string trendMode = DetectTrendMode();
if (displacementMode != trendMode)
{
    Print("[PRE-Zone] Rejected | Displacement ({0}) against SMA trend ({1})",
        displacementMode, trendMode);
    return null;
}
```

**Files Modified:**
- `Jcamp_1M_scalping.cs` - Added trend validation
- Copied to cAlgo build directory

---

## TEST G READY

**Test G Settings Created:**
- Location: `Backtest/V3 baseline/Period 2/Round2/TestG_parameters.cbotset`
- Key change: `EnablePreZoneSystem: True` (was False in TestA)
- All entry filters: DISABLED (same as TestA)

**Comparison Target:**
- TestA (no PRE-Zone): 224 trades, 20.5% win rate, -$1,531
- TestG (with PRE-Zone + trend fix): Target >30% win rate

---

## NEXT STEPS

### 1. Rebuild Bot in cTrader
- Open cTrader
- Build the bot (code already copied to cAlgo directory)

### 2. Run Test G Backtest
- Period: Oct 2024 - Jun 2025 (Period 2)
- Import settings from: `TestG_parameters.cbotset`
- Save results to: `Backtest/V3 baseline/Period 2/Round2/TestG/`

### 3. Analyze Results
**Success Criteria:**
- Trade count: 20-60 trades (not 0, not 200+)
- Win rate: >30% (vs 20% without PRE-Zone)
- Profit factor: >1.2
- No critical errors in logs

**If Successful:** Proceed to PRE-Zone parameter optimization
**If Fails:** Deeper investigation needed

---

## KEY FILES

| File | Purpose |
|------|---------|
| `Jcamp_1M_scalping.cs` | Main bot code (edited) |
| `Backtest/V3 baseline/Period 2/Round2/TestG_parameters.cbotset` | Test G settings |
| `Docs/NEXT_SESSION_PRE-ZONE_FIX.md` | Original investigation plan |

---

## HYPOTHESIS RECAP

**Current System (PRE-Zone OFF):**
Weak fractals → garbage zones → 20% win rate

**Fixed PRE-Zone System (PRE-Zone ON + trend validation):**
Displacement + FVG + Fractal + **TREND VALIDATION** → quality zones → higher win rate?

The trend validation fix ensures PRE-Zones only form in the direction of the SMA trend, which should significantly improve quality.

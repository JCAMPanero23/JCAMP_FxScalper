# Enhanced Entry System - Design Specification

**Status**: Approved for Implementation
**Date**: 2026-03-12
**Branch**: enhance-entry-system

---

## Problem Statement

The current system creates trading zones based on Williams Fractals, which require 2 bars of confirmation after a swing forms. On M15 timeframe, this means zones are created **30+ minutes after the swing**, by which time price has often already moved away from the entry zone.

**Current Flow (Delayed):**
```
M15 Swing Forms → Wait 2 bars (30 min) → Fractal Confirms → Draw Rectangle → Price already gone
```

**Enhanced Flow (Immediate):**
```
M15 Displacement → FVG Forms (next bar) → Create PRE-Zone → Price enters → Trade
```

---

## Solution Overview

Implement a **Three-Stage Zone Lifecycle** with **Displacement + FVG** as the trigger for immediate zone creation, while keeping the existing fractal system as a fallback.

### Key Components

1. **Displacement Detection** - Identify institutional impulse candles
2. **Enhanced FVG Engine** - Add quality filters and displacement linking
3. **Three-Stage Zone Lifecycle** - PRE → VALID → ARMED
4. **Parallel Zone System** - PRE-zones coexist with fractal zones as fallback

---

## Component 1: Displacement Detection

### Definition

A displacement candle is an unusually large impulse candle indicating institutional momentum.

### Detection Rules

```
Displacement = TRUE if:
   Candle Body Size >= ATRMultiplier × ATR(ATRPeriod)

Default: Body Size >= 1.5 × ATR(14)
```

### ATR Indicator Initialization

**IMPORTANT**: Add ATR indicator to the cBot:

```csharp
// In class fields
private AverageTrueRange atr;

// In OnStart() after m15Bars is initialized
atr = Indicators.AverageTrueRange(m15Bars, ATRPeriod, MovingAverageType.Simple);
```

### Data Structure

```csharp
class DisplacementCandle
{
    int BarIndex;              // Which M15 bar
    DateTime Time;             // When it occurred
    double ImpulseSize;        // Body size in pips
    double ATRMultiple;        // How many × ATR
    bool IsBullish;            // Direction (close > open)
    double OriginPrice;        // Zone anchor point
}
```

### Origin Price Logic

- **Bullish Displacement**: Origin = Candle LOW (where move started)
- **Bearish Displacement**: Origin = Candle HIGH (where move started)

### Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| Enable PRE-Zone System | true | bool | Master toggle for new system |
| ATR Period | 14 | 5-50 | Period for ATR calculation |
| ATR Multiplier | 1.5 | 1.0-3.0 | Minimum impulse size as ATR multiple |

---

## Component 2: Enhanced FVG Engine

### Current System

- Detects 3-candle FVG pattern on M15
- No minimum gap size filter
- 50-bar lookback
- Used only for swing scoring (15% weight)

### Enhancements

| Feature | Current | Enhanced |
|---------|---------|----------|
| Minimum Gap Size | None | >= 1.5 pips |
| Max Age | 50 bars | 30 bars (configurable) |
| Quality Flag | None | IsHighQuality (formed during displacement) |
| Purpose | Scoring only | Scoring + Zone Creation Trigger |

### Enhanced Data Structure

```csharp
class FairValueGap
{
    // Existing fields
    DateTime Time;
    double TopPrice;
    double BottomPrice;
    bool IsBullish;
    bool IsFilled;

    // NEW fields
    bool IsHighQuality;        // Candle B (impulse) meets displacement criteria
    double GapSizeInPips;      // For filtering
    int DisplacementBarIndex;  // Links to impulse candle (-1 if none)
}
```

### FVG + Displacement Linking (Clarified)

**FVG Structure Reminder:**
- Candle A (idx-1): Before the impulse
- **Candle B (idx): The IMPULSE candle** ← This is checked for displacement
- Candle C (idx+1): After the impulse

**IsHighQuality = TRUE when:**
```
Candle B (the impulse candle at idx) meets displacement criteria:
   BodySize(CandleB) >= ATRMultiplier × ATR(14)
```

### FVG Detection Timing (Clarified)

FVG detection requires Candle C to be **closed**. This means:
- Displacement occurs on bar N (Candle B)
- FVG is confirmed on bar N+1 (when Candle C closes)
- PRE-zone is created on bar N+1

**This is still 15 minutes faster than fractal confirmation (which needs bar N+2).**

### Filtering Logic

```
1. Calculate gap size in pips
2. IF gap size < MinFVGSizePips (1.5) → Discard
3. IF age > FVGMaxAgeBars (30) → Discard
4. Check if Candle B (impulse) meets displacement criteria
5. IF YES → Mark IsHighQuality = true
6. Only IsHighQuality FVGs trigger PRE-zone creation
```

### Multiple FVGs Rule

If a single displacement candle creates multiple FVGs:
- Select the FVG **in the same direction** as the displacement
- Bullish displacement → use Bullish FVG only
- Bearish displacement → use Bearish FVG only
- If multiple same-direction FVGs, use the **largest gap size**

### Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| Min FVG Size (pips) | 1.5 | 0.5-5.0 | Minimum gap to consider |
| FVG Max Age (bars) | 30 | 10-100 | Maximum age before discarding |

---

## Component 3: Three-Stage Zone Lifecycle

### Zone States

```
┌─────────────────────────────────────────────────────────────────┐
│                        ZONE LIFECYCLE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   [Displacement + High-Quality FVG]                              │
│              │                                                   │
│              ▼                                                   │
│      ┌─────────────┐                                             │
│      │  PRE-ZONE   │ ─── Expiry: 60 min                         │
│      │  Score: New │                                             │
│      └──────┬──────┘                                             │
│             │                                                    │
│    [Williams Fractal confirms within tolerance]                  │
│             │                                                    │
│             ▼                                                    │
│      ┌─────────────┐                                             │
│      │ VALID-ZONE  │ ─── Expiry: 120 min (extended)             │
│      │  Confirmed  │                                             │
│      └──────┬──────┘                                             │
│             │                                                    │
│    [Price within MaxDistanceToArm]                               │
│             │                                                    │
│             ▼                                                    │
│      ┌─────────────┐                                             │
│      │ ARMED-ZONE  │ ─── Ready for entry                        │
│      │  Trading    │                                             │
│      └─────────────┘                                             │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### State Definitions

| State | Trigger | Expiry | Can Trade? |
|-------|---------|--------|------------|
| PRE | Displacement + High-Quality FVG | 60 min | Yes (if ARMED) |
| VALID | Williams Fractal confirms at zone | 120 min | Yes (if ARMED) |
| ARMED | Price within MaxDistanceToArm pips | Same as parent | Yes |
| EXPIRED | Time exceeds expiry | - | No |
| INVALIDATED | Wrong-direction breakout | - | No |

### Fractal Confirmation Tolerance

**Fractal "confirms at zone" when:**
```
Fractal Price is within FractalZoneTolerance pips of Zone Origin

SELL Zone: |FractalHigh - ZoneOrigin| <= 5 pips
BUY Zone:  |FractalLow - ZoneOrigin| <= 5 pips

Default FractalZoneTolerance: 5 pips (configurable)
```

### Zone Storage Strategy

**Single Active Zone Model:**
- Only ONE zone can be active at a time (matching current system)
- New `TradingZone` object replaces existing state variables

**Variable Mapping:**
```csharp
// OLD variables (still populated for compatibility)
swingTopPrice    ← TradingZone.TopPrice
swingBottomPrice ← TradingZone.BottomPrice
hasActiveSwing   ← TradingZone.State == Armed
hasValidRectangle ← TradingZone.State != Expired && State != Invalidated
currentMode      ← TradingZone.Mode
rectangleExpiryTime ← TradingZone.ExpiryTime

// NEW variable
private TradingZone activeZone;  // The single active zone (or null)
```

This ensures **existing entry logic works unchanged** - it still reads `swingTopPrice`, `swingBottomPrice`, etc.

### Zone Data Structure

```csharp
enum ZoneState { Pre, Valid, Armed, Expired, Invalidated }

class TradingZone
{
    // Identity
    string Id;
    ZoneState State;

    // Price Levels
    double TopPrice;           // Upper boundary
    double BottomPrice;        // Lower boundary
    double OriginPrice;        // Displacement origin (for fractal matching)

    // Timing
    DateTime CreatedTime;
    DateTime ExpiryTime;

    // Source References
    DisplacementCandle Displacement;
    FairValueGap FVG;
    int? FractalBarIndex;      // Set when upgraded to VALID

    // Scoring (PRE-zone formula)
    double DisplacementScore;   // 40%
    double FVGScore;            // 25%
    double SessionScore;        // 25%
    double PeriodScore;         // 10%
    double TotalScore;

    // Direction
    string Mode;                // "BUY" or "SELL"
}
```

### Zone Width Calculation

```
Zone Width: 4 pips (matches existing rectangle width)

SELL Zone (bearish displacement):
├── Top = Displacement Origin (High) + 2 pips
└── Bottom = Displacement Origin (High) - 2 pips

BUY Zone (bullish displacement):
├── Top = Displacement Origin (Low) + 2 pips
└── Bottom = Displacement Origin (Low) - 2 pips
```

**Note**: This differs from fractal zones which use candle body (close-to-high or close-to-low). PRE-zones use fixed 4-pip width for consistency.

### Zone Invalidation Rules (Detailed)

**Invalidation occurs when:**

```
SELL Zone Invalidated:
  M1 candle body closes ABOVE zone top
  (close > ZoneTopPrice AND open > ZoneTopPrice)

BUY Zone Invalidated:
  M1 candle body closes BELOW zone bottom
  (close < ZoneBottomPrice AND open < ZoneBottomPrice)
```

This matches existing `ProcessBreakoutEntry()` invalidation logic exactly.

**Invalidation applies to ALL zone states** (PRE, VALID, ARMED).

### ARMED Zone Expiry Behavior

**Rule**: ARMED zones remain active until **entry or invalidation**, regardless of expiry timer.

```
IF Zone.State == Armed:
    Ignore expiry timer
    Zone remains active until:
      - Entry is triggered (trade executed)
      - OR Invalidation occurs (wrong-direction breakout)
```

### Overlapping Zones Rule

**Only one zone per direction exists:**
- If new PRE-zone created while existing zone active:
  - Compare scores
  - Keep higher-scoring zone
  - Expire lower-scoring zone
- System tracks ONE `activeZone` at a time

### Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| PRE-Zone Expiry (min) | 60 | 30-120 | Time until PRE-zone expires |
| VALID-Zone Expiry (min) | 120 | 60-240 | Time until VALID-zone expires |
| Fractal Zone Tolerance (pips) | 5 | 2-10 | Max distance for fractal to confirm zone |

---

## Component 4: PRE-Zone Scoring System

### Score Formula

PRE-zones use a different scoring formula than fractal zones:

```
PRE-Zone Score =
    (DisplacementStrength × 0.40) +
    (FVGQuality × 0.25) +
    (SessionAlignment × 0.25) +
    (OptimalPeriod × 0.10)
```

### Component Calculations

#### Displacement Strength (40%)

```
ATRMultiple = CandleBodySize / ATR(14)

Score:
  ATRMultiple >= 3.0  → 1.0 (exceptional)
  ATRMultiple >= 2.5  → 0.9
  ATRMultiple >= 2.0  → 0.8
  ATRMultiple >= 1.5  → 0.7 (minimum)
  ATRMultiple < 1.5   → 0.0 (not a displacement)
```

#### FVG Quality (25%)

```
GapSizeInPips = (TopPrice - BottomPrice) / PipSize

Score:
  GapSize >= 5.0 pips  → 1.0 (large gap)
  GapSize >= 3.0 pips  → 0.8
  GapSize >= 2.0 pips  → 0.6
  GapSize >= 1.5 pips  → 0.5 (minimum)
  GapSize < 1.5 pips   → 0.0 (filtered out)
```

#### Session Alignment (25%)

**New overload function** (existing function requires swingIndex):

```csharp
// NEW overload for PRE-zones
private double CalculateSessionAlignmentForZone(double zonePrice, DateTime zoneTime, string mode)
{
    // Get session that contains zoneTime
    SessionLevels session = GetSessionForTime(zoneTime);
    if (session == null) return 0.5;

    // Check alignment with session high/low
    double targetLevel = mode == "SELL" ? session.High : session.Low;
    double distancePips = Math.Abs(zonePrice - targetLevel) / Symbol.PipSize;

    if (distancePips <= 0) return 1.0;      // AT session level
    if (distancePips <= 5) return 0.85;     // NEAR
    if (distancePips <= 10) return 0.7;     // CLOSE
    return 0.5;                              // Not aligned
}
```

#### Optimal Period (10%)

**Uses existing `GetOptimalPeriod()` function with positive-only scores:**

```
BestOverlap (13:00-17:00)    → 1.0
GoodLondonOpen (08:00-12:00) → 0.75
None (neutral times)          → 0.5
DangerDeadZone (04:00-08:00) → 0.25
DangerLateNY (20:00-00:00)   → 0.25
```

**Note**: PRE-zone scoring uses **positive values only** (0.25 minimum), unlike existing session alignment which can return -0.5 for danger zones. This is intentional - PRE-zones still form during danger zones but with lower scores.

### Minimum Score Threshold

PRE-zones require minimum score of **0.50** (lower than fractal's 0.60 since we want to catch early moves).

---

## Component 5: Integration with Existing System

### M15 Bar Processing Flow

```
OnBar (M15):
│
├── IF EnablePreZoneSystem == FALSE:
│   └── Skip to step 6 (use fractal-only mode)
│
├── 1. UpdateH1Levels()              [NO CHANGE]
├── 2. UpdateM15Levels()             [NO CHANGE]
├── 3. UpdateSessionTracking()       [NO CHANGE]
├── 4. DetectDisplacement()          [NEW]
├── 5. DetectFVGs()                  [UPGRADED]
├── 6. DetectTrendMode()             [NO CHANGE]
│
├── 7. ZONE CREATION LOGIC           [NEW]
│   │
│   ├── IF EnablePreZoneSystem AND Displacement + High-Quality FVG:
│   │   └── CreatePreZone()
│   │   └── Sync to legacy variables (swingTopPrice, etc.)
│   │
│   ├── ELSE IF Williams Fractal found:
│   │   ├── IF PRE-zone exists within tolerance:
│   │   │   └── UpgradeToValidZone()
│   │   └── ELSE:
│   │       └── CreateFractalZone() (fallback)
│   │       └── Sync to legacy variables
│   │
│   └── UpdateZoneStates()
│       ├── Check expiry (skip if ARMED)
│       ├── Check proximity (arm zones)
│       └── Check invalidation
│       └── Sync state to legacy variables
│
└── 8. DrawSwingRectangle()          [MODIFIED - color by state]
```

### Legacy Variable Sync Function

```csharp
private void SyncZoneToLegacyVariables()
{
    if (activeZone != null && activeZone.State != ZoneState.Expired
        && activeZone.State != ZoneState.Invalidated)
    {
        swingTopPrice = activeZone.TopPrice;
        swingBottomPrice = activeZone.BottomPrice;
        hasValidRectangle = true;
        hasActiveSwing = (activeZone.State == ZoneState.Armed);
        currentMode = activeZone.Mode;
        rectangleExpiryTime = activeZone.ExpiryTime;
    }
    else
    {
        hasValidRectangle = false;
        hasActiveSwing = false;
    }
}
```

### Priority Rules

| Scenario | Action |
|----------|--------|
| PRE-zone and Fractal within tolerance | Upgrade PRE to VALID |
| PRE-zone and Fractal NOT within tolerance | Keep higher scoring, expire other |
| Multiple PRE-zones same direction | Keep highest scoring, expire others |
| No PRE-zone, Fractal found | Create fractal zone (fallback) |
| EnablePreZoneSystem = false | Use fractal zones only |

### Entry Logic Integration

**Entry logic is UNCHANGED.** The `SyncZoneToLegacyVariables()` function populates the existing variables that entry logic reads:

- `ProcessBreakoutEntry()` reads `swingTopPrice`, `swingBottomPrice` ← populated from `activeZone`
- `ProcessRetestEntry()` reads `swingTopPrice`, `swingBottomPrice` ← populated from `activeZone`
- `ExecuteBuyTrade()` / `ExecuteSellTrade()` - No changes needed

---

## New Functions to Implement

| Function | Purpose |
|----------|---------|
| `DetectDisplacement()` | Scan M15 bar for impulse candle |
| `IsDisplacementCandle()` | Check if bar meets displacement criteria |
| `CreatePreZone()` | Create PRE-zone from displacement + FVG |
| `UpgradeToValidZone()` | Upgrade PRE → VALID when fractal confirms |
| `UpdateZoneStates()` | Manage zone lifecycle (expiry, arming, invalidation) |
| `CalculatePreZoneScore()` | New scoring formula for PRE-zones |
| `CalculateDisplacementStrength()` | Score based on ATR multiple |
| `CalculateFVGQuality()` | Score based on gap size |
| `CalculateSessionAlignmentForZone()` | Session alignment using zone price/time |
| `GetActiveZone()` | Return current tradeable zone (or null) |
| `SyncZoneToLegacyVariables()` | Populate swingTopPrice, etc. from activeZone |

## Modified Functions

| Function | Modification |
|----------|-------------|
| `OnStart()` | Initialize ATR indicator |
| `DetectFVGs()` | Add min size filter, max age filter, IsHighQuality flag |
| `DrawSwingRectangle()` | Color-code by zone state (PRE=Yellow, VALID=Blue, ARMED=Green) |

## Unchanged Functions

- `UpdateH1Levels()`
- `UpdateM15Levels()`
- `UpdateSessionTracking()`
- `DetectTrendMode()`
- `FindSignificantSwing()` (still used as fallback)
- `CalculateSwingScore()` (still used for fractal zones)
- `CalculateSessionAlignment()` (kept for fractal zones)
- `GetOptimalPeriod()` (reused)
- `ProcessEntryLogic()`
- `ProcessBreakoutEntry()` ← reads legacy variables, unchanged
- `ProcessRetestEntry()` ← reads legacy variables, unchanged
- `ExecuteBuyTrade()`
- `ExecuteSellTrade()`

---

## Console Output

### Displacement Detection
```
[Displacement] Bullish impulse at 14:30 | Size: 18.5 pips | ATR x 2.1
[Displacement] Bearish impulse at 15:45 | Size: 22.0 pips | ATR x 2.5
```

### FVG Detection
```
[FVG] High-quality Bullish gap | Zone: 1.09500 - 1.09685 | Size: 18.5 pips
[FVG] Filtered: gap too small (0.8 pips < 1.5 min)
```

### Zone Lifecycle
```
[PRE-Zone] Created SELL zone | Price: 1.09500-1.09540 | Score: 0.78 | Expiry: 15:30
[PRE-Zone] Scoring: Disp=0.85 FVG=0.72 Session=0.85 Period=1.0
[Zone] Upgraded to VALID | Fractal confirmed at bar 145 | New expiry: 16:30
[Zone] ARMED | Price within 8 pips of zone
[Zone] Expired | No entry triggered | Was: PRE-Zone at 1.09520
[Zone] Invalidated | Body closed above zone top
```

---

## Parameters Summary

### New Parameters

| Parameter | Default | Group |
|-----------|---------|-------|
| Enable PRE-Zone System | true | Displacement Detection |
| ATR Period | 14 | Displacement Detection |
| ATR Multiplier | 1.5 | Displacement Detection |
| PRE-Zone Expiry (min) | 60 | Displacement Detection |
| VALID-Zone Expiry (min) | 120 | Displacement Detection |
| Fractal Zone Tolerance (pips) | 5 | Displacement Detection |
| Min FVG Size (pips) | 1.5 | FVG Detection |
| FVG Max Age (bars) | 30 | FVG Detection |

### Existing Parameters (Unchanged)

All existing parameters remain functional for fallback fractal system.

---

## Testing Approach

### Unit Tests

1. **Displacement Detection**
   - Verify ATR indicator initialization
   - Verify ATR calculation
   - Verify impulse size threshold
   - Verify origin price (high for bearish, low for bullish)

2. **FVG Filtering**
   - Verify minimum size filter
   - Verify max age filter
   - Verify IsHighQuality flag when Candle B is displacement

3. **Zone Lifecycle**
   - PRE-zone creation timing (one bar after displacement)
   - VALID upgrade on fractal confirmation within tolerance
   - ARMED state on price proximity
   - Expiry at correct times (skip if ARMED)
   - Invalidation on wrong-direction breakout

4. **Legacy Variable Sync**
   - Verify swingTopPrice matches activeZone.TopPrice
   - Verify entry logic works unchanged

### Integration Tests

1. **Backtest: PRE-Zone vs Fractal Timing**
   - Measure: How many minutes earlier are PRE-zones created?
   - Expected: 15 minutes earlier on average

2. **Backtest: Hit Rate Comparison**
   - PRE-zone entries vs Fractal-only entries
   - Expected: More entries captured (price hadn't moved away)

3. **Backtest: Win Rate Comparison**
   - PRE-zone trades vs Fractal-only trades
   - Expected: Similar or better (filtered by displacement + FVG)

---

## Success Criteria

1. PRE-zones created **one bar after displacement** (15 min faster than fractal)
2. Existing fractal system works as fallback when no PRE-zone or when disabled
3. Entry logic unchanged - seamless integration via legacy variable sync
4. Console output shows clear zone lifecycle
5. Backtest shows improved entry timing

---

## Estimated Implementation

| Component | Lines of Code |
|-----------|---------------|
| Displacement Detection | ~150 |
| FVG Enhancements | ~50 |
| Zone Lifecycle Management | ~200 |
| PRE-Zone Scoring | ~80 |
| Legacy Variable Sync | ~30 |
| **Total New Code** | **~510 lines** |

---

## Files Modified

- `Jcamp_1M_scalping.cs` - All changes in single file

---

**Design Complete. Ready for Implementation Planning.**

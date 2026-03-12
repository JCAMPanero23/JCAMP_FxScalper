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
M15 Displacement → FVG Forms → Create PRE-Zone (IMMEDIATE) → Price enters → Trade
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
    bool IsHighQuality;        // Formed during displacement candle
    double GapSizeInPips;      // For filtering
    int DisplacementBarIndex;  // Links to impulse candle (-1 if none)
}
```

### Filtering Logic

```
1. Calculate gap size in pips
2. IF gap size < MinFVGSizePips (1.5) → Discard
3. IF age > FVGMaxAgeBars (30) → Discard
4. IF formed during displacement → Mark IsHighQuality = true
5. Only IsHighQuality FVGs trigger PRE-zone creation
```

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
│    [Williams Fractal confirms at zone price]                     │
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

### Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| PRE-Zone Expiry (min) | 60 | 30-120 | Time until PRE-zone expires |
| VALID-Zone Expiry (min) | 120 | 60-240 | Time until VALID-zone expires |

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

**Reuse existing `CalculateSessionAlignment()` function.**

```
Check if zone price aligns with session high/low:
  AT session level (±0 pips)    → 1.0
  NEAR session level (±5 pips)  → 0.85
  CLOSE to session (±10 pips)   → 0.7
  Not aligned                    → 0.5
```

#### Optimal Period (10%)

**Reuse existing `GetOptimalPeriod()` function.**

```
BestOverlap (13:00-17:00)    → 1.0
GoodLondonOpen (08:00-12:00) → 0.75
None (neutral times)          → 0.5
DangerDeadZone (04:00-08:00) → 0.25
DangerLateNY (20:00-00:00)   → 0.25
```

### Minimum Score Threshold

PRE-zones require minimum score of **0.50** (lower than fractal's 0.60 since we want to catch early moves).

---

## Component 5: Integration with Existing System

### M15 Bar Processing Flow

```
OnBar (M15):
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
│   ├── IF Displacement + High-Quality FVG:
│   │   └── CreatePreZone()
│   │
│   ├── ELSE IF Williams Fractal found:
│   │   ├── IF PRE-zone exists at price:
│   │   │   └── UpgradeToValidZone()
│   │   └── ELSE:
│   │       └── CreateFractalZone() (fallback)
│   │
│   └── UpdateZoneStates()
│       ├── Check expiry
│       ├── Check proximity (arm zones)
│       └── Check invalidation
│
└── 8. UpdateSwingZone()             [MODIFIED - uses TradingZone]
```

### Priority Rules

| Scenario | Action |
|----------|--------|
| PRE-zone and Fractal at same level | Upgrade PRE to VALID |
| PRE-zone and Fractal at different levels | Keep highest scoring only |
| Multiple PRE-zones | Keep highest scoring, expire others |
| No PRE-zone, Fractal found | Use fractal zone (fallback) |

### Entry Logic

**No changes to entry logic.** Existing functions work with any zone type:

- `ProcessBreakoutEntry()` - Uses zone's TopPrice/BottomPrice
- `ProcessRetestEntry()` - Uses zone's TopPrice/BottomPrice
- `ExecuteBuyTrade()` / `ExecuteSellTrade()` - Unchanged

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
| `GetActiveZone()` | Return current tradeable zone (if any) |

## Modified Functions

| Function | Modification |
|----------|-------------|
| `DetectFVGs()` | Add min size filter, max age filter, IsHighQuality flag |
| `UpdateSwingZone()` | Use TradingZone instead of raw rectangle values |
| `DrawSwingRectangle()` | Color-code by zone state (PRE/VALID/ARMED) |

## Unchanged Functions

- `UpdateH1Levels()`
- `UpdateM15Levels()`
- `UpdateSessionTracking()`
- `DetectTrendMode()`
- `FindSignificantSwing()` (still used as fallback)
- `CalculateSwingScore()` (still used for fractal zones)
- `CalculateSessionAlignment()` (reused for PRE-zones)
- `GetOptimalPeriod()` (reused for PRE-zones)
- `ProcessEntryLogic()`
- `ProcessBreakoutEntry()`
- `ProcessRetestEntry()`
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
[Zone] Invalidated | Wrong-direction breakout
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
| Min FVG Size (pips) | 1.5 | FVG Detection |
| FVG Max Age (bars) | 30 | FVG Detection |

### Existing Parameters (Unchanged)

All existing parameters remain functional for fallback fractal system.

---

## Testing Approach

### Unit Tests

1. **Displacement Detection**
   - Verify ATR calculation
   - Verify impulse size threshold
   - Verify origin price (high for bearish, low for bullish)

2. **FVG Filtering**
   - Verify minimum size filter
   - Verify max age filter
   - Verify IsHighQuality flag

3. **Zone Lifecycle**
   - PRE-zone creation timing
   - VALID upgrade on fractal confirmation
   - ARMED state on price proximity
   - Expiry at correct times
   - Invalidation on wrong-direction breakout

### Integration Tests

1. **Backtest: PRE-Zone vs Fractal Timing**
   - Measure: How many minutes earlier are PRE-zones created?
   - Expected: 15-30 minutes earlier on average

2. **Backtest: Hit Rate Comparison**
   - PRE-zone entries vs Fractal-only entries
   - Expected: More entries captured (price hadn't moved away)

3. **Backtest: Win Rate Comparison**
   - PRE-zone trades vs Fractal-only trades
   - Expected: Similar or better (filtered by displacement + FVG)

---

## Success Criteria

1. PRE-zones created **immediately** after displacement + FVG (no 2-bar delay)
2. Existing fractal system works as fallback when no PRE-zone
3. Entry logic unchanged - seamless integration
4. Console output shows clear zone lifecycle
5. Backtest shows improved entry timing

---

## Estimated Implementation

| Component | Lines of Code |
|-----------|---------------|
| Displacement Detection | ~150 |
| FVG Enhancements | ~50 |
| Zone Lifecycle Management | ~200 |
| PRE-Zone Scoring | ~50 |
| **Total New Code** | **~450 lines** |

---

## Files Modified

- `Jcamp_1M_scalping.cs` - All changes in single file

---

**Design Complete. Ready for Implementation Planning.**

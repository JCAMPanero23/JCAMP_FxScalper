# Enhanced Entry System v2.0 Design Specification

**Version:** 2.0.0
**Date:** 2026-03-16
**Status:** Approved for Implementation

## Overview

This document specifies structural changes to the Jcamp 1M Scalping bot to address a 72% loss rate observed with proper SL/TP implementation. The core issues identified:

1. Fixed 4-pip zone width (arbitrary, too small)
2. 8-pip SL distance (within normal M1 noise)
3. No entry confirmation (pending orders fill without validation)
4. No momentum/trend filtering beyond basic SMA

## Goals

- Reduce loss rate from 72% to target <50%
- Use real market structure (FVG boundaries) instead of fixed zones
- Require rejection candle confirmation before entry
- Adapt SL to market volatility via ATR
- Filter entries with RSI compression-expansion pattern
- Maintain configurability to avoid overfitting

## Non-Goals

- Complete strategy rewrite
- Multi-symbol support
- New exit logic (existing Chandelier SL retained)

---

## Design

### 1. Versioning System

Add version tracking for backtest traceability:

```csharp
private const string BOT_VERSION = "2.0.0";
private const string VERSION_DATE = "2026-03-16";
private const string VERSION_NOTES = "FVG zones, rejection confirmation, RSI compression-expansion, ATR SL";
```

Log version on startup. Versioning scheme:
- Major: Breaking changes to entry logic
- Minor: New features/parameters
- Patch: Bug fixes

### 2. FVG-Based Zone Creation

**Problem:** Current zone is always 4 pips regardless of market structure.

**Solution:** Use actual FVG boundaries with configurable size percentage.

```csharp
double fvgHeight = fvg.TopPrice - fvg.BottomPrice;
double zoneCenter = (fvg.TopPrice + fvg.BottomPrice) / 2;
double adjustedHeight = fvgHeight * (FVGZoneSizePercent / 100.0);

double topPrice = zoneCenter + (adjustedHeight / 2);
double bottomPrice = zoneCenter - (adjustedHeight / 2);
```

| FVGZoneSizePercent | Behavior |
|-------------------|----------|
| 50% | Inner half of FVG (tighter) |
| 100% | Full FVG boundaries (default) |
| 150% | FVG + extension (catches wicks) |

### 3. Rejection Candle Confirmation

**Problem:** Pending orders fill without price confirmation.

**Solution:** Require rejection pattern before order can fill successfully.

**Supported Patterns (all configurable):**

| Pattern | BUY Signal | SELL Signal |
|---------|------------|-------------|
| Wick Rejection | Lower wick > body | Upper wick > body |
| Pin Bar | Lower wick ≥ MinWickRatio × body | Upper wick ≥ MinWickRatio × body |
| Engulfing | Bullish candle engulfs previous | Bearish candle engulfs previous |

**Validation Logic:**
- On each bar, check if rejection candle formed
- Cancel pending order if no rejection within MaxBarsWithoutRejection (default: 5)
- Mark order as "rejection confirmed" when pattern detected

### 4. ATR-Based Stop Loss

**Problem:** Fixed 8-pip SL is within M1 noise range.

**Solution:** SL = MAX(zone boundary + buffer, ATR × multiplier)

```csharp
double atrBasedSL = atrM1.Result.LastValue * SLATRMultiplier;
double zoneBoundarySL = zone.FVGBottomPrice - (SLBufferPips * Symbol.PipSize);
double slPrice = Math.Min(zoneBoundarySL, entryPrice - atrBasedSL);  // For BUY
```

SL adapts to volatility:
- Low volatility: ~12 pips (ATR-based)
- Normal: ~18 pips (ATR-based)
- High volatility: ~30 pips (ATR-based)
- Large FVG: Uses zone boundary if larger

### 5. RSI Compression-Expansion Filter

**Concept:** Enter when RSI "coils" in neutral zone then expands in trade direction.

**RSI Scale:**
```
80+ ──────────── Extreme Overbought (no entry)
60-80 ────────── BUY Expansion Zone ✓
40-60 ────────── Compression Zone (energy building)
20-40 ────────── SELL Expansion Zone ✓
0-20 ─────────── Extreme Oversold (no entry)
```

**Detection Logic:**
1. Count bars where RSI was within 40-60 in last 15 bars
2. Require minimum 6 bars of compression
3. Current RSI must be in expansion zone (60-80 for BUY, 20-40 for SELL)

### 6. Dual SMA Trend Filter

**Existing:** SMA 200 trend filter

**Enhancement:** Add configurable fast SMA (20-100), require price above/below BOTH.

| Mode | Condition |
|------|-----------|
| BUY | Price > SMA200 AND Price > SMAFast |
| SELL | Price < SMA200 AND Price < SMAFast |

### 7. Pending Order Validation Flow

**New Flow:**
```
Zone Armed → Place pending order → Each bar:
├── Rejection candle formed? → Mark confirmed
├── RSI compression-expansion valid? → Keep order
├── Trend filters valid? → Keep order
├── Max bars exceeded without rejection? → Cancel
└── Filters invalid? → Cancel
```

---

## New Parameters

### Zone Configuration
| Parameter | Type | Default | Min | Max | Step |
|-----------|------|---------|-----|-----|------|
| FVGZoneSizePercent | int | 100 | 50 | 150 | 25 |

### Rejection Patterns
| Parameter | Type | Default | Min | Max | Step |
|-----------|------|---------|-----|-----|------|
| EnableWickRejection | bool | true | - | - | - |
| EnableEngulfingPattern | bool | true | - | - | - |
| EnablePinBar | bool | true | - | - | - |
| MinWickRatio | double | 2.0 | 1.5 | 3.0 | 0.5 |
| MaxBarsWithoutRejection | int | 5 | 3 | 10 | 1 |

### ATR Stop Loss
| Parameter | Type | Default | Min | Max | Step |
|-----------|------|---------|-----|-----|------|
| SLATRMultiplier | double | 1.5 | 1.0 | 2.5 | 0.25 |

### RSI Compression-Expansion
| Parameter | Type | Default | Min | Max | Step |
|-----------|------|---------|-----|-----|------|
| EnableRSICompression | bool | true | - | - | - |
| RSIPeriod | int | 7 | 5 | 14 | 1 |
| RSICompressionLow | int | 40 | 35 | 45 | 5 |
| RSICompressionHigh | int | 60 | 55 | 65 | 5 |
| RSICompressionMinBars | int | 6 | 4 | 10 | 2 |
| RSICompressionLookback | int | 15 | 10 | 25 | 5 |
| RSIExpansionBuyMin | int | 60 | - | - | - |
| RSIExpansionBuyMax | int | 80 | - | - | - |
| RSIExpansionSellMin | int | 20 | - | - | - |
| RSIExpansionSellMax | int | 40 | - | - | - |

### Dual SMA
| Parameter | Type | Default | Min | Max | Step |
|-----------|------|---------|-----|-----|------|
| EnableDualSMA | bool | true | - | - | - |
| FastSMAPeriod | int | 50 | 20 | 100 | 10 |

---

## Implementation Approach

**Method:** Incremental Refactor (Approach A)
- Modify existing methods one at a time
- Add new parameters alongside existing ones
- Test each change with backtests
- Version tracking for rollback capability

**Files to Modify:**
- `Jcamp_1M_scalping.cs` (main bot file)

**Estimated Changes:**
- ~15-20 method modifications
- ~18 new parameters
- 3 new indicator initializations (RSI, Fast SMA, tracking)
- 5 new helper methods (rejection detection, RSI compression, validation)

---

## Testing Strategy

1. **Unit Verification:** Each new method logged and tested individually
2. **Backtest Comparison:** Same date range (01/04/2024 - 30/06/2024)
3. **Parameter Optimization:** Start with defaults, then optimize key parameters
4. **A/B Metrics:**
   - Win rate: Target >50% (currently 28%)
   - Profit factor: Target >1.5 (currently 1.06)
   - Trade count: Expect reduction (quality over quantity)

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Over-filtering reduces trades too much | Low trade count | All filters configurable, can disable |
| RSI compression too strict | Missed opportunities | Adjustable lookback and min bars |
| ATR SL too wide | Poor RR ratio | Multiplier configurable (1.0-2.5) |
| Rejection detection false positives | Bad entries | Multiple patterns, configurable |

---

## Success Criteria

- [ ] Win rate improves from 28% to >45%
- [ ] Profit factor improves from 1.06 to >1.3
- [ ] No positions opened without SL/TP
- [ ] All new parameters logged and traceable
- [ ] Version number logged on startup

---

## Appendix: Complete Entry Flow

```
STEP 1: ZONE CREATION
├── Displacement detected (M15 or M1)
├── FVG found matching displacement
├── Zone = FVG boundaries × FVGZoneSizePercent
└── Zone scored and validated

STEP 2: ZONE ARMED
├── Price within MaxDistanceToArm of zone
├── Check Dual SMA filter
├── Check RSI Compression-Expansion filter
└── All pass? → Place pending order

STEP 3: PENDING ORDER VALIDATION (Each bar)
├── Check rejection candle (Wick/Engulfing/PinBar)
├── Re-validate filters
├── Cancel if no rejection in 5 bars
└── Cancel if filters invalid

STEP 4: ORDER FILLS
├── Entry at zone boundary + offset
├── SL = MAX(FVG boundary + buffer, ATR × multiplier)
├── TP = Entry + (SL distance × RR ratio)
└── Chandelier trailing activates at X% of TP
```

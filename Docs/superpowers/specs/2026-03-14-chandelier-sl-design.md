# Chandelier Stop Loss with Trailing TP

**Date:** 2026-03-14
**Status:** Draft
**Branch:** enhance-entry-system

## Overview

Add a chandelier-style trailing stop loss that activates after price reaches a configurable fraction of the risk-reward distance. Once activated, the SL trails using ATR-based chandelier calculation. Optionally, the TP can also trail ahead of the chandelier SL.

## Problem Statement

The current system uses fixed SL/TP set at entry. While this provides discipline, it doesn't:
- Lock in profits when trades move favorably
- Allow extended profit capture on strong momentum moves
- Protect gains after significant favorable movement

## Design

### Activation Logic

The chandelier SL activates when price reaches a configurable percentage of the RR distance:

```
Activation Price (LONG)  = Entry + (TP - Entry) × ChandelierActivationRR
Activation Price (SHORT) = Entry - (Entry - TP) × ChandelierActivationRR
```

**Example (LONG):**
- Entry: 1.1000
- SL: 1.0980 (20 pip risk)
- TP: 1.1060 (60 pip reward, 3R)
- ChandelierActivationRR: 0.75
- Activation at: 1.1000 + (60 × 0.75) = 1.1045

### Three-Phase Behavior

#### Phase 1: Pre-Activation
- Trade runs with original fixed SL and TP
- No chandelier logic active

#### Phase 2: Activation (Price hits X% of RR)
- SL immediately moves to **Breakeven + Commission**
  - LONG: `New SL = Entry + Commission (in price)`
  - SHORT: `New SL = Entry - Commission (in price)`
- Commission read from `Symbol.Commission` (per-lot, converted to price)
- **TP remains at original level** (no change yet)
- This is the "floor" - SL cannot move backwards from here

#### Phase 3: Chandelier Trailing (Chandelier value exceeds BE+commission)
- Once the chandelier calculation produces a value better than BE+commission:
  - **SL trails using chandelier formula**
  - **TP starts trailing** (if TrailingTP mode enabled)

### Chandelier Calculation

Using M1 timeframe with existing ATR settings:

```
LONG:  Chandelier SL = Highest High (lookback bars) - (ATR × ATRMultiplier)
SHORT: Chandelier SL = Lowest Low (lookback bars) + (ATR × ATRMultiplier)
```

- **ATR Period:** Existing `ATRPeriod` parameter (default 14)
- **ATR Multiplier:** Existing `ATRMultiplier` parameter (default 1.5)
- **Lookback:** New configurable `ChandelierLookback` parameter

### TP Modes

Three modes controlled by `ChandelierTPMode` parameter:

| Mode | Behavior |
|------|----------|
| `KeepOriginal` | TP stays at original level throughout |
| `RemoveTP` | TP is removed on activation; exit only via chandelier SL |
| `TrailingTP` | TP trails ahead of chandelier SL by `TrailingTPOffset` pips |

**TrailingTP Logic:**
- TP only starts moving when chandelier SL moves beyond BE+commission
- Formula: `Trailing TP = Chandelier SL + TrailingTPOffset × PipSize` (LONG)
- Formula: `Trailing TP = Chandelier SL - TrailingTPOffset × PipSize` (SHORT)
- TP only moves in favorable direction (never backwards)

### Floor Protection

The SL can only move in the favorable direction:
- LONG: SL can only increase (move up)
- SHORT: SL can only decrease (move down)

This prevents the chandelier from widening the stop during consolidation.

## Parameters

### New Parameters

| Parameter | Group | Default | Min | Max | Step | Description |
|-----------|-------|---------|-----|-----|------|-------------|
| `EnableChandelierSL` | Chandelier SL | true | - | - | - | Enable/disable chandelier trailing |
| `ChandelierActivationRR` | Chandelier SL | 0.75 | 0.5 | 0.85 | 0.05 | RR fraction to activate chandelier |
| `ChandelierLookback` | Chandelier SL | 22 | 10 | 30 | 2 | Bars for highest high / lowest low |
| `ChandelierTPMode` | Chandelier SL | TrailingTP | - | - | - | KeepOriginal, RemoveTP, TrailingTP |
| `TrailingTPOffset` | Chandelier SL | 10 | 5 | 20 | 1 | Pips ahead of chandelier SL (TrailingTP mode) |

### Existing Parameters Used

| Parameter | Current Default | Usage |
|-----------|-----------------|-------|
| `ATRPeriod` | 14 | ATR calculation for chandelier |
| `ATRMultiplier` | 1.5 | ATR multiplier for chandelier distance |

## Implementation

### Position Tracking

Add tracking fields per position:
```csharp
private Dictionary<int, ChandelierState> _chandelierStates;

private class ChandelierState
{
    public bool IsActivated { get; set; }
    public double EntryPrice { get; set; }
    public double OriginalTP { get; set; }
    public double OriginalSL { get; set; }
    public double BreakevenPrice { get; set; }  // Entry + commission
    public double CurrentTrailingSL { get; set; }
    public double CurrentTrailingTP { get; set; }
    public bool TPTrailingStarted { get; set; }
}
```

### Event Handlers

Add `OnTick()` or `OnBar()` handler to:
1. Check activation condition for each position
2. Calculate chandelier values
3. Update SL/TP via `ModifyPosition()`

### Key Methods

```csharp
private void CheckChandelierActivation(Position position, ChandelierState state)
private double CalculateChandelierSL(TradeType tradeType)
private void UpdatePositionStops(Position position, ChandelierState state)
private double GetCommissionInPrice(Position position)
```

### SL/TP Modification

Use cTrader's `ModifyPosition()` API:
```csharp
ModifyPosition(position, newStopLoss, newTakeProfit);
```

## Edge Cases

1. **Commission is 0 (spread-based broker):** BE+commission = BE exactly
2. **Chandelier wider than original SL:** Floor protection prevents this
3. **Position closed before activation:** No chandelier logic runs
4. **Multiple positions:** Each position tracked independently
5. **Lookback exceeds available bars:** Use available bars (min 1)

## Testing

1. **Activation test:** Verify SL moves to BE+commission at correct price level
2. **Trailing test:** Verify chandelier calculation updates SL correctly
3. **Floor test:** Verify SL never moves backwards
4. **TP modes:** Test all three modes (KeepOriginal, RemoveTP, TrailingTP)
5. **TrailingTP start:** Verify TP only moves after chandelier exceeds BE

## Risks

- **Slippage on SL modification:** Broker may reject rapid modifications
- **Commission calculation:** Verify `Symbol.Commission` returns correct value
- **Bar data availability:** Ensure M1 bars available for lookback

## Success Criteria

1. Chandelier activates at correct RR percentage
2. SL starts at BE+commission (never below)
3. Chandelier trails correctly using ATR formula
4. TP behaves according to selected mode
5. Floor protection prevents SL from moving backwards
6. Works correctly for both LONG and SHORT trades

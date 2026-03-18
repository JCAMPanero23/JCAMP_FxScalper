# Exhaustion Exit Protection - v3.0

**Date:** 2026-03-18
**Status:** Design Approved
**Version:** 3.0.0
**Branch:** TBD

## Overview

Add intelligent profit protection that detects market exhaustion signals and exits positions before chandelier stop loss is hit. Uses price structure analysis (swing detection) combined with RSI divergence to identify when a favorable trend is losing momentum and likely to reverse.

## Problem Statement

The current chandelier trailing stop system (v2.0) protects profits well, but:
- Waits for price to hit trailing SL before exiting
- Can give back significant profit during reversals
- Doesn't anticipate trend exhaustion, only reacts to it
- Chandelier trails price action but doesn't read market structure

**Goal:** Exit positions proactively when market structure shows exhaustion, before price reaches trailing SL.

## Design

### Architecture Overview

The exhaustion exit logic extends the existing chandelier system by adding exhaustion pattern detection to the `ProcessChandelierStops()` flow:

```
OnBar() (M1)
  └─> ProcessChandelierStops()
       ├─> ProcessSinglePosition()
       │    ├─> Check Activation (existing)
       │    ├─> Trail Chandelier (existing)
       │    └─> [NEW] Check Exhaustion Exit
       │         ├─> Update swing history
       │         ├─> Detect pattern + RSI divergence
       │         ├─> Handle confirmation state
       │         └─> Close position if confirmed
       └─> Clean up states for closed positions
```

### Activation Conditions

Exhaustion exit only activates when **ALL** conditions are met:

1. `EnableExhaustionExit` = true (user enabled)
2. Chandelier is active (`IsActivated = true`)
3. Chandelier has made `MinChandelierMovesBeforeExit` trailing moves (default: 2)
   - Excludes the initial BE activation move
   - Counts actual SL trail increments
   - Ensures we only exit positions that have locked in profit

**Rationale:** We only want to exit winning positions that are already protected. If chandelier hasn't trailed at least 2 times, let the position develop normally.

### Exit Flow States

```
Monitoring → (Pattern + Divergence Detected) → PatternDetected
                                                      ↓
                                    (Next Bar: Check invalidation)
                                                      ↓
                              ┌─────────────────────┴──────────────────────┐
                              ↓                                            ↓
                    (Price breaks level)                         (Price respects level)
                              ↓                                            ↓
                        Invalidated                                   Confirmed
                              ↓                                            ↓
                    Reset → Monitoring                            Close Position
```

**State Definitions:**
- **Monitoring:** Continuously tracking swings and RSI, waiting for pattern
- **PatternDetected:** 2 consecutive divergent swings found, waiting for confirmation bar
- **Confirmed:** Next bar confirms reversal continuing → close position immediately
- **Invalidated:** Next bar breaks pattern → reset to monitoring (false alarm)

## Exhaustion Detection Logic

### 1. Swing Detection (N-Bar Method)

**For SELL positions - Detect Higher Lows (HL):**

A swing low is the lowest low within the last `ExhaustionSwingBars` (default: 8) bars.

```csharp
SwingLow = Min(Bars.LowPrices[i-N+1 ... i])
```

Track last 3 swing lows in history. Detect pattern:
```
HL1: SwingLow[1] > SwingLow[0]  (price making higher low)
HL2: SwingLow[2] > SwingLow[1]  (price making another higher low)
```

**For BUY positions - Detect Lower Highs (LH):**

A swing high is the highest high within the last `ExhaustionSwingBars` bars.

```csharp
SwingHigh = Max(Bars.HighPrices[i-N+1 ... i])
```

Track last 3 swing highs. Detect pattern:
```
LH1: SwingHigh[1] < SwingHigh[0]  (price making lower high)
LH2: SwingHigh[2] < SwingHigh[1]  (price making another lower high)
```

**Why N-bar swings?** More robust than simple bar-to-bar comparison. Filters noise while still catching M1 exhaustion signals.

### 2. RSI Divergence Detection

Use separate RSI indicator (`ExhaustionRSIPeriod` = 14) for divergence analysis.

**For SELL positions - Bullish Divergence (Exhaustion of Downtrend):**

Price making Higher Lows BUT RSI making Lower Lows:

```
Price: HL1 (Swing[1] > Swing[0]) AND HL2 (Swing[2] > Swing[1])
RSI:   LL1 (RSI[1] < RSI[0])     AND LL2 (RSI[2] < RSI[1])
```

**Visual Example:**
```
Price:  ↗   ↗↗  (Higher Lows - bullish structure forming)
RSI:    ↘   ↘↘  (Lower Lows - momentum weakening)
→ Bullish divergence: SELL position exhausting
```

**For BUY positions - Bearish Divergence (Exhaustion of Uptrend):**

Price making Lower Highs BUT RSI making Higher Highs:

```
Price: LH1 (Swing[1] < Swing[0]) AND LH2 (Swing[2] < Swing[1])
RSI:   HH1 (RSI[1] > RSI[0])     AND HH2 (RSI[2] > RSI[1])
```

**Visual Example:**
```
Price:  ↘   ↘↘  (Lower Highs - bearish structure forming)
RSI:    ↗   ↗↗  (Higher Highs - momentum weakening)
→ Bearish divergence: BUY position exhausting
```

**Why RSI divergence?** Classic technical analysis signal that trend momentum is exhausting even as price structure changes. Significantly reduces false positives.

### 3. Confirmation & Invalidation

When pattern + divergence detected, enter **PatternDetected** state and wait for next bar.

**For SELL positions:**

Store `ConfirmationPrice = HL2` (the second higher low level).

On next M1 bar:
- **INVALIDATE** if `Bars.LowPrices.Last(0) < ConfirmationPrice`
  - Price broke below HL2 → downtrend resuming → false alarm
  - Reset to Monitoring, keep position open
- **CONFIRM** if `Bars.LowPrices.Last(0) >= ConfirmationPrice`
  - Price respects HL2 level → reversal continuing
  - Close position at market immediately

**For BUY positions:**

Store `ConfirmationPrice = LH2` (the second lower high level).

On next M1 bar:
- **INVALIDATE** if `Bars.HighPrices.Last(0) > ConfirmationPrice`
  - Price broke above LH2 → uptrend resuming → false alarm
  - Reset to Monitoring, keep position open
- **CONFIRM** if `Bars.HighPrices.Last(0) <= ConfirmationPrice`
  - Price respects LH2 level → reversal continuing
  - Close position at market immediately

**Why confirmation bar?** Prevents premature exits on noise. If the trend is truly reversing, price will respect the new structure. If it's a false signal, price will break through and we avoid the exit.

## State Tracking

### Extended ChandelierState Class

```csharp
private class ChandelierState
{
    // Existing fields (v2.0 - unchanged)
    public int PositionId { get; set; }
    public bool IsActivated { get; set; }
    public double EntryPrice { get; set; }
    public double OriginalTP { get; set; }
    public double OriginalSL { get; set; }
    public double ActivationPrice { get; set; }
    public double BreakevenPrice { get; set; }
    public double CurrentTrailingSL { get; set; }
    public double CurrentTrailingTP { get; set; }
    public double HighestTrailingSL { get; set; }
    public double HighestTrailingTP { get; set; }
    public bool TPTrailingStarted { get; set; }

    // NEW v3.0: Exhaustion exit tracking
    public int ChandelierMoveCount { get; set; }              // Trailing moves count (excludes BE activation)
    public List<SwingPoint> SwingHistory { get; set; }        // Last 3 swings for pattern detection
    public ExhaustionState ExhaustionStatus { get; set; }     // Current exhaustion state
    public double ConfirmationPrice { get; set; }             // HL2/LH2 level for invalidation check
    public int ConfirmationBarIndex { get; set; }             // Bar index when pattern detected
}
```

### Supporting Classes

```csharp
private class SwingPoint
{
    public double Price { get; set; }        // Swing high or low price
    public double RSIValue { get; set; }     // RSI value at that swing point
    public int BarIndex { get; set; }        // M1 bar index when swing formed
}

private enum ExhaustionState
{
    Monitoring,          // Watching for pattern
    PatternDetected,     // 2 HL/LH + divergence found, waiting confirmation
    Confirmed,           // Confirmation bar validates pattern → close position
    Invalidated          // Confirmation bar breaks pattern → reset to monitoring
}
```

### State Initialization

When position opens or chandelier activates:
```csharp
state.ChandelierMoveCount = 0;
state.SwingHistory = new List<SwingPoint>();
state.ExhaustionStatus = ExhaustionState.Monitoring;
state.ConfirmationPrice = 0;
state.ConfirmationBarIndex = 0;
```

## Implementation Details

### RSI Indicator Setup

```csharp
// In class fields
private RelativeStrengthIndex _exhaustionRSI;

// In OnStart()
_exhaustionRSI = Indicators.RelativeStrengthIndex(Bars.ClosePrices, ExhaustionRSIPeriod);
```

### Key Methods

```csharp
// Main exhaustion check (called from ProcessSinglePosition)
private void CheckExhaustionExit(Position position, ChandelierState state)

// Swing detection
private bool DetectSwingLow(int barsBack, out double swingPrice, out double rsiValue)
private bool DetectSwingHigh(int barsBack, out double swingPrice, out double rsiValue)

// Pattern recognition
private bool CheckBullishDivergence(ChandelierState state)  // SELL exhaustion
private bool CheckBearishDivergence(ChandelierState state)  // BUY exhaustion

// Confirmation logic
private void CheckSellConfirmation(Position position, ChandelierState state)
private void CheckBuyConfirmation(Position position, ChandelierState state)
```

### Chandelier Move Counter

Increment `ChandelierMoveCount` ONLY when SL actually trails (not on initial BE activation):

```csharp
// In TrailChandelierStop() after ModifyPosition() succeeds
if (state.IsActivated && slActuallyMoved)
{
    state.ChandelierMoveCount++;
}
```

Check threshold before exhaustion monitoring:
```csharp
if (state.ChandelierMoveCount < MinChandelierMovesBeforeExit)
{
    return;  // Not enough trailing moves yet, skip exhaustion check
}
```

## Parameters

### New Parameter Group

```csharp
#region Parameters - Exhaustion Exit

[Parameter("=== EXHAUSTION EXIT v3.0 ===", DefaultValue = "")]
public string ExhaustionHeader { get; set; }

[Parameter("Enable Exhaustion Exit", DefaultValue = false, Group = "Exhaustion Exit")]
public bool EnableExhaustionExit { get; set; }

[Parameter("Min Chandelier Moves", DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 1, Group = "Exhaustion Exit")]
public int MinChandelierMovesBeforeExit { get; set; }

[Parameter("Swing Lookback Bars", DefaultValue = 8, MinValue = 3, MaxValue = 15, Step = 1, Group = "Exhaustion Exit")]
public int ExhaustionSwingBars { get; set; }

[Parameter("RSI Period", DefaultValue = 14, MinValue = 6, MaxValue = 21, Step = 1, Group = "Exhaustion Exit")]
public int ExhaustionRSIPeriod { get; set; }

#endregion
```

### Parameter Summary

| Parameter | Default | Min | Max | Step | Description |
|-----------|---------|-----|-----|------|-------------|
| `EnableExhaustionExit` | false | - | - | - | Enable/disable exhaustion exit protection |
| `MinChandelierMovesBeforeExit` | 2 | 1 | 5 | 1 | Chandelier trailing moves required before exhaustion monitoring activates |
| `ExhaustionSwingBars` | 8 | 3 | 15 | 1 | M1 bars for swing detection lookback window |
| `ExhaustionRSIPeriod` | 14 | 6 | 21 | 1 | RSI period for divergence detection (separate from entry RSI-6) |

### Optimization Ranges

For backtesting optimization:

```
MinChandelierMovesBeforeExit: [1, 2, 3, 4, 5]  (5 combinations)
ExhaustionSwingBars: [5, 8, 10, 12] (4 combinations)
ExhaustionRSIPeriod: [6, 10, 14, 18] (4 combinations)

Total: 5 × 4 × 4 = 80 combinations
```

**Optimization Strategy:**
- Start with defaults (2, 8, 14)
- Sweep MinChandelierMoves first (most impactful)
- Then optimize SwingBars for noise vs responsiveness
- Finally tune RSI period for divergence sensitivity

## Edge Cases & Error Handling

### Safety Checks

**1. Insufficient Bar History**
```csharp
if (Bars.Count < ExhaustionSwingBars + 5)
{
    return;  // Not enough bars, skip exhaustion check
}
```

**2. Chandelier Not Active**
```csharp
if (!state.IsActivated)
{
    return;  // Chandelier must be active
}
```

**3. RSI Not Ready**
```csharp
if (_exhaustionRSI.Result.IsNaN())
{
    return;  // RSI not calculated yet
}
```

**4. Position Already Closed**
```csharp
var position = Positions.Find(label: BotLabel, symbolName: Symbol.Name);
if (position == null)
{
    _chandelierStates.Remove(state.PositionId);
    return;
}
```

### Edge Case Handling

| Scenario | Handling |
|----------|----------|
| **Bot restart with open position** | State lost; exhaustion resets to Monitoring (conservative, avoids false signals) |
| **Multiple positions same symbol** | Each tracked independently with separate ChandelierState |
| **Chandelier disabled mid-trade** | Exhaustion automatically disabled (requires chandelier active) |
| **Position closes naturally (TP/SL) during PatternDetected** | State cleaned up in `Positions.Closed` event handler |
| **Swing detection finds no swing** | Skip update, wait for next bar, no state change |
| **RSI values exactly equal** | No divergence (require strict inequality < or >) |
| **ConfirmationPrice == CurrentPrice exactly** | Treated as "not broken" (>= or <= checks), pattern remains valid |
| **Exhaustion disabled after pattern detected** | Pattern abandoned; no exit triggered (feature disabled takes precedence) |
| **Two swings detected same bar** | Keep most recent swing only, update history once per bar |

## Logging Strategy

### Key Events to Log

```csharp
// Chandelier move counter
Print("[EXHAUSTION] Position {0} chandelier moved {1} times (threshold: {2})",
    position.Id, state.ChandelierMoveCount, MinChandelierMovesBeforeExit);

// Swing detection
Print("[EXHAUSTION] {0} swing detected at {1:F5}, RSI: {2:F1}",
    position.TradeType == TradeType.Sell ? "Low" : "High", swingPrice, rsiValue);

// Pattern + divergence detection
Print("[EXHAUSTION] {0} Pattern: Price {1} {2:F5} → {3:F5} → {4:F5}, RSI {5} {6:F1} → {7:F1} → {8:F1}",
    position.TradeType, pricePattern, swing0, swing1, swing2, rsiPattern, rsi0, rsi1, rsi2);

// Confirmation wait
Print("[EXHAUSTION] Waiting confirmation. {0} level: {1:F5}",
    position.TradeType == TradeType.Sell ? "HL2" : "LH2", state.ConfirmationPrice);

// Invalidation
Print("[EXHAUSTION] INVALIDATED - Price broke {0} level {1:F5}, current: {2:F5}",
    position.TradeType == TradeType.Sell ? "HL2" : "LH2",
    state.ConfirmationPrice, currentPrice);

// Confirmation & exit
Print("[EXHAUSTION] CONFIRMED - Closing {0} position {1} at {2:F5}, Profit: {3:F2} pips",
    position.TradeType, position.Id, closePrice, position.Pips);
```

**Log Prefix:** `[EXHAUSTION]` for easy filtering in backtests.

## Testing Strategy

### Unit Test Cases

**Test 1: Swing Detection Accuracy**
- Input: M1 bars with known swing points
- Expected: DetectSwingLow/High returns correct prices and indices
- Verify: SwingHistory populated with accurate data

**Test 2: Pattern Without Divergence**
- Input: 2 consecutive HL but RSI also making HL (no divergence)
- Expected: Pattern NOT detected
- Verify: State remains Monitoring

**Test 3: Pattern With Divergence**
- Input: 2 consecutive HL, RSI making LL (bullish divergence)
- Expected: Pattern detected, state → PatternDetected
- Verify: ConfirmationPrice = HL2, waiting confirmation

**Test 4: Confirmation - Invalidation**
- Input: Pattern detected, next bar breaks below HL2
- Expected: State → Invalidated → Monitoring
- Verify: Position remains open, swing history cleared

**Test 5: Confirmation - Valid Exit**
- Input: Pattern detected, next bar respects HL2
- Expected: State → Confirmed, position closed
- Verify: ClosePosition() called at market

**Test 6: Threshold Not Met**
- Input: Chandelier made only 1 move, pattern detected
- Expected: Exhaustion check skipped
- Verify: No exit triggered, pattern ignored

### Backtest Scenarios

**Scenario 1: Strong Trend (Should NOT Exit)**
- Setup: Enter SELL in strong downtrend
- Behavior: Chandelier trails aggressively, no HL patterns
- Expected: Position runs to natural exit (TP or chandelier SL)
- Verify: No exhaustion exits logged

**Scenario 2: Exhaustion Reversal (Should Exit)**
- Setup: Enter SELL, price moves down favorably
- Behavior: Chandelier trails 2+ times, then 2 HL + divergence
- Confirmation: Next bar respects HL2
- Expected: Position closes via exhaustion exit
- Verify: Exit logged, profit locked before reversal

**Scenario 3: False Alarm (Should NOT Exit)**
- Setup: Enter SELL, chandelier trails 2+ times
- Behavior: 2 HL + divergence detected
- Confirmation: Next bar breaks below HL2 (downtrend resumes)
- Expected: Pattern invalidated, position continues
- Verify: Invalidation logged, no exit

**Scenario 4: Early Pattern (Should Ignore)**
- Setup: Enter SELL, chandelier activates at BE
- Behavior: Pattern forms before 2nd trailing move
- Expected: Pattern ignored (threshold not met)
- Verify: Exhaustion check skipped

### Backtest Analysis

**Key Metrics:**
- Net profit: With vs without exhaustion exit
- Win rate: Change in percentage
- Avg win size: Does exhaustion reduce average winner?
- Max adverse excursion: Does it protect against large reversals?
- Exit distribution: % chandelier vs % exhaustion vs % TP/SL

**Visual Log Analysis:**
Search logs for:
```
[EXHAUSTION] Pattern detected
[EXHAUSTION] CONFIRMED - Closing
[EXHAUSTION] INVALIDATED
```

Calculate:
- Pattern detection rate (per 100 trades)
- Confirmation rate (confirmed / total patterns)
- Invalidation rate (invalidated / total patterns)

**Success Criteria:**
- Exhaustion exits improve net profit OR reduce drawdown
- Confirmation rate > 50% (more valid signals than false alarms)
- Average exhaustion exit profit > average chandelier exit profit

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Too many false positives** | Exits winners too early | RSI divergence requirement filters noise; confirmation bar adds second check |
| **Over-optimization** | Parameters fit backtest but fail forward | Conservative defaults; wide optimization ranges; test on unseen data |
| **Complexity** | Harder to debug and maintain | Clear state machine; extensive logging; modular methods |
| **Parameter sensitivity** | Small changes drastically affect results | Backtesting will reveal; use step increments to find stable regions |
| **Resource usage** | Additional RSI indicator and swing tracking | Minimal (one RSI instance, small swing history list) |

## Success Criteria

1. ✅ Exhaustion detection activates only after chandelier has trailed 2+ times
2. ✅ Pattern recognition correctly identifies 2 consecutive HL/LH
3. ✅ RSI divergence requirement filters out non-exhaustion patterns
4. ✅ Confirmation bar prevents premature exits on false signals
5. ✅ Invalidation logic keeps position when trend resumes
6. ✅ Works correctly for both BUY and SELL trades
7. ✅ Backtest shows improvement in profit protection or drawdown reduction
8. ✅ No crashes or errors during optimization sweeps

## Version History

- **v1.0:** Base system with fixed SL/TP
- **v2.0:** Chandelier trailing stop with configurable TP modes
- **v3.0:** Exhaustion exit protection with RSI divergence + confirmation ← THIS SPEC

## Next Steps

1. ✅ Design approved by user
2. ⏳ Write implementation plan (writing-plans skill)
3. ⏳ Implement v3.0 code changes
4. ⏳ Unit testing with known swing data
5. ⏳ Backtest validation on historical data
6. ⏳ Optimization sweep on key parameters
7. ⏳ Compare v3.0 vs v2.0 performance
8. ⏳ Deploy to live testing with exhaustion disabled initially
9. ⏳ Enable exhaustion after validating chandelier works correctly

## Notes

- **Conservative defaults:** EnableExhaustionExit = false by default. User must opt-in after validating v2.0 chandelier system.
- **Separate RSI:** Uses RSI-14 for divergence (classic standard), separate from entry system's RSI-6.
- **Modular design:** Exhaustion logic is self-contained within chandelier system for easy debugging and potential future extraction.
- **Backtest-first approach:** Feature must prove value in backtesting before live deployment.

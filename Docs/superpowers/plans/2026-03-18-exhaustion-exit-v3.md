# Exhaustion Exit Protection v3.0 Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add intelligent profit protection that detects market exhaustion via swing patterns + RSI divergence, exiting positions before chandelier SL is hit.

**Architecture:** Extend existing `ChandelierState` class with exhaustion tracking fields. Add new methods to `ProcessSinglePosition()` flow for swing detection, divergence analysis, and confirmation logic. Uses separate RSI-14 indicator for divergence detection.

**Tech Stack:** C# / cTrader API / cAlgo

**Spec:** `docs/superpowers/specs/2026-03-18-exhaustion-exit-design.md`

---

## File Structure

**Single file modification:**
- **Modify:** `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs`
  - Add exhaustion parameters (lines ~540-560)
  - Extend `ChandelierState` class with exhaustion fields (lines ~307-324)
  - Add `SwingPoint` class and `ExhaustionState` enum (after ChandelierState)
  - Add exhaustion RSI indicator field (lines ~725)
  - Initialize exhaustion RSI in OnStart() (lines ~760-770)
  - Add exhaustion methods at end of Chandelier section (~4900-5200)
  - Integrate exhaustion check into `ProcessSinglePosition()` (line ~4650)
  - Increment chandelier move counter in `TrailChandelierStop()` (line ~4850)

**No new files** - all changes integrate into existing bot file.

---

## Task 1: Add Exhaustion Parameters

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:540-560`

- [ ] **Step 1: Add exhaustion parameter group**

Add after the Enhanced Entry v2.0 parameters section (around line 540):

```csharp
#endregion

#region Parameters - Exhaustion Exit v3.0

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

- [ ] **Step 2: Verify parameters compile**

Run: Copy file to cAlgo and rebuild bot in cTrader
Expected: Build succeeds, new "Exhaustion Exit" parameter group appears in bot settings

- [ ] **Step 3: Commit parameters**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add exhaustion exit parameters

Add 4 new parameters for exhaustion exit protection:
- EnableExhaustionExit (default: false)
- MinChandelierMovesBeforeExit (default: 2)
- ExhaustionSwingBars (default: 8)
- ExhaustionRSIPeriod (default: 14)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 2: Extend State Tracking Classes

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:307-327`

- [ ] **Step 1: Add exhaustion fields to ChandelierState**

Modify the `ChandelierState` class (around line 307):

```csharp
private class ChandelierState
{
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
    public TradeType TradeDirection { get; set; }
    public double PriceWatermark { get; set; }
    public int LastIncrementCount { get; set; }

    // NEW v3.0: Exhaustion exit tracking
    public int ChandelierMoveCount { get; set; }
    public List<SwingPoint> SwingHistory { get; set; }
    public ExhaustionState ExhaustionStatus { get; set; }
    public double ConfirmationPrice { get; set; }
    public int ConfirmationBarIndex { get; set; }
}
```

- [ ] **Step 2: Add SwingPoint class**

Add immediately after `ChandelierState` class:

```csharp
/// <summary>
/// Represents a swing point (high or low) with RSI value
/// </summary>
private class SwingPoint
{
    public double Price { get; set; }
    public double RSIValue { get; set; }
    public int BarIndex { get; set; }
}
```

- [ ] **Step 3: Add ExhaustionState enum**

Add after `SwingPoint` class:

```csharp
/// <summary>
/// Exhaustion pattern detection state machine
/// </summary>
private enum ExhaustionState
{
    Monitoring,          // Watching for pattern
    PatternDetected,     // 2 HL/LH + divergence found, waiting confirmation
    Confirmed,           // Confirmation bar validates pattern
    Invalidated          // Confirmation bar breaks pattern
}
```

- [ ] **Step 4: Verify compilation**

Run: Copy file to cAlgo and rebuild bot
Expected: Build succeeds, no errors

- [ ] **Step 5: Commit state classes**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Extend chandelier state with exhaustion tracking

Add new fields to ChandelierState:
- ChandelierMoveCount
- SwingHistory (List<SwingPoint>)
- ExhaustionStatus enum
- ConfirmationPrice and ConfirmationBarIndex

Add supporting classes:
- SwingPoint (price, RSI, bar index)
- ExhaustionState enum (state machine)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 3: Initialize Exhaustion RSI Indicator

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:725` (add field)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:765` (initialize in OnStart)

- [ ] **Step 1: Add RSI indicator field**

Add after existing indicator fields (around line 725):

```csharp
// Chandelier trailing stop tracking
private Dictionary<int, ChandelierState> _chandelierStates;

// v3.0: Exhaustion exit RSI indicator
private RelativeStrengthIndex _exhaustionRSI;
```

- [ ] **Step 2: Initialize RSI in OnStart()**

Add in `OnStart()` method after existing indicator initialization (around line 765):

```csharp
// Initialize chandelier state tracking
_chandelierStates = new Dictionary<int, ChandelierState>();

// v3.0: Initialize exhaustion RSI indicator (M1 timeframe)
_exhaustionRSI = Indicators.RelativeStrengthIndex(Bars.ClosePrices, ExhaustionRSIPeriod);
Print("[INIT] Exhaustion RSI initialized with period {0}", ExhaustionRSIPeriod);
```

- [ ] **Step 3: Verify initialization**

Run: Copy to cAlgo, rebuild, run backtest with EnableExhaustionExit=false
Expected: Log shows "[INIT] Exhaustion RSI initialized with period 14"

- [ ] **Step 4: Commit RSI initialization**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Initialize exhaustion RSI indicator

Add _exhaustionRSI field and initialize in OnStart().
Uses separate RSI-14 for divergence detection.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 4: Initialize Exhaustion State on Position Open

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4077-4090` (market BUY)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4176-4189` (market SELL)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:5074-5087` (pending BUY)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:5098-5111` (pending SELL)

- [ ] **Step 1: Initialize exhaustion state in market BUY**

Find the chandelier state initialization for market BUY orders (around line 4077). Add exhaustion fields:

```csharp
var state = new ChandelierState
{
    PositionId = result.Position.Id,
    IsActivated = false,
    EntryPrice = entryPrice,
    OriginalTP = takeProfit,
    OriginalSL = stopLoss,
    ActivationPrice = entryPrice + ((takeProfit - entryPrice) * ChandelierActivationRR),
    BreakevenPrice = entryPrice + GetCommissionInPrice(result.Position),
    CurrentTrailingSL = stopLoss,
    CurrentTrailingTP = takeProfit,
    HighestTrailingSL = 0,
    HighestTrailingTP = 0,
    TPTrailingStarted = false,
    TradeDirection = TradeType.Buy,
    PriceWatermark = entryPrice,
    LastIncrementCount = 0,
    // v3.0: Initialize exhaustion state
    ChandelierMoveCount = 0,
    SwingHistory = new List<SwingPoint>(),
    ExhaustionStatus = ExhaustionState.Monitoring,
    ConfirmationPrice = 0,
    ConfirmationBarIndex = 0
};
```

- [ ] **Step 2: Initialize exhaustion state in market SELL**

Find chandelier state initialization for market SELL orders (around line 4176). Add same exhaustion fields:

```csharp
var state = new ChandelierState
{
    PositionId = result.Position.Id,
    IsActivated = false,
    EntryPrice = entryPrice,
    OriginalTP = takeProfit,
    OriginalSL = stopLoss,
    ActivationPrice = entryPrice - ((entryPrice - takeProfit) * ChandelierActivationRR),
    BreakevenPrice = entryPrice - GetCommissionInPrice(result.Position),
    CurrentTrailingSL = stopLoss,
    CurrentTrailingTP = takeProfit,
    HighestTrailingSL = double.MaxValue,
    HighestTrailingTP = 0,
    TPTrailingStarted = false,
    TradeDirection = TradeType.Sell,
    PriceWatermark = entryPrice,
    LastIncrementCount = 0,
    // v3.0: Initialize exhaustion state
    ChandelierMoveCount = 0,
    SwingHistory = new List<SwingPoint>(),
    ExhaustionStatus = ExhaustionState.Monitoring,
    ConfirmationPrice = 0,
    ConfirmationBarIndex = 0
};
```

- [ ] **Step 3: Initialize exhaustion state in pending BUY (OnPositionOpened)**

Find chandelier state initialization for pending BUY orders (around line 5074):

```csharp
var state = new ChandelierState
{
    PositionId = args.Position.Id,
    IsActivated = false,
    EntryPrice = entryPrice,
    OriginalTP = takeProfit,
    OriginalSL = stopLoss,
    ActivationPrice = entryPrice + ((takeProfit - entryPrice) * ChandelierActivationRR),
    BreakevenPrice = entryPrice + GetCommissionInPrice(args.Position),
    CurrentTrailingSL = stopLoss,
    CurrentTrailingTP = takeProfit,
    HighestTrailingSL = 0,
    HighestTrailingTP = 0,
    TPTrailingStarted = false,
    TradeDirection = TradeType.Buy,
    PriceWatermark = entryPrice,
    LastIncrementCount = 0,
    // v3.0: Initialize exhaustion state
    ChandelierMoveCount = 0,
    SwingHistory = new List<SwingPoint>(),
    ExhaustionStatus = ExhaustionState.Monitoring,
    ConfirmationPrice = 0,
    ConfirmationBarIndex = 0
};
```

- [ ] **Step 4: Initialize exhaustion state in pending SELL (OnPositionOpened)**

Find chandelier state initialization for pending SELL orders (around line 5098):

```csharp
var state = new ChandelierState
{
    PositionId = args.Position.Id,
    IsActivated = false,
    EntryPrice = entryPrice,
    OriginalTP = takeProfit,
    OriginalSL = stopLoss,
    ActivationPrice = entryPrice - ((entryPrice - takeProfit) * ChandelierActivationRR),
    BreakevenPrice = entryPrice - GetCommissionInPrice(args.Position),
    CurrentTrailingSL = stopLoss,
    CurrentTrailingTP = takeProfit,
    HighestTrailingSL = double.MaxValue,
    HighestTrailingTP = 0,
    TPTrailingStarted = false,
    TradeDirection = TradeType.Sell,
    PriceWatermark = entryPrice,
    LastIncrementCount = 0,
    // v3.0: Initialize exhaustion state
    ChandelierMoveCount = 0,
    SwingHistory = new List<SwingPoint>(),
    ExhaustionStatus = ExhaustionState.Monitoring,
    ConfirmationPrice = 0,
    ConfirmationBarIndex = 0
};
```

- [ ] **Step 5: Verify initialization**

Run: Copy to cAlgo, rebuild, run backtest with EnableExhaustionExit=false
Expected: Build succeeds, no runtime errors

- [ ] **Step 6: Commit state initialization**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Initialize exhaustion state on position open

Initialize exhaustion tracking fields in all 4 position open paths:
- Market BUY
- Market SELL
- Pending BUY (OnPositionOpened)
- Pending SELL (OnPositionOpened)

All positions start in Monitoring state with empty swing history.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 5: Implement Chandelier Move Counter

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4850` (in TrailChandelierStop method)

- [ ] **Step 1: Increment counter when SL trails**

Find the `TrailChandelierStop()` method where `ModifyPosition()` is called (around line 4850). After successful SL update, increment counter:

```csharp
// Existing code: ModifyPosition called here
if (shouldUpdate)
{
    string reason;
    if (!ValidateSLDistance(position, proposedSL, out reason))
    {
        return;
    }

    double oldSL = state.CurrentTrailingSL;
    state.CurrentTrailingSL = proposedSL;

    // Update increment count tracker (for CurrentPrice mode)
    if (TrailModeSelection == TrailMode.CurrentPrice)
    {
        state.LastIncrementCount = newIncrementCount;
    }

    if (TrailModeSelection == TrailMode.Watermark)
    {
        state.HighestTrailingSL = proposedSL;
    }

    // v3.0: Increment chandelier move counter (excludes BE activation)
    state.ChandelierMoveCount++;
    Print("[CHANDELIER] Position {0} trailing move #{1} | SL: {2:F5} → {3:F5}",
        position.Id, state.ChandelierMoveCount, oldSL, proposedSL);

    // Existing TP trailing code continues...
```

- [ ] **Step 2: Verify counter increments**

Run: Copy to cAlgo, rebuild, backtest with EnableChandelierSL=true, EnableExhaustionExit=false
Expected: Log shows "[CHANDELIER] Position X trailing move #1", "#2", etc.

- [ ] **Step 3: Commit move counter**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add chandelier move counter

Increment ChandelierMoveCount each time SL trails.
Excludes BE activation move, only counts actual trailing.
Logged for debugging.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 6: Implement Swing Detection Methods

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4900` (add methods at end of chandelier section)

- [ ] **Step 1: Add DetectSwingLow method**

Add at end of Chandelier methods section (around line 4900):

```csharp
#region Exhaustion Exit Methods - v3.0

/// <summary>
/// Detects swing low on M1: lowest low in last N bars
/// Returns true if swing detected, outputs swing price and RSI value
/// </summary>
private bool DetectSwingLow(int barsBack, out double swingPrice, out double rsiValue)
{
    swingPrice = 0;
    rsiValue = 0;

    // Check enough bars available
    int lookbackStart = barsBack + ExhaustionSwingBars;
    if (Bars.Count < lookbackStart + 1)
    {
        return false;  // Not enough bars
    }

    // Find lowest low in lookback window
    double lowestLow = double.MaxValue;
    int lowestIndex = -1;

    for (int i = lookbackStart; i >= barsBack; i--)
    {
        double low = Bars.LowPrices.Last(i);
        if (low < lowestLow)
        {
            lowestLow = low;
            lowestIndex = i;
        }
    }

    if (lowestIndex == -1)
        return false;

    // Get RSI value at swing point
    if (_exhaustionRSI.Result.Last(lowestIndex).IsNaN())
        return false;

    swingPrice = lowestLow;
    rsiValue = _exhaustionRSI.Result.Last(lowestIndex);
    return true;
}

/// <summary>
/// Detects swing high on M1: highest high in last N bars
/// Returns true if swing detected, outputs swing price and RSI value
/// </summary>
private bool DetectSwingHigh(int barsBack, out double swingPrice, out double rsiValue)
{
    swingPrice = 0;
    rsiValue = 0;

    // Check enough bars available
    int lookbackStart = barsBack + ExhaustionSwingBars;
    if (Bars.Count < lookbackStart + 1)
    {
        return false;  // Not enough bars
    }

    // Find highest high in lookback window
    double highestHigh = double.MinValue;
    int highestIndex = -1;

    for (int i = lookbackStart; i >= barsBack; i--)
    {
        double high = Bars.HighPrices.Last(i);
        if (high > highestHigh)
        {
            highestHigh = high;
            highestIndex = i;
        }
    }

    if (highestIndex == -1)
        return false;

    // Get RSI value at swing point
    if (_exhaustionRSI.Result.Last(highestIndex).IsNaN())
        return false;

    swingPrice = highestHigh;
    rsiValue = _exhaustionRSI.Result.Last(highestIndex);
    return true;
}

#endregion
```

- [ ] **Step 2: Verify compilation**

Run: Copy to cAlgo, rebuild
Expected: Build succeeds

- [ ] **Step 3: Commit swing detection**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add swing detection methods

Implement DetectSwingLow and DetectSwingHigh:
- N-bar lookback window (configurable)
- Returns swing price and RSI value at swing point
- Handles insufficient bar data gracefully

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 7: Implement RSI Divergence Detection

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4990` (add methods after swing detection)

- [ ] **Step 1: Add CheckBullishDivergence method**

Add after swing detection methods:

```csharp
/// <summary>
/// Checks for bullish divergence: price making HL, RSI making LL
/// Indicates SELL position exhaustion (downtrend weakening)
/// </summary>
private bool CheckBullishDivergence(ChandelierState state)
{
    if (state.SwingHistory.Count < 3)
        return false;

    // Need 3 swing lows: oldest to newest
    var swing0 = state.SwingHistory[0];  // First swing low
    var swing1 = state.SwingHistory[1];  // Second swing low (HL1)
    var swing2 = state.SwingHistory[2];  // Third swing low (HL2)

    // Price making Higher Lows (bullish structure)
    bool priceHL1 = swing1.Price > swing0.Price;
    bool priceHL2 = swing2.Price > swing1.Price;

    // RSI making Lower Lows (momentum weakening = divergence)
    bool rsiLL1 = swing1.RSIValue < swing0.RSIValue;
    bool rsiLL2 = swing2.RSIValue < swing1.RSIValue;

    bool hasDivergence = priceHL1 && priceHL2 && rsiLL1 && rsiLL2;

    if (hasDivergence)
    {
        Print("[EXHAUSTION] Bullish Divergence | Price HL: {0:F5} → {1:F5} → {2:F5} | RSI LL: {3:F1} → {4:F1} → {5:F1}",
            swing0.Price, swing1.Price, swing2.Price,
            swing0.RSIValue, swing1.RSIValue, swing2.RSIValue);
    }

    return hasDivergence;
}

/// <summary>
/// Checks for bearish divergence: price making LH, RSI making HH
/// Indicates BUY position exhaustion (uptrend weakening)
/// </summary>
private bool CheckBearishDivergence(ChandelierState state)
{
    if (state.SwingHistory.Count < 3)
        return false;

    // Need 3 swing highs: oldest to newest
    var swing0 = state.SwingHistory[0];  // First swing high
    var swing1 = state.SwingHistory[1];  // Second swing high (LH1)
    var swing2 = state.SwingHistory[2];  // Third swing high (LH2)

    // Price making Lower Highs (bearish structure)
    bool priceLH1 = swing1.Price < swing0.Price;
    bool priceLH2 = swing2.Price < swing1.Price;

    // RSI making Higher Highs (momentum weakening = divergence)
    bool rsiHH1 = swing1.RSIValue > swing0.RSIValue;
    bool rsiHH2 = swing2.RSIValue > swing1.RSIValue;

    bool hasDivergence = priceLH1 && priceLH2 && rsiHH1 && rsiHH2;

    if (hasDivergence)
    {
        Print("[EXHAUSTION] Bearish Divergence | Price LH: {0:F5} → {1:F5} → {2:F5} | RSI HH: {3:F1} → {4:F1} → {5:F1}",
            swing0.Price, swing1.Price, swing2.Price,
            swing0.RSIValue, swing1.RSIValue, swing2.RSIValue);
    }

    return hasDivergence;
}
```

- [ ] **Step 2: Verify compilation**

Run: Copy to cAlgo, rebuild
Expected: Build succeeds

- [ ] **Step 3: Commit divergence detection**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add RSI divergence detection

Implement CheckBullishDivergence and CheckBearishDivergence:
- Bullish: price HL + RSI LL (SELL exhaustion)
- Bearish: price LH + RSI HH (BUY exhaustion)
- Requires 3 swings in history
- Logs divergence when detected

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 8: Implement Confirmation Logic

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:5080` (add methods after divergence detection)

- [ ] **Step 1: Add CheckSellConfirmation method**

Add after divergence methods:

```csharp
/// <summary>
/// Checks confirmation bar for SELL position exhaustion
/// Invalidates if price breaks below HL2, confirms if respects HL2
/// </summary>
private void CheckSellConfirmation(Position position, ChandelierState state)
{
    double currentLow = Bars.LowPrices.Last(0);

    // INVALIDATE: Price broke below HL2 (downtrend resuming)
    if (currentLow < state.ConfirmationPrice)
    {
        Print("[EXHAUSTION] INVALIDATED - SELL Position {0} | Price broke below HL2 {1:F5}, low: {2:F5}",
            position.Id, state.ConfirmationPrice, currentLow);

        // Reset to monitoring for next pattern
        state.SwingHistory.Clear();
        state.ExhaustionStatus = ExhaustionState.Monitoring;
        state.ConfirmationPrice = 0;
        state.ConfirmationBarIndex = 0;
        return;
    }

    // CONFIRM: Price respects HL2 level (reversal continuing)
    state.ExhaustionStatus = ExhaustionState.Confirmed;
    Print("[EXHAUSTION] CONFIRMED - Closing SELL position {0} at {1:F5} | Profit: {2:F2} pips",
        position.Id, Symbol.Ask, position.Pips);

    ClosePosition(position);
}

/// <summary>
/// Checks confirmation bar for BUY position exhaustion
/// Invalidates if price breaks above LH2, confirms if respects LH2
/// </summary>
private void CheckBuyConfirmation(Position position, ChandelierState state)
{
    double currentHigh = Bars.HighPrices.Last(0);

    // INVALIDATE: Price broke above LH2 (uptrend resuming)
    if (currentHigh > state.ConfirmationPrice)
    {
        Print("[EXHAUSTION] INVALIDATED - BUY Position {0} | Price broke above LH2 {1:F5}, high: {2:F5}",
            position.Id, state.ConfirmationPrice, currentHigh);

        // Reset to monitoring for next pattern
        state.SwingHistory.Clear();
        state.ExhaustionStatus = ExhaustionState.Monitoring;
        state.ConfirmationPrice = 0;
        state.ConfirmationBarIndex = 0;
        return;
    }

    // CONFIRM: Price respects LH2 level (reversal continuing)
    state.ExhaustionStatus = ExhaustionState.Confirmed;
    Print("[EXHAUSTION] CONFIRMED - Closing BUY position {0} at {1:F5} | Profit: {2:F2} pips",
        position.Id, Symbol.Bid, position.Pips);

    ClosePosition(position);
}
```

- [ ] **Step 2: Verify compilation**

Run: Copy to cAlgo, rebuild
Expected: Build succeeds

- [ ] **Step 3: Commit confirmation logic**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add confirmation/invalidation logic

Implement CheckSellConfirmation and CheckBuyConfirmation:
- SELL: Invalidate if price breaks below HL2, confirm if respects
- BUY: Invalidate if price breaks above LH2, confirm if respects
- Closes position on confirmation
- Resets to Monitoring on invalidation

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 9: Implement Main Exhaustion Check Method

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:5150` (add main exhaustion method)

- [ ] **Step 1: Add CheckExhaustionExit method**

Add after confirmation methods:

```csharp
/// <summary>
/// Main exhaustion exit logic - called from ProcessSinglePosition
/// Manages swing tracking, pattern detection, and confirmation
/// </summary>
private void CheckExhaustionExit(Position position, ChandelierState state)
{
    // Safety checks
    if (!EnableExhaustionExit) return;
    if (!state.IsActivated) return;
    if (state.ChandelierMoveCount < MinChandelierMovesBeforeExit) return;
    if (Bars.Count < ExhaustionSwingBars + 5) return;

    // Check if in confirmation state (waiting for next bar)
    if (state.ExhaustionStatus == ExhaustionState.PatternDetected)
    {
        // Check if we're on the confirmation bar (next bar after detection)
        if (Bars.Count > state.ConfirmationBarIndex)
        {
            if (position.TradeType == TradeType.Sell)
            {
                CheckSellConfirmation(position, state);
            }
            else
            {
                CheckBuyConfirmation(position, state);
            }
        }
        return;  // Don't update swings during confirmation wait
    }

    // Update swing history (Monitoring state)
    if (state.ExhaustionStatus == ExhaustionState.Monitoring)
    {
        double swingPrice, rsiValue;
        bool swingDetected = false;

        if (position.TradeType == TradeType.Sell)
        {
            // Detect swing lows for SELL positions
            swingDetected = DetectSwingLow(0, out swingPrice, out rsiValue);
        }
        else
        {
            // Detect swing highs for BUY positions
            swingDetected = DetectSwingHigh(0, out swingPrice, out rsiValue);
        }

        // Add to swing history if new swing detected
        if (swingDetected)
        {
            // Check if this is a new swing (different from last)
            bool isNewSwing = state.SwingHistory.Count == 0 ||
                              Math.Abs(state.SwingHistory[state.SwingHistory.Count - 1].Price - swingPrice) > Symbol.PipSize * 0.1;

            if (isNewSwing)
            {
                state.SwingHistory.Add(new SwingPoint
                {
                    Price = swingPrice,
                    RSIValue = rsiValue,
                    BarIndex = Bars.Count
                });

                // Keep only last 3 swings
                if (state.SwingHistory.Count > 3)
                {
                    state.SwingHistory.RemoveAt(0);
                }

                Print("[EXHAUSTION] Position {0} | New {1} swing: {2:F5}, RSI: {3:F1} | History count: {4}",
                    position.Id,
                    position.TradeType == TradeType.Sell ? "LOW" : "HIGH",
                    swingPrice, rsiValue, state.SwingHistory.Count);
            }
        }

        // Check for pattern + divergence (need 3 swings)
        if (state.SwingHistory.Count >= 3)
        {
            bool hasDivergence = false;

            if (position.TradeType == TradeType.Sell)
            {
                hasDivergence = CheckBullishDivergence(state);
            }
            else
            {
                hasDivergence = CheckBearishDivergence(state);
            }

            if (hasDivergence)
            {
                // Pattern detected! Enter confirmation wait state
                state.ExhaustionStatus = ExhaustionState.PatternDetected;
                state.ConfirmationPrice = state.SwingHistory[2].Price;  // HL2 or LH2
                state.ConfirmationBarIndex = Bars.Count;

                Print("[EXHAUSTION] Pattern detected on Position {0} | Waiting confirmation | Level: {1:F5}",
                    position.Id, state.ConfirmationPrice);
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Run: Copy to cAlgo, rebuild
Expected: Build succeeds

- [ ] **Step 3: Commit main exhaustion method**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Add main exhaustion exit logic

Implement CheckExhaustionExit method:
- Safety checks (enabled, activated, move count, bars)
- Handles PatternDetected state (confirmation wait)
- Updates swing history in Monitoring state
- Detects pattern + divergence (calls sub-methods)
- Transitions to PatternDetected when exhaustion found

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 10: Integrate Exhaustion Check into ProcessSinglePosition

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4650` (in ProcessSinglePosition method)

- [ ] **Step 1: Add exhaustion check call**

Find `ProcessSinglePosition()` method (around line 4630). Add exhaustion check after chandelier trailing:

```csharp
private void ProcessSinglePosition(Position position, ChandelierState state)
{
    double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;

    // Phase 1: Check for activation
    if (!state.IsActivated)
    {
        bool shouldActivate = position.TradeType == TradeType.Buy
            ? currentPrice >= state.ActivationPrice
            : currentPrice <= state.ActivationPrice;

        if (shouldActivate)
        {
            ActivateChandelier(position, state);
        }
        return;  // Don't trail until activated
    }

    // Phase 2 & 3: Trail the stop
    TrailChandelierStop(position, state);

    // v3.0: Phase 4: Check exhaustion exit
    CheckExhaustionExit(position, state);
}
```

- [ ] **Step 2: Test integration with feature disabled**

Run: Copy to cAlgo, rebuild, backtest with EnableExhaustionExit=false
Expected: No exhaustion logs, chandelier works normally

- [ ] **Step 3: Test integration with feature enabled**

Run: Backtest with EnableExhaustionExit=true, EnableChandelierSL=true
Expected:
- Logs show "[EXHAUSTION] New swing" messages
- Logs show "[EXHAUSTION] Pattern detected" when divergence found
- Logs show "[EXHAUSTION] CONFIRMED" or "[EXHAUSTION] INVALIDATED"

- [ ] **Step 4: Commit integration**

```bash
git add "Jcamp_1M_scalping.cs"
git commit -m "feat(v3): Integrate exhaustion check into chandelier flow

Add CheckExhaustionExit() call to ProcessSinglePosition.
Runs after chandelier trailing (Phase 4).
Feature complete and functional.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 11: Copy to cAlgo and Full Integration Test

**Files:**
- Copy: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` → cAlgo location

- [ ] **Step 1: Copy file to cAlgo build location**

Run:
```bash
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
```

- [ ] **Step 2: Rebuild bot in cTrader**

Manual: Open cTrader, rebuild bot
Expected: Build succeeds with no errors or warnings

- [ ] **Step 3: Run backtest with exhaustion disabled**

Settings:
- EnableExhaustionExit = false
- EnableChandelierSL = true
- Period: Oct 2024 - Jun 2025 (baseline period)

Expected:
- Backtest runs successfully
- No exhaustion logs
- Performance matches v2.0 baseline

- [ ] **Step 4: Run backtest with exhaustion enabled**

Settings:
- EnableExhaustionExit = true
- MinChandelierMovesBeforeExit = 2
- ExhaustionSwingBars = 8
- ExhaustionRSIPeriod = 14
- Same period: Oct 2024 - Jun 2025

Expected:
- Backtest runs successfully
- Logs show exhaustion detection working
- Some positions close via exhaustion exit
- Record: # exhaustion exits, # invalidated patterns, profit impact

- [ ] **Step 5: Analyze results**

Check logs for:
```
[EXHAUSTION] Pattern detected
[EXHAUSTION] CONFIRMED - Closing
[EXHAUSTION] INVALIDATED
```

Calculate metrics:
- Total exhaustion exits vs chandelier exits
- Confirmation rate: confirmed / (confirmed + invalidated)
- Average profit at exhaustion exit vs chandelier exit
- Net profit comparison: v3.0 vs v2.0

- [ ] **Step 6: Document test results**

Create: `D:\JCAMP_FxScalper\Backtest\v3.0-exhaustion-initial-test.txt`

Record:
- Test settings (all parameters)
- Backtest period and results
- # exhaustion exits
- # invalidated patterns
- Confirmation rate
- Profit comparison
- Sample log excerpts showing exhaustion in action

- [ ] **Step 7: Commit test results**

```bash
git add "Backtest/v3.0-exhaustion-initial-test.txt"
git commit -m "test(v3): Document initial exhaustion exit backtest results

Tested v3.0 with defaults on Oct 2024 - Jun 2025 period.
Feature working correctly, exhaustion exits firing.
[Add key metrics from test]

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 12: Update Version Documentation

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:1-10` (update version header)
- Modify: `D:\JCAMP_FxScalper\CLAUDE.md` (update context doc)

- [ ] **Step 1: Update bot version in code**

Find version comment at top of `Jcamp_1M_scalping.cs` (around line 5):

```csharp
/// <summary>
/// JCAMP FxScalper - M1 Scalping Bot
/// Version: 3.0.0
/// Features:
/// - Enhanced Entry System v2.0 (FVG zones, RSI compression/expansion, Dual SMA filter)
/// - Pending order entry with expiry
/// - Chandelier trailing stop loss
/// - Exhaustion exit protection (swing divergence + confirmation)
/// - Daily loss limit
/// - ATR-based dynamic SL/TP
/// </summary>
```

- [ ] **Step 2: Update CLAUDE.md**

Add to version history in `CLAUDE.md`:

```markdown
## Version History

### v3.0.0 (2026-03-18)
- Exhaustion exit protection with RSI divergence
- Swing-based pattern detection (N-bar lookback)
- Confirmation bar with invalidation logic
- Activates after chandelier makes 2+ trailing moves
- Conservative defaults (disabled by default)

### v2.0.0 (2026-03-17)
- Enhanced entry system with pending orders
- Chandelier trailing stop loss
- ...
```

- [ ] **Step 3: Commit documentation updates**

```bash
git add "Jcamp_1M_scalping.cs" "CLAUDE.md"
git commit -m "docs(v3): Update version to 3.0.0

Update version header and documentation for v3.0 release.
Exhaustion exit protection feature complete.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Testing Checklist

### Manual Verification

- [ ] Build succeeds with no errors
- [ ] New "Exhaustion Exit" parameter group appears in bot settings
- [ ] Bot runs with exhaustion disabled (backward compatible)
- [ ] Exhaustion RSI initializes correctly
- [ ] Chandelier move counter increments on each trail
- [ ] Swing detection logs show swings being tracked
- [ ] Pattern detection logs show when divergence found
- [ ] Confirmation logic logs show confirmed/invalidated outcomes
- [ ] Positions close on confirmed exhaustion
- [ ] Positions continue on invalidated patterns

### Backtest Scenarios

**Scenario 1: Feature Disabled (Baseline)**
- Settings: EnableExhaustionExit=false
- Expected: v2.0 behavior, no exhaustion logs

**Scenario 2: Feature Enabled - Strong Trend**
- Expected: No patterns detected, positions run to chandelier/TP

**Scenario 3: Feature Enabled - Exhaustion Reversal**
- Expected: Pattern detected → confirmed → position closed early

**Scenario 4: Feature Enabled - False Alarm**
- Expected: Pattern detected → invalidated → position continues

**Scenario 5: Threshold Not Met**
- Expected: Pattern ignored until chandelier makes 2+ moves

### Performance Comparison

Compare v3.0 (exhaustion enabled) vs v2.0 (exhaustion disabled):

| Metric | v2.0 | v3.0 | Change |
|--------|------|------|--------|
| Net Profit | TBD | TBD | TBD |
| Win Rate | TBD | TBD | TBD |
| Avg Win Size | TBD | TBD | TBD |
| Max DD | TBD | TBD | TBD |
| # Exhaustion Exits | 0 | TBD | TBD |
| Confirmation Rate | N/A | TBD | TBD |

---

## Success Criteria

- [x] All 12 tasks completed
- [ ] Code compiles without errors
- [ ] Backtest with exhaustion disabled matches v2.0 baseline
- [ ] Backtest with exhaustion enabled shows feature working
- [ ] Exhaustion exits fire when patterns confirmed
- [ ] Invalidation logic prevents false exits
- [ ] Threshold requirement (2+ moves) enforced
- [ ] Confirmation rate > 40% (more valid signals than noise)
- [ ] No runtime errors or crashes
- [ ] Logs clearly show exhaustion logic flow

---

## Optimization Next Steps (Post-Implementation)

After validating v3.0 works correctly:

1. **Optimize ExhaustionSwingBars** (5, 8, 10, 12, 15)
   - Test noise vs responsiveness trade-off

2. **Optimize MinChandelierMovesBeforeExit** (1, 2, 3, 4, 5)
   - Test early vs late protection activation

3. **Optimize ExhaustionRSIPeriod** (6, 10, 14, 18)
   - Test divergence sensitivity

4. **Compare optimal v3.0 vs optimal v2.0**
   - Determine if exhaustion improves net profit or drawdown

5. **Forward test on unseen data** (2025 period not used in optimization)
   - Validate optimization doesn't overfit

---

## Notes

- **Conservative deployment:** Feature defaults to disabled (EnableExhaustionExit=false)
- **Backward compatible:** v3.0 with exhaustion disabled = v2.0 behavior
- **Testing strategy:** Validate on known backtest period before live deployment
- **Logs are essential:** Use `[EXHAUSTION]` prefix for easy filtering and analysis
- **Optimization timing:** Only optimize after confirming feature works correctly in baseline test

---

**Plan complete! Ready for execution via superpowers:subagent-driven-development.**

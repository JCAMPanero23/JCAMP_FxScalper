# Chandelier Stop Loss Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add chandelier trailing stop loss that activates at configurable RR fraction, starts at BE+commission, and optionally trails TP ahead of SL.

**Architecture:** Extend `Jcamp_1M_scalping.cs` with ChandelierState tracking per position, process in existing OnBar() handler, use ModifyPosition() API to update SL/TP dynamically.

**Tech Stack:** cTrader cAlgo API (C#), existing AverageTrueRange indicator (`atrM1`)

---

## File Structure

**Modified:**
- `Jcamp_1M_scalping.cs` - All changes in single file (follows existing pattern)

**Sections to add/modify:**
1. New enum `ChandelierTPMode` after `EntryMode` enum (~line 127)
2. New parameters in Trade Management region (~line 80)
3. New `ChandelierState` class after existing classes (~line 250)
4. New tracking dictionary in Private Fields region (~line 460)
5. Initialize dictionary in OnStart() (~line 510)
6. Chandelier logic in OnBar() (~line 630)
7. New region for chandelier methods (~line 3350)
8. Hook into ExecuteBuyTrade/ExecuteSellTrade (~lines 3267, 3332)

---

## Chunk 1: Parameters and Enum

### Task 1: Add ChandelierTPMode Enum

**Files:**
- Modify: `Jcamp_1M_scalping.cs:127` (after EntryMode enum)

- [ ] **Step 1: Add the enum definition**

Insert after line 127 (after `#endregion` of Entry Mode Enum):

```csharp
        #region Chandelier TP Mode Enum

        public enum ChandelierTPMode
        {
            KeepOriginal,   // TP stays at original level throughout
            RemoveTP,       // TP removed on activation; exit via chandelier SL only
            TrailingTP      // TP trails ahead of chandelier SL by offset
        }

        #endregion
```

- [ ] **Step 2: Verify syntax**

Run: Build the project (Ctrl+Shift+B in Visual Studio or cTrader automate build)
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add ChandelierTPMode enum"
```

---

### Task 2: Add Chandelier Parameters

**Files:**
- Modify: `Jcamp_1M_scalping.cs:80` (after Trade Management parameters)

**IMPORTANT:** Task 1 (enum) MUST be completed and built successfully before this task, as the parameter references `ChandelierTPMode.TrailingTP`.

- [ ] **Step 1: Add new parameter region**

Insert after line 80 (after `MagicNumber` parameter, before `#endregion` of Trade Management):

```csharp
        #endregion

        #region Parameters - Chandelier Stop Loss

        [Parameter("=== CHANDELIER SL ===", DefaultValue = "")]
        public string ChandelierHeader { get; set; }

        [Parameter("Enable Chandelier SL", DefaultValue = true, Group = "Chandelier SL")]
        public bool EnableChandelierSL { get; set; }

        [Parameter("Activation RR Fraction", DefaultValue = 0.75, MinValue = 0.5, MaxValue = 0.85, Step = 0.05, Group = "Chandelier SL")]
        public double ChandelierActivationRR { get; set; }

        [Parameter("Chandelier Lookback Bars", DefaultValue = 22, MinValue = 10, MaxValue = 30, Step = 2, Group = "Chandelier SL")]
        public int ChandelierLookback { get; set; }

        [Parameter("TP Mode", DefaultValue = ChandelierTPMode.TrailingTP, Group = "Chandelier SL")]
        public ChandelierTPMode ChandelierTPModeSelection { get; set; }

        [Parameter("Trailing TP Offset (pips)", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 20.0, Step = 1.0, Group = "Chandelier SL")]
        public double TrailingTPOffset { get; set; }
```

- [ ] **Step 2: Remove duplicate `#endregion`**

The original `#endregion` for Trade Management needs adjustment - ensure proper region nesting.

- [ ] **Step 3: Verify syntax**

Run: Build the project
Expected: No errors, parameters appear in cTrader bot settings

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add chandelier SL parameters"
```

---

## Chunk 2: ChandelierState Class and Tracking

### Task 3: Add ChandelierState Class

**Files:**
- Modify: `Jcamp_1M_scalping.cs:~250` (after TradingZone class, before Private Fields)

- [ ] **Step 1: Add the ChandelierState class**

Insert after the TradingZone class (find `#endregion` after TradingZone):

```csharp
        #region Chandelier State Class

        /// <summary>
        /// Tracks chandelier trailing stop state for each position
        /// </summary>
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
        }

        #endregion
```

- [ ] **Step 2: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add ChandelierState tracking class"
```

---

### Task 4: Add Tracking Dictionary

**Files:**
- Modify: `Jcamp_1M_scalping.cs:~460` (Private Fields region)

- [ ] **Step 1: Add dictionary field**

Find the Private Fields region and add near other tracking variables:

```csharp
        // Chandelier trailing stop tracking
        private Dictionary<int, ChandelierState> _chandelierStates;
```

- [ ] **Step 2: Initialize in OnStart()**

Find `OnStart()` method (~line 510) and add initialization:

```csharp
            // Initialize chandelier state tracking
            _chandelierStates = new Dictionary<int, ChandelierState>();
```

- [ ] **Step 3: Add using statement**

Add `using System.Collections.Generic;` at top of file (line 2, after `using System;`):

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
```

This is required for `Dictionary<int, ChandelierState>`. The file does not currently have this import.

- [ ] **Step 4: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add state tracking dictionary"
```

---

## Chunk 3: Core Chandelier Methods

### Task 5: Add Helper Methods

**Files:**
- Modify: `Jcamp_1M_scalping.cs:~3350` (new region after ExecuteBuyTrade)

- [ ] **Step 1: Add chandelier methods region**

Insert after `ExecuteBuyTrade()` method:

```csharp
        #region Chandelier Trailing Stop Methods

        /// <summary>
        /// Gets commission cost in price terms for breakeven calculation
        /// </summary>
        private double GetCommissionInPrice(Position position)
        {
            // Symbol.Commission is per-lot per-side in account currency
            double commissionPerLot = Symbol.Commission * 2;  // Round trip
            if (commissionPerLot <= 0) return 0;

            // Convert to price movement
            double commissionInPrice = (commissionPerLot / position.VolumeInUnits) * Symbol.LotSize;
            return commissionInPrice;
        }

        /// <summary>
        /// Calculates the chandelier stop loss value
        /// </summary>
        private double CalculateChandelierSL(TradeType tradeType)
        {
            int lookback = Math.Min(ChandelierLookback, Bars.Count - 1);
            if (lookback < 1) return 0;

            double atrValue = atrM1.Result.LastValue;
            double atrDistance = atrValue * ATRMultiplier;

            if (tradeType == TradeType.Buy)
            {
                // LONG: Highest High - ATR
                double highestHigh = 0;
                for (int i = 1; i <= lookback; i++)
                {
                    if (Bars.HighPrices.Last(i) > highestHigh)
                        highestHigh = Bars.HighPrices.Last(i);
                }
                return highestHigh - atrDistance;
            }
            else
            {
                // SHORT: Lowest Low + ATR
                double lowestLow = double.MaxValue;
                for (int i = 1; i <= lookback; i++)
                {
                    if (Bars.LowPrices.Last(i) < lowestLow)
                        lowestLow = Bars.LowPrices.Last(i);
                }
                return lowestLow + atrDistance;
            }
        }

        #endregion
```

- [ ] **Step 2: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add helper methods for commission and SL calculation"
```

---

### Task 6: Add Main Chandelier Processing Method

**Files:**
- Modify: `Jcamp_1M_scalping.cs` (add to Chandelier region)

- [ ] **Step 1: Add the main processing method**

Add after `CalculateChandelierSL()`:

```csharp
        /// <summary>
        /// Processes chandelier trailing stop for all tracked positions
        /// Called from OnBar()
        /// </summary>
        private void ProcessChandelierStops()
        {
            if (!EnableChandelierSL) return;

            // Get positions opened by this bot
            var myPositions = Positions.Where(p => p.Label == MagicNumber.ToString()).ToList();

            // Clean up states for closed positions
            var closedIds = _chandelierStates.Keys.Where(id => !myPositions.Any(p => p.Id == id)).ToList();
            foreach (var id in closedIds)
            {
                _chandelierStates.Remove(id);
            }

            // Process each open position
            foreach (var position in myPositions)
            {
                if (!_chandelierStates.TryGetValue(position.Id, out var state))
                    continue;  // Position not tracked (opened before bot start)

                ProcessSinglePosition(position, state);
            }
        }

        /// <summary>
        /// Processes chandelier logic for a single position
        /// </summary>
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
        }
```

- [ ] **Step 2: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add main processing loop"
```

---

### Task 7: Add Activation and Trailing Methods

**Files:**
- Modify: `Jcamp_1M_scalping.cs` (add to Chandelier region)

- [ ] **Step 1: Add activation method**

Add after `ProcessSinglePosition()`:

```csharp
        /// <summary>
        /// Activates chandelier mode - moves SL to BE+commission
        /// </summary>
        private void ActivateChandelier(Position position, ChandelierState state)
        {
            state.IsActivated = true;
            state.CurrentTrailingSL = state.BreakevenPrice;
            state.HighestTrailingSL = state.BreakevenPrice;

            // Determine new TP based on mode
            double? newTP = null;
            if (ChandelierTPModeSelection == ChandelierTPMode.RemoveTP)
            {
                newTP = null;  // Remove TP
            }
            else
            {
                newTP = state.OriginalTP;  // Keep original for now
            }

            // Modify position
            ModifyPosition(position, state.CurrentTrailingSL, newTP);

            Print("[CHANDELIER] Position {0} activated at {1:F5}, SL moved to BE+comm: {2:F5}",
                position.Id, position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask, state.CurrentTrailingSL);
        }

        /// <summary>
        /// Trails the chandelier stop loss (and optionally TP)
        /// </summary>
        private void TrailChandelierStop(Position position, ChandelierState state)
        {
            double chandelierSL = CalculateChandelierSL(position.TradeType);
            if (chandelierSL <= 0) return;

            bool isBuy = position.TradeType == TradeType.Buy;
            double newSL = state.CurrentTrailingSL;
            double? newTP = position.TakeProfit;

            // Check if chandelier provides a better SL
            bool chandelierBetter = isBuy
                ? chandelierSL > state.HighestTrailingSL
                : chandelierSL < state.HighestTrailingSL;

            if (chandelierBetter)
            {
                // Check minimum movement threshold (0.5 pips)
                double movement = Math.Abs(chandelierSL - state.CurrentTrailingSL) / Symbol.PipSize;
                if (movement >= 0.5)
                {
                    double oldSL = state.CurrentTrailingSL;
                    state.CurrentTrailingSL = chandelierSL;
                    state.HighestTrailingSL = chandelierSL;
                    newSL = chandelierSL;

                    Print("[CHANDELIER] Position {0} SL trailed: {1:F5} → {2:F5}",
                        position.Id, oldSL, newSL);

                    // Start TP trailing if mode is TrailingTP and SL moved beyond BE
                    if (ChandelierTPModeSelection == ChandelierTPMode.TrailingTP && !state.TPTrailingStarted)
                    {
                        bool beyondBE = isBuy
                            ? chandelierSL > state.BreakevenPrice
                            : chandelierSL < state.BreakevenPrice;

                        if (beyondBE)
                        {
                            state.TPTrailingStarted = true;
                            Print("[CHANDELIER] Position {0} TP trailing started", position.Id);
                        }
                    }

                    // Trail TP if enabled and started
                    if (state.TPTrailingStarted && ChandelierTPModeSelection == ChandelierTPMode.TrailingTP)
                    {
                        double trailingTP = isBuy
                            ? chandelierSL + (TrailingTPOffset * Symbol.PipSize)
                            : chandelierSL - (TrailingTPOffset * Symbol.PipSize);

                        // TP only moves in favorable direction
                        bool tpBetter = isBuy
                            ? trailingTP > state.HighestTrailingTP
                            : trailingTP < state.HighestTrailingTP;

                        if (tpBetter || state.HighestTrailingTP == 0)
                        {
                            double oldTP = state.CurrentTrailingTP;
                            state.CurrentTrailingTP = trailingTP;
                            state.HighestTrailingTP = trailingTP;
                            newTP = trailingTP;

                            Print("[CHANDELIER] Position {0} TP trailed: {1:F5} → {2:F5}",
                                position.Id, oldTP, newTP);
                        }
                    }

                    // Apply modifications
                    ModifyPosition(position, newSL, newTP);
                }
            }
        }
```

- [ ] **Step 2: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): add activation and trailing logic"
```

---

## Chunk 4: Integration

### Task 8: Hook Into OnBar()

**Files:**
- Modify: `Jcamp_1M_scalping.cs:787` (OnBar method, after UpdateZoneStates block)

- [ ] **Step 1: Add chandelier processing call**

Insert at line 787 (after `UpdateZoneStates()` block ends, before `ProcessEntryLogic()` call):

```csharp
            // ============================================================
            // CHANDELIER TRAILING STOP: Process on every M1 bar close
            // ============================================================
            ProcessChandelierStops();

```

**Location context:**
```csharp
            // Phase 4: Update zone states (expiry, arming, invalidation)
            if (EnablePreZoneSystem && activeZone != null)
            {
                UpdateZoneStates();
            }

            // ============================================================
            // CHANDELIER TRAILING STOP: Process on every M1 bar close  <-- INSERT HERE
            // ============================================================
            ProcessChandelierStops();

            // Phase 1B: Entry detection on M1 bar close
```

- [ ] **Step 2: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): integrate processing into OnBar"
```

---

### Task 9: Hook Into Trade Execution

**Files:**
- Modify: `Jcamp_1M_scalping.cs:3267` (ExecuteSellTrade success block)
- Modify: `Jcamp_1M_scalping.cs:3332` (ExecuteBuyTrade success block)

- [ ] **Step 1: Add state initialization for SELL trades**

In `ExecuteSellTrade()`, after `if (result.IsSuccessful)` block starts, add:

```csharp
                // Initialize chandelier state tracking
                if (EnableChandelierSL)
                {
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
                        HighestTrailingSL = double.MaxValue,  // For SHORT, lower is better
                        HighestTrailingTP = 0,
                        TPTrailingStarted = false,
                        TradeDirection = TradeType.Sell
                    };
                    _chandelierStates[result.Position.Id] = state;

                    Print("[CHANDELIER] Tracking SELL position {0}, activation at {1:F5}",
                        result.Position.Id, state.ActivationPrice);
                }
```

- [ ] **Step 2: Add state initialization for BUY trades**

In `ExecuteBuyTrade()`, after `if (result.IsSuccessful)` block starts, add:

```csharp
                // Initialize chandelier state tracking
                if (EnableChandelierSL)
                {
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
                        HighestTrailingSL = 0,  // For LONG, higher is better
                        HighestTrailingTP = 0,
                        TPTrailingStarted = false,
                        TradeDirection = TradeType.Buy
                    };
                    _chandelierStates[result.Position.Id] = state;

                    Print("[CHANDELIER] Tracking BUY position {0}, activation at {1:F5}",
                        result.Position.Id, state.ActivationPrice);
                }
```

- [ ] **Step 3: Verify syntax**

Run: Build the project
Expected: No errors

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): hook state initialization into trade execution"
```

---

## Chunk 5: Testing and Final Commit

### Task 10: Manual Testing

- [ ] **Step 1: Build and deploy**

Build the project and ensure it appears in cTrader Automate

- [ ] **Step 2: Configure test parameters**

Set parameters:
- EnableChandelierSL: true
- ChandelierActivationRR: 0.75
- ChandelierLookback: 22
- ChandelierTPModeSelection: TrailingTP
- TrailingTPOffset: 10

- [ ] **Step 3: Run backtest**

Run a short backtest and verify in logs:
- `[CHANDELIER] Tracking BUY/SELL position...` appears on trade entry
- `[CHANDELIER] Position X activated...` appears when price hits activation level
- `[CHANDELIER] Position X SL trailed...` appears as SL moves
- `[CHANDELIER] Position X TP trailing started...` appears when TP starts moving

- [ ] **Step 4: Test all TP modes**

Repeat backtest with:
- ChandelierTPModeSelection: KeepOriginal (verify TP never changes)
- ChandelierTPModeSelection: RemoveTP (verify TP removed on activation)
- ChandelierTPModeSelection: TrailingTP (verify TP trails ahead of SL)

- [ ] **Step 5: Verify floor protection**

Check logs to ensure SL never moves backwards (only favorable direction)

---

### Task 11: Final Commit

- [ ] **Step 1: Final commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(chandelier): complete chandelier trailing SL implementation

- Activates at configurable RR fraction (default 75%)
- Starts at breakeven + commission
- Trails using ATR-based chandelier calculation
- Three TP modes: KeepOriginal, RemoveTP, TrailingTP
- Floor protection prevents SL from moving backwards
- Logging for debugging and audit"
```

- [ ] **Step 2: Update spec status**

Change spec status from "Draft" to "Implemented" in:
`Docs/superpowers/specs/2026-03-14-chandelier-sl-design.md`

```bash
git add Docs/superpowers/specs/2026-03-14-chandelier-sl-design.md
git commit -m "docs: mark chandelier SL spec as implemented"
```

---

## Summary

| Task | Description | Estimated Steps |
|------|-------------|-----------------|
| 1 | Add ChandelierTPMode enum | 3 |
| 2 | Add chandelier parameters | 4 |
| 3 | Add ChandelierState class | 3 |
| 4 | Add tracking dictionary | 5 |
| 5 | Add helper methods | 3 |
| 6 | Add main processing method | 3 |
| 7 | Add activation and trailing methods | 3 |
| 8 | Hook into OnBar() | 3 |
| 9 | Hook into trade execution | 4 |
| 10 | Manual testing | 5 |
| 11 | Final commit | 2 |

**Total: 11 tasks, 38 steps**

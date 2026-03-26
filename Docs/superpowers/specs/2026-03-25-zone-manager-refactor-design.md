# Zone Manager Refactor - Design Specification

**Status:** Approved for Implementation
**Date:** 2026-03-25
**Branch:** enhance-entry-system

---

## Problem Statement

Visual investigation of the trading bot revealed 5 critical issues with the current zone management:

1. **Price Too Far From Zone** - Zones stay active even when price moves far away
2. **Danger Session** - PRE-zones remain active when entering danger sessions
3. **Zone Too Small** - Small FVG zones cause tiny SLs that get stopped out easily
4. **Missing Trades** - Entry logic only runs on M15 bar close, missing opportunities
5. **Zone Creation With Open Trade** - New zones keep forming while a position is open

Additionally, there's no way to debug and analyze which entry systems perform best.

---

## Solution Overview

Refactor zone management into a dedicated `ZoneManager` class that consolidates all zone lifecycle logic, plus a `DebugTradeLogger` for performance analysis.

### Key Components

1. **ZoneManager** - Handles zone creation, validation, invalidation, and entry
2. **DebugTradeLogger** - Captures first 3 trades per category for replay and tallies performance
3. **Reversal Entry System** - Placeholder for future implementation

---

## Component 1: ZoneManager

### Class Structure

```csharp
public class ZoneManager
{
    // Dependencies
    private readonly Robot _robot;
    private readonly DebugTradeLogger _logger;

    // State
    private TradingZone _activeZone;

    // Parameters (passed from main bot)
    public double MaxPriceDistancePips { get; set; }
    public double MinimumSLPips { get; set; }
    public bool EnableReversalEntry { get; set; }
    public double ReversalDistancePips { get; set; }

    // Main entry points
    public void OnBar()           // Called every M1 bar
    public void OnM15Bar()        // Called every M15 bar

    // Zone lifecycle
    public TradingZone CreatePreZone(DisplacementCandle disp, FairValueGap fvg)
    public TradingZone CreateFractalZone(int swingIndex, string mode)
    public void ValidateZone()    // Runs all validation checks
    public void InvalidateZone(TradingZone zone, string reason)

    // Entry processing
    public void ProcessEntries()  // Check for entry triggers
    private void ProcessStandardEntry(TradingZone zone)
    private void ProcessReversalEntry(TradingZone zone)  // Placeholder

    // Validation checks
    private bool CheckPriceDistance()
    private bool CheckDangerSession()
    private bool CheckExistingTrade()
    private double EnsureMinimumSL(double calculatedSL)

    // Helpers
    public bool HasOpenPosition()
    public TradingZone GetActiveZone()
}
```

### New Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| Max Price Distance (pips) | 15.0 | 10-30 | Invalidate zone if price moves this far away |
| Minimum SL (pips) | 5.0 | 3-10 | Floor for stop loss distance |
| Enable Reversal Entry | false | bool | Placeholder for reversal system |
| Reversal Distance (pips) | 10.0 | 5-20 | Distance for reversal entry trigger |

---

## Component 2: Validation Rules

### Issue #1: Price Too Far From Zone

```csharp
// In ValidateZone()
double distanceFromZone = CalculateDistanceFromZone();
if (distanceFromZone > MaxPriceDistancePips)
{
    InvalidateZone(zone, "Price moved too far away");
}
```

**Behavior:** If price moves more than `MaxPriceDistancePips` away from an ARMED zone without triggering entry, zone is invalidated.

### Issue #2: Danger Session Invalidation

```csharp
// In ValidateZone()
OptimalPeriod currentPeriod = GetOptimalPeriod(Server.Time);
if (currentPeriod == OptimalPeriod.DangerDeadZone ||
    currentPeriod == OptimalPeriod.DangerLateNY)
{
    InvalidateZone(zone, "Entered danger session");
}
```

**Behavior:** Active zones (PRE, VALID, ARMED) are invalidated when market enters danger session (04:00-08:00 UTC or 20:00-00:00 UTC).

### Issue #3: Minimum SL Pips

```csharp
// In CalculateStopLoss()
double calculatedSL = ...; // existing logic
double minimumSL = MinimumSLPips * Symbol.PipSize;
return Math.Max(calculatedSL, minimumSL);
```

**Behavior:** SL is never smaller than `MinimumSLPips`, regardless of zone size.

### Issue #4: Check Entry Every M1 Bar

```csharp
// In main bot OnBar() - runs every M1 bar
zoneManager.ProcessEntries(); // Check ARMED zones for entry

// In main bot - when M15 bar detected
zoneManager.OnM15Bar(); // Detect new fractals
```

**Behavior:** Entry logic runs on every M1 bar close, not just M15.

### Issue #5: Prevent Zone Creation With Open Trade

```csharp
// In CreatePreZone() and CreateFractalZone()
if (HasOpenPosition())
{
    Print("[ZoneManager] Zone creation blocked - position already open");
    return null;
}
```

**Behavior:** No new zones created while a position is open. Existing zones also don't arm.

---

## Component 3: Reversal Entry System (Placeholder)

### Concept

When price moves away from zone without triggering, optionally place a limit order betting on reversal back to the zone.

```
Zone at 1.1000
Price moves to 1.1015 (+15 pips away)
  → Standard: Invalidate zone
  → Reversal: Place BUY LIMIT at 1.1010 (hoping price returns)
```

### Placeholder Implementation

```csharp
private void ProcessReversalEntry(TradingZone zone)
{
    if (!EnableReversalEntry)
        return;

    // TODO: Implement reversal entry logic
    // - Place limit order at zone price
    // - Track in DebugTradeLogger as "Reversal" entry system
    Print("[Reversal] PLACEHOLDER - Not yet implemented");
}
```

**For now:** Parameter exists but logic is placeholder. Will be tracked separately in DebugTradeLogger when implemented.

---

## Component 4: DebugTradeLogger

### Purpose

Capture detailed trade information for analysis and visual replay. Track performance by zone type, entry system, direction, and result.

### Trade Capture Matrix

```
                        PRE-ZONE                          SWING ZONE
                 BUY            SELL              BUY            SELL
              Win   Loss     Win   Loss       Win   Loss     Win   Loss
Standard     [1-3] [1-3]    [1-3] [1-3]      [1-3] [1-3]    [1-3] [1-3]
Reversal     [1-3] [1-3]    [1-3] [1-3]      [1-3] [1-3]    [1-3] [1-3]
```

**16 categories × 3 trades each = up to 48 trades logged in detail**

### Class Structure

```csharp
public class DebugTradeLogger
{
    // Trade storage
    private Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, List<TradeRecord>>>>> _trades;
    // Key structure: [ZoneType][EntrySystem][Direction][Result] = List<TradeRecord>

    // Tally counters
    private Dictionary<string, int> _winCount;   // "PRE-Zone_Standard_BUY" → count
    private Dictionary<string, int> _lossCount;

    // Constants
    private const int MAX_TRADES_PER_CATEGORY = 3;
    private readonly string _logFolder = @"D:\JCAMP_FxScalper\DebugLogs\";

    // Trade recording
    public void RecordTradeOpen(TradeRecord trade)
    public void RecordTradeClose(TradeRecord trade, string result, double plPips, string exitReason)

    // Output
    public void SaveDetailedLog()    // Full trade details
    public void SaveSummaryLog()     // Tally + replay timestamps
    public void PrintSummaryToLog()  // Print to cTrader log at end of backtest

    // Helpers
    private bool ShouldCaptureTrade(string zoneType, string entrySystem, string direction, string result)
    private string GetCategoryKey(string zoneType, string entrySystem, string direction)
}

public class TradeRecord
{
    public int TradeId { get; set; }
    public string ZoneType { get; set; }        // "PRE-Zone", "Swing"
    public string EntrySystem { get; set; }     // "Standard", "Reversal"
    public string Direction { get; set; }       // "BUY", "SELL"
    public DateTime EntryTime { get; set; }
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public string Result { get; set; }          // "Win", "Loss"
    public double PLPips { get; set; }
    public DateTime ExitTime { get; set; }
    public string ExitReason { get; set; }      // "SL", "TP", "Chandelier", etc.
}
```

### Output Files

Location: `D:\JCAMP_FxScalper\DebugLogs\`

#### trades_detailed_YYYYMMDD_HHMMSS.txt

```
=== DETAILED TRADE LOG ===
Generated: 2026-03-25 14:30:22
Backtest Period: 2024-10-01 to 2025-06-30

--- PRE-ZONE STANDARD BUY WIN #1 ---
Entry Time: 2024-10-15 14:32:00 (Use for Visual Replay)
Zone Type: PRE-Zone
Entry System: Standard
Entry: 1.10234 | SL: 1.10134 | TP: 1.10584
Exit Time: 2024-10-15 16:45:00
Exit Reason: TP Hit
P/L: +35.0 pips

--- PRE-ZONE STANDARD BUY WIN #2 ---
...

--- SWING STANDARD SELL LOSS #1 ---
...
```

#### trades_summary_YYYYMMDD_HHMMSS.txt

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

=== ENTRY SYSTEM PERFORMANCE (Combined) ===

STANDARD ENTRY:
  Total: Wins: 20  Losses: 83  Win Rate: 19.4%

REVERSAL ENTRY:
  Total: Wins: 0   Losses: 0   Win Rate: N/A

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

SWING Standard BUY Win:
  1. 2024-10-11 10:20
  2. 2024-10-19 14:05
  3. 2024-10-25 11:30

SWING Standard BUY Loss:
  1. 2024-10-13 15:40
  2. 2024-10-17 09:55
  3. 2024-10-21 16:15

... (continues for all categories)
```

---

## Integration with Main Bot

### OnStart()

```csharp
// Initialize ZoneManager and DebugTradeLogger
_debugLogger = new DebugTradeLogger();
_zoneManager = new ZoneManager(this, _debugLogger)
{
    MaxPriceDistancePips = this.MaxPriceDistancePips,
    MinimumSLPips = this.MinimumSLPips,
    EnableReversalEntry = this.EnableReversalEntry,
    ReversalDistancePips = this.ReversalDistancePips
};
```

### OnBar() - Every M1 Bar

```csharp
// Validate active zone
_zoneManager.ValidateZone();

// Process entries (checks ARMED zones)
_zoneManager.ProcessEntries();

// Existing M1 logic...
```

### On M15 Bar Detection

```csharp
// Fractal detection and zone creation
_zoneManager.OnM15Bar();
```

### OnStop()

```csharp
// Save debug logs at end of backtest
_debugLogger.SaveDetailedLog();
_debugLogger.SaveSummaryLog();
_debugLogger.PrintSummaryToLog();
```

---

## Migration Plan

### Code to Move to ZoneManager

From `Jcamp_1M_scalping.cs`:
- `CreatePreZone()` → `ZoneManager.CreatePreZone()`
- `UpdateSwingZone()` → `ZoneManager.CreateFractalZone()`
- `UpdateZoneStates()` → `ZoneManager.ValidateZone()`
- `CheckZoneProximity()` → `ZoneManager` internal
- `CheckZoneInvalidation()` → `ZoneManager` internal
- `PlaceBuyPendingOrder()` / `PlaceSellPendingOrder()` → `ZoneManager.ProcessStandardEntry()`

### Code to Keep in Main Bot

- Indicator initialization (SMA, ATR, RSI)
- M15 bar detection
- Fractal detection logic (FindSignificantSwing)
- Position management (Chandelier, TP management)
- Session management

---

## Testing Strategy

1. **Unit Tests per Validation Rule**
   - Test price distance invalidation
   - Test danger session invalidation
   - Test minimum SL enforcement
   - Test position check blocks zone creation

2. **Integration Test**
   - Run backtest with all fixes enabled
   - Verify trade count is reasonable
   - Check debug logs are generated correctly

3. **Visual Verification**
   - Use replay timestamps from debug log
   - Verify zones invalidate correctly in Visual Mode

---

## Success Criteria

1. **Issue #1:** Zones invalidate when price > MaxPriceDistancePips away
2. **Issue #2:** Zones invalidate when entering danger session
3. **Issue #3:** No SL smaller than MinimumSLPips
4. **Issue #4:** Entries trigger on M1 bar close (not waiting for M15)
5. **Issue #5:** No new zones created with open position
6. **Debug Logging:** Files generated in DebugLogs folder with correct format

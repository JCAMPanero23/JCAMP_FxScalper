# Zone Manager Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 5 zone management issues and add debug logging to analyze zone/entry performance

**Architecture:** Add validation rules directly to existing zone methods, add new parameters, create DebugTradeLogger class for trade capture and analysis output. Minimal refactoring - enhance existing code rather than extracting to separate ZoneManager class (simpler, less risky).

**Tech Stack:** C# cAlgo/cTrader API

---

## File Structure

**Files to Modify:**
- `Jcamp_1M_scalping.cs` - Add parameters, validation rules, DebugTradeLogger class

**Output Folder:**
- `D:\JCAMP_FxScalper\DebugLogs\` - Trade logs generated during backtest

---

## Task 1: Add New Parameters

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:145-185` (Parameters region)

- [ ] **Step 1: Add Zone Manager parameters section after Entry Filters region**

Add after line ~185 (after `MaxDailyLosingTrades` parameter):

```csharp
        #endregion

        #region Parameters - Zone Management

        [Parameter("=== ZONE MANAGEMENT ===", DefaultValue = "")]
        public string ZoneManagementHeader { get; set; }

        [Parameter("Max Price Distance (pips)", DefaultValue = 15.0, MinValue = 10.0, MaxValue = 30.0, Step = 5.0, Group = "Zone Management")]
        public double MaxPriceDistancePips { get; set; }

        [Parameter("Minimum SL (pips)", DefaultValue = 5.0, MinValue = 3.0, MaxValue = 10.0, Step = 1.0, Group = "Zone Management")]
        public double MinimumSLPips { get; set; }

        [Parameter("Enable Reversal Entry", DefaultValue = false, Group = "Zone Management")]
        public bool EnableReversalEntry { get; set; }

        [Parameter("Reversal Distance (pips)", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 20.0, Step = 5.0, Group = "Zone Management")]
        public double ReversalDistancePips { get; set; }

        [Parameter("Enable Debug Logging", DefaultValue = true, Group = "Zone Management")]
        public bool EnableDebugLogging { get; set; }

        #endregion
```

- [ ] **Step 2: Copy file to cAlgo location**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 3: Verify compilation in cTrader**

Build in cTrader to verify parameters appear correctly.

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(zone): Add zone management parameters"
```

---

## Task 2: Add TradeRecord and DebugTradeLogger Classes

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (add after ChandelierState class, ~line 350)

- [ ] **Step 1: Add TradeRecord class**

Add after the `ChandelierState` class (around line 350):

```csharp
        #region Debug Trade Logger Classes

        /// <summary>
        /// Represents a single trade for debug logging
        /// </summary>
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
            public string Result { get; set; }          // "Win", "Loss", null if open
            public double PLPips { get; set; }
            public DateTime? ExitTime { get; set; }
            public string ExitReason { get; set; }      // "SL", "TP", "Chandelier", etc.
        }

        #endregion
```

- [ ] **Step 2: Add DebugTradeLogger class**

Add immediately after TradeRecord class:

```csharp
        #region Debug Trade Logger

        /// <summary>
        /// Captures and logs trade data for performance analysis
        /// Tracks first 3 trades per category (ZoneType × EntrySystem × Direction × Result)
        /// </summary>
        public class DebugTradeLogger
        {
            private const int MAX_TRADES_PER_CATEGORY = 3;
            private readonly string _logFolder;
            private readonly Robot _robot;

            // Trade storage: [ZoneType][EntrySystem][Direction][Result] = List<TradeRecord>
            private Dictionary<string, List<TradeRecord>> _detailedTrades;

            // Tally counters: "PRE-Zone_Standard_BUY" → (wins, losses)
            private Dictionary<string, (int wins, int losses)> _tallies;

            // Open trades awaiting close
            private Dictionary<int, TradeRecord> _openTrades;

            public DebugTradeLogger(Robot robot, string logFolder = @"D:\JCAMP_FxScalper\DebugLogs\")
            {
                _robot = robot;
                _logFolder = logFolder;
                _detailedTrades = new Dictionary<string, List<TradeRecord>>();
                _tallies = new Dictionary<string, (int wins, int losses)>();
                _openTrades = new Dictionary<int, TradeRecord>();

                // Ensure log folder exists
                if (!System.IO.Directory.Exists(_logFolder))
                {
                    System.IO.Directory.CreateDirectory(_logFolder);
                }
            }

            /// <summary>
            /// Records a trade when it opens
            /// </summary>
            public void RecordTradeOpen(int positionId, string zoneType, string entrySystem,
                string direction, DateTime entryTime, double entryPrice, double sl, double tp)
            {
                var trade = new TradeRecord
                {
                    TradeId = positionId,
                    ZoneType = zoneType,
                    EntrySystem = entrySystem,
                    Direction = direction,
                    EntryTime = entryTime,
                    EntryPrice = entryPrice,
                    StopLoss = sl,
                    TakeProfit = tp
                };

                _openTrades[positionId] = trade;
                _robot.Print("[DebugLog] Trade opened | ID: {0} | {1} {2} {3}",
                    positionId, zoneType, entrySystem, direction);
            }

            /// <summary>
            /// Records trade result when it closes
            /// </summary>
            public void RecordTradeClose(int positionId, double exitPrice, DateTime exitTime,
                string exitReason, double plPips)
            {
                if (!_openTrades.TryGetValue(positionId, out var trade))
                {
                    _robot.Print("[DebugLog] Warning: No open trade found for ID {0}", positionId);
                    return;
                }

                trade.ExitTime = exitTime;
                trade.ExitReason = exitReason;
                trade.PLPips = plPips;
                trade.Result = plPips >= 0 ? "Win" : "Loss";

                // Update tallies
                string tallyKey = GetTallyKey(trade.ZoneType, trade.EntrySystem, trade.Direction);
                if (!_tallies.ContainsKey(tallyKey))
                {
                    _tallies[tallyKey] = (0, 0);
                }
                var current = _tallies[tallyKey];
                if (trade.Result == "Win")
                    _tallies[tallyKey] = (current.wins + 1, current.losses);
                else
                    _tallies[tallyKey] = (current.wins, current.losses + 1);

                // Store detailed trade if under limit
                string detailKey = GetDetailKey(trade.ZoneType, trade.EntrySystem, trade.Direction, trade.Result);
                if (!_detailedTrades.ContainsKey(detailKey))
                {
                    _detailedTrades[detailKey] = new List<TradeRecord>();
                }
                if (_detailedTrades[detailKey].Count < MAX_TRADES_PER_CATEGORY)
                {
                    _detailedTrades[detailKey].Add(trade);
                }

                _openTrades.Remove(positionId);
                _robot.Print("[DebugLog] Trade closed | ID: {0} | {1} | {2:F1} pips | {3}",
                    positionId, trade.Result, plPips, exitReason);
            }

            private string GetTallyKey(string zoneType, string entrySystem, string direction)
            {
                return $"{zoneType}_{entrySystem}_{direction}";
            }

            private string GetDetailKey(string zoneType, string entrySystem, string direction, string result)
            {
                return $"{zoneType}_{entrySystem}_{direction}_{result}";
            }

            /// <summary>
            /// Saves detailed trade log to file
            /// </summary>
            public void SaveDetailedLog()
            {
                string filename = $"trades_detailed_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filepath = System.IO.Path.Combine(_logFolder, filename);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== DETAILED TRADE LOG ===");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // Order: PRE-Zone first, then Swing; Standard first, then Reversal
                var zoneTypes = new[] { "PRE-Zone", "Swing" };
                var entrySystems = new[] { "Standard", "Reversal" };
                var directions = new[] { "BUY", "SELL" };
                var results = new[] { "Win", "Loss" };

                foreach (var zoneType in zoneTypes)
                {
                    foreach (var entrySystem in entrySystems)
                    {
                        foreach (var direction in directions)
                        {
                            foreach (var result in results)
                            {
                                string key = GetDetailKey(zoneType, entrySystem, direction, result);
                                if (!_detailedTrades.ContainsKey(key) || _detailedTrades[key].Count == 0)
                                    continue;

                                int tradeNum = 0;
                                foreach (var trade in _detailedTrades[key])
                                {
                                    tradeNum++;
                                    sb.AppendLine($"--- {zoneType.ToUpper()} {entrySystem.ToUpper()} {direction} {result.ToUpper()} #{tradeNum} ---");
                                    sb.AppendLine($"Entry Time: {trade.EntryTime:yyyy-MM-dd HH:mm} (Use for Visual Replay)");
                                    sb.AppendLine($"Zone Type: {trade.ZoneType}");
                                    sb.AppendLine($"Entry System: {trade.EntrySystem}");
                                    sb.AppendLine($"Entry: {trade.EntryPrice:F5} | SL: {trade.StopLoss:F5} | TP: {trade.TakeProfit:F5}");
                                    sb.AppendLine($"Exit Time: {trade.ExitTime:yyyy-MM-dd HH:mm}");
                                    sb.AppendLine($"Exit Reason: {trade.ExitReason}");
                                    sb.AppendLine($"P/L: {(trade.PLPips >= 0 ? "+" : "")}{trade.PLPips:F1} pips");
                                    sb.AppendLine();
                                }
                            }
                        }
                    }
                }

                System.IO.File.WriteAllText(filepath, sb.ToString());
                _robot.Print("[DebugLog] Detailed log saved: {0}", filepath);
            }

            /// <summary>
            /// Saves summary log with tallies and replay timestamps
            /// </summary>
            public void SaveSummaryLog()
            {
                string filename = $"trades_summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filepath = System.IO.Path.Combine(_logFolder, filename);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== ZONE TYPE PERFORMANCE ===");
                sb.AppendLine();

                // PRE-Zone stats
                WriteSectionStats(sb, "PRE-ZONE", "PRE-Zone");
                sb.AppendLine();

                // Swing Zone stats
                WriteSectionStats(sb, "SWING ZONE", "Swing");
                sb.AppendLine();

                // Comparison
                sb.AppendLine("=== COMPARISON ===");
                var preStats = GetZoneStats("PRE-Zone");
                var swingStats = GetZoneStats("Swing");
                sb.AppendLine($"PRE-Zone Win Rate:   {preStats.winRate:F1}%");
                sb.AppendLine($"Swing Zone Win Rate: {swingStats.winRate:F1}%");
                sb.AppendLine();

                // Entry system stats
                sb.AppendLine("=== ENTRY SYSTEM PERFORMANCE (Combined) ===");
                sb.AppendLine();
                WriteEntrySystemStats(sb, "STANDARD ENTRY", "Standard");
                WriteEntrySystemStats(sb, "REVERSAL ENTRY", "Reversal");
                sb.AppendLine();

                // Replay timestamps
                sb.AppendLine("=== REPLAY TIMESTAMPS ===");
                sb.AppendLine("(Copy these times to Visual Mode backtest)");
                sb.AppendLine();
                WriteReplayTimestamps(sb);

                System.IO.File.WriteAllText(filepath, sb.ToString());
                _robot.Print("[DebugLog] Summary log saved: {0}", filepath);
            }

            private void WriteSectionStats(System.Text.StringBuilder sb, string header, string zoneType)
            {
                sb.AppendLine($"{header}:");
                foreach (var direction in new[] { "BUY", "SELL" })
                {
                    string key = GetTallyKey(zoneType, "Standard", direction);
                    var stats = _tallies.ContainsKey(key) ? _tallies[key] : (0, 0);
                    int total = stats.wins + stats.losses;
                    double winRate = total > 0 ? (stats.wins * 100.0 / total) : 0;
                    sb.AppendLine($"  Standard {direction}:  Wins: {stats.wins,-3}  Losses: {stats.losses,-3}  Win Rate: {winRate:F1}%");
                }
                var zoneStats = GetZoneStats(zoneType);
                sb.AppendLine($"  TOTAL:         Wins: {zoneStats.wins,-3}  Losses: {zoneStats.losses,-3}  Win Rate: {zoneStats.winRate:F1}%");
            }

            private (int wins, int losses, double winRate) GetZoneStats(string zoneType)
            {
                int wins = 0, losses = 0;
                foreach (var direction in new[] { "BUY", "SELL" })
                {
                    foreach (var entrySystem in new[] { "Standard", "Reversal" })
                    {
                        string key = GetTallyKey(zoneType, entrySystem, direction);
                        if (_tallies.ContainsKey(key))
                        {
                            wins += _tallies[key].wins;
                            losses += _tallies[key].losses;
                        }
                    }
                }
                int total = wins + losses;
                double winRate = total > 0 ? (wins * 100.0 / total) : 0;
                return (wins, losses, winRate);
            }

            private void WriteEntrySystemStats(System.Text.StringBuilder sb, string header, string entrySystem)
            {
                int wins = 0, losses = 0;
                foreach (var zoneType in new[] { "PRE-Zone", "Swing" })
                {
                    foreach (var direction in new[] { "BUY", "SELL" })
                    {
                        string key = GetTallyKey(zoneType, entrySystem, direction);
                        if (_tallies.ContainsKey(key))
                        {
                            wins += _tallies[key].wins;
                            losses += _tallies[key].losses;
                        }
                    }
                }
                int total = wins + losses;
                double winRate = total > 0 ? (wins * 100.0 / total) : 0;
                sb.AppendLine($"{header}:");
                sb.AppendLine($"  Total: Wins: {wins}  Losses: {losses}  Win Rate: {(total > 0 ? winRate.ToString("F1") + "%" : "N/A")}");
            }

            private void WriteReplayTimestamps(System.Text.StringBuilder sb)
            {
                var zoneTypes = new[] { "PRE-Zone", "Swing" };
                var entrySystems = new[] { "Standard", "Reversal" };
                var directions = new[] { "BUY", "SELL" };
                var results = new[] { "Win", "Loss" };

                foreach (var zoneType in zoneTypes)
                {
                    foreach (var entrySystem in entrySystems)
                    {
                        foreach (var direction in directions)
                        {
                            foreach (var result in results)
                            {
                                string key = GetDetailKey(zoneType, entrySystem, direction, result);
                                if (!_detailedTrades.ContainsKey(key) || _detailedTrades[key].Count == 0)
                                    continue;

                                string displayZone = zoneType == "PRE-Zone" ? "PRE-ZONE" : "SWING";
                                sb.AppendLine($"{displayZone} {entrySystem} {direction} {result}:");
                                int num = 0;
                                foreach (var trade in _detailedTrades[key])
                                {
                                    num++;
                                    sb.AppendLine($"  {num}. {trade.EntryTime:yyyy-MM-dd HH:mm}");
                                }
                                sb.AppendLine();
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Prints summary to cTrader log
            /// </summary>
            public void PrintSummaryToLog()
            {
                _robot.Print("========== DEBUG TRADE SUMMARY ==========");

                var preStats = GetZoneStats("PRE-Zone");
                var swingStats = GetZoneStats("Swing");

                _robot.Print("PRE-Zone:  Wins: {0}  Losses: {1}  Win Rate: {2:F1}%",
                    preStats.wins, preStats.losses, preStats.winRate);
                _robot.Print("Swing:     Wins: {0}  Losses: {1}  Win Rate: {2:F1}%",
                    swingStats.wins, swingStats.losses, swingStats.winRate);

                _robot.Print("==========================================");
            }
        }

        #endregion
```

- [ ] **Step 3: Add DebugTradeLogger field and initialize in OnStart**

Find the private fields section (around line 380-450) and add:

```csharp
        // Debug logging
        private DebugTradeLogger _debugLogger;
```

Find `OnStart()` method and add initialization after indicator setup:

```csharp
            // Initialize debug logger
            if (EnableDebugLogging)
            {
                _debugLogger = new DebugTradeLogger(this);
                Print("[DEBUG] DebugTradeLogger initialized");
            }
```

- [ ] **Step 4: Copy file to cAlgo and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

Build in cTrader to verify.

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(debug): Add DebugTradeLogger class and TradeRecord"
```

---

## Task 3: Implement Issue #1 - Price Too Far From Zone

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3009-3090` (UpdateZoneStates method)

- [ ] **Step 1: Add CheckPriceDistanceInvalidation method**

Add after `CheckZoneInvalidation()` method (around line 3157):

```csharp
        /// <summary>
        /// Checks if price has moved too far from an ARMED zone
        /// Issue #1 Fix: Invalidate zones when price moves away without triggering entry
        /// </summary>
        private bool CheckPriceDistanceInvalidation()
        {
            if (activeZone == null || activeZone.State != ZoneState.Armed)
                return false;

            double currentPrice = Symbol.Bid;
            double distancePips;

            if (activeZone.Mode == "SELL")
            {
                // For SELL zone at high, price should be approaching from below
                // If price drops far below the zone, invalidate
                distancePips = (activeZone.BottomPrice - currentPrice) / Symbol.PipSize;
            }
            else // BUY
            {
                // For BUY zone at low, price should be approaching from above
                // If price rises far above the zone, invalidate
                distancePips = (currentPrice - activeZone.TopPrice) / Symbol.PipSize;
            }

            if (distancePips > MaxPriceDistancePips)
            {
                Print("[Zone] Price distance {0:F1} pips > Max {1:F1} pips - INVALIDATING",
                    distancePips, MaxPriceDistancePips);
                return true;
            }

            return false;
        }
```

- [ ] **Step 2: Call the check in UpdateZoneStates**

In `UpdateZoneStates()` method, add after the existing invalidation check (around line 3024):

```csharp
            // Issue #1: Check if price moved too far from ARMED zone
            if (CheckPriceDistanceInvalidation())
            {
                activeZone.State = ZoneState.Invalidated;
                Print("[Zone] Invalidated | Price moved too far away");

                // Cancel pending order if exists
                if (EntryExecution == EntryExecutionMode.PendingStop)
                {
                    CancelZonePendingOrder(activeZone.Id, "Price too far from zone");
                }

                SyncZoneToLegacyVariables();
                return;
            }
```

- [ ] **Step 3: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "fix(zone): Invalidate ARMED zones when price too far (Issue #1)"
```

---

## Task 4: Implement Issue #2 - Danger Session Invalidation

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3009-3090` (UpdateZoneStates method)

- [ ] **Step 1: Add danger session check in UpdateZoneStates**

In `UpdateZoneStates()` method, add after the price distance check:

```csharp
            // Issue #2: Invalidate active zones when entering danger session
            if (EnableSessionFilter)
            {
                DateTime currentTime = Bars.OpenTimes.LastValue;
                OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);
                if (currentPeriod == OptimalPeriod.DangerDeadZone || currentPeriod == OptimalPeriod.DangerLateNY)
                {
                    activeZone.State = ZoneState.Invalidated;
                    Print("[Zone] Invalidated | Entered danger session ({0})",
                        currentPeriod == OptimalPeriod.DangerDeadZone ? "04:00-08:00 UTC" : "20:00-00:00 UTC");

                    // Cancel pending order if exists
                    if (EntryExecution == EntryExecutionMode.PendingStop)
                    {
                        CancelZonePendingOrder(activeZone.Id, "Entered danger session");
                    }

                    SyncZoneToLegacyVariables();
                    return;
                }
            }
```

- [ ] **Step 2: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "fix(zone): Invalidate zones when entering danger session (Issue #2)"
```

---

## Task 5: Implement Issue #3 - Minimum SL Pips

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:4301-4510` (PlaceBuyPendingOrder and PlaceSellPendingOrder)

- [ ] **Step 1: Add minimum SL enforcement in PlaceBuyPendingOrder**

In `PlaceBuyPendingOrder()` method, after the SL calculation (around line 4350), add:

```csharp
            // Issue #3: Enforce minimum SL
            double minimumSLDistance = MinimumSLPips * Symbol.PipSize;
            if ((entryPrice - slPrice) < minimumSLDistance)
            {
                double oldSL = slPrice;
                slPrice = entryPrice - minimumSLDistance;
                slPips = MinimumSLPips;
                Print("[v3.0] BUY SL enforced minimum | Old: {0:F5} | New: {1:F5} ({2:F1} pips)",
                    oldSL, slPrice, slPips);
            }
```

- [ ] **Step 2: Add minimum SL enforcement in PlaceSellPendingOrder**

In `PlaceSellPendingOrder()` method, after the SL calculation (around line 4465), add:

```csharp
            // Issue #3: Enforce minimum SL
            double minimumSLDistance = MinimumSLPips * Symbol.PipSize;
            if ((slPrice - entryPrice) < minimumSLDistance)
            {
                double oldSL = slPrice;
                slPrice = entryPrice + minimumSLDistance;
                slPips = MinimumSLPips;
                Print("[v3.0] SELL SL enforced minimum | Old: {0:F5} | New: {1:F5} ({2:F1} pips)",
                    oldSL, slPrice, slPips);
            }
```

- [ ] **Step 3: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "fix(zone): Enforce minimum SL pips (Issue #3)"
```

---

## Task 6: Implement Issue #5 - Prevent Zone Creation With Open Trade

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:2885-2984` (CreatePreZone)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3728-3834` (UpdateSwingZone)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3009-3090` (UpdateZoneStates - arming logic)

- [ ] **Step 1: Add position check helper method**

Add after `CheckZoneInvalidation()` method:

```csharp
        /// <summary>
        /// Checks if there's an open position for this bot
        /// Issue #5: Block zone creation and arming when position is open
        /// </summary>
        private bool HasOpenPosition()
        {
            var positions = Positions.FindAll(MagicNumber.ToString(), SymbolName);
            return positions.Length > 0;
        }
```

- [ ] **Step 2: Add check at start of CreatePreZone**

In `CreatePreZone()` method, add at the very beginning (after the method signature):

```csharp
            // Issue #5: Block zone creation when position is open
            if (HasOpenPosition())
            {
                Print("[PRE-Zone] Blocked | Position already open");
                return null;
            }
```

- [ ] **Step 3: Add check at start of UpdateSwingZone**

In `UpdateSwingZone()` method, add at the very beginning (after the method signature):

```csharp
            // Issue #5: Block zone creation when position is open
            if (HasOpenPosition())
            {
                Print("[SwingZone] Blocked | Position already open");
                return;
            }
```

- [ ] **Step 4: Add check before arming in UpdateZoneStates**

In `UpdateZoneStates()`, modify the arming section (around line 3051) to include position check:

```csharp
            if (activeZone.State == ZoneState.Valid)
            {
                // Issue #5: Don't arm zones when position is open
                if (HasOpenPosition())
                {
                    return;  // Silently skip - don't arm while position open
                }

                if (CheckZoneProximity())
                {
                    // ... existing arming logic
```

- [ ] **Step 5: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 6: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "fix(zone): Prevent zone creation/arming with open trade (Issue #5)"
```

---

## Task 7: Integrate Debug Logging Into Trade Flow

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (PlaceBuyPendingOrder, PlaceSellPendingOrder, OnPositionClosed)

- [ ] **Step 1: Add zone type tracking field to TradingZone class**

In `TradingZone` class, add:

```csharp
            // Debug tracking
            public bool IsPreZone { get; set; }  // true = PRE-Zone, false = Swing
```

- [ ] **Step 2: Set IsPreZone=true in CreatePreZone**

In `CreatePreZone()` method, when creating the zone object, add:

```csharp
                IsPreZone = true
```

- [ ] **Step 3: Set IsPreZone=false in UpdateSwingZone**

In `UpdateSwingZone()` method, when creating the activeZone object, add:

```csharp
                IsPreZone = false
```

- [ ] **Step 4: Record trade open in PlaceBuyPendingOrder**

At the end of `PlaceBuyPendingOrder()` method, inside the success block (after creating ZonePendingOrder), add:

```csharp
                // Record for debug logging
                if (EnableDebugLogging && _debugLogger != null)
                {
                    string zoneType = zone.IsPreZone ? "PRE-Zone" : "Swing";
                    _debugLogger.RecordTradeOpen(result.PendingOrder.Id, zoneType, "Standard",
                        "BUY", Server.Time, entryPrice, slPrice, tpPrice);
                }
```

- [ ] **Step 5: Record trade open in PlaceSellPendingOrder**

At the end of `PlaceSellPendingOrder()` method, inside the success block, add:

```csharp
                // Record for debug logging
                if (EnableDebugLogging && _debugLogger != null)
                {
                    string zoneType = zone.IsPreZone ? "PRE-Zone" : "Swing";
                    _debugLogger.RecordTradeOpen(result.PendingOrder.Id, zoneType, "Standard",
                        "SELL", Server.Time, entryPrice, slPrice, tpPrice);
                }
```

- [ ] **Step 6: Find or create OnPositionClosed handler**

Search for existing `OnPositionClosed` or `Positions_Closed` handler. If not found, add subscription in OnStart and handler:

In OnStart(), add:
```csharp
            Positions.Closed += OnPositionClosed;
```

Add the handler method:
```csharp
        private void OnPositionClosed(PositionClosedEventArgs args)
        {
            var position = args.Position;

            // Only process our positions
            if (position.Label != MagicNumber.ToString())
                return;

            // Record for debug logging
            if (EnableDebugLogging && _debugLogger != null)
            {
                double plPips = position.Pips;
                string exitReason = DetermineExitReason(position);
                _debugLogger.RecordTradeClose(position.Id, position.CurrentPrice,
                    Server.Time, exitReason, plPips);
            }
        }

        private string DetermineExitReason(Position position)
        {
            // Check what triggered the close
            if (position.StopLoss.HasValue &&
                Math.Abs(position.CurrentPrice - position.StopLoss.Value) < Symbol.PipSize * 2)
            {
                return "SL Hit";
            }
            if (position.TakeProfit.HasValue &&
                Math.Abs(position.CurrentPrice - position.TakeProfit.Value) < Symbol.PipSize * 2)
            {
                return "TP Hit";
            }
            // Chandelier or manual
            return "Chandelier/Manual";
        }
```

- [ ] **Step 7: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 8: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(debug): Integrate trade logging into entry flow"
```

---

## Task 8: Add Debug Log Output on OnStop

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (OnStop method)

- [ ] **Step 1: Add debug log output in OnStop**

Find the `OnStop()` method and add:

```csharp
        protected override void OnStop()
        {
            // Save debug logs at end of backtest
            if (EnableDebugLogging && _debugLogger != null)
            {
                _debugLogger.SaveDetailedLog();
                _debugLogger.SaveSummaryLog();
                _debugLogger.PrintSummaryToLog();
            }

            // Existing OnStop code...
        }
```

- [ ] **Step 2: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(debug): Output debug logs on backtest stop"
```

---

## Task 9: Add Reversal Entry Placeholder

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (UpdateZoneStates)

- [ ] **Step 1: Add placeholder method**

Add after `CheckPriceDistanceInvalidation()` method:

```csharp
        /// <summary>
        /// Placeholder for reversal entry system
        /// When price moves away from zone without triggering, consider placing limit order
        /// </summary>
        private void ProcessReversalEntry(TradingZone zone)
        {
            if (!EnableReversalEntry)
                return;

            // TODO: Implement reversal entry logic
            // - Calculate limit order price at zone boundary
            // - Place limit order betting on price return
            // - Track separately in DebugTradeLogger as "Reversal" entry system
            Print("[Reversal] PLACEHOLDER - Not yet implemented");
        }
```

- [ ] **Step 2: Call placeholder before invalidation due to price distance**

In `UpdateZoneStates()`, modify the price distance invalidation to call reversal first:

```csharp
            // Issue #1: Check if price moved too far from ARMED zone
            if (CheckPriceDistanceInvalidation())
            {
                // Try reversal entry before invalidating
                ProcessReversalEntry(activeZone);

                activeZone.State = ZoneState.Invalidated;
                // ... rest of invalidation code
```

- [ ] **Step 3: Copy and verify compilation**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(zone): Add reversal entry placeholder"
```

---

## Task 10: Update Version and Final Verification

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (Version region)

- [ ] **Step 1: Update version info**

Update the version region at the top of the file:

```csharp
        #region Version Info
        private const string BOT_VERSION = "3.1.0";
        private const string VERSION_DATE = "2026-03-27";
        private const string VERSION_NOTES = "Zone management fixes: price distance, danger session, min SL, position check + debug logging";
        #endregion
```

- [ ] **Step 2: Copy final version to cAlgo**

Run: `cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"`

- [ ] **Step 3: Run short backtest to verify**

Run backtest in cTrader:
- Period: 1 week (e.g., 2024-10-01 to 2024-10-07)
- Check log for debug output
- Verify files created in `D:\JCAMP_FxScalper\DebugLogs\`

- [ ] **Step 4: Final commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "chore: Update version to 3.1.0"
```

---

## Task 11: Update CLAUDE.md Version History

**Files:**
- Modify: `D:\JCAMP_FxScalper\CLAUDE.md`

- [ ] **Step 1: Add v3.1.0 entry**

Add to version history section:

```markdown
### v3.1.0 (2026-03-27)
**Feature:** Zone Management Fixes + Debug Logging
- Issue #1: Invalidate ARMED zones when price moves > MaxPriceDistancePips away
- Issue #2: Invalidate all active zones when entering danger session
- Issue #3: Enforce MinimumSLPips floor for stop loss
- Issue #5: Block zone creation and arming when position is open
- DebugTradeLogger: Captures first 3 trades per category (16 categories total)
- Outputs detailed and summary logs to D:\JCAMP_FxScalper\DebugLogs\
- Reversal entry system placeholder (not yet implemented)
- Design spec: `Docs/superpowers/specs/2026-03-25-zone-manager-refactor-design.md`
- Implementation plan: `Docs/superpowers/plans/2026-03-27-zone-manager-refactor.md`
```

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: Update CLAUDE.md with v3.1.0 changes"
```

---

## Success Criteria

After completing all tasks:

1. **Issue #1:** ARMED zones invalidate when price > MaxPriceDistancePips away
2. **Issue #2:** All active zones invalidate when entering danger session
3. **Issue #3:** No SL smaller than MinimumSLPips
4. **Issue #5:** No new zones created or armed with open position
5. **Debug Logging:** Files generated in DebugLogs folder with:
   - trades_detailed_*.txt - First 3 trades per category
   - trades_summary_*.txt - Win/loss tallies and replay timestamps
6. **Reversal Entry:** Placeholder exists, parameter visible, prints "PLACEHOLDER" message

---

## Notes

- Issue #4 (M1 bar entry checking) is already implemented - `ProcessEntryLogic()` is called on every M1 bar
- The design spec suggested a full ZoneManager class extraction, but this plan takes a simpler approach by adding validation rules directly to existing methods. This is less risky and faster to implement.
- The DebugTradeLogger tracks pending order IDs, but cTrader may assign different IDs when orders fill. The implementation handles this by tracking position IDs when available.

# Enhanced Entry System Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement PRE-zone system that creates trading zones immediately after displacement + FVG detection, solving the "price already moved away" problem.

**Architecture:** Add displacement detection and three-stage zone lifecycle (PRE→VALID→ARMED) alongside existing fractal system. New `TradingZone` class manages zone state while syncing to legacy variables for backward compatibility with entry logic.

**Tech Stack:** C# / cAlgo API / cTrader

**Spec:** `Docs/superpowers/specs/2026-03-12-enhanced-entry-system-design.md`

---

## File Structure

**Single file modification:** `Jcamp_1M_scalping.cs`

| Section | Changes |
|---------|---------|
| Parameters | Add 8 new parameters for PRE-zone system |
| Enums/Classes | Add `ZoneState` enum, `TradingZone` class, `DisplacementCandle` class |
| Private Fields | Add `activeZone`, `atr`, `lastDisplacement` fields |
| OnStart | Initialize ATR indicator |
| OnBar | Add displacement detection, zone creation logic, zone state updates |
| New Region | "Phase 4: PRE-Zone System" with all new functions |
| DetectFVGs | Enhance with min size, max age, IsHighQuality flag |
| DrawSwingRectangle | Color-code by zone state |

---

## Chunk 1: Data Structures & Parameters

### Task 1: Add New Parameters

**Files:**
- Modify: `Jcamp_1M_scalping.cs:208-220` (after FVG Detection parameters)

- [ ] **Step 1: Add PRE-Zone System parameters after FVG Detection region**

```csharp
#region Parameters - PRE-Zone System

[Parameter("=== PRE-ZONE SYSTEM ===", DefaultValue = "")]
public string PreZoneHeader { get; set; }

[Parameter("Enable PRE-Zone System", DefaultValue = true, Group = "PRE-Zone System")]
public bool EnablePreZoneSystem { get; set; }

[Parameter("ATR Period", DefaultValue = 14, MinValue = 5, MaxValue = 50, Group = "PRE-Zone System")]
public int ATRPeriod { get; set; }

[Parameter("ATR Multiplier", DefaultValue = 1.5, MinValue = 1.0, MaxValue = 3.0, Step = 0.1, Group = "PRE-Zone System")]
public double ATRMultiplier { get; set; }

[Parameter("PRE-Zone Expiry (min)", DefaultValue = 60, MinValue = 30, MaxValue = 120, Group = "PRE-Zone System")]
public int PreZoneExpiryMinutes { get; set; }

[Parameter("VALID-Zone Expiry (min)", DefaultValue = 120, MinValue = 60, MaxValue = 240, Group = "PRE-Zone System")]
public int ValidZoneExpiryMinutes { get; set; }

[Parameter("Fractal Zone Tolerance (pips)", DefaultValue = 5.0, MinValue = 2.0, MaxValue = 10.0, Step = 0.5, Group = "PRE-Zone System")]
public double FractalZoneTolerancePips { get; set; }

[Parameter("Min PRE-Zone Score", DefaultValue = 0.50, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "PRE-Zone System")]
public double MinPreZoneScore { get; set; }

#endregion
```

- [ ] **Step 2: Verify code compiles in cTrader**

Build the cBot and verify no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add PRE-Zone System parameters

Add 8 new parameters for displacement detection and zone lifecycle:
- EnablePreZoneSystem (master toggle)
- ATRPeriod, ATRMultiplier (displacement thresholds)
- PreZoneExpiryMinutes, ValidZoneExpiryMinutes (zone timing)
- FractalZoneTolerancePips (fractal confirmation)
- MinPreZoneScore (quality filter)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 2: Add ZoneState Enum and DisplacementCandle Class

**Files:**
- Modify: `Jcamp_1M_scalping.cs:120-207` (after Entry Mode Enum, before Session Enums)

- [ ] **Step 1: Add ZoneState enum after EntryMode enum**

```csharp
#region Zone State Enum

/// <summary>
/// Zone lifecycle states for PRE-zone system
/// Phase 4 Implementation
/// </summary>
public enum ZoneState
{
    Pre,          // Created from displacement + FVG, not yet confirmed
    Valid,        // Confirmed by Williams Fractal
    Armed,        // Price within proximity, ready for entry
    Expired,      // Time limit exceeded
    Invalidated   // Wrong-direction breakout
}

#endregion
```

- [ ] **Step 2: Add DisplacementCandle class after ZoneState enum**

```csharp
#region Displacement Candle Class

/// <summary>
/// Represents a displacement (impulse) candle
/// Phase 4 Implementation
/// </summary>
public class DisplacementCandle
{
    public int BarIndex { get; set; }
    public DateTime Time { get; set; }
    public double ImpulseSize { get; set; }      // Body size in pips
    public double ATRMultiple { get; set; }      // How many × ATR
    public bool IsBullish { get; set; }          // Direction (close > open)
    public double OriginPrice { get; set; }      // Zone anchor point
}

#endregion
```

- [ ] **Step 3: Verify code compiles**

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add ZoneState enum and DisplacementCandle class

ZoneState: Pre, Valid, Armed, Expired, Invalidated
DisplacementCandle: tracks impulse candle properties

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 3: Add TradingZone Class

**Files:**
- Modify: `Jcamp_1M_scalping.cs` (after DisplacementCandle class)

- [ ] **Step 1: Add TradingZone class**

```csharp
#region Trading Zone Class

/// <summary>
/// Represents a trading zone with full lifecycle management
/// Phase 4 Implementation
/// </summary>
public class TradingZone
{
    // Identity
    public string Id { get; set; }
    public ZoneState State { get; set; }

    // Price Levels
    public double TopPrice { get; set; }
    public double BottomPrice { get; set; }
    public double OriginPrice { get; set; }      // Displacement origin for fractal matching

    // Timing
    public DateTime CreatedTime { get; set; }
    public DateTime ExpiryTime { get; set; }

    // Source References
    public DisplacementCandle Displacement { get; set; }
    public FairValueGap FVG { get; set; }
    public int? FractalBarIndex { get; set; }    // Set when upgraded to VALID

    // Scoring
    public double DisplacementScore { get; set; }
    public double FVGScore { get; set; }
    public double SessionScore { get; set; }
    public double PeriodScore { get; set; }
    public double TotalScore { get; set; }

    // Direction
    public string Mode { get; set; }             // "BUY" or "SELL"

    /// <summary>
    /// Creates a unique ID for this zone
    /// </summary>
    public static string GenerateId(DateTime time, string mode)
    {
        return $"Zone_{mode}_{time:yyyyMMdd_HHmmss}";
    }
}

#endregion
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add TradingZone class for zone lifecycle management

Includes price levels, timing, source references, scoring, and direction.
Will replace direct use of swingTopPrice/swingBottomPrice variables.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 4: Enhance FairValueGap Class with New Fields

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Find existing FairValueGap class (around line 195-207)

- [ ] **Step 1: Read current FairValueGap class location**

Search for `class FairValueGap` in the file.

- [ ] **Step 2: Add new fields to FairValueGap class**

Add these fields to the existing class:

```csharp
// NEW fields for Phase 4
public bool IsHighQuality { get; set; }          // Candle B meets displacement criteria
public double GapSizeInPips { get; set; }        // For filtering
public int DisplacementBarIndex { get; set; }    // Links to impulse candle (-1 if none)
```

- [ ] **Step 3: Verify code compiles**

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: enhance FairValueGap with quality tracking fields

Add IsHighQuality, GapSizeInPips, DisplacementBarIndex
for linking FVGs to displacement candles.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 5: Add Private Fields for PRE-Zone System

**Files:**
- Modify: `Jcamp_1M_scalping.cs:323-335` (after Phase 3 FVG tracking, before Visualization tracking)

- [ ] **Step 1: Add new private fields**

```csharp
// Phase 4: PRE-Zone System
private AverageTrueRange atr;                    // ATR indicator for displacement detection
private TradingZone activeZone = null;           // Current active zone (or null)
private DisplacementCandle lastDisplacement = null;  // Most recent displacement detected

// Phase 4: Zone colors
private readonly Color ColorPreZone = Color.FromArgb(60, 255, 255, 0);    // Yellow (PRE)
private readonly Color ColorValidZone = Color.FromArgb(60, 0, 128, 255);  // Blue (VALID)
private readonly Color ColorArmedZone = Color.FromArgb(60, 0, 255, 0);    // Green (ARMED)
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add private fields for PRE-Zone system

Add ATR indicator, activeZone, lastDisplacement fields.
Add zone state colors (Yellow/Blue/Green).

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 2: ATR Initialization & Displacement Detection

### Task 6: Initialize ATR Indicator in OnStart

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - OnStart method (around line 340-460)

- [ ] **Step 1: Find where m15Bars is initialized in OnStart**

Look for the line that initializes `m15Bars`.

- [ ] **Step 2: Add ATR initialization after m15Bars**

Add this line after m15Bars is set:

```csharp
// Phase 4: Initialize ATR indicator for displacement detection
if (EnablePreZoneSystem)
{
    atr = Indicators.AverageTrueRange(m15Bars, ATRPeriod, MovingAverageType.Simple);
    Print("[PRE-Zone] ATR indicator initialized | Period: {0} | Multiplier: {1:F1}x", ATRPeriod, ATRMultiplier);
}
```

- [ ] **Step 3: Add PRE-Zone system status to OnStart logging**

Find the existing initialization logging and add:

```csharp
// Log PRE-Zone system status
Print("PRE-Zone System: {0} | ATR: {1} | Multiplier: {2:F1}x | Min Score: {3:F2}",
    EnablePreZoneSystem ? "ON" : "OFF",
    ATRPeriod,
    ATRMultiplier,
    MinPreZoneScore);
```

- [ ] **Step 4: Verify code compiles and run quick test**

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: initialize ATR indicator in OnStart

ATR used for displacement detection when PRE-Zone system enabled.
Adds status logging for PRE-Zone configuration.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 7: Add Displacement Detection Functions

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add new region after Phase 3 FVG Detection

- [ ] **Step 1: Create Phase 4 PRE-Zone System region**

Add after the `#endregion` of Phase 3 FVG Detection (around line 1433):

```csharp
#region Phase 4: PRE-Zone System

/// <summary>
/// Detects if the current M15 bar is a displacement (impulse) candle
/// Displacement = Body Size >= ATRMultiplier × ATR
/// Phase 4 Implementation
/// </summary>
private DisplacementCandle DetectDisplacement()
{
    if (!EnablePreZoneSystem || atr == null)
        return null;

    int lastIdx = m15Bars.Count - 2;  // Last completed bar
    if (lastIdx < 1)
        return null;

    // Calculate body size
    double open = m15Bars.OpenPrices[lastIdx];
    double close = m15Bars.ClosePrices[lastIdx];
    double high = m15Bars.HighPrices[lastIdx];
    double low = m15Bars.LowPrices[lastIdx];
    double bodySize = Math.Abs(close - open);

    // Get ATR value
    double atrValue = atr.Result[lastIdx];
    if (atrValue <= 0)
        return null;

    // Check displacement threshold
    double atrMultiple = bodySize / atrValue;
    if (atrMultiple < ATRMultiplier)
        return null;

    // Displacement detected!
    bool isBullish = close > open;
    double originPrice = isBullish ? low : high;  // Where move started
    double bodySizePips = bodySize / Symbol.PipSize;

    var displacement = new DisplacementCandle
    {
        BarIndex = lastIdx,
        Time = m15Bars.OpenTimes[lastIdx],
        ImpulseSize = bodySizePips,
        ATRMultiple = atrMultiple,
        IsBullish = isBullish,
        OriginPrice = originPrice
    };

    Print("[Displacement] {0} impulse at {1} | Size: {2:F1} pips | ATR x {3:F1}",
        isBullish ? "Bullish" : "Bearish",
        displacement.Time.ToString("HH:mm"),
        bodySizePips,
        atrMultiple);

    return displacement;
}

/// <summary>
/// Checks if a specific bar index qualifies as a displacement candle
/// Used for FVG quality checking
/// Phase 4 Implementation
/// </summary>
private bool IsDisplacementCandle(int barIndex)
{
    if (!EnablePreZoneSystem || atr == null)
        return false;

    if (barIndex < 1 || barIndex >= m15Bars.Count)
        return false;

    double open = m15Bars.OpenPrices[barIndex];
    double close = m15Bars.ClosePrices[barIndex];
    double bodySize = Math.Abs(close - open);

    double atrValue = atr.Result[barIndex];
    if (atrValue <= 0)
        return false;

    return (bodySize / atrValue) >= ATRMultiplier;
}

#endregion
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add displacement detection functions

DetectDisplacement(): Scans last M15 bar for impulse candle
IsDisplacementCandle(): Checks specific bar for displacement criteria
Both use ATR-based threshold (default 1.5x ATR).

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 3: Enhanced FVG Detection

### Task 8: Enhance DetectFVGs with Quality Filtering

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - DetectFVGs method (around line 1292-1362)

- [ ] **Step 1: Read current DetectFVGs implementation**

- [ ] **Step 2: Update DetectFVGs to add filtering and IsHighQuality flag**

Replace the method with enhanced version:

```csharp
/// <summary>
/// Detects Fair Value Gaps (FVGs) on M15 timeframe
/// ENHANCED: Adds minimum size filter, max age filter, and displacement quality flag
/// Phase 4 Enhancement
/// </summary>
private void DetectFVGs()
{
    activeFVGs.Clear();

    int lookback = Math.Min(FVGLookbackBars, m15Bars.Count - 3);

    for (int i = 1; i < lookback - 1; i++)
    {
        int idx = m15Bars.Count - 1 - i;  // Impulse candle index (Candle B)

        if (idx - 1 < 0 || idx + 1 >= m15Bars.Count)
            continue;

        // Check age limit (Phase 4 enhancement)
        if (i > FVGMaxAgeBars)
            continue;

        double candleA_High = m15Bars.HighPrices[idx - 1];
        double candleA_Low = m15Bars.LowPrices[idx - 1];
        double candleC_High = m15Bars.HighPrices[idx + 1];
        double candleC_Low = m15Bars.LowPrices[idx + 1];

        // Check if Candle B (impulse) is a displacement candle
        bool isDisplacement = IsDisplacementCandle(idx);

        // BULLISH FVG: Candle A's HIGH is BELOW Candle C's LOW
        if (candleA_High < candleC_Low)
        {
            double gapSize = candleC_Low - candleA_High;
            double gapSizePips = gapSize / Symbol.PipSize;

            // Phase 4: Minimum size filter
            if (gapSizePips < MinFVGSizePips)
            {
                Print("[FVG] Filtered: gap too small ({0:F1} pips < {1:F1} min)", gapSizePips, MinFVGSizePips);
                continue;
            }

            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                BottomPrice = candleA_High,
                TopPrice = candleC_Low,
                IsBullish = true,
                IsFilled = false,
                // Phase 4 new fields
                IsHighQuality = isDisplacement,
                GapSizeInPips = gapSizePips,
                DisplacementBarIndex = isDisplacement ? idx : -1
            };

            fvg.IsFilled = IsFVGFilled(fvg, idx + 1);

            if (!fvg.IsFilled)
            {
                activeFVGs.Add(fvg);
                Print("[FVG] {0} Bullish gap | Zone: {1:F5} - {2:F5} | Size: {3:F1} pips",
                    fvg.IsHighQuality ? "High-quality" : "Standard",
                    fvg.BottomPrice, fvg.TopPrice, gapSizePips);
            }
        }

        // BEARISH FVG: Candle A's LOW is ABOVE Candle C's HIGH
        if (candleA_Low > candleC_High)
        {
            double gapSize = candleA_Low - candleC_High;
            double gapSizePips = gapSize / Symbol.PipSize;

            // Phase 4: Minimum size filter
            if (gapSizePips < MinFVGSizePips)
            {
                Print("[FVG] Filtered: gap too small ({0:F1} pips < {1:F1} min)", gapSizePips, MinFVGSizePips);
                continue;
            }

            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                TopPrice = candleA_Low,
                BottomPrice = candleC_High,
                IsBullish = false,
                IsFilled = false,
                // Phase 4 new fields
                IsHighQuality = isDisplacement,
                GapSizeInPips = gapSizePips,
                DisplacementBarIndex = isDisplacement ? idx : -1
            };

            fvg.IsFilled = IsFVGFilled(fvg, idx + 1);

            if (!fvg.IsFilled)
            {
                activeFVGs.Add(fvg);
                Print("[FVG] {0} Bearish gap | Zone: {1:F5} - {2:F5} | Size: {3:F1} pips",
                    fvg.IsHighQuality ? "High-quality" : "Standard",
                    fvg.TopPrice, fvg.BottomPrice, gapSizePips);
            }
        }
    }

    int highQualityCount = activeFVGs.Count(f => f.IsHighQuality);
    Print("[FVG] Scan complete | Active: {0} | High-quality: {1}", activeFVGs.Count, highQualityCount);
}
```

- [ ] **Step 3: Add using System.Linq if not present**

Check top of file for `using System.Linq;` - the `.Count()` extension method requires it.

- [ ] **Step 4: Verify code compiles**

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: enhance DetectFVGs with quality filtering

- Add minimum gap size filter (MinFVGSizePips)
- Add max age filter (FVGMaxAgeBars)
- Add IsHighQuality flag when Candle B is displacement
- Track GapSizeInPips and DisplacementBarIndex
- Log high-quality vs standard FVGs

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 4: PRE-Zone Scoring Functions

### Task 9: Add PRE-Zone Scoring Functions

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add to Phase 4 region

- [ ] **Step 1: Add scoring functions to Phase 4 region**

```csharp
/// <summary>
/// Calculates displacement strength score (40% of PRE-zone score)
/// Based on ATR multiple of the impulse candle
/// Phase 4 Implementation
/// </summary>
private double CalculateDisplacementStrength(double atrMultiple)
{
    if (atrMultiple >= 3.0) return 1.0;   // Exceptional
    if (atrMultiple >= 2.5) return 0.9;
    if (atrMultiple >= 2.0) return 0.8;
    if (atrMultiple >= 1.5) return 0.7;   // Minimum
    return 0.0;                            // Not a displacement
}

/// <summary>
/// Calculates FVG quality score (25% of PRE-zone score)
/// Based on gap size in pips
/// Phase 4 Implementation
/// </summary>
private double CalculateFVGQuality(double gapSizePips)
{
    if (gapSizePips >= 5.0) return 1.0;   // Large gap
    if (gapSizePips >= 3.0) return 0.8;
    if (gapSizePips >= 2.0) return 0.6;
    if (gapSizePips >= 1.5) return 0.5;   // Minimum
    return 0.0;                            // Too small (filtered)
}

/// <summary>
/// Calculates session alignment score for PRE-zones (25% of score)
/// Checks if zone price aligns with session high/low
/// Phase 4 Implementation
/// </summary>
private double CalculateSessionAlignmentForZone(double zonePrice, DateTime zoneTime, string mode)
{
    SessionLevels session = GetSessionForTime(zoneTime);
    if (session == null)
        return 0.5;  // Neutral if no session found

    double targetLevel = mode == "SELL" ? session.High : session.Low;
    double distancePips = Math.Abs(zonePrice - targetLevel) / Symbol.PipSize;

    if (distancePips <= 0) return 1.0;    // AT session level
    if (distancePips <= 5) return 0.85;   // NEAR
    if (distancePips <= 10) return 0.7;   // CLOSE
    return 0.5;                            // Not aligned
}

/// <summary>
/// Calculates optimal period score for PRE-zones (10% of score)
/// Uses positive-only values (no negative penalties)
/// Phase 4 Implementation
/// </summary>
private double CalculateOptimalPeriodScore(DateTime time)
{
    OptimalPeriod period = GetOptimalPeriod(time);

    switch (period)
    {
        case OptimalPeriod.BestOverlap:
            return 1.0;
        case OptimalPeriod.GoodLondonOpen:
            return 0.75;
        case OptimalPeriod.DangerDeadZone:
        case OptimalPeriod.DangerLateNY:
            return 0.25;
        default:
            return 0.5;  // Neutral times
    }
}

/// <summary>
/// Calculates total PRE-zone score
/// Formula: Displacement(40%) + FVG(25%) + Session(25%) + Period(10%)
/// Phase 4 Implementation
/// </summary>
private double CalculatePreZoneScore(DisplacementCandle displacement, FairValueGap fvg, string mode)
{
    double dispScore = CalculateDisplacementStrength(displacement.ATRMultiple);
    double fvgScore = CalculateFVGQuality(fvg.GapSizeInPips);
    double sessionScore = CalculateSessionAlignmentForZone(displacement.OriginPrice, displacement.Time, mode);
    double periodScore = CalculateOptimalPeriodScore(displacement.Time);

    double totalScore =
        (dispScore * 0.40) +
        (fvgScore * 0.25) +
        (sessionScore * 0.25) +
        (periodScore * 0.10);

    Print("[PRE-Zone] Scoring: Disp={0:F2} FVG={1:F2} Session={2:F2} Period={3:F2} | Total={4:F2}",
        dispScore, fvgScore, sessionScore, periodScore, totalScore);

    return totalScore;
}
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add PRE-zone scoring functions

- CalculateDisplacementStrength(): 40% weight, ATR multiple based
- CalculateFVGQuality(): 25% weight, gap size based
- CalculateSessionAlignmentForZone(): 25% weight, session high/low
- CalculateOptimalPeriodScore(): 10% weight, positive-only values
- CalculatePreZoneScore(): combines all components

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 5: Zone Lifecycle Management

### Task 10: Add Zone Creation and Upgrade Functions

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add to Phase 4 region

- [ ] **Step 1: Add zone creation function**

```csharp
/// <summary>
/// Creates a PRE-zone from displacement + high-quality FVG
/// Zone is created immediately (15 min faster than fractal)
/// Phase 4 Implementation
/// </summary>
private TradingZone CreatePreZone(DisplacementCandle displacement, FairValueGap fvg)
{
    // Determine mode based on displacement direction
    // Bearish displacement (price dropped) = SELL zone at high
    // Bullish displacement (price rose) = BUY zone at low
    string mode = displacement.IsBullish ? "BUY" : "SELL";

    // Calculate zone boundaries (4 pips total width)
    double originPrice = displacement.OriginPrice;
    double topPrice = originPrice + (2 * Symbol.PipSize);
    double bottomPrice = originPrice - (2 * Symbol.PipSize);

    // Calculate score
    double score = CalculatePreZoneScore(displacement, fvg, mode);

    // Check minimum score threshold
    if (score < MinPreZoneScore)
    {
        Print("[PRE-Zone] Rejected | Score {0:F2} < Min {1:F2}", score, MinPreZoneScore);
        return null;
    }

    // Check if existing zone has higher score
    if (activeZone != null && activeZone.State != ZoneState.Expired && activeZone.State != ZoneState.Invalidated)
    {
        if (activeZone.TotalScore >= score)
        {
            Print("[PRE-Zone] Rejected | Existing zone has higher score ({0:F2} >= {1:F2})",
                activeZone.TotalScore, score);
            return null;
        }
        Print("[PRE-Zone] Replacing existing zone (new score {0:F2} > old {1:F2})",
            score, activeZone.TotalScore);
    }

    // Create the zone
    var zone = new TradingZone
    {
        Id = TradingZone.GenerateId(displacement.Time, mode),
        State = ZoneState.Pre,
        TopPrice = topPrice,
        BottomPrice = bottomPrice,
        OriginPrice = originPrice,
        CreatedTime = Server.Time,
        ExpiryTime = Server.Time.AddMinutes(PreZoneExpiryMinutes),
        Displacement = displacement,
        FVG = fvg,
        FractalBarIndex = null,
        DisplacementScore = CalculateDisplacementStrength(displacement.ATRMultiple),
        FVGScore = CalculateFVGQuality(fvg.GapSizeInPips),
        SessionScore = CalculateSessionAlignmentForZone(originPrice, displacement.Time, mode),
        PeriodScore = CalculateOptimalPeriodScore(displacement.Time),
        TotalScore = score,
        Mode = mode
    };

    Print("[PRE-Zone] Created {0} zone | Price: {1:F5}-{2:F5} | Score: {3:F2} | Expiry: {4}",
        mode, bottomPrice, topPrice, score, zone.ExpiryTime.ToString("HH:mm"));

    return zone;
}

/// <summary>
/// Upgrades PRE-zone to VALID when Williams Fractal confirms
/// Extends expiry time to ValidZoneExpiryMinutes
/// Phase 4 Implementation
/// </summary>
private void UpgradeToValidZone(TradingZone zone, int fractalBarIndex)
{
    if (zone == null || zone.State != ZoneState.Pre)
        return;

    zone.State = ZoneState.Valid;
    zone.FractalBarIndex = fractalBarIndex;
    zone.ExpiryTime = Server.Time.AddMinutes(ValidZoneExpiryMinutes);

    Print("[Zone] Upgraded to VALID | Fractal confirmed at bar {0} | New expiry: {1}",
        fractalBarIndex, zone.ExpiryTime.ToString("HH:mm"));
}
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add zone creation and upgrade functions

CreatePreZone(): Creates PRE-zone from displacement + FVG
- Calculates 4-pip zone around origin
- Checks minimum score threshold
- Handles zone replacement (higher score wins)

UpgradeToValidZone(): Upgrades PRE to VALID on fractal confirmation
- Extends expiry time
- Records fractal bar index

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 11: Add Zone State Management Functions

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add to Phase 4 region

- [ ] **Step 1: Add zone state management functions**

```csharp
/// <summary>
/// Updates zone states: checks expiry, proximity (arming), invalidation
/// Called on every M1 bar
/// Phase 4 Implementation
/// </summary>
private void UpdateZoneStates()
{
    if (activeZone == null)
        return;

    // Skip if already expired or invalidated
    if (activeZone.State == ZoneState.Expired || activeZone.State == ZoneState.Invalidated)
        return;

    // Check invalidation first (applies to all states)
    if (CheckZoneInvalidation())
    {
        activeZone.State = ZoneState.Invalidated;
        Print("[Zone] Invalidated | Body closed wrong direction");
        SyncZoneToLegacyVariables();
        return;
    }

    // Check expiry (skip if ARMED - armed zones stay until entry or invalidation)
    if (activeZone.State != ZoneState.Armed)
    {
        if (Server.Time > activeZone.ExpiryTime)
        {
            activeZone.State = ZoneState.Expired;
            Print("[Zone] Expired | No entry triggered | Was: {0} at {1:F5}",
                activeZone.State == ZoneState.Pre ? "PRE-Zone" : "VALID-Zone",
                activeZone.OriginPrice);
            SyncZoneToLegacyVariables();
            return;
        }
    }

    // Check proximity for arming (if not already armed)
    if (activeZone.State == ZoneState.Pre || activeZone.State == ZoneState.Valid)
    {
        if (CheckZoneProximity())
        {
            activeZone.State = ZoneState.Armed;
            double distancePips = GetDistanceToZone();
            Print("[Zone] ARMED | Price within {0:F1} pips of zone", distancePips);
            SyncZoneToLegacyVariables();
        }
    }
}

/// <summary>
/// Checks if current price is within MaxDistanceToArm of the zone
/// Phase 4 Implementation
/// </summary>
private bool CheckZoneProximity()
{
    if (activeZone == null)
        return false;

    double currentPrice = Symbol.Bid;
    double distancePips = GetDistanceToZone();

    return distancePips <= MaxDistanceToArm;
}

/// <summary>
/// Gets distance from current price to zone in pips
/// Phase 4 Implementation
/// </summary>
private double GetDistanceToZone()
{
    if (activeZone == null)
        return double.MaxValue;

    double currentPrice = Symbol.Bid;

    if (activeZone.Mode == "SELL")
    {
        // For SELL, price should be approaching from below
        return (activeZone.BottomPrice - currentPrice) / Symbol.PipSize;
    }
    else
    {
        // For BUY, price should be approaching from above
        return (currentPrice - activeZone.TopPrice) / Symbol.PipSize;
    }
}

/// <summary>
/// Checks if zone should be invalidated (wrong-direction breakout)
/// Phase 4 Implementation
/// </summary>
private bool CheckZoneInvalidation()
{
    if (activeZone == null)
        return false;

    // Get last closed M1 candle
    int lastIdx = Bars.Count - 2;
    if (lastIdx < 0)
        return false;

    double open = Bars.OpenPrices[lastIdx];
    double close = Bars.ClosePrices[lastIdx];

    if (activeZone.Mode == "SELL")
    {
        // SELL zone invalidated if body closes ABOVE zone top
        return (close > activeZone.TopPrice && open > activeZone.TopPrice);
    }
    else
    {
        // BUY zone invalidated if body closes BELOW zone bottom
        return (close < activeZone.BottomPrice && open < activeZone.BottomPrice);
    }
}

/// <summary>
/// Syncs activeZone to legacy variables for backward compatibility
/// Entry logic reads these variables unchanged
/// Phase 4 Implementation
/// </summary>
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

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add zone state management functions

UpdateZoneStates(): Main lifecycle manager
- Checks invalidation (wrong-direction breakout)
- Checks expiry (skips if ARMED)
- Checks proximity for arming

CheckZoneProximity(): Distance check for arming
CheckZoneInvalidation(): Wrong-direction breakout check
GetDistanceToZone(): Distance calculation
SyncZoneToLegacyVariables(): Backward compatibility sync

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 6: Integration with OnBar

### Task 12: Add PRE-Zone Logic to OnBar

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - OnBar method (around line 467-600)

- [ ] **Step 1: Read current OnBar structure to understand where to add calls**

- [ ] **Step 2: Add displacement detection after DetectFVGs call**

Find where `DetectFVGs()` is called and add after it:

```csharp
// Phase 4: Detect displacement and create PRE-zone if applicable
if (EnablePreZoneSystem)
{
    lastDisplacement = DetectDisplacement();

    // Check for high-quality FVG that matches displacement direction
    if (lastDisplacement != null)
    {
        var matchingFVG = FindMatchingHighQualityFVG(lastDisplacement);
        if (matchingFVG != null)
        {
            var newZone = CreatePreZone(lastDisplacement, matchingFVG);
            if (newZone != null)
            {
                activeZone = newZone;
                SyncZoneToLegacyVariables();

                if (ShowRectangles)
                {
                    DrawZoneRectangle();
                }
            }
        }
    }
}
```

- [ ] **Step 3: Add helper function to find matching FVG**

Add to Phase 4 region:

```csharp
/// <summary>
/// Finds a high-quality FVG that matches the displacement direction
/// If multiple, returns the largest gap
/// Phase 4 Implementation
/// </summary>
private FairValueGap FindMatchingHighQualityFVG(DisplacementCandle displacement)
{
    // Find high-quality FVGs in the same direction
    var matchingFVGs = activeFVGs
        .Where(f => f.IsHighQuality && f.IsBullish == displacement.IsBullish)
        .OrderByDescending(f => f.GapSizeInPips)
        .ToList();

    if (matchingFVGs.Count == 0)
    {
        Print("[PRE-Zone] No matching high-quality FVG for {0} displacement",
            displacement.IsBullish ? "bullish" : "bearish");
        return null;
    }

    return matchingFVGs[0];  // Return largest gap
}
```

- [ ] **Step 4: Verify code compiles**

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: integrate PRE-zone detection into OnBar

- Add displacement detection after FVG detection
- Find matching high-quality FVG (same direction, largest gap)
- Create PRE-zone if displacement + FVG found
- Sync to legacy variables and draw rectangle

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 13: Add Fractal-to-PRE-Zone Upgrade Logic

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add logic after FindSignificantSwing

- [ ] **Step 1: Find where FindSignificantSwing result is processed**

- [ ] **Step 2: Add fractal confirmation check before creating fractal zone**

After `FindSignificantSwing()` returns a valid swing index, add:

```csharp
// Phase 4: Check if fractal confirms existing PRE-zone
if (EnablePreZoneSystem && activeZone != null && activeZone.State == ZoneState.Pre)
{
    double fractalPrice = currentMode == "SELL" ?
        m15Bars.HighPrices[significantSwingIdx] :
        m15Bars.LowPrices[significantSwingIdx];

    double distanceToZone = Math.Abs(fractalPrice - activeZone.OriginPrice) / Symbol.PipSize;

    if (distanceToZone <= FractalZoneTolerancePips)
    {
        // Fractal confirms PRE-zone - upgrade to VALID
        UpgradeToValidZone(activeZone, significantSwingIdx);
        SyncZoneToLegacyVariables();

        if (ShowRectangles)
        {
            DrawZoneRectangle();  // Redraw with VALID color
        }

        // Skip normal fractal zone creation
        return;
    }
}
```

- [ ] **Step 3: Verify code compiles**

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add fractal confirmation for PRE-zone upgrade

When Williams Fractal forms within FractalZoneTolerancePips of PRE-zone:
- Upgrade PRE to VALID
- Extend expiry time
- Redraw rectangle with VALID color

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

### Task 14: Add UpdateZoneStates Call to M1 Processing

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - OnBar method, M1 processing section

- [ ] **Step 1: Find M1 bar processing section in OnBar**

Look for where `ProcessEntryLogic()` is called.

- [ ] **Step 2: Add UpdateZoneStates before ProcessEntryLogic**

```csharp
// Phase 4: Update zone states (expiry, arming, invalidation)
if (EnablePreZoneSystem && activeZone != null)
{
    UpdateZoneStates();
}
```

- [ ] **Step 3: Verify code compiles**

- [ ] **Step 4: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add zone state updates to M1 processing

UpdateZoneStates() called every M1 bar to:
- Check zone expiry
- Check proximity for arming
- Check invalidation

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 7: Visualization Updates

### Task 15: Add Zone Rectangle Drawing Function

**Files:**
- Modify: `Jcamp_1M_scalping.cs` - Add to Phase 4 region or Visualization region

- [ ] **Step 1: Add DrawZoneRectangle function**

```csharp
/// <summary>
/// Draws rectangle for active zone with color based on state
/// PRE = Yellow, VALID = Blue, ARMED = Green
/// Phase 4 Implementation
/// </summary>
private void DrawZoneRectangle()
{
    if (activeZone == null || !ShowRectangles)
        return;

    // Remove old zone rectangle if exists
    string oldRectName = $"ZoneRect_{activeZone.Id}";
    if (Chart.FindObject(oldRectName) != null)
    {
        Chart.RemoveObject(oldRectName);
    }

    // Select color based on state
    Color rectColor;
    switch (activeZone.State)
    {
        case ZoneState.Pre:
            rectColor = ColorPreZone;      // Yellow
            break;
        case ZoneState.Valid:
            rectColor = ColorValidZone;    // Blue
            break;
        case ZoneState.Armed:
            rectColor = ColorArmedZone;    // Green
            break;
        default:
            return;  // Don't draw expired/invalidated
    }

    // Calculate rectangle times
    DateTime startTime = activeZone.CreatedTime;
    DateTime endTime = activeZone.ExpiryTime;

    // Draw rectangle
    string rectName = $"ZoneRect_{activeZone.Id}";
    var rect = Chart.DrawRectangle(rectName, startTime, activeZone.TopPrice,
        endTime, activeZone.BottomPrice, rectColor);
    rect.IsFilled = true;

    // Add label
    string stateLabel = activeZone.State.ToString().ToUpper();
    string labelName = $"ZoneLabel_{activeZone.Id}";
    Chart.DrawText(labelName, $"{activeZone.Mode} {stateLabel} ({activeZone.TotalScore:F2})",
        startTime, activeZone.TopPrice + (5 * Symbol.PipSize), rectColor);
}
```

- [ ] **Step 2: Verify code compiles**

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat: add zone rectangle drawing with state colors

DrawZoneRectangle():
- PRE zones = Yellow
- VALID zones = Blue
- ARMED zones = Green
- Includes score label

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Chunk 8: Testing & Validation

### Task 16: Manual Backtest - PRE-Zone Detection

**No code changes - testing task**

- [ ] **Step 1: Configure cTrader backtest**

```
Symbol: EURUSD
Timeframe: M1
Period: 2025-01-15 to 2025-01-22 (1 week)
Visual Mode: ON
Enable Trading: FALSE
Enable PRE-Zone System: TRUE
ATR Multiplier: 1.5
Min FVG Size: 1.5 pips
```

- [ ] **Step 2: Run backtest and verify console output**

Expected output:
```
[PRE-Zone] ATR indicator initialized | Period: 14 | Multiplier: 1.5x
[Displacement] Bullish impulse at XX:XX | Size: XX.X pips | ATR x X.X
[FVG] High-quality Bullish gap | Zone: X.XXXXX - X.XXXXX | Size: X.X pips
[PRE-Zone] Created BUY zone | Price: X.XXXXX-X.XXXXX | Score: X.XX | Expiry: XX:XX
```

- [ ] **Step 3: Verify zone rectangles appear on chart**

- Yellow rectangles for PRE-zones
- Blue rectangles when upgraded to VALID
- Green rectangles when ARMED

- [ ] **Step 4: Document test results**

Create test log noting:
- Number of displacements detected
- Number of PRE-zones created
- Any errors or unexpected behavior

---

### Task 17: Manual Backtest - Zone Lifecycle

**No code changes - testing task**

- [ ] **Step 1: Run 48-hour backtest with trading enabled**

```
Enable Trading: TRUE
Risk Per Trade: 0.5%
```

- [ ] **Step 2: Verify zone state transitions**

Check console for:
- PRE → VALID upgrades
- VALID/PRE → ARMED transitions
- Expiry messages
- Invalidation messages

- [ ] **Step 3: Verify trades execute correctly**

- Trades should trigger from ARMED zones
- SL/TP should be calculated correctly
- Entry logic should work unchanged

- [ ] **Step 4: Compare timing**

Note when PRE-zones are created vs. when fractals would have confirmed.
Expected: PRE-zones ~15 minutes earlier.

---

### Task 18: Commit Final Testing Results

- [ ] **Step 1: Create validation document**

```bash
echo "PRE-Zone System Validation - $(date)
======================================
Displacement Detection: PASS/FAIL
FVG Quality Filtering: PASS/FAIL
PRE-Zone Creation: PASS/FAIL
Zone State Transitions: PASS/FAIL
Legacy Variable Sync: PASS/FAIL
Trade Execution: PASS/FAIL
" > Docs/PRE_ZONE_VALIDATION.md
```

- [ ] **Step 2: Commit validation results**

```bash
git add Docs/PRE_ZONE_VALIDATION.md
git commit -m "test: add PRE-zone system validation results

Document backtest results for enhanced entry system.

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"
```

---

## Summary

**Total Tasks:** 18
**Estimated New Code:** ~510 lines
**Files Modified:** 1 (`Jcamp_1M_scalping.cs`)

**Key Deliverables:**
1. Displacement detection (ATR-based)
2. Enhanced FVG with quality filtering
3. Three-stage zone lifecycle (PRE → VALID → ARMED)
4. PRE-zone scoring system
5. Legacy variable sync for backward compatibility
6. Color-coded zone visualization

**Success Criteria:**
- PRE-zones created ~15 minutes faster than fractal zones
- Existing entry logic works unchanged
- Zone state transitions work correctly
- Backtest shows improved entry timing

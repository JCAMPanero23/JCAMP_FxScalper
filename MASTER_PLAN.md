# Significant Swing Detection & Rectangle Drawing - Jcamp 1M Scalping
## MASTER IMPLEMENTATION PLAN

**Date Created:** 2026-03-07
**Strategy:** Jcamp 1M Scalping with M15 Swing Detection
**Approach:** Two-Stage Structure with Advanced Scoring (Institutional/SMC)

---

## 📋 Table of Contents

1. [Current Situation](#current-situation)
2. [Problem Analysis](#problem-analysis)
3. [Proposed Solutions](#proposed-solutions)
4. [Implementation Strategy](#implementation-strategy)
5. [Phase 1A: Basic Validity & Scoring](#phase-1a-basic-validity--scoring)
6. [Phase 1B: Entry Logic](#phase-1b-entry-logic)
7. [Phase 1C: M1 Market Structure TP](#phase-1c-m1-market-structure-tp)
8. [Phase 2: Session Awareness](#phase-2-session-awareness)
9. [Phase 3: FVG Detection](#phase-3-fvg-detection)
10. [Entry Logic Details](#entry-logic-details)
11. [Testing & Verification](#testing--verification)
12. [Success Criteria](#success-criteria)

---

## Current Situation

### What's Working ✅
- ✅ cBot draws rectangles at M15 swing points (Williams Fractals)
- ✅ Rectangles visible in backtest on M1 and M15 charts
- ✅ Mode label shows BUY/SELL based on EMA 200
- ✅ All-in-one system (no separate indicator needed)

### The Problem ❌
- ❌ Current logic finds FIRST valid swing in lookback period (30 bars)
- ❌ May draw rectangles for insignificant/minor swings
- ❌ Doesn't filter for the MOST SIGNIFICANT swing
- ❌ No awareness of broader market structure (100 bars context)

### User Requirements
1. Strategy should be aware of significant levels from **up to 100 bars back** (market structure)
2. Only create rectangles for **CORRECT/SIGNIFICANT** swings (not every swing)
3. Better logic to predict which swing is worthy of rectangle drawing
4. Rectangle timing: Draw from swing bar + **60 minutes** forward (changed from 50)

---

## Problem Analysis

### Current Flow
```
New M15 bar → Scan last 30 bars backwards → Find FIRST Williams Fractal → Draw rectangle
```

### Issues
1. **First != Best** - First swing found may not be most significant
2. **No scoring** - All valid fractals treated equally
3. **Recency bias** - Finds recent swings even if weak
4. **No context** - Ignores market structure beyond 30 bars

### What Makes a Swing "Significant"?

**Option A: Strength-Based (Fractal Quality)**
- Measure how far swing extends beyond neighboring bars
- Higher/lower = more significant
- Score = distance from adjacent bars

**Option B: Extremity-Based (Highest/Lowest)**
- Find absolute highest high or lowest low in lookback
- Most extreme swing in period
- Simple but effective

**Option C: Time-Weighted (Recency + Strength)**
- Combine recency with strength
- Recent + strong swings score higher
- Balances fresh swings with quality

**Option D: Untested Levels**
- Swings that price hasn't revisited yet
- Check if rectangle zone has been tested (price re-entered)
- Prefer untested swings for fresh opportunities

**Option E: Multi-Timeframe Confluence**
- Swings that align with H1 or H4 levels
- Stronger if multiple timeframes agree
- More complex but robust

**Option F: Rectangle Validity Window**
- Only consider swings where rectangle end time > current time
- Skip swings that would create expired rectangles
- Ensures rectangles are always forward-looking

---

## Proposed Solutions

### Approach 1: Extremity Filter (Simplest)
**Logic:** Find the HIGHEST high (SELL) or LOWEST low (BUY) in lookback period

**Pros:**
- Very simple to implement
- Clear and predictable
- Guaranteed to find most extreme swing

**Cons:**
- May find very old swings (e.g., 95 bars ago)
- Rectangle might be expired by the time it's drawn
- No consideration for recency

---

### Approach 2: Validity-First Filter (Recommended)
**Logic:** Find swings where rectangle would still be VALID (not expired), then choose strongest

**Pros:**
- Ensures rectangles are always forward-looking
- Combines validity with quality
- Prevents drawing useless historical rectangles

**Cons:**
- Slightly more complex
- May skip some swings if they're too old

**Validity Check:**
```csharp
DateTime rectangleEndTime = m15Bars.OpenTimes[swingIndex].AddMinutes(60);
DateTime currentTime = m15Bars.OpenTimes.LastValue;
bool isValid = rectangleEndTime > currentTime;
```

---

### Approach 3: Scored Multi-Criteria (Advanced)
**Logic:** Score each swing based on multiple factors, choose highest score

**Scoring Factors:**
1. **Strength** (50%): How far from neighboring bars
2. **Recency** (30%): How recent the swing is
3. **Validity** (20%): How much time left in rectangle window

**Pros:**
- Most sophisticated
- Balances multiple factors
- Highly customizable

**Cons:**
- More complex to tune
- Requires parameter optimization
- May need adjustments per pair

---

### Approach 4: Two-Stage Detection (Market Structure + Entry) ✅ SELECTED
**Logic:** Separate market structure awareness from rectangle drawing

**Stage 1: Market Structure (100 bars)**
- Scan 100 bars to identify key support/resistance levels
- Store significant swings in memory
- Update on each new M15 bar

**Stage 2: Rectangle Drawing (Recent + Valid)**
- Look for recent swings (last 10-15 bars)
- Check if swing aligns with Stage 1 structure
- Draw rectangle only if:
  - Recent enough (rectangle still valid)
  - Aligns with broader structure
  - Strong enough (minimum swing strength)

**Pros:**
- Best of both worlds (structure + timing)
- Professional approach
- Most accurate

**Cons:**
- Most complex to implement
- Requires more testing
- Higher computational cost

---

## Implementation Strategy

### User's Choice: Approach 4 - Two-Stage Structure with Advanced Scoring

**User Requirements:**
- ✅ Two-stage market structure detection
- ✅ Scoring system for swing significance
- ✅ Session awareness (Asian, London, New York)
- ✅ Session High/Low identification
- ✅ FVG (Fair Value Gap) detection
- ✅ Strength and weakness identification
- ✅ 100-bar market structure context
- ✅ Rectangle width: 60 minutes (changed from 50)

This is a **professional/institutional** approach combining:
- Smart Money Concepts (SMC) - FVGs, liquidity levels
- Session-based trading - respecting market sessions
- Multi-criteria scoring - quality over quantity
- Market structure - broader context awareness

---

## Phase 1A: Basic Validity & Scoring

### Goal
Get basic validity filtering working first, then build sophisticated features on top

### Changes
1. Increase lookback to 100 bars
2. Add rectangle validity check (60-minute window)
3. Change rectangle width default to 60 minutes
4. Find all valid swings (not just first one)
5. Score swings and choose highest score

### New Parameters
```csharp
[Parameter("Market Structure Lookback", DefaultValue = 100, MinValue = 30, MaxValue = 200)]
public int StructureLookbackBars { get; set; }

[Parameter("Rectangle Width (Minutes)", DefaultValue = 60, MinValue = 30, MaxValue = 120)]
public int RectangleWidthMinutes { get; set; }

[Parameter("Minimum Swing Score", DefaultValue = 0.60, MinValue = 0.0, MaxValue = 1.0)]
public double MinimumSwingScore { get; set; }
```

### Refactor: FindRecentSwingPoint → FindSignificantSwing

**OLD Logic:** Return first valid fractal
**NEW Logic:** Find ALL valid fractals, score them, return highest score

```csharp
private int FindSignificantSwing(string mode)
{
    // Phase 1: Find all Williams Fractals
    var swingCandidates = FindAllSwings(mode);

    if (swingCandidates.Count == 0)
        return -1;

    // Phase 2: Score each swing
    var scoredSwings = new List<(int index, double score)>();

    foreach (var swingIdx in swingCandidates)
    {
        double score = CalculateSwingScore(swingIdx, mode);

        if (score >= MinimumSwingScore)
        {
            scoredSwings.Add((swingIdx, score));
            Print("[SwingScoring] Bar {0} | Score: {1:F2}", swingIdx, score);
        }
    }

    // Phase 3: Return highest scoring swing
    if (scoredSwings.Count == 0)
        return -1;

    var bestSwing = scoredSwings.OrderByDescending(s => s.score).First();

    Print("[SignificantSwing] Selected bar {0} | Score: {1:F2}",
        bestSwing.index, bestSwing.score);

    return bestSwing.index;
}
```

### Swing Scoring System (Phase 1A)

**4 Scoring Components:**

| Factor | Weight | Purpose |
|--------|--------|---------|
| **Validity Score** | 25% | Rectangle must be forward-looking (not expired) |
| **Extremity Score** | 35% | Prefer highest highs (SELL) or lowest lows (BUY) |
| **Fractal Strength** | 25% | Measure fractal quality (distance from neighbors) |
| **Candle Strength** | 15% | Strong candle bodies preferred over doji/wicks |

**Total Score Formula:**
```csharp
totalScore = (validityScore * 0.25) +
             (extremityScore * 0.35) +
             (fractalStrength * 0.25) +
             (candleStrength * 0.15)
```

**Threshold:** Only draw rectangle if `totalScore >= 0.60` (configurable)

### Individual Scoring Methods

#### 1. Validity Score (25%)
```csharp
private double CalculateValidityScore(int swingIndex)
{
    DateTime swingTime = m15Bars.OpenTimes[swingIndex];
    DateTime rectangleEnd = swingTime.AddMinutes(RectangleWidthMinutes);
    DateTime currentTime = m15Bars.OpenTimes.LastValue;

    TimeSpan remaining = rectangleEnd - currentTime;

    if (remaining.TotalMinutes <= 0)
        return 0; // Expired, invalid

    // Score based on how much time remains (max 1.0 if full 60 min)
    return Math.Min(remaining.TotalMinutes / RectangleWidthMinutes, 1.0);
}
```

#### 2. Extremity Score (35%)
```csharp
private double CalculateExtremityScore(int swingIndex, string mode)
{
    int lookback = Math.Min(StructureLookbackBars, m15Bars.Count);

    if (mode == "SELL")
    {
        double swingHigh = m15Bars.HighPrices[swingIndex];
        double highestHigh = m15Bars.HighPrices.Maximum(lookback);
        double avgHigh = m15Bars.HighPrices.Average(lookback);

        if (highestHigh == avgHigh) return 0.5;

        return (swingHigh - avgHigh) / (highestHigh - avgHigh);
    }
    else // BUY mode
    {
        double swingLow = m15Bars.LowPrices[swingIndex];
        double lowestLow = m15Bars.LowPrices.Minimum(lookback);
        double avgLow = m15Bars.LowPrices.Average(lookback);

        if (avgLow == lowestLow) return 0.5;

        return (avgLow - swingLow) / (avgLow - lowestLow);
    }
}
```

#### 3. Fractal Strength Score (25%)
```csharp
private double CalculateFractalStrength(int swingIndex, string mode)
{
    if (mode == "SELL")
    {
        double swingHigh = m15Bars.HighPrices[swingIndex];
        double maxNeighbor = Math.Max(
            Math.Max(m15Bars.HighPrices[swingIndex - 1], m15Bars.HighPrices[swingIndex - 2]),
            Math.Max(m15Bars.HighPrices[swingIndex + 1], m15Bars.HighPrices[swingIndex + 2])
        );

        double strength = swingHigh - maxNeighbor;
        double avgRange = CalculateAverageRange(20);

        return Math.Min(strength / avgRange, 1.0);
    }
    else // BUY mode
    {
        double swingLow = m15Bars.LowPrices[swingIndex];
        double minNeighbor = Math.Min(
            Math.Min(m15Bars.LowPrices[swingIndex - 1], m15Bars.LowPrices[swingIndex - 2]),
            Math.Min(m15Bars.LowPrices[swingIndex + 1], m15Bars.LowPrices[swingIndex + 2])
        );

        double strength = minNeighbor - swingLow;
        double avgRange = CalculateAverageRange(20);

        return Math.Min(strength / avgRange, 1.0);
    }
}
```

#### 4. Candle Strength Score (15%)
```csharp
private double CalculateCandleStrength(int swingIndex)
{
    double open = m15Bars.OpenPrices[swingIndex];
    double close = m15Bars.ClosePrices[swingIndex];
    double high = m15Bars.HighPrices[swingIndex];
    double low = m15Bars.LowPrices[swingIndex];

    double bodySize = Math.Abs(close - open);
    double totalSize = high - low;

    if (totalSize == 0) return 0.3;

    double bodyRatio = bodySize / totalSize;

    if (bodyRatio >= 0.70) return 1.0; // Strong candle
    if (bodyRatio >= 0.50) return 0.6; // Medium candle
    return 0.3; // Weak candle
}
```

### Testing Phase 1A

**Backtest Setup:**
- Symbol: EURUSD
- Timeframe: M1
- Period: 1 month
- Visual mode: ON
- Enable Trading: false (just observe rectangles)

**What to Verify:**
1. ✅ Rectangles only drawn for valid swings (not expired)
2. ✅ Rectangle width = 60 minutes
3. ✅ Fewer rectangles than before (only significant swings)
4. ✅ Console shows swing scores (>= 0.60)
5. ✅ Rectangles at stronger/more extreme swings

---

## Phase 1B: Entry Logic

### Goal
Add breakout entry detection and trades with proper risk management

### Entry Mode Options

**Parameter:**
```csharp
[Parameter("Entry Mode", DefaultValue = EntryMode.Breakout, Group = "Entry Logic")]
public EntryMode EntryModeSelection { get; set; }

public enum EntryMode
{
    Breakout,       // DEFAULT: Enter when body closes beyond rectangle
    RetestConfirm   // ALTERNATIVE: Wait for retest after breakout
}
```

### Entry Logic Clarification

**BREAKOUT Entry (Default):**
- **SELL Mode:** Price enters rectangle from ABOVE → M1 candle body closes BELOW rectangle bottom → SELL
- **BUY Mode:** Price enters rectangle from BELOW → M1 candle body closes ABOVE rectangle top → BUY

**RETEST Entry (Alternative):**
- **SELL Mode:** Price breaks below rectangle → retests rectangle bottom from below → rejection candle → SELL
- **BUY Mode:** Price breaks above rectangle → retests rectangle top from above → rejection candle → BUY

### Implementation

#### Track Rectangle State
```csharp
private bool hasActiveSwing = false;
private double swingTopPrice = 0;
private double swingBottomPrice = 0;
private bool hasBreakoutOccurred = false;  // For retest mode
private double breakoutPrice = 0;          // For retest mode
```

#### Dynamic Position Sizing (Risk-Based)
```csharp
[Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0, Group = "Risk Management")]
public double RiskPercent { get; set; }

[Parameter("SL Buffer Pips", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 10.0, Group = "Risk Management")]
public double SLBufferPips { get; set; }

private double CalculatePositionSize(double slDistancePips)
{
    double riskAmount = Account.Balance * (RiskPercent / 100.0);
    double pipValuePerLot = Symbol.PipValue * Symbol.LotSize;
    double lotSize = riskAmount / (slDistancePips * pipValuePerLot);

    // Normalize to broker limits
    double volumeInUnits = Symbol.QuantityToVolumeInUnits(lotSize);
    volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

    // Ensure minimum volume
    if (volumeInUnits < Symbol.VolumeInUnitsMin)
        return 0;

    return volumeInUnits;
}
```

#### Monitor M1 Candles in OnBar() - Breakout Mode
```csharp
protected override void OnBar()
{
    // ... existing M15 bar processing ...

    // Entry logic on M1 bar close (more reliable than OnTick)
    if (!EnableTrading || !hasActiveSwing)
        return;

    var positions = Positions.FindAll(MagicNumber.ToString(), SymbolName);
    if (positions.Length >= MaxPositions)
        return;

    // Get CLOSED M1 candle (index -2 is last completed bar)
    int lastIdx = Bars.Count - 2;
    double candleOpen = Bars.OpenPrices[lastIdx];
    double candleClose = Bars.ClosePrices[lastIdx];
    double candleHigh = Bars.HighPrices[lastIdx];
    double candleLow = Bars.LowPrices[lastIdx];

    if (EntryModeSelection == EntryMode.Breakout)
    {
        ProcessBreakoutEntry(candleOpen, candleClose, candleHigh, candleLow);
    }
    else
    {
        ProcessRetestEntry(candleOpen, candleClose, candleHigh, candleLow);
    }
}

private void ProcessBreakoutEntry(double open, double close, double high, double low)
{
    if (currentMode == "SELL")
    {
        // Invalidate if body closes ABOVE rectangle (wrong direction breakout)
        if (close > swingTopPrice && open > swingTopPrice)
        {
            Print("[RectangleInvalid] SELL rectangle invalidated - body closed above");
            hasActiveSwing = false;
            return;
        }

        // TRIGGER: Whole body BELOW rectangle = price broke through support
        bool isTriggerCandle = (open < swingBottomPrice) && (close < swingBottomPrice);

        // Additional check: candle must have touched/entered the rectangle first
        bool hadRectangleInteraction = (high >= swingBottomPrice);

        if (isTriggerCandle && hadRectangleInteraction)
        {
            ExecuteSellTrade();
        }
    }
    else if (currentMode == "BUY")
    {
        // Invalidate if body closes BELOW rectangle (wrong direction breakout)
        if (close < swingBottomPrice && open < swingBottomPrice)
        {
            Print("[RectangleInvalid] BUY rectangle invalidated - body closed below");
            hasActiveSwing = false;
            return;
        }

        // TRIGGER: Whole body ABOVE rectangle = price broke through resistance
        bool isTriggerCandle = (open > swingTopPrice) && (close > swingTopPrice);

        // Additional check: candle must have touched/entered the rectangle first
        bool hadRectangleInteraction = (low <= swingTopPrice);

        if (isTriggerCandle && hadRectangleInteraction)
        {
            ExecuteBuyTrade();
        }
    }
}

private void ProcessRetestEntry(double open, double close, double high, double low)
{
    if (currentMode == "SELL")
    {
        // Phase 1: Detect initial breakout
        if (!hasBreakoutOccurred && close < swingBottomPrice && open < swingBottomPrice)
        {
            hasBreakoutOccurred = true;
            breakoutPrice = swingBottomPrice;
            Print("[Retest] SELL breakout detected, waiting for retest of {0:F5}", breakoutPrice);
            return;
        }

        // Phase 2: Wait for retest (price comes back to rectangle bottom)
        if (hasBreakoutOccurred)
        {
            // Check if candle retested the level (wick touched)
            bool retested = (high >= breakoutPrice - (2 * Symbol.PipSize));

            // Check for rejection (bearish candle closing below retest level)
            bool isRejection = (close < open) && (close < breakoutPrice);

            if (retested && isRejection)
            {
                ExecuteSellTrade();
                hasBreakoutOccurred = false;
            }
        }
    }
    else if (currentMode == "BUY")
    {
        // Phase 1: Detect initial breakout
        if (!hasBreakoutOccurred && close > swingTopPrice && open > swingTopPrice)
        {
            hasBreakoutOccurred = true;
            breakoutPrice = swingTopPrice;
            Print("[Retest] BUY breakout detected, waiting for retest of {0:F5}", breakoutPrice);
            return;
        }

        // Phase 2: Wait for retest
        if (hasBreakoutOccurred)
        {
            bool retested = (low <= breakoutPrice + (2 * Symbol.PipSize));
            bool isRejection = (close > open) && (close > breakoutPrice);

            if (retested && isRejection)
            {
                ExecuteBuyTrade();
                hasBreakoutOccurred = false;
            }
        }
    }
}
```

#### Trade Execution with Dynamic Sizing and 3R
```csharp
private void ExecuteSellTrade()
{
    double entryPrice = Symbol.Bid;

    // SL = rectangle top + buffer (above the zone we're selling from)
    double stopLoss = swingTopPrice + (SLBufferPips * Symbol.PipSize);

    // Calculate risk in pips
    double riskPips = (stopLoss - entryPrice) / Symbol.PipSize;

    // Calculate position size based on risk
    double volume = CalculatePositionSize(riskPips);
    if (volume <= 0)
    {
        Print("[SELL] Position size too small for risk parameters - skipping");
        return;
    }

    // TP = 3R minimum, adjusted for market structure
    double initialTP = entryPrice - (riskPips * 3.0 * Symbol.PipSize);
    double finalTP = AdjustTPForMarketStructure(entryPrice, initialTP, "SELL");

    // Calculate actual RR after adjustment
    double rewardPips = (entryPrice - finalTP) / Symbol.PipSize;
    double actualRR = rewardPips / riskPips;

    var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume,
        MagicNumber.ToString(), stopLoss, finalTP);

    if (result.IsSuccessful)
    {
        Print("✅ SELL TRIGGERED | Entry: {0:F5} | SL: {1:F5} | TP: {2:F5}",
            entryPrice, stopLoss, finalTP);
        Print("   Risk: {0:F1} pips | Reward: {1:F1} pips | RR: 1:{2:F1}",
            riskPips, rewardPips, actualRR);
        Print("   Volume: {0:F2} lots | Risk Amount: ${1:F2}",
            Symbol.VolumeInUnitsToQuantity(volume), Account.Balance * (RiskPercent / 100.0));

        if (TradeOnNewSwingOnly)
            hasActiveSwing = false;
    }
}

private void ExecuteBuyTrade()
{
    double entryPrice = Symbol.Ask;

    // SL = rectangle bottom - buffer (below the zone we're buying from)
    double stopLoss = swingBottomPrice - (SLBufferPips * Symbol.PipSize);

    // Calculate risk in pips
    double riskPips = (entryPrice - stopLoss) / Symbol.PipSize;

    // Calculate position size based on risk
    double volume = CalculatePositionSize(riskPips);
    if (volume <= 0)
    {
        Print("[BUY] Position size too small for risk parameters - skipping");
        return;
    }

    // TP = 3R minimum, adjusted for market structure
    double initialTP = entryPrice + (riskPips * 3.0 * Symbol.PipSize);
    double finalTP = AdjustTPForMarketStructure(entryPrice, initialTP, "BUY");

    // Calculate actual RR after adjustment
    double rewardPips = (finalTP - entryPrice) / Symbol.PipSize;
    double actualRR = rewardPips / riskPips;

    var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volume,
        MagicNumber.ToString(), stopLoss, finalTP);

    if (result.IsSuccessful)
    {
        Print("✅ BUY TRIGGERED | Entry: {0:F5} | SL: {1:F5} | TP: {2:F5}",
            entryPrice, stopLoss, finalTP);
        Print("   Risk: {0:F1} pips | Reward: {1:F1} pips | RR: 1:{2:F1}",
            riskPips, rewardPips, actualRR);
        Print("   Volume: {0:F2} lots | Risk Amount: ${1:F2}",
            Symbol.VolumeInUnitsToQuantity(volume), Account.Balance * (RiskPercent / 100.0));

        if (TradeOnNewSwingOnly)
            hasActiveSwing = false;
    }
}
```

### Testing Phase 1B
- Run backtest on M1, visual mode ON
- **Enable trading = TRUE**
- Verify entries ONLY when whole body beyond rectangle
- Verify rectangle invalidates when body closes opposite side
- Check SL = rectangle edge + spread
- Check TP = 3R from entry

---

## Phase 1C: Hybrid Market Structure TP (M15 + H1)

### Goal
Adjust TP using multi-timeframe structure: H1 levels take priority, M15 as fallback

### Rationale
- H1 levels are stronger/more significant than M15
- M15 levels provide granularity when H1 levels are too far
- Minimum 3R is always guaranteed

### Parameters
```csharp
[Parameter("Use H1 Levels for TP", DefaultValue = true, Group = "TP Management")]
public bool UseH1LevelsForTP { get; set; }

[Parameter("H1 Level Proximity Pips", DefaultValue = 50, MinValue = 10, MaxValue = 200, Group = "TP Management")]
public int H1LevelProximityPips { get; set; }

[Parameter("Minimum RR Ratio", DefaultValue = 3.0, MinValue = 2.0, MaxValue = 10.0, Group = "TP Management")]
public double MinimumRRRatio { get; set; }
```

### Implementation

```csharp
private Bars h1Bars;
private List<double> h1Supports = new List<double>();
private List<double> h1Resistances = new List<double>();

protected override void OnStart()
{
    // ... existing initialization ...

    // Get H1 bars for TP structure
    h1Bars = MarketData.GetBars(TimeFrame.Hour);
}

private void UpdateH1Levels()
{
    h1Supports.Clear();
    h1Resistances.Clear();

    int barsToScan = Math.Min(200, h1Bars.Count - 5);

    for (int i = 2; i < barsToScan - 2; i++)
    {
        int idx = h1Bars.Count - 1 - i;

        // Williams Fractal Up (Resistance)
        if (h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx - 1] &&
            h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx - 2] &&
            h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx + 1] &&
            h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx + 2])
        {
            h1Resistances.Add(h1Bars.HighPrices[idx]);
        }

        // Williams Fractal Down (Support)
        if (h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx - 1] &&
            h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx - 2] &&
            h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx + 1] &&
            h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx + 2])
        {
            h1Supports.Add(h1Bars.LowPrices[idx]);
        }
    }
}

private double AdjustTPForMarketStructure(double entryPrice, double initialTP, string mode)
{
    double minTPDistance = Math.Abs(initialTP - entryPrice); // 3R distance
    double h1ProximityDistance = H1LevelProximityPips * Symbol.PipSize;

    if (mode == "SELL")
    {
        // STEP 1: Check for H1 support level (highest priority)
        if (UseH1LevelsForTP)
        {
            double bestH1Support = FindBestH1Support(entryPrice, initialTP, h1ProximityDistance);

            if (bestH1Support > 0)
            {
                double h1TPDistance = entryPrice - bestH1Support;

                // Only use H1 level if it gives at least minimum RR
                if (h1TPDistance >= minTPDistance)
                {
                    Print("[TP] Using H1 support at {0:F5} | RR: {1:F1}",
                        bestH1Support, h1TPDistance / (entryPrice - swingTopPrice));
                    return bestH1Support;
                }
            }
        }

        // STEP 2: Fall back to M15 support level
        double m15Support = FindM15Support(entryPrice, initialTP);

        if (m15Support > 0)
        {
            double m15TPDistance = entryPrice - m15Support;

            if (m15TPDistance >= minTPDistance)
            {
                Print("[TP] Using M15 support at {0:F5} | RR: {1:F1}",
                    m15Support, m15TPDistance / (entryPrice - swingTopPrice));
                return m15Support;
            }
        }

        // STEP 3: Use default 3R TP
        Print("[TP] Using default 3R TP at {0:F5}", initialTP);
        return initialTP;
    }
    else // BUY
    {
        // STEP 1: Check for H1 resistance level (highest priority)
        if (UseH1LevelsForTP)
        {
            double bestH1Resistance = FindBestH1Resistance(entryPrice, initialTP, h1ProximityDistance);

            if (bestH1Resistance > 0)
            {
                double h1TPDistance = bestH1Resistance - entryPrice;

                if (h1TPDistance >= minTPDistance)
                {
                    Print("[TP] Using H1 resistance at {0:F5} | RR: {1:F1}",
                        bestH1Resistance, h1TPDistance / (swingBottomPrice - entryPrice));
                    return bestH1Resistance;
                }
            }
        }

        // STEP 2: Fall back to M15 resistance level
        double m15Resistance = FindM15Resistance(entryPrice, initialTP);

        if (m15Resistance > 0)
        {
            double m15TPDistance = m15Resistance - entryPrice;

            if (m15TPDistance >= minTPDistance)
            {
                Print("[TP] Using M15 resistance at {0:F5} | RR: {1:F1}",
                    m15Resistance, m15TPDistance / (swingBottomPrice - entryPrice));
                return m15Resistance;
            }
        }

        // STEP 3: Use default 3R TP
        Print("[TP] Using default 3R TP at {0:F5}", initialTP);
        return initialTP;
    }
}

private double FindBestH1Support(double entryPrice, double minTP, double maxDistance)
{
    // Find H1 support below entry, within proximity, and below minTP
    return h1Supports
        .Where(s => s < entryPrice && s <= minTP && (entryPrice - s) <= maxDistance)
        .OrderByDescending(s => s) // Closest to entry (but still profitable)
        .FirstOrDefault();
}

private double FindBestH1Resistance(double entryPrice, double minTP, double maxDistance)
{
    return h1Resistances
        .Where(r => r > entryPrice && r >= minTP && (r - entryPrice) <= maxDistance)
        .OrderBy(r => r) // Closest to entry
        .FirstOrDefault();
}

private double FindM15Support(double entryPrice, double minTP)
{
    int lookback = Math.Min(100, m15Bars.Count);

    for (int i = 2; i < lookback - 2; i++)
    {
        int idx = m15Bars.Count - 1 - i;

        // Check for Williams Fractal support
        if (m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 1] &&
            m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 2] &&
            m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 1] &&
            m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 2])
        {
            double level = m15Bars.LowPrices[idx];

            // Level must be below entry and at/beyond minimum TP
            if (level < entryPrice && level <= minTP)
                return level;
        }
    }

    return 0; // No suitable level found
}

private double FindM15Resistance(double entryPrice, double minTP)
{
    int lookback = Math.Min(100, m15Bars.Count);

    for (int i = 2; i < lookback - 2; i++)
    {
        int idx = m15Bars.Count - 1 - i;

        if (m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 1] &&
            m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 2] &&
            m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 1] &&
            m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 2])
        {
            double level = m15Bars.HighPrices[idx];

            if (level > entryPrice && level >= minTP)
                return level;
        }
    }

    return 0;
}
```

---

## Phase 2: Session Awareness

### Goal
Identify trading sessions and session highs/lows, draw visual session boxes

### Trading Sessions (UTC Times)
- **Asian (Tokyo):** 00:00 - 09:00 UTC
- **London:** 08:00 - 17:00 UTC
- **New York:** 13:00 - 22:00 UTC
- **Overlap (London + NY):** 13:00 - 17:00 UTC (high liquidity)

### Session Classes
```csharp
private enum TradingSession
{
    Asian,
    London,
    NewYork,
    Overlap
}

private class SessionLevels
{
    public TradingSession Session { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
}

private List<SessionLevels> recentSessions = new List<SessionLevels>();
```

### Session Detection (Fixed - Independent Tracking)
```csharp
// Track sessions independently to avoid overlap issues
private class SessionState
{
    public bool IsAsian { get; set; }
    public bool IsLondon { get; set; }
    public bool IsNewYork { get; set; }
    public bool IsOverlap => IsLondon && IsNewYork;

    public override string ToString()
    {
        var active = new List<string>();
        if (IsAsian) active.Add("Asian");
        if (IsLondon) active.Add("London");
        if (IsNewYork) active.Add("NY");
        if (IsOverlap) active.Add("(Overlap)");
        return active.Count > 0 ? string.Join("+", active) : "Off-Session";
    }
}

private SessionState GetSessionState(DateTime time)
{
    int hourUTC = time.Hour;

    return new SessionState
    {
        IsAsian = hourUTC >= 0 && hourUTC < 9,     // 00:00-09:00 UTC
        IsLondon = hourUTC >= 8 && hourUTC < 17,   // 08:00-17:00 UTC
        IsNewYork = hourUTC >= 13 && hourUTC < 22  // 13:00-22:00 UTC
    };
}

// Get the PRIMARY session for scoring (most significant at current time)
private TradingSession GetPrimarySession(DateTime time)
{
    var state = GetSessionState(time);

    // Overlap is highest priority (most liquidity)
    if (state.IsOverlap)
        return TradingSession.Overlap;

    // London and NY individually
    if (state.IsLondon)
        return TradingSession.London;

    if (state.IsNewYork)
        return TradingSession.NewYork;

    if (state.IsAsian)
        return TradingSession.Asian;

    return TradingSession.None;
}
```

### Session Visual Boxes
```csharp
// Session colors (with transparency for visibility)
Asian Session:    Color.FromArgb(30, 255, 255, 0)   // Light Yellow
London Session:   Color.FromArgb(30, 0, 128, 255)   // Light Blue
New York Session: Color.FromArgb(30, 255, 128, 0)   // Light Orange
Overlap Session:  Color.FromArgb(40, 128, 0, 255)   // Light Purple

private void DrawSessionBox(SessionLevels session)
{
    string boxName = string.Format("Session_{0}_{1}",
        session.Session, session.StartTime.ToString("yyyyMMdd_HH"));

    Color boxColor = GetSessionColor(session.Session);

    var box = Chart.DrawRectangle(boxName,
        session.StartTime, session.High,
        session.EndTime, session.Low,
        boxColor);

    box.IsFilled = true;
    box.IsInteractive = false;
    box.ZIndex = -1; // Behind swing rectangles
}
```

### Session Alignment Score (20%)
```csharp
private double CalculateSessionAlignment(int swingIndex, string mode)
{
    if (!EnableSessionFilter)
        return 0.5;

    DateTime swingTime = m15Bars.OpenTimes[swingIndex];
    var session = GetSessionForTime(swingTime);

    if (session == null)
        return 0.5;

    double swingPrice = mode == "SELL" ?
        m15Bars.HighPrices[swingIndex] :
        m15Bars.LowPrices[swingIndex];

    double distanceToSessionLevel = mode == "SELL" ?
        Math.Abs(swingPrice - session.High) :
        Math.Abs(swingPrice - session.Low);

    double threshold = 10 * Symbol.PipSize; // Within 10 pips

    if (distanceToSessionLevel <= threshold)
        return 1.0; // At session level
    else
        return 0.5;
}
```

### New Parameters (Phase 2)
```csharp
[Parameter("Enable Session Filter", DefaultValue = true)]
public bool EnableSessionFilter { get; set; }

[Parameter("Show Session Boxes", DefaultValue = true, Group = "Visualization")]
public bool ShowSessionBoxes { get; set; }
```

---

## Phase 3: FVG Detection

### Goal
Identify Fair Value Gaps (imbalances) and prefer swings that align

### What is FVG
- Gap between candle[i-1].High and candle[i+1].Low (bullish FVG)
- Gap between candle[i-1].Low and candle[i+1].High (bearish FVG)
- Represents inefficient price delivery
- Price tends to "fill" these gaps

### FVG Classes
```csharp
private class FairValueGap
{
    public DateTime Time { get; set; }
    public double TopPrice { get; set; }
    public double BottomPrice { get; set; }
    public bool IsBullish { get; set; }
    public bool IsFilled { get; set; }
}

private List<FairValueGap> activeFVGs = new List<FairValueGap>();
```

### FVG Detection Logic (Fixed Indexing)
```csharp
/// <summary>
/// FVG Structure:
/// Candle A (idx-1) = BEFORE the impulse
/// Candle B (idx)   = IMPULSE candle (the big move)
/// Candle C (idx+1) = AFTER the impulse
///
/// Bullish FVG: Gap between A.High and C.Low (price gapped up)
/// Bearish FVG: Gap between A.Low and C.High (price gapped down)
/// </summary>
private void DetectFVGs()
{
    activeFVGs.Clear(); // Reset each scan

    int lookback = Math.Min(FVGLookbackBars, m15Bars.Count - 3);

    // Start at 1 to ensure we have candle before and after
    for (int i = 1; i < lookback - 1; i++)
    {
        int idx = m15Bars.Count - 1 - i; // Impulse candle index

        // Bounds check
        if (idx - 1 < 0 || idx + 1 >= m15Bars.Count)
            continue;

        // Get candle data
        double candleA_High = m15Bars.HighPrices[idx - 1]; // Before impulse
        double candleA_Low = m15Bars.LowPrices[idx - 1];
        double candleC_High = m15Bars.HighPrices[idx + 1]; // After impulse
        double candleC_Low = m15Bars.LowPrices[idx + 1];

        // BULLISH FVG: Candle A's HIGH is BELOW Candle C's LOW
        // This means price gapped UP, leaving an unfilled zone
        if (candleA_High < candleC_Low)
        {
            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                BottomPrice = candleA_High,   // Bottom of gap
                TopPrice = candleC_Low,       // Top of gap
                IsBullish = true,
                IsFilled = false
            };

            // Check if gap has been filled by subsequent price action
            fvg.IsFilled = IsFVGFilled(fvg, idx + 1);

            if (!fvg.IsFilled)
            {
                activeFVGs.Add(fvg);
                Print("[FVG] Bullish gap detected at {0} | Zone: {1:F5} - {2:F5}",
                    fvg.Time, fvg.BottomPrice, fvg.TopPrice);
            }
        }

        // BEARISH FVG: Candle A's LOW is ABOVE Candle C's HIGH
        // This means price gapped DOWN, leaving an unfilled zone
        if (candleA_Low > candleC_High)
        {
            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                TopPrice = candleA_Low,       // Top of gap
                BottomPrice = candleC_High,   // Bottom of gap
                IsBullish = false,
                IsFilled = false
            };

            fvg.IsFilled = IsFVGFilled(fvg, idx + 1);

            if (!fvg.IsFilled)
            {
                activeFVGs.Add(fvg);
                Print("[FVG] Bearish gap detected at {0} | Zone: {1:F5} - {2:F5}",
                    fvg.Time, fvg.TopPrice, fvg.BottomPrice);
            }
        }
    }

    Print("[FVG] Scan complete | Active FVGs: {0}", activeFVGs.Count);
}

/// <summary>
/// Checks if an FVG has been filled by price action after creation
/// FVG is filled when price returns to cover the gap
/// </summary>
private bool IsFVGFilled(FairValueGap fvg, int startIdx)
{
    // Scan from startIdx to current bar
    for (int i = startIdx; i < m15Bars.Count; i++)
    {
        if (fvg.IsBullish)
        {
            // Bullish FVG filled when price drops INTO the gap
            if (m15Bars.LowPrices[i] <= fvg.TopPrice)
                return true;
        }
        else
        {
            // Bearish FVG filled when price rises INTO the gap
            if (m15Bars.HighPrices[i] >= fvg.BottomPrice)
                return true;
        }
    }

    return false;
}
```

### FVG Alignment Score (15%)
```csharp
private double CalculateFVGAlignment(int swingIndex, string mode)
{
    if (!EnableFVGFilter || activeFVGs.Count == 0)
        return 0.5;

    double swingPrice = mode == "SELL" ?
        m15Bars.HighPrices[swingIndex] :
        m15Bars.LowPrices[swingIndex];

    foreach (var fvg in activeFVGs)
    {
        // Check if swing price within FVG zone
        if (swingPrice >= fvg.BottomPrice && swingPrice <= fvg.TopPrice)
            return 1.0; // Strong alignment

        // Check if near FVG (within 5 pips)
        double distanceToFVG = Math.Min(
            Math.Abs(swingPrice - fvg.TopPrice),
            Math.Abs(swingPrice - fvg.BottomPrice)
        );

        if (distanceToFVG <= 5 * Symbol.PipSize)
            return 0.7; // Near FVG
    }

    return 0.3; // No FVG alignment
}
```

### New Parameters (Phase 3)
```csharp
[Parameter("Enable FVG Filter", DefaultValue = true)]
public bool EnableFVGFilter { get; set; }

[Parameter("FVG Lookback Bars", DefaultValue = 50, MinValue = 20, MaxValue = 100)]
public int FVGLookbackBars { get; set; }
```

### Final Scoring (All Phases Complete) with Decomposition Logging
```csharp
// Configurable weights for optimization
[Parameter("Weight: Validity", DefaultValue = 0.20, Group = "Score Weights")]
public double WeightValidity { get; set; }

[Parameter("Weight: Extremity", DefaultValue = 0.25, Group = "Score Weights")]
public double WeightExtremity { get; set; }

[Parameter("Weight: Fractal", DefaultValue = 0.15, Group = "Score Weights")]
public double WeightFractal { get; set; }

[Parameter("Weight: Session", DefaultValue = 0.20, Group = "Score Weights")]
public double WeightSession { get; set; }

[Parameter("Weight: FVG", DefaultValue = 0.15, Group = "Score Weights")]
public double WeightFVG { get; set; }

[Parameter("Weight: Candle", DefaultValue = 0.05, Group = "Score Weights")]
public double WeightCandle { get; set; }

[Parameter("Enable Score Logging", DefaultValue = true, Group = "Debugging")]
public bool EnableScoreLogging { get; set; }

private double CalculateSwingScore(int swingIndex, string mode)
{
    // Calculate all components
    double validityScore = CalculateValidityScore(swingIndex);

    // Early exit if rectangle would be expired
    if (validityScore == 0)
    {
        if (EnableScoreLogging)
            Print("[Score] Bar {0} | REJECTED - Rectangle expired (validity=0)", swingIndex);
        return 0;
    }

    double extremityScore = CalculateExtremityScore(swingIndex, mode);
    double fractalStrength = CalculateFractalStrength(swingIndex, mode);
    double sessionAlignment = CalculateSessionAlignment(swingIndex, mode);
    double fvgAlignment = CalculateFVGAlignment(swingIndex, mode);
    double candleStrength = CalculateCandleStrength(swingIndex);

    // Calculate weighted total
    double totalScore =
        (validityScore * WeightValidity) +
        (extremityScore * WeightExtremity) +
        (fractalStrength * WeightFractal) +
        (sessionAlignment * WeightSession) +
        (fvgAlignment * WeightFVG) +
        (candleStrength * WeightCandle);

    // Decomposition logging for optimization analysis
    if (EnableScoreLogging)
    {
        Print("[ScoreDecomposition] Bar {0} | Mode: {1}", swingIndex, mode);
        Print("   Validity:  {0:F3} × {1:F2} = {2:F3}",
            validityScore, WeightValidity, validityScore * WeightValidity);
        Print("   Extremity: {0:F3} × {1:F2} = {2:F3}",
            extremityScore, WeightExtremity, extremityScore * WeightExtremity);
        Print("   Fractal:   {0:F3} × {1:F2} = {2:F3}",
            fractalStrength, WeightFractal, fractalStrength * WeightFractal);
        Print("   Session:   {0:F3} × {1:F2} = {2:F3}",
            sessionAlignment, WeightSession, sessionAlignment * WeightSession);
        Print("   FVG:       {0:F3} × {1:F2} = {2:F3}",
            fvgAlignment, WeightFVG, fvgAlignment * WeightFVG);
        Print("   Candle:    {0:F3} × {1:F2} = {2:F3}",
            candleStrength, WeightCandle, candleStrength * WeightCandle);
        Print("   TOTAL:     {0:F3} | Threshold: {1:F2} | {2}",
            totalScore, MinimumSwingScore,
            totalScore >= MinimumSwingScore ? "✓ PASS" : "✗ FAIL");
    }

    return totalScore;
}
```

### Score Analysis Helper (for Backtest Review)
```csharp
/// <summary>
/// Exports score data for external analysis
/// Call periodically to track scoring patterns
/// </summary>
private void LogScoreAnalytics(int swingIndex, string mode, double totalScore, bool wasTraded)
{
    // Format: timestamp, bar, mode, score, validity, extremity, fractal, session, fvg, candle, traded
    string logLine = string.Format("{0},{1},{2},{3:F3},{4},{5},{6},{7},{8},{9},{10}",
        m15Bars.OpenTimes[swingIndex].ToString("yyyy-MM-dd HH:mm"),
        swingIndex,
        mode,
        totalScore,
        CalculateValidityScore(swingIndex).ToString("F3"),
        CalculateExtremityScore(swingIndex, mode).ToString("F3"),
        CalculateFractalStrength(swingIndex, mode).ToString("F3"),
        CalculateSessionAlignment(swingIndex, mode).ToString("F3"),
        CalculateFVGAlignment(swingIndex, mode).ToString("F3"),
        CalculateCandleStrength(swingIndex).ToString("F3"),
        wasTraded ? "1" : "0"
    );

    Print("[ANALYTICS] {0}", logLine);
}
```

---

## Entry Logic Details (Revised)

### Trading Rules Summary

**SELL Mode (Rectangle at Swing HIGH):**
1. EMA 200 on M15 indicates **bearish** trend (price < EMA)
2. Rectangle drawn at swing HIGH zone (bullish M15 candle: Close to High)
3. Price enters rectangle from ABOVE (tests resistance)
4. Wait for **M1 candle body to close BELOW rectangle bottom**
5. That M1 candle = **trigger candle** (breakout confirmation)
6. **Entry:** SELL at market (Symbol.Bid)
7. **Stop Loss:** Rectangle TOP + SL buffer pips
8. **Take Profit:** 3R minimum, adjusted for H1/M15 structure

**BUY Mode (Rectangle at Swing LOW):**
1. EMA 200 on M15 indicates **bullish** trend (price > EMA)
2. Rectangle drawn at swing LOW zone (bearish M15 candle: Close to Low)
3. Price enters rectangle from BELOW (tests support)
4. Wait for **M1 candle body to close ABOVE rectangle top**
5. That M1 candle = **trigger candle** (breakout confirmation)
6. **Entry:** BUY at market (Symbol.Ask)
7. **Stop Loss:** Rectangle BOTTOM - SL buffer pips
8. **Take Profit:** 3R minimum, adjusted for H1/M15 structure

### Visual Flow
```
SELL Mode Flow:
                    ┌──────────────────────┐
   Price from above │     RECTANGLE        │
        ↓           │   (Swing HIGH zone)  │
   ┌────────────────┼──────────────────────┤
   │  Price enters  │                      │
   │  rectangle     │   Close to High      │
   ├────────────────┼──────────────────────┤
   │                │                      │
   └────────────────┴──────────────────────┘
        ↓
   Price breaks BELOW rectangle
   M1 body FULLY below bottom
        ↓
   === SELL ENTRY ===


BUY Mode Flow:
   === BUY ENTRY ===
        ↑
   Price breaks ABOVE rectangle
   M1 body FULLY above top
        ↑
   ┌────────────────┬──────────────────────┐
   │                │                      │
   ├────────────────┼──────────────────────┤
   │  Price enters  │   Close to Low       │
   │  rectangle     │                      │
   ├────────────────┼──────────────────────┤
   │  Price from    │     RECTANGLE        │
   │  below ↑       │   (Swing LOW zone)   │
                    └──────────────────────┘
```

### Trigger Candle Detection (On M1 Bar Close)
```csharp
// Use CLOSED bar (index -2) for reliable detection
int lastIdx = Bars.Count - 2;
double candleOpen = Bars.OpenPrices[lastIdx];
double candleClose = Bars.ClosePrices[lastIdx];
double candleHigh = Bars.HighPrices[lastIdx];
double candleLow = Bars.LowPrices[lastIdx];

// SELL Mode - WHOLE BODY must be BELOW rectangle bottom
bool isTriggerCandle =
    (candleOpen < swingBottomPrice) &&
    (candleClose < swingBottomPrice);
// BOTH open AND close below rectangle bottom = breakout confirmed
// Wick CAN go into rectangle, but body must stay below

// Additional: Candle must have interacted with rectangle (wick touched)
bool hadInteraction = (candleHigh >= swingBottomPrice);

// BUY Mode - WHOLE BODY must be ABOVE rectangle top
bool isTriggerCandle =
    (candleOpen > swingTopPrice) &&
    (candleClose > swingTopPrice);
// BOTH open AND close above rectangle top = breakout confirmed

bool hadInteraction = (candleLow <= swingTopPrice);
```

### Rectangle Invalidation
```csharp
// SELL Mode - Rectangle becomes INVALID if body breaks ABOVE (wrong direction)
// Both open AND close must be above the top = confirmed move against us
bool isInvalidated = (candleOpen > swingTopPrice) && (candleClose > swingTopPrice);

// BUY Mode - Rectangle becomes INVALID if body breaks BELOW (wrong direction)
bool isInvalidated = (candleOpen < swingBottomPrice) && (candleClose < swingBottomPrice);
```

### Important Rules
1. **Wicks are allowed** to penetrate rectangle - only body matters
2. **Body = open to close range** (the filled part of candle)
3. **Breakout entry:** Whole body passes THROUGH rectangle to other side
4. **Invalidation:** Whole body exits rectangle in WRONG direction
5. **Interaction required:** Trigger candle must have touched the rectangle (wick proof)
6. Once invalidated, rectangle is ignored - wait for next swing detection

---

## Testing & Verification

### Phase 1A Testing (Basic Scoring)

**Backtest Setup:**
- Symbol: EURUSD
- Timeframe: M1
- Period: 1 month
- Visual mode: ON
- Enable Trading: false

**What to Verify:**
1. ✅ Rectangles only drawn for valid swings (not expired)
2. ✅ Rectangle width = 60 minutes
3. ✅ Fewer rectangles than before
4. ✅ Console shows swing scores (>= 0.60)
5. ✅ Rectangles at stronger/more extreme swings

### Phase 1B Testing (Entry Logic)

**Backtest Setup:**
- Same as Phase 1A
- Enable Trading: **TRUE**

**What to Verify:**
1. ✅ Entries ONLY when whole body beyond rectangle
2. ✅ Rectangle invalidates when body closes opposite side
3. ✅ SL = rectangle edge + spread
4. ✅ TP = 3R from entry
5. ✅ Review trade quality (win rate, RR achieved)

### Phase 2 Testing (Sessions)

**What to Verify:**
1. ✅ Session boxes draw correctly
2. ✅ Session high/low levels tracked
3. ✅ Swings near session levels score higher
4. ✅ Asian/London/NY sessions detected correctly
5. ✅ Boxes have different colors per session

### Phase 3 Testing (FVGs)

**What to Verify:**
1. ✅ FVGs detected correctly (3-candle gaps)
2. ✅ Unfilled FVGs tracked
3. ✅ Swings at FVG zones score higher
4. ✅ Console shows FVG alignment scores

---

## Success Criteria

### Phase 1A: Basic Scoring ✅ COMPLETE
- [x] Williams Fractals detected with candle type filtering
- [x] Multi-criteria scoring: Validity (25%), Extremity (35%), Fractal (25%), Candle (15%)
- [x] Rectangle validity check (60-minute window)
- [x] Minimum score threshold (0.60 default)
- [x] Rectangle visualization on M1/M15 charts
- [x] Mode label (BUY/SELL) based on EMA 200

### Phase 1B: Entry Logic (Next)
- [ ] Dynamic position sizing (risk-based)
- [ ] Breakout entry mode: body closes beyond rectangle
- [ ] Retest entry mode: breakout + retest confirmation (optional)
- [ ] Rectangle invalidation on wrong-direction breakout
- [ ] SL = rectangle edge + buffer
- [ ] TP = 3R minimum with structure adjustment

### Phase 1C: Hybrid TP
- [ ] M15 structure levels for TP
- [ ] H1 structure levels (priority over M15)
- [ ] Minimum RR enforcement

### Phase 2: Session Awareness
- [ ] Independent session tracking (Asian/London/NY)
- [ ] Overlap detection (London+NY)
- [ ] Session high/low tracking
- [ ] Session alignment scoring
- [ ] Visual session boxes (optional)

### Phase 3: FVG Detection
- [ ] Correct FVG indexing (A-B-C pattern)
- [ ] FVG fill detection
- [ ] FVG alignment scoring
- [ ] Visual FVG boxes (optional)

### Scoring System
- [ ] Configurable weights via parameters
- [ ] Score decomposition logging
- [ ] Analytics export for optimization

---

## Parameter Tuning Guide

### Minimum Swing Score
- Default: 0.60
- Lower (0.50): More rectangles, more trades, lower quality
- Higher (0.70): Fewer rectangles, fewer trades, higher quality

### Score Weights
- Adjust based on backtest results
- If too many expired rectangles: increase validity weight
- If missing good swings: increase extremity weight
- If too many weak fractals: increase fractal strength weight

### Session Filter
- Enable if trading during specific sessions only
- Disable for 24/7 strategy

### FVG Filter
- Enable for SMC/institutional approach
- Disable for simpler strategy

---

## Implementation Timeline

**Phase 1A (Basic Scoring):** ✅ COMPLETE
- Core scoring system (Validity, Extremity, Fractal, Candle)
- Rectangle visualization
- Mode detection (EMA 200)
- Implemented in `Jcamp_1M_scalping.cs`

**Phase 1B (Entry Logic):** NEXT
- Dynamic position sizing (risk-based)
- Breakout entry mode implementation
- Retest entry mode (alternative)
- Rectangle invalidation logic
- Trade execution with proper SL/TP

**Phase 1C (Hybrid TP):**
- H1 level detection
- M15 level fallback
- TP adjustment logic
- Minimum RR enforcement

**Phase 2 (Sessions):**
- Independent session tracking
- Session high/low detection
- Session alignment scoring
- Visual boxes (optional)

**Phase 3 (FVGs):**
- FVG detection (fixed indexing)
- FVG fill tracking
- FVG alignment scoring

**Remaining Estimate:** 6-8 hours for Phases 1B through 3

---

## Summary

This plan transforms the cBot from a simple "first swing found" approach to a **sophisticated institutional-grade swing detection system** with:

### Core Improvements
1. **Market Structure Awareness** - 100 bar lookback for context
2. **Smart Swing Selection** - Multi-criteria scoring system (6 factors)
3. **Validity Guarantee** - Rectangles always forward-looking (60-minute window)
4. **Quality Filter** - Only swings scoring >= 0.60 get rectangles

### Advanced Features (Phased)
5. **Session Awareness** - Track Asian/London/NY session highs/lows
6. **Visual Session Boxes** - Color-coded session backgrounds
7. **FVG Detection** - Identify Fair Value Gaps (Smart Money Concepts)
8. **Strength Analysis** - Candle body ratios, fractal quality
9. **Tunable Scoring** - Adjust weights for different market conditions

### Implementation Strategy
- **Phase 1A first** - Basic scoring (validity, extremity, fractal strength)
- **Phase 1B second** - Entry logic with breakout detection
- **Phase 1C third** - M1 market structure TP adjustment
- **Phase 2 fourth** - Add session awareness + visual boxes
- **Phase 3 fifth** - Add FVG detection

Each phase is tested and verified before moving to the next.

### Expected Results
- ✅ Higher quality rectangles (fewer, better)
- ✅ No expired rectangles (60-minute valid window)
- ✅ Better trade setups (session levels, FVGs)
- ✅ Improved win rate in backtests
- ✅ Professional/institutional approach

**Start with Phase 1A, verify it works, then incrementally add remaining phases.**

---

## Potential Future Upgrades

### Multiple Active Rectangles (Deferred)
Currently the bot tracks only ONE active rectangle at a time. Future upgrade could support multiple concurrent zones:

```csharp
// Future implementation
private class SwingZone
{
    public string Id { get; set; }
    public double TopPrice { get; set; }
    public double BottomPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Mode { get; set; }
    public ZoneState State { get; set; } // Pending, Active, Triggered, Expired, Invalidated
    public double Score { get; set; }
}

public enum ZoneState
{
    Pending,      // Zone created, waiting for price interaction
    Active,       // Price has entered zone
    Triggered,    // Trade executed from this zone
    Expired,      // Time window passed without trigger
    Invalidated   // Wrong-direction breakout occurred
}

private List<SwingZone> activeZones = new List<SwingZone>();
```

**Benefits:**
- Handle overlapping sessions with multiple valid zones
- Track zone performance metrics
- Support multiple entries from different zones

**Complexity:** Medium-High - requires state machine for zone lifecycle

### Walk-Forward Optimization Framework (Deferred)
Automated parameter optimization with out-of-sample validation:
- Score weight optimization
- Minimum score threshold tuning
- Per-pair/per-session parameter sets

### Correlation Filter (Deferred)
When trading multiple pairs:
- Detect correlated pairs (EUR/USD vs GBP/USD)
- Limit concurrent positions in correlated pairs
- Hedge detection and management

---

**End of Master Plan**

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
Add breakout entry detection and trades

### Implementation

#### Track Rectangle State
```csharp
private bool hasActiveSwing = false;
private double swingTopPrice = 0;
private double swingBottomPrice = 0;
```

#### Monitor M1 Candles in OnTick()
```csharp
protected override void OnTick()
{
    if (!EnableTrading || !hasActiveSwing)
        return;

    var positions = Positions.FindAll(MagicNumber.ToString(), SymbolName);
    if (positions.Length >= MaxPositions)
        return;

    // Get current M1 candle
    int lastIdx = Bars.Count - 1;
    double candleOpen = Bars.OpenPrices[lastIdx];
    double candleClose = Bars.ClosePrices[lastIdx];

    // Check for rectangle invalidation
    if (currentMode == "SELL")
    {
        // Invalidate if body closes ABOVE rectangle
        if (candleClose > swingTopPrice)
        {
            Print("[RectangleInvalid] SELL rectangle invalidated - body closed above");
            hasActiveSwing = false;
            return;
        }

        // Check for trigger candle (whole body BELOW rectangle)
        bool isTriggerCandle =
            (candleOpen < swingBottomPrice) &&
            (candleClose < swingBottomPrice);

        if (isTriggerCandle)
        {
            ExecuteSellTrade();
        }
    }
    else if (currentMode == "BUY")
    {
        // Invalidate if body closes BELOW rectangle
        if (candleClose < swingBottomPrice)
        {
            Print("[RectangleInvalid] BUY rectangle invalidated - body closed below");
            hasActiveSwing = false;
            return;
        }

        // Check for trigger candle (whole body ABOVE rectangle)
        bool isTriggerCandle =
            (candleOpen > swingTopPrice) &&
            (candleClose > swingTopPrice);

        if (isTriggerCandle)
        {
            ExecuteBuyTrade();
        }
    }
}
```

#### Trade Execution with 3R
```csharp
private void ExecuteSellTrade()
{
    double volume = Symbol.QuantityToVolumeInUnits(LotSize);
    double entryPrice = Symbol.Ask;

    // SL = rectangle top + spread buffer
    double spreadBuffer = Symbol.Spread * Symbol.PipSize;
    double stopLoss = swingTopPrice + spreadBuffer;

    // Calculate risk in pips
    double riskPips = (stopLoss - entryPrice) / Symbol.PipSize;

    // TP = 3R minimum
    double takeProfitPips = riskPips * 3.0;
    double takeProfit = entryPrice - (takeProfitPips * Symbol.PipSize);

    var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume,
        MagicNumber.ToString(), stopLoss, takeProfit);

    if (result.IsSuccessful)
    {
        Print("✅ SELL TRIGGERED | Entry: {0:F5} | SL: {1:F5} | TP: {2:F5}",
            entryPrice, stopLoss, takeProfit);
        Print("   Risk: {0:F1} pips | Reward: {1:F1} pips | RR: 1:{2:F1}",
            riskPips, takeProfitPips, takeProfitPips / riskPips);

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

## Phase 1C: M1 Market Structure TP

### Goal
Adjust TP based on M1 support/resistance levels

### Implementation
1. Scan M1 bars for recent swing highs/lows (last 50-100 bars)
2. Identify next support (SELL trades) or resistance (BUY trades)
3. Adjust TP to align with structure level
4. Ensure minimum 3R, but allow higher if structure permits

```csharp
private double AdjustTPForMarketStructure(double initialTP, string mode)
{
    // Scan M1 for next structure level
    int lookback = Math.Min(100, Bars.Count);

    if (mode == "SELL")
    {
        // Find next support level below entry
        double nextSupport = FindNextSupport(initialTP, lookback);

        // Use structure level if reasonable
        if (nextSupport < initialTP)
            return nextSupport;
    }
    else // BUY
    {
        // Find next resistance level above entry
        double nextResistance = FindNextResistance(initialTP, lookback);

        if (nextResistance > initialTP)
            return nextResistance;
    }

    return initialTP; // Use 3R if no better structure found
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

### Session Detection
```csharp
private TradingSession GetSessionForTime(DateTime time)
{
    int hourUTC = time.Hour;

    // Overlap (London + New York): 13:00-17:00 UTC
    if (hourUTC >= 13 && hourUTC < 17)
        return TradingSession.Overlap;

    // Asian (Tokyo): 00:00-09:00 UTC
    if (hourUTC >= 0 && hourUTC < 9)
        return TradingSession.Asian;

    // London: 08:00-17:00 UTC
    if (hourUTC >= 8 && hourUTC < 17)
        return TradingSession.London;

    // New York: 13:00-22:00 UTC
    if (hourUTC >= 13 && hourUTC < 22)
        return TradingSession.NewYork;

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

### FVG Detection Logic
```csharp
private void DetectFVGs()
{
    int lookback = Math.Min(FVGLookbackBars, m15Bars.Count - 3);

    for (int i = 2; i < lookback; i++)
    {
        int idx = m15Bars.Count - 1 - i;

        // Bullish FVG: candle[i-1].High < candle[i+1].Low
        if (m15Bars.HighPrices[idx - 1] < m15Bars.LowPrices[idx + 1])
        {
            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                BottomPrice = m15Bars.HighPrices[idx - 1],
                TopPrice = m15Bars.LowPrices[idx + 1],
                IsBullish = true,
                IsFilled = false
            };

            fvg.IsFilled = IsFVGFilled(fvg, idx);

            if (!fvg.IsFilled)
                activeFVGs.Add(fvg);
        }

        // Bearish FVG: candle[i-1].Low > candle[i+1].High
        if (m15Bars.LowPrices[idx - 1] > m15Bars.HighPrices[idx + 1])
        {
            var fvg = new FairValueGap
            {
                Time = m15Bars.OpenTimes[idx],
                TopPrice = m15Bars.LowPrices[idx - 1],
                BottomPrice = m15Bars.HighPrices[idx + 1],
                IsBullish = false,
                IsFilled = false
            };

            fvg.IsFilled = IsFVGFilled(fvg, idx);

            if (!fvg.IsFilled)
                activeFVGs.Add(fvg);
        }
    }
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

### Final Scoring (All Phases Complete)
```csharp
private double CalculateSwingScore(int swingIndex, string mode)
{
    double validityScore = CalculateValidityScore(swingIndex);
    if (validityScore == 0) return 0;

    double extremityScore = CalculateExtremityScore(swingIndex, mode);
    double fractalStrength = CalculateFractalStrength(swingIndex, mode);
    double sessionAlignment = CalculateSessionAlignment(swingIndex, mode);
    double fvgAlignment = CalculateFVGAlignment(swingIndex, mode);
    double candleStrength = CalculateCandleStrength(swingIndex);

    double totalScore =
        (validityScore * 0.20) +
        (extremityScore * 0.25) +
        (fractalStrength * 0.15) +
        (sessionAlignment * 0.20) +
        (fvgAlignment * 0.15) +
        (candleStrength * 0.05);

    return totalScore;
}
```

---

## Entry Logic Details

### User's Trading Rules

**SELL Mode (Rectangle at Swing HIGH):**
1. Rectangle drawn at swing HIGH zone (Close to High)
2. Wait for **M1 candle body to close BELOW rectangle**
3. That M1 candle = **trigger candle**
4. **Entry:** SELL at trigger candle's **close price**
5. **Stop Loss:** Upper edge of rectangle + spread buffer
6. **Take Profit:** 3R (3x risk) minimum, adjusted for M1 market structure

**BUY Mode (Rectangle at Swing LOW):**
1. Rectangle drawn at swing LOW zone (Close to Low)
2. Wait for **M1 candle body to close ABOVE rectangle**
3. That M1 candle = **trigger candle**
4. **Entry:** BUY at trigger candle's **close price**
5. **Stop Loss:** Lower edge of rectangle - spread buffer
6. **Take Profit:** 3R (3x risk) minimum, adjusted for M1 market structure

### Trigger Candle Detection
```csharp
// SELL Mode - WHOLE BODY must be below rectangle
bool isTriggerCandle =
    (Bars.OpenPrices.LastValue < swingBottomPrice) &&
    (Bars.ClosePrices.LastValue < swingBottomPrice);
// BOTH open AND close below rectangle bottom
// Wick CAN go into rectangle, but body must stay below

// BUY Mode - WHOLE BODY must be above rectangle
bool isTriggerCandle =
    (Bars.OpenPrices.LastValue > swingTopPrice) &&
    (Bars.ClosePrices.LastValue > swingTopPrice);
// BOTH open AND close above rectangle top
// Wick CAN go into rectangle, but body must stay above
```

### Rectangle Invalidation
```csharp
// SELL Mode - Rectangle becomes INVALID if body closes ABOVE
bool isRectangleInvalid = Bars.ClosePrices.LastValue > swingTopPrice;
// Body closing above (opposite side) = rectangle invalidated

// BUY Mode - Rectangle becomes INVALID if body closes BELOW
bool isRectangleInvalid = Bars.ClosePrices.LastValue < swingBottomPrice;
// Body closing below (opposite side) = rectangle invalidated
```

### Important Rules
1. **Wicks are allowed** to penetrate rectangle in any direction
2. **Only candle BODY position matters** for entry and invalidation
3. **Body = open to close range** (not including wicks)
4. **Trigger entry:** Whole body on breakout side (below for SELL, above for BUY)
5. **Invalidate rectangle:** Body closes on opposite side (above for SELL, below for BUY)
6. Once invalidated, ignore that rectangle and wait for new one

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

### Minimum Requirements
- [ ] Rectangles never expired (validity score works)
- [ ] Rectangle width = 60 minutes
- [ ] Only high-quality swings get rectangles (score >= 0.60)
- [ ] Market structure awareness (100 bar lookback)
- [ ] Backtest shows better trade quality vs old approach

### Advanced Requirements (Phases 2-3)
- [ ] Session levels tracked correctly
- [ ] Session boxes visible on chart
- [ ] FVGs detected and aligned
- [ ] Scoring system tunable via parameters
- [ ] Win rate improves in backtests

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

**Phase 1A (Basic Scoring):** 2-3 hours
- Core scoring system
- Validity + extremity + fractal strength
- Testing and debugging

**Phase 1B (Entry Logic):** 1-2 hours
- Breakout detection
- Rectangle invalidation
- Trade execution

**Phase 1C (M1 Structure TP):** 1 hour
- M1 swing detection
- TP adjustment logic

**Phase 2 (Sessions):** 2-3 hours
- Session detection
- Session high/low tracking
- Visual boxes
- Integration and testing

**Phase 3 (FVGs):** 2-3 hours
- FVG detection logic
- FVG tracking and filling
- Integration and testing

**Total:** 8-12 hours for full implementation

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

**End of Master Plan**

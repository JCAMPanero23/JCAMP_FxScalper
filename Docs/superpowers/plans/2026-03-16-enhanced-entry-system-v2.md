# Enhanced Entry System v2.0 Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement structural improvements to reduce 72% loss rate by using FVG-based zones, rejection candle confirmation, ATR-based SL, RSI compression-expansion filter, and dual SMA trend filter.

**Architecture:** Incremental refactor of existing `Jcamp_1M_scalping.cs`. Add new parameters alongside existing ones, add helper methods for filters, modify zone creation and pending order logic. All features configurable to avoid overfitting.

**Tech Stack:** cTrader/cAlgo C# (.NET), cAlgo.API indicators (ATR, RSI, SMA)

**Spec:** `D:\JCAMP_FxScalper\Docs\superpowers\specs\2026-03-16-enhanced-entry-system-v2-design.md`

---

## File Structure

**Files to Modify:**
- `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (main bot file, ~4500 lines)

**Copy Target (after each task):**
- `C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs`

**Testing:** Backtest in cTrader with date range 01/04/2024 - 30/06/2024, EURUSD M1

---

## Chunk 1: Foundation (Versioning + Parameters + Indicators)

### Task 1: Add Versioning System

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:17-20` (after class declaration)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:700-740` (OnStart logging)

- [ ] **Step 1: Add version constants after class declaration**

After line 17 (`public class Jcamp_1M_scalping : Robot`), add:

```csharp
        #region Version Info
        private const string BOT_VERSION = "2.0.0";
        private const string VERSION_DATE = "2026-03-16";
        private const string VERSION_NOTES = "FVG zones, rejection confirmation, RSI compression-expansion, ATR SL";
        #endregion
```

- [ ] **Step 2: Add version logging in OnStart**

Find the OnStart method (around line 700) and add at the very beginning:

```csharp
            // Log version info
            Print("========================================");
            Print($"Jcamp 1M Scalping v{BOT_VERSION} ({VERSION_DATE})");
            Print($"Notes: {VERSION_NOTES}");
            Print("========================================");
```

- [ ] **Step 3: Verify and commit**

```bash
# Copy to cAlgo location
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"

# Commit
cd D:\JCAMP_FxScalper
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add versioning system for backtest traceability"
```

---

### Task 2: Add New Parameters - Zone Configuration

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (after FVG Detection parameters, around line 450)

- [ ] **Step 1: Add FVGZoneSizePercent parameter**

After the FVG Detection parameters section (around line 450), add:

```csharp
        #region Parameters - Enhanced Entry v2.0

        [Parameter("=== ENHANCED ENTRY v2.0 ===", DefaultValue = "")]
        public string EnhancedEntryHeader { get; set; }

        // Zone Configuration
        [Parameter("FVG Zone Size %", DefaultValue = 100, MinValue = 50, MaxValue = 150, Step = 25, Group = "Enhanced Entry")]
        public int FVGZoneSizePercent { get; set; }

        #endregion
```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add FVGZoneSizePercent parameter"
```

---

### Task 3: Add New Parameters - Rejection Patterns

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in Enhanced Entry region)

- [ ] **Step 1: Add rejection pattern parameters**

Add after FVGZoneSizePercent in the Enhanced Entry region:

```csharp
        // Rejection Pattern Configuration
        [Parameter("Enable Wick Rejection", DefaultValue = true, Group = "Enhanced Entry")]
        public bool EnableWickRejection { get; set; }

        [Parameter("Enable Engulfing Pattern", DefaultValue = true, Group = "Enhanced Entry")]
        public bool EnableEngulfingPattern { get; set; }

        [Parameter("Enable Pin Bar", DefaultValue = true, Group = "Enhanced Entry")]
        public bool EnablePinBar { get; set; }

        [Parameter("Min Wick Ratio", DefaultValue = 2.0, MinValue = 1.5, MaxValue = 3.0, Step = 0.5, Group = "Enhanced Entry")]
        public double MinWickRatio { get; set; }

        [Parameter("Max Bars Without Rejection", DefaultValue = 5, MinValue = 3, MaxValue = 10, Step = 1, Group = "Enhanced Entry")]
        public int MaxBarsWithoutRejection { get; set; }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add rejection pattern parameters"
```

---

### Task 4: Add New Parameters - ATR Stop Loss

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in Enhanced Entry region)

- [ ] **Step 1: Add ATR SL parameter**

```csharp
        // ATR Stop Loss Configuration
        [Parameter("SL ATR Multiplier", DefaultValue = 1.5, MinValue = 1.0, MaxValue = 2.5, Step = 0.25, Group = "Enhanced Entry")]
        public double SLATRMultiplier { get; set; }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add SLATRMultiplier parameter"
```

---

### Task 5: Add New Parameters - RSI Compression-Expansion

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in Enhanced Entry region)

- [ ] **Step 1: Add RSI parameters**

```csharp
        // RSI Compression-Expansion Configuration
        [Parameter("Enable RSI Compression", DefaultValue = true, Group = "Enhanced Entry")]
        public bool EnableRSICompression { get; set; }

        [Parameter("RSI Period", DefaultValue = 7, MinValue = 5, MaxValue = 14, Step = 1, Group = "Enhanced Entry")]
        public int RSIPeriod { get; set; }

        [Parameter("RSI Compression Low", DefaultValue = 40, MinValue = 35, MaxValue = 45, Step = 5, Group = "Enhanced Entry")]
        public int RSICompressionLow { get; set; }

        [Parameter("RSI Compression High", DefaultValue = 60, MinValue = 55, MaxValue = 65, Step = 5, Group = "Enhanced Entry")]
        public int RSICompressionHigh { get; set; }

        [Parameter("RSI Compression Min Bars", DefaultValue = 6, MinValue = 4, MaxValue = 10, Step = 2, Group = "Enhanced Entry")]
        public int RSICompressionMinBars { get; set; }

        [Parameter("RSI Compression Lookback", DefaultValue = 15, MinValue = 10, MaxValue = 25, Step = 5, Group = "Enhanced Entry")]
        public int RSICompressionLookback { get; set; }

        [Parameter("RSI Expansion Buy Min", DefaultValue = 60, Group = "Enhanced Entry")]
        public int RSIExpansionBuyMin { get; set; }

        [Parameter("RSI Expansion Buy Max", DefaultValue = 80, Group = "Enhanced Entry")]
        public int RSIExpansionBuyMax { get; set; }

        [Parameter("RSI Expansion Sell Min", DefaultValue = 20, Group = "Enhanced Entry")]
        public int RSIExpansionSellMin { get; set; }

        [Parameter("RSI Expansion Sell Max", DefaultValue = 40, Group = "Enhanced Entry")]
        public int RSIExpansionSellMax { get; set; }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add RSI compression-expansion parameters"
```

---

### Task 6: Add New Parameters - Dual SMA and False Positive Filters

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in Enhanced Entry region)

- [ ] **Step 1: Add Dual SMA parameters**

```csharp
        // Dual SMA Configuration
        [Parameter("Enable Dual SMA", DefaultValue = true, Group = "Enhanced Entry")]
        public bool EnableDualSMA { get; set; }

        [Parameter("Fast SMA Period", DefaultValue = 50, MinValue = 20, MaxValue = 100, Step = 10, Group = "Enhanced Entry")]
        public int FastSMAPeriod { get; set; }

        // False Positive Filters
        [Parameter("Min Rejection ATR Ratio", DefaultValue = 0.5, MinValue = 0.3, MaxValue = 1.0, Step = 0.1, Group = "Enhanced Entry")]
        public double MinRejectionATRRatio { get; set; }

        [Parameter("Displacement Range ATR", DefaultValue = 1.5, MinValue = 1.0, MaxValue = 2.5, Step = 0.25, Group = "Enhanced Entry")]
        public double DisplacementRangeATR { get; set; }
```

- [ ] **Step 2: Close the parameters region and commit**

The `#endregion` for Parameters - Enhanced Entry v2.0 should already be there. Verify it's properly closed.

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add dual SMA and false positive filter parameters"
```

---

### Task 7: Initialize New Indicators

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:598-610` (private fields)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:657-665` (OnStart indicator init)

- [ ] **Step 1: Add indicator fields**

Find the private fields section (around line 598-610) and add:

```csharp
        // v2.0 indicators
        private RelativeStrengthIndex rsiM1;              // RSI for compression-expansion filter
        private SimpleMovingAverage smaFast;              // Fast SMA for dual trend filter
```

- [ ] **Step 2: Initialize indicators in OnStart**

Find the indicator initialization section in OnStart (around line 657-665, after ATR init) and add:

```csharp
            // v2.0: Initialize RSI and Fast SMA
            rsiM1 = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RSIPeriod);
            smaFast = Indicators.SimpleMovingAverage(m15Bars.ClosePrices, FastSMAPeriod);
            Print("[v2.0] RSI Period: {0} | Fast SMA Period: {1}", RSIPeriod, FastSMAPeriod);
```

- [ ] **Step 3: Add logging for new parameters in OnStart**

Find where parameters are logged (around line 720-740) and add:

```csharp
            // v2.0 Enhanced Entry parameters
            Print("=== ENHANCED ENTRY v2.0 ===");
            Print("FVG Zone Size: {0}%", FVGZoneSizePercent);
            Print("Rejection: Wick={0} Engulf={1} PinBar={2} | WickRatio={3:F1} | MaxBars={4}",
                EnableWickRejection, EnableEngulfingPattern, EnablePinBar, MinWickRatio, MaxBarsWithoutRejection);
            Print("SL ATR Multiplier: {0:F2}", SLATRMultiplier);
            Print("RSI Compression: Enabled={0} | Period={1} | Range={2}-{3} | MinBars={4} | Lookback={5}",
                EnableRSICompression, RSIPeriod, RSICompressionLow, RSICompressionHigh, RSICompressionMinBars, RSICompressionLookback);
            Print("RSI Expansion: Buy={0}-{1} | Sell={2}-{3}",
                RSIExpansionBuyMin, RSIExpansionBuyMax, RSIExpansionSellMin, RSIExpansionSellMax);
            Print("Dual SMA: Enabled={0} | FastPeriod={1}", EnableDualSMA, FastSMAPeriod);
            Print("False Positive Filters: RejectionATR={0:F1} | DisplacementATR={1:F1}",
                MinRejectionATRRatio, DisplacementRangeATR);
```

- [ ] **Step 4: Copy, rebuild and commit**

```bash
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"

git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Initialize RSI and Fast SMA indicators"
```

- [ ] **Step 5: Verify in cTrader**

1. Rebuild bot in cTrader
2. Run quick backtest (1 week)
3. Check log for version info and new parameter values
4. Verify no errors

---

## Chunk 2: Helper Methods (Filters and Detection)

### Task 8: Add Candle Significance Filter

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (add new region after Phase 3 FVG Detection, around line 2000)

- [ ] **Step 1: Add IsSignificantCandle helper method**

Add a new region after the FVG detection methods:

```csharp
        #region v2.0 Helper Methods

        /// <summary>
        /// Checks if a candle has significant range relative to ATR
        /// Used to filter out noise candles in rejection pattern detection
        /// </summary>
        private bool IsSignificantCandle(int barIdx)
        {
            if (barIdx < 0 || barIdx >= Bars.Count)
                return false;

            double high = Bars.HighPrices[barIdx];
            double low = Bars.LowPrices[barIdx];
            double range = high - low;
            double atr = atrM1.Result[barIdx];

            bool isSignificant = range >= atr * MinRejectionATRRatio;

            if (!isSignificant)
            {
                Print("[v2.0] Candle {0} not significant | Range: {1:F1} pips | ATR: {2:F1} pips | Ratio: {3:F2}",
                    barIdx, range / Symbol.PipSize, atr / Symbol.PipSize, range / atr);
            }

            return isSignificant;
        }

        #endregion
```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add IsSignificantCandle helper method"
```

---

### Task 9: Add Displacement Momentum Filter

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:2137-2185` (DetectM1Displacement method)

- [ ] **Step 1: Add range check to DetectM1Displacement**

Find the `DetectM1Displacement` method (around line 2137) and add the range check after body size calculation. The method currently checks body size against ATR. Add range check after line ~2158:

Find this existing code:
```csharp
            // Check displacement threshold (same multiplier as M15)
            if (bodySize < atrValue * ATRMultiplier)
                return null;
```

Add the range check right after:
```csharp
            // v2.0: Check displacement range (catches wicks showing momentum)
            double candleRange = high - low;
            if (candleRange < atrValue * DisplacementRangeATR)
            {
                Print("[v2.0] M1 Displacement rejected | Range: {0:F1} pips < {1:F1}x ATR ({2:F1} pips)",
                    candleRange / Symbol.PipSize, DisplacementRangeATR, atrValue * DisplacementRangeATR / Symbol.PipSize);
                return null;
            }
```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add displacement range ATR filter"
```

---

### Task 10: Add Rejection Candle Detection - Wick Rejection

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add CheckWickRejection method**

Add to the v2.0 Helper Methods region:

```csharp
        /// <summary>
        /// Checks for wick rejection pattern
        /// BUY: Lower wick > body (rejection of lower prices)
        /// SELL: Upper wick > body (rejection of higher prices)
        /// </summary>
        private bool CheckWickRejection(int barIdx, string mode)
        {
            if (!EnableWickRejection)
                return false;

            if (!IsSignificantCandle(barIdx))
                return false;

            double open = Bars.OpenPrices[barIdx];
            double close = Bars.ClosePrices[barIdx];
            double high = Bars.HighPrices[barIdx];
            double low = Bars.LowPrices[barIdx];

            double body = Math.Abs(close - open);
            double upperWick = high - Math.Max(open, close);
            double lowerWick = Math.Min(open, close) - low;

            // Prevent division by zero
            if (body < Symbol.PipSize * 0.1)
                body = Symbol.PipSize * 0.1;

            if (mode == "BUY")
            {
                // BUY rejection: lower wick > body
                bool isRejection = lowerWick > body;
                if (isRejection)
                {
                    Print("[v2.0] Wick rejection BUY | LowerWick: {0:F1} pips > Body: {1:F1} pips",
                        lowerWick / Symbol.PipSize, body / Symbol.PipSize);
                }
                return isRejection;
            }
            else // SELL
            {
                // SELL rejection: upper wick > body
                bool isRejection = upperWick > body;
                if (isRejection)
                {
                    Print("[v2.0] Wick rejection SELL | UpperWick: {0:F1} pips > Body: {1:F1} pips",
                        upperWick / Symbol.PipSize, body / Symbol.PipSize);
                }
                return isRejection;
            }
        }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add wick rejection detection"
```

---

### Task 11: Add Rejection Candle Detection - Pin Bar

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add CheckPinBar method**

```csharp
        /// <summary>
        /// Checks for pin bar pattern (stronger wick rejection)
        /// BUY: Lower wick >= MinWickRatio × body
        /// SELL: Upper wick >= MinWickRatio × body
        /// </summary>
        private bool CheckPinBar(int barIdx, string mode)
        {
            if (!EnablePinBar)
                return false;

            if (!IsSignificantCandle(barIdx))
                return false;

            double open = Bars.OpenPrices[barIdx];
            double close = Bars.ClosePrices[barIdx];
            double high = Bars.HighPrices[barIdx];
            double low = Bars.LowPrices[barIdx];

            double body = Math.Abs(close - open);
            double upperWick = high - Math.Max(open, close);
            double lowerWick = Math.Min(open, close) - low;

            // Prevent division by zero
            if (body < Symbol.PipSize * 0.1)
                body = Symbol.PipSize * 0.1;

            if (mode == "BUY")
            {
                // BUY pin bar: lower wick >= MinWickRatio × body
                bool isPinBar = lowerWick >= MinWickRatio * body;
                if (isPinBar)
                {
                    Print("[v2.0] Pin bar BUY | LowerWick: {0:F1} pips >= {1:F1}x Body ({2:F1} pips)",
                        lowerWick / Symbol.PipSize, MinWickRatio, body / Symbol.PipSize);
                }
                return isPinBar;
            }
            else // SELL
            {
                // SELL pin bar: upper wick >= MinWickRatio × body
                bool isPinBar = upperWick >= MinWickRatio * body;
                if (isPinBar)
                {
                    Print("[v2.0] Pin bar SELL | UpperWick: {0:F1} pips >= {1:F1}x Body ({2:F1} pips)",
                        upperWick / Symbol.PipSize, MinWickRatio, body / Symbol.PipSize);
                }
                return isPinBar;
            }
        }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add pin bar detection"
```

---

### Task 12: Add Rejection Candle Detection - Engulfing

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add CheckEngulfingPattern method**

```csharp
        /// <summary>
        /// Checks for engulfing pattern
        /// BUY: Bullish candle engulfs previous bearish candle
        /// SELL: Bearish candle engulfs previous bullish candle
        /// </summary>
        private bool CheckEngulfingPattern(int barIdx, string mode)
        {
            if (!EnableEngulfingPattern)
                return false;

            if (barIdx < 1 || barIdx >= Bars.Count)
                return false;

            if (!IsSignificantCandle(barIdx))
                return false;

            // Current candle
            double open = Bars.OpenPrices[barIdx];
            double close = Bars.ClosePrices[barIdx];
            bool isBullish = close > open;
            bool isBearish = close < open;

            // Previous candle
            double prevOpen = Bars.OpenPrices[barIdx - 1];
            double prevClose = Bars.ClosePrices[barIdx - 1];
            bool prevBullish = prevClose > prevOpen;
            bool prevBearish = prevClose < prevOpen;

            double currBody = Math.Abs(close - open);
            double prevBody = Math.Abs(prevClose - prevOpen);

            if (mode == "BUY")
            {
                // Bullish engulfing: current bullish engulfs previous bearish
                bool isEngulfing = isBullish && prevBearish && currBody > prevBody &&
                                   close > prevOpen && open < prevClose;
                if (isEngulfing)
                {
                    Print("[v2.0] Engulfing BUY | CurrBody: {0:F1} pips engulfs PrevBody: {1:F1} pips",
                        currBody / Symbol.PipSize, prevBody / Symbol.PipSize);
                }
                return isEngulfing;
            }
            else // SELL
            {
                // Bearish engulfing: current bearish engulfs previous bullish
                bool isEngulfing = isBearish && prevBullish && currBody > prevBody &&
                                   close < prevOpen && open > prevClose;
                if (isEngulfing)
                {
                    Print("[v2.0] Engulfing SELL | CurrBody: {0:F1} pips engulfs PrevBody: {1:F1} pips",
                        currBody / Symbol.PipSize, prevBody / Symbol.PipSize);
                }
                return isEngulfing;
            }
        }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add engulfing pattern detection"
```

---

### Task 13: Add Combined Rejection Check

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add HasRejectionConfirmation method**

```csharp
        /// <summary>
        /// Checks if any rejection pattern is present
        /// Returns true if ANY enabled pattern is detected
        /// </summary>
        private bool HasRejectionConfirmation(int barIdx, string mode)
        {
            // Check all enabled patterns
            if (CheckWickRejection(barIdx, mode))
                return true;

            if (CheckPinBar(barIdx, mode))
                return true;

            if (CheckEngulfingPattern(barIdx, mode))
                return true;

            return false;
        }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add combined rejection check method"
```

---

### Task 14: Add RSI Compression-Expansion Filter

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add CheckRSICompressionExpansion method**

```csharp
        /// <summary>
        /// Checks RSI compression-expansion pattern
        /// 1. Count bars where RSI was in compression zone (40-60) in lookback period
        /// 2. Require minimum compression bars
        /// 3. Current RSI must be in expansion zone (60-80 for BUY, 20-40 for SELL)
        /// </summary>
        private bool CheckRSICompressionExpansion(string mode)
        {
            if (!EnableRSICompression)
                return true;  // Pass if disabled

            int currentIdx = Bars.Count - 1;
            if (currentIdx < RSICompressionLookback)
                return false;

            // Count compression bars in lookback
            int compressionBars = 0;
            for (int i = 1; i <= RSICompressionLookback; i++)
            {
                int idx = currentIdx - i;
                if (idx < 0) break;

                double rsiValue = rsiM1.Result[idx];
                if (rsiValue >= RSICompressionLow && rsiValue <= RSICompressionHigh)
                {
                    compressionBars++;
                }
            }

            // Check minimum compression bars
            if (compressionBars < RSICompressionMinBars)
            {
                Print("[v2.0] RSI compression failed | Bars: {0} < Min: {1}", compressionBars, RSICompressionMinBars);
                return false;
            }

            // Check current RSI is in expansion zone
            double currentRSI = rsiM1.Result[currentIdx];

            if (mode == "BUY")
            {
                // BUY expansion: RSI in 60-80
                bool inExpansion = currentRSI >= RSIExpansionBuyMin && currentRSI <= RSIExpansionBuyMax;
                if (inExpansion)
                {
                    Print("[v2.0] RSI compression-expansion BUY | Compression: {0} bars | Current RSI: {1:F1}",
                        compressionBars, currentRSI);
                }
                else
                {
                    Print("[v2.0] RSI expansion failed BUY | RSI: {0:F1} not in {1}-{2}",
                        currentRSI, RSIExpansionBuyMin, RSIExpansionBuyMax);
                }
                return inExpansion;
            }
            else // SELL
            {
                // SELL expansion: RSI in 20-40
                bool inExpansion = currentRSI >= RSIExpansionSellMin && currentRSI <= RSIExpansionSellMax;
                if (inExpansion)
                {
                    Print("[v2.0] RSI compression-expansion SELL | Compression: {0} bars | Current RSI: {1:F1}",
                        compressionBars, currentRSI);
                }
                else
                {
                    Print("[v2.0] RSI expansion failed SELL | RSI: {0:F1} not in {1}-{2}",
                        currentRSI, RSIExpansionSellMin, RSIExpansionSellMax);
                }
                return inExpansion;
            }
        }

```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add RSI compression-expansion filter"
```

---

### Task 15: Add Dual SMA Trend Filter

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (continue in v2.0 Helper Methods region)

- [ ] **Step 1: Add CheckDualSMAFilter method**

```csharp
        /// <summary>
        /// Checks dual SMA trend filter
        /// BUY: Price > SMA200 AND Price > SMAFast
        /// SELL: Price < SMA200 AND Price < SMAFast
        /// </summary>
        private bool CheckDualSMAFilter(string mode)
        {
            if (!EnableDualSMA)
                return true;  // Pass if disabled

            double currentPrice = Symbol.Bid;
            double sma200Value = sma.Result.LastValue;
            double smaFastValue = smaFast.Result.LastValue;

            if (mode == "BUY")
            {
                bool aboveBoth = currentPrice > sma200Value && currentPrice > smaFastValue;
                if (aboveBoth)
                {
                    Print("[v2.0] Dual SMA BUY | Price: {0:F5} > SMA200: {1:F5} AND > SMAFast({2}): {3:F5}",
                        currentPrice, sma200Value, FastSMAPeriod, smaFastValue);
                }
                else
                {
                    Print("[v2.0] Dual SMA failed BUY | Price: {0:F5} | SMA200: {1:F5} | SMAFast: {2:F5}",
                        currentPrice, sma200Value, smaFastValue);
                }
                return aboveBoth;
            }
            else // SELL
            {
                bool belowBoth = currentPrice < sma200Value && currentPrice < smaFastValue;
                if (belowBoth)
                {
                    Print("[v2.0] Dual SMA SELL | Price: {0:F5} < SMA200: {1:F5} AND < SMAFast({2}): {3:F5}",
                        currentPrice, sma200Value, FastSMAPeriod, smaFastValue);
                }
                else
                {
                    Print("[v2.0] Dual SMA failed SELL | Price: {0:F5} | SMA200: {1:F5} | SMAFast: {2:F5}",
                        currentPrice, sma200Value, smaFastValue);
                }
                return belowBoth;
            }
        }

```

- [ ] **Step 2: Close the v2.0 Helper Methods region and commit**

```csharp
        // End of v2.0 Helper Methods region is handled by existing #endregion
```

```bash
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"

git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add dual SMA trend filter"
```

---

## Chunk 3: Core Logic Modifications

### Task 16: Modify Zone Creation to Use FVG Boundaries

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:2330-2333` (CreatePreZone method)

- [ ] **Step 1: Replace fixed 4-pip zone with FVG-based zone**

Find the zone boundary calculation in CreatePreZone (around line 2330):

```csharp
            // Calculate zone boundaries (4 pips total width)
            double originPrice = displacement.OriginPrice;
            double topPrice = originPrice + (2 * Symbol.PipSize);
            double bottomPrice = originPrice - (2 * Symbol.PipSize);
```

Replace with:

```csharp
            // v2.0: Calculate zone boundaries from FVG with configurable size percentage
            double fvgHeight = fvg.HighPrice - fvg.LowPrice;
            double zoneCenter = (fvg.HighPrice + fvg.LowPrice) / 2;
            double adjustedHeight = fvgHeight * (FVGZoneSizePercent / 100.0);

            double topPrice = zoneCenter + (adjustedHeight / 2);
            double bottomPrice = zoneCenter - (adjustedHeight / 2);
            double originPrice = zoneCenter;  // Use center as reference

            Print("[v2.0] FVG Zone | FVG: {0:F5}-{1:F5} ({2:F1} pips) | Size: {3}% | Zone: {4:F5}-{5:F5}",
                fvg.LowPrice, fvg.HighPrice, fvgHeight / Symbol.PipSize, FVGZoneSizePercent, bottomPrice, topPrice);
```

- [ ] **Step 2: Also store FVG boundaries in TradingZone**

Find where zone properties are set (around line 2365-2383) and add FVG price storage. The TradingZone class needs two new properties. First, add to the TradingZone class definition (around line 252):

```csharp
            public double FVGTopPrice { get; set; }       // Original FVG top for SL calculation
            public double FVGBottomPrice { get; set; }    // Original FVG bottom for SL calculation
```

Then in CreatePreZone, add the FVG prices to the zone:

```csharp
                FVGTopPrice = fvg.HighPrice,
                FVGBottomPrice = fvg.LowPrice,
```

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Use FVG boundaries for zone creation"
```

---

### Task 17: Add v2.0 Filter Checks to Zone Arming

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:2454-2476` (UpdateZoneStates method)

- [ ] **Step 1: Add filter checks before placing pending order**

Find the zone arming section in UpdateZoneStates (around line 2454):

```csharp
            // Check proximity for arming (if not already armed)
            if (activeZone.State == ZoneState.Pre || activeZone.State == ZoneState.Valid)
            {
                if (CheckZoneProximity())
                {
                    activeZone.State = ZoneState.Armed;
```

Add v2.0 filter checks after proximity check but before arming:

```csharp
            // Check proximity for arming (if not already armed)
            if (activeZone.State == ZoneState.Pre || activeZone.State == ZoneState.Valid)
            {
                if (CheckZoneProximity())
                {
                    // v2.0: Check filters before arming
                    if (!CheckDualSMAFilter(activeZone.Mode))
                    {
                        Print("[v2.0] Zone not armed | Dual SMA filter failed");
                        return;
                    }

                    if (!CheckRSICompressionExpansion(activeZone.Mode))
                    {
                        Print("[v2.0] Zone not armed | RSI compression-expansion failed");
                        return;
                    }

                    activeZone.State = ZoneState.Armed;
```

- [ ] **Step 2: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add dual SMA and RSI filters to zone arming"
```

---

### Task 18: Add Pending Order Validation with Rejection Check

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs` (add tracking field and validation method)

- [ ] **Step 1: Add tracking fields for pending order validation**

Add to the private fields section (around line 609):

```csharp
        // v2.0: Pending order validation tracking
        private int _pendingOrderBarsWaiting = 0;
        private bool _rejectionConfirmed = false;
```

- [ ] **Step 2: Add ValidatePendingOrderFilters method**

Add to the v2.0 Helper Methods region:

```csharp
        /// <summary>
        /// Validates pending order each bar:
        /// 1. Check for rejection candle
        /// 2. Re-validate filters
        /// 3. Cancel if max bars exceeded without rejection
        /// </summary>
        private void ValidatePendingOrderFilters()
        {
            if (activeZone == null || activeZone.State != ZoneState.Armed)
                return;

            if (EntryExecution != EntryExecutionMode.PendingStop)
                return;

            // Check if we have a pending order for this zone
            if (!_zonePendingOrders.ContainsKey(activeZone.Id))
                return;

            _pendingOrderBarsWaiting++;

            // Check for rejection confirmation
            int currentBar = Bars.Count - 1;
            if (!_rejectionConfirmed && HasRejectionConfirmation(currentBar, activeZone.Mode))
            {
                _rejectionConfirmed = true;
                Print("[v2.0] Rejection confirmed for zone {0} | Bar: {1}", activeZone.Id, _pendingOrderBarsWaiting);
            }

            // Re-validate filters
            if (!CheckDualSMAFilter(activeZone.Mode))
            {
                CancelZonePendingOrder(activeZone.Id, "v2.0: Dual SMA filter invalid");
                ResetPendingOrderTracking();
                return;
            }

            if (!CheckRSICompressionExpansion(activeZone.Mode))
            {
                CancelZonePendingOrder(activeZone.Id, "v2.0: RSI filter invalid");
                ResetPendingOrderTracking();
                return;
            }

            // Cancel if max bars exceeded without rejection
            if (!_rejectionConfirmed && _pendingOrderBarsWaiting >= MaxBarsWithoutRejection)
            {
                CancelZonePendingOrder(activeZone.Id, $"v2.0: No rejection in {MaxBarsWithoutRejection} bars");
                ResetPendingOrderTracking();
                return;
            }
        }

        /// <summary>
        /// Resets pending order validation tracking
        /// </summary>
        private void ResetPendingOrderTracking()
        {
            _pendingOrderBarsWaiting = 0;
            _rejectionConfirmed = false;
        }

```

- [ ] **Step 3: Call validation in OnBar**

Find the OnBar method and add the validation call. Around line 802 where CheckExpiredPendingOrders is called, add:

```csharp
            // v2.0: Validate pending order filters
            ValidatePendingOrderFilters();
```

- [ ] **Step 4: Reset tracking when zone is armed**

In UpdateZoneStates, after the zone is armed and pending order is placed (around line 2474), add:

```csharp
                    // v2.0: Reset pending order tracking when new order placed
                    ResetPendingOrderTracking();
```

- [ ] **Step 5: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Add pending order validation with rejection check"
```

---

### Task 19: Modify Stop Loss Calculation to Use ATR

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3708-3710` (PlaceBuyPendingOrder)
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs:3793-3795` (PlaceSellPendingOrder)

- [ ] **Step 1: Modify BUY SL calculation**

Find the SL calculation in PlaceBuyPendingOrder (around line 3708):

```csharp
            // Calculate SL at zone bottom - buffer
            double slPrice = zone.BottomPrice - (SLBufferPips * Symbol.PipSize);
            double slPips = (entryPrice - slPrice) / Symbol.PipSize;
```

Replace with:

```csharp
            // v2.0: Calculate SL using MAX(FVG boundary + buffer, ATR × multiplier)
            double zoneBoundarySL = zone.FVGBottomPrice - (SLBufferPips * Symbol.PipSize);
            double atrBasedSL = entryPrice - (atrM1.Result.LastValue * SLATRMultiplier);
            double slPrice = Math.Min(zoneBoundarySL, atrBasedSL);  // Take the wider SL
            double slPips = (entryPrice - slPrice) / Symbol.PipSize;

            Print("[v2.0] BUY SL | Zone: {0:F5} | ATR: {1:F5} | Final: {2:F5} ({3:F1} pips)",
                zoneBoundarySL, atrBasedSL, slPrice, slPips);
```

- [ ] **Step 2: Modify SELL SL calculation**

Find the SL calculation in PlaceSellPendingOrder (around line 3793):

```csharp
            // Calculate SL at zone top + buffer
            double slPrice = zone.TopPrice + (SLBufferPips * Symbol.PipSize);
            double slPips = (slPrice - entryPrice) / Symbol.PipSize;
```

Replace with:

```csharp
            // v2.0: Calculate SL using MAX(FVG boundary + buffer, ATR × multiplier)
            double zoneBoundarySL = zone.FVGTopPrice + (SLBufferPips * Symbol.PipSize);
            double atrBasedSL = entryPrice + (atrM1.Result.LastValue * SLATRMultiplier);
            double slPrice = Math.Max(zoneBoundarySL, atrBasedSL);  // Take the wider SL
            double slPips = (slPrice - entryPrice) / Symbol.PipSize;

            Print("[v2.0] SELL SL | Zone: {0:F5} | ATR: {1:F5} | Final: {2:F5} ({3:F1} pips)",
                zoneBoundarySL, atrBasedSL, slPrice, slPips);
```

- [ ] **Step 3: Commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Use ATR-based stop loss calculation"
```

---

### Task 20: Final Integration and Testing

**Files:**
- Modify: `D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs`

- [ ] **Step 1: Update version to indicate completion**

Update the version constants:

```csharp
        private const string BOT_VERSION = "2.0.0";
        private const string VERSION_DATE = "2026-03-16";
        private const string VERSION_NOTES = "FVG zones, rejection confirmation, RSI compression-expansion, ATR SL, dual SMA";
```

- [ ] **Step 2: Copy to cAlgo and rebuild**

```bash
cp "D:\JCAMP_FxScalper\Jcamp_1M_scalping.cs" "C:\Users\Jcamp_Laptop\Documents\cAlgo\Sources\Robots\Jcamp_1M_scalping\Jcamp_1M_scalping\Jcamp_1M_scalping.cs"
```

- [ ] **Step 3: Run full backtest**

1. Open cTrader
2. Rebuild the bot
3. Run backtest: 01/04/2024 - 30/06/2024, EURUSD M1
4. Check logs for:
   - Version info logged
   - All new parameters logged
   - v2.0 filter logs appearing
   - No errors

- [ ] **Step 4: Verify metrics**

Expected changes:
- Trade count: Should be lower (more filtering)
- Each trade should have: SL logged, rejection status, RSI status, SMA status

- [ ] **Step 5: Final commit**

```bash
git add Jcamp_1M_scalping.cs
git commit -m "feat(v2): Complete Enhanced Entry System v2.0 implementation"
```

---

## Summary

**Total Tasks:** 20
**Estimated Changes:** ~400 lines added/modified

**Key Files:**
- `Jcamp_1M_scalping.cs` - All modifications

**Testing Strategy:**
1. After each task, copy to cAlgo and rebuild
2. After Chunk 1: Verify parameters appear and indicators initialize
3. After Chunk 2: Verify helper methods work (check logs)
4. After Chunk 3: Full backtest to verify entry flow

**Success Criteria:**
- [ ] Win rate improves from 28% to >45%
- [ ] Profit factor improves from 1.06 to >1.3
- [ ] No positions opened without SL/TP
- [ ] All new parameters logged and traceable
- [ ] Version number logged on startup

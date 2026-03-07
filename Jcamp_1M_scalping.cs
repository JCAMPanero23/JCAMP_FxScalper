using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    /// <summary>
    /// Jcamp 1M Scalping Strategy
    /// Based on M15 EMA 200 trend detection and swing rectangle entry zones
    /// SELL Mode: Enter SELL when price enters swing HIGH rectangle (Close to High)
    /// BUY Mode: Enter BUY when price enters swing LOW rectangle (Close to Low)
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Jcamp_1M_scalping : Robot
    {
        #region Parameters - Trend Detection

        [Parameter("=== TREND DETECTION ===", DefaultValue = "")]
        public string TrendHeader { get; set; }

        [Parameter("EMA Period", DefaultValue = 200, MinValue = 50, MaxValue = 500, Group = "Trend Detection")]
        public int EMAPeriod { get; set; }

        [Parameter("Swing Lookback Bars (M15)", DefaultValue = 100, MinValue = 10, MaxValue = 200, Group = "Trend Detection")]
        public int SwingLookbackBars { get; set; }

        [Parameter("Minimum Swing Score", DefaultValue = 0.60, MinValue = 0.0, MaxValue = 1.0, Group = "Trend Detection")]
        public double MinimumSwingScore { get; set; }

        #endregion

        #region Parameters - Trade Management

        [Parameter("=== TRADE MANAGEMENT ===", DefaultValue = "")]
        public string TradeHeader { get; set; }

        [Parameter("Lot Size", DefaultValue = 0.01, MinValue = 0.01, MaxValue = 100, Group = "Trade Management")]
        public double LotSize { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 20, MinValue = 5, MaxValue = 200, Group = "Trade Management")]
        public int StopLossPips { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 40, MinValue = 10, MaxValue = 500, Group = "Trade Management")]
        public int TakeProfitPips { get; set; }

        [Parameter("Max Positions", DefaultValue = 1, MinValue = 1, MaxValue = 10, Group = "Trade Management")]
        public int MaxPositions { get; set; }

        [Parameter("Magic Number", DefaultValue = 100001, Group = "Trade Management")]
        public int MagicNumber { get; set; }

        #endregion

        #region Parameters - Entry Filters

        [Parameter("=== ENTRY FILTERS ===", DefaultValue = "")]
        public string EntryHeader { get; set; }

        [Parameter("Enable Trading", DefaultValue = false, Group = "Entry Filters")]
        public bool EnableTrading { get; set; }

        [Parameter("Trade on New Swing Only", DefaultValue = true, Group = "Entry Filters")]
        public bool TradeOnNewSwingOnly { get; set; }

        #endregion

        #region Parameters - Visualization

        [Parameter("=== VISUALIZATION ===", DefaultValue = "")]
        public string VisualHeader { get; set; }

        [Parameter("Show Rectangles", DefaultValue = true, Group = "Visualization")]
        public bool ShowRectangles { get; set; }

        [Parameter("Rectangle Width (Minutes)", DefaultValue = 60, MinValue = 10, MaxValue = 200, Group = "Visualization")]
        public int RectangleWidthMinutes { get; set; }

        [Parameter("Show Mode Label", DefaultValue = true, Group = "Visualization")]
        public bool ShowModeLabel { get; set; }

        [Parameter("BUY Color", DefaultValue = "Green", Group = "Visualization")]
        public string BuyColorName { get; set; }

        [Parameter("SELL Color", DefaultValue = "Red", Group = "Visualization")]
        public string SellColorName { get; set; }

        [Parameter("Rectangle Transparency", DefaultValue = 80, MinValue = 0, MaxValue = 255, Group = "Visualization")]
        public int RectangleTransparency { get; set; }

        [Parameter("Max Rectangles", DefaultValue = 10, MinValue = 1, MaxValue = 50, Group = "Visualization")]
        public int MaxRectangles { get; set; }

        #endregion

        #region Private Fields

        private Bars m15Bars;
        private ExponentialMovingAverage ema200_m15;
        private bool isM15Chart;

        // State tracking
        private string currentMode = "";
        private DateTime lastM15BarTime;
        private DateTime lastSwingTime = DateTime.MinValue;

        // Current swing rectangle zone
        private double swingTopPrice = 0;
        private double swingBottomPrice = 0;
        private bool hasActiveSwing = false;

        // Visualization tracking
        private int rectangleCounter = 0;
        private class RectangleInfo
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        private System.Collections.Generic.List<RectangleInfo> drawnRectangles = new System.Collections.Generic.List<RectangleInfo>();
        private ChartStaticText modeLabel;

        #endregion

        #region Initialization

        protected override void OnStart()
        {
            // Validate chart timeframe - accept M1 or M15
            if (TimeFrame != TimeFrame.Minute && TimeFrame != TimeFrame.Minute15)
            {
                Print("ERROR: This cBot must run on M1 or M15 timeframe!");
                Print("Current timeframe: {0}", TimeFrame);
                Stop();
                return;
            }

            // Check if we're running on M15 chart
            isM15Chart = (TimeFrame == TimeFrame.Minute15);

            Print("========================================");
            Print("=== JCAMP 1M SCALPING BOT STARTED ===");
            Print("========================================");

            // Always use MarketData.GetBars for M15 data to ensure consistency
            // between M1 and M15 chart runs (same data source = same results)
            m15Bars = MarketData.GetBars(TimeFrame.Minute15);
            ema200_m15 = Indicators.ExponentialMovingAverage(m15Bars.ClosePrices, EMAPeriod);

            if (isM15Chart)
            {
                Print("Chart: M15 | Analysis: M15 (via MarketData.GetBars)");
            }
            else
            {
                Print("Chart: M1 | Analysis: M15 (via MarketData.GetBars)");
            }

            // Initialize state
            lastM15BarTime = m15Bars.OpenTimes.LastValue;

            Print("EMA Period: {0} | Swing Lookback: {1} bars", EMAPeriod, SwingLookbackBars);
            Print("Lot Size: {0} | SL: {1} pips | TP: {2} pips", LotSize, StopLossPips, TakeProfitPips);
            Print("Max Positions: {0} | Magic: {1}", MaxPositions, MagicNumber);
            Print("Trading Enabled: {0}", EnableTrading);
            Print("Visualization: Rectangles={0} | Mode Label={1}", ShowRectangles, ShowModeLabel);
            Print("========================================");

            // Detect initial mode and show label immediately
            if (m15Bars.Count >= EMAPeriod + 5)
            {
                currentMode = DetectTrendMode();
                Print("Initial Mode: {0}", currentMode);

                if (ShowModeLabel)
                {
                    UpdateModeDisplay(currentMode);
                }
            }
        }

        #endregion

        #region OnBar - Main Trading Loop

        protected override void OnBar()
        {
            // Check if enough M15 bars for EMA calculation
            if (m15Bars.Count < EMAPeriod + 5)
            {
                Print("Waiting for {0} M15 bars for EMA calculation (current: {1})",
                    EMAPeriod + 5, m15Bars.Count);
                return;
            }

            // Always update mode display on M1 bars (keeps label visible on M1 chart)
            if (!isM15Chart && ShowModeLabel && !string.IsNullOrEmpty(currentMode))
            {
                UpdateModeDisplay(currentMode);
            }

            // Only process swing detection when a NEW M15 bar appears
            bool isNewM15Bar = (m15Bars.OpenTimes.LastValue != lastM15BarTime);

            if (!isNewM15Bar)
                return;

            lastM15BarTime = m15Bars.OpenTimes.LastValue;

            Print("=== NEW M15 BAR: {0} ===", lastM15BarTime);

            // 1. Detect current trend mode
            string newMode = DetectTrendMode();

            // 2. Update mode if changed
            if (newMode != currentMode && !string.IsNullOrEmpty(newMode))
            {
                currentMode = newMode;
                Print(">>> MODE CHANGED: {0} MODE <<<", currentMode);
            }

            // 3. Find significant swing point based on mode (with scoring)
            int swingIndex = FindSignificantSwing(currentMode);

            if (swingIndex == -1)
            {
                Print("[{0}] No significant swing found (score >= {1:F2}) in last {2} M15 bars",
                    currentMode, MinimumSwingScore, SwingLookbackBars);
                hasActiveSwing = false;
                return;
            }

            // 4. Update swing rectangle zone
            DateTime swingTime = m15Bars.OpenTimes[swingIndex];

            // Check if this is a new swing
            bool isNewSwing = (swingTime != lastSwingTime);

            if (isNewSwing)
            {
                UpdateSwingZone(swingIndex, currentMode);
                lastSwingTime = swingTime;
                hasActiveSwing = true;
            }
        }

        #endregion

        #region OnTick - Entry Logic

        protected override void OnTick()
        {
            if (!EnableTrading || !hasActiveSwing)
                return;

            // Check if we already have max positions
            var positions = Positions.FindAll(MagicNumber.ToString(), SymbolName);
            if (positions.Length >= MaxPositions)
                return;

            // Get current price
            double currentPrice = Symbol.Bid;

            // Check if price is inside the swing rectangle zone
            bool isPriceInZone = currentPrice >= swingBottomPrice && currentPrice <= swingTopPrice;

            if (!isPriceInZone)
                return;

            // Execute trade based on mode
            if (currentMode == "SELL" && positions.Length == 0)
            {
                ExecuteSellTrade();
            }
            else if (currentMode == "BUY" && positions.Length == 0)
            {
                ExecuteBuyTrade();
            }
        }

        #endregion

        #region Trend Detection

        /// <summary>
        /// Detects trend mode using M15 price vs MA 200
        /// Uses custom SMA calculation to ensure consistency between M1 and M15 charts
        /// (EMA depends on all historical data, SMA only uses last N bars)
        /// </summary>
        private string DetectTrendMode()
        {
            int lastIdx = m15Bars.Count - 1;

            double currentPrice = m15Bars.ClosePrices[lastIdx];

            // Use custom SMA calculation (only last 200 bars) for consistency
            // This avoids EMA's dependency on different historical data lengths
            double smaValue = CalculateSMA(EMAPeriod);

            string mode = currentPrice > smaValue ? "BUY" : "SELL";

            // Debug: Show bar count and time to diagnose M1 vs M15 discrepancies
            Print("[TrendDetection] Chart: {0} | BarCount: {1} | LastBarTime: {2}",
                isM15Chart ? "M15" : "M1", m15Bars.Count, m15Bars.OpenTimes[lastIdx]);
            Print("[TrendDetection] M15 Price: {0:F5} | SMA{1}: {2:F5} | Mode: {3}",
                currentPrice, EMAPeriod, smaValue, mode);

            return mode;
        }

        /// <summary>
        /// Calculates Simple Moving Average over last N bars
        /// Consistent regardless of total historical data loaded
        /// </summary>
        private double CalculateSMA(int periods)
        {
            int count = Math.Min(periods, m15Bars.Count);

            if (count <= 0)
                return 0;

            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += m15Bars.ClosePrices.Last(i);
            }

            return sum / count;
        }

        #endregion

        #region Swing Point Detection

        /// <summary>
        /// Finds significant swing using Williams Fractals with multi-criteria scoring
        /// Phase 1A: Validity, Extremity, Fractal Strength, Candle Quality
        /// Returns the highest scoring swing that meets minimum threshold
        /// </summary>
        private int FindSignificantSwing(string mode)
        {
            // Step 1: Find ALL Williams Fractals in lookback period
            var allSwings = new System.Collections.Generic.List<int>();
            int barsToScan = Math.Min(SwingLookbackBars, m15Bars.Count - 5);

            // Guard against insufficient bars for fractal detection
            if (barsToScan < 5)
            {
                Print("[SwingDetection] Not enough bars to scan: {0}", barsToScan);
                return -1;
            }

            for (int i = 2; i < barsToScan - 2; i++)
            {
                int idx = m15Bars.Count - 1 - i;

                if (IsWilliamsFractal(idx, mode))
                {
                    allSwings.Add(idx);
                }
            }

            if (allSwings.Count == 0)
            {
                Print("[SwingDetection] No Williams Fractals found in {0} bars", barsToScan);
                return -1;
            }

            Print("[SwingDetection] Found {0} Williams Fractals, scoring...", allSwings.Count);

            // Step 2: Score each swing
            var scoredSwings = new System.Collections.Generic.List<System.Tuple<int, double>>();

            foreach (var idx in allSwings)
            {
                double score = CalculateSwingScore(idx, mode);

                if (score >= MinimumSwingScore)
                {
                    scoredSwings.Add(new System.Tuple<int, double>(idx, score));
                    Print("[SwingScore] Bar {0} | Score: {1:F2} ✓", idx, score);
                }
                else
                {
                    Print("[SwingScore] Bar {0} | Score: {1:F2} ✗ (below {2:F2})",
                        idx, score, MinimumSwingScore);
                }
            }

            if (scoredSwings.Count == 0)
            {
                Print("[SwingDetection] No swings scored >= {0:F2}", MinimumSwingScore);
                return -1;
            }

            // Step 3: Return highest scoring swing
            var bestSwing = scoredSwings.OrderByDescending(s => s.Item2).First();

            Print("[SignificantSwing] ✅ Selected Bar {0} | Score: {1:F2} | Price: {2:F5} | Time: {3}",
                bestSwing.Item1,
                bestSwing.Item2,
                mode == "SELL" ? m15Bars.HighPrices[bestSwing.Item1] : m15Bars.LowPrices[bestSwing.Item1],
                m15Bars.OpenTimes[bestSwing.Item1]);

            return bestSwing.Item1;
        }

        /// <summary>
        /// Checks if bar at index is a valid Williams Fractal
        /// SELL Mode: Swing HIGH from BULLISH candle
        /// BUY Mode: Swing LOW from BEARISH candle
        /// </summary>
        private bool IsWilliamsFractal(int idx, string mode)
        {
            if (mode == "SELL")
            {
                // Williams Fractal Up: High[i] > High[i±1] and High[i] > High[i±2]
                bool isSwingHigh = m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 1] &&
                                   m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 2] &&
                                   m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 1] &&
                                   m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 2];

                // Must be BULLISH candle
                bool isBullishCandle = m15Bars.ClosePrices[idx] > m15Bars.OpenPrices[idx];

                return isSwingHigh && isBullishCandle;
            }
            else // BUY mode
            {
                // Williams Fractal Down: Low[i] < Low[i±1] and Low[i] < Low[i±2]
                bool isSwingLow = m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 1] &&
                                  m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 2] &&
                                  m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 1] &&
                                  m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 2];

                // Must be BEARISH candle
                bool isBearishCandle = m15Bars.ClosePrices[idx] < m15Bars.OpenPrices[idx];

                return isSwingLow && isBearishCandle;
            }
        }

        #endregion

        #region Swing Scoring System (Phase 1A)

        /// <summary>
        /// Calculates total score for a swing point
        /// Phase 1A: Validity (25%) + Extremity (35%) + Fractal Strength (25%) + Candle (15%)
        /// </summary>
        private double CalculateSwingScore(int swingIndex, string mode)
        {
            // Validity score - CRITICAL (must be > 0)
            double validityScore = CalculateValidityScore(swingIndex);

            if (validityScore == 0)
            {
                // Rectangle would be expired - skip this swing
                return 0;
            }

            // Calculate other scoring components
            double extremityScore = CalculateExtremityScore(swingIndex, mode);
            double fractalStrength = CalculateFractalStrength(swingIndex, mode);
            double candleStrength = CalculateCandleStrength(swingIndex);

            // Phase 1A weights (redistributed from original plan since session/FVG not yet implemented)
            double totalScore =
                (validityScore * 0.25) +    // 20% → 25% (critical for forward-looking rectangles)
                (extremityScore * 0.35) +   // 25% → 35% (most extreme swings preferred)
                (fractalStrength * 0.25) +  // 15% → 25% (fractal quality matters)
                (candleStrength * 0.15);    // 5% → 15% (candle body strength)

            return totalScore;
        }

        /// <summary>
        /// Validity Score: Ensures rectangle is forward-looking (not expired)
        /// Returns 0 if rectangle would be in the past, otherwise 0-1 based on time remaining
        /// </summary>
        private double CalculateValidityScore(int swingIndex)
        {
            DateTime swingTime = m15Bars.OpenTimes[swingIndex];
            DateTime rectangleEnd = swingTime.AddMinutes(RectangleWidthMinutes);
            DateTime currentTime = m15Bars.OpenTimes.LastValue;

            TimeSpan remaining = rectangleEnd - currentTime;

            if (remaining.TotalMinutes <= 0)
            {
                // Rectangle is expired - invalid
                return 0;
            }

            // Score based on how much time remains (normalized to rectangle width)
            // Full rectangle width remaining = 1.0, just started = lower score
            double score = Math.Min(remaining.TotalMinutes / RectangleWidthMinutes, 1.0);

            return score;
        }

        /// <summary>
        /// Extremity Score: How extreme the swing is compared to market structure
        /// Higher/lower swings score better
        /// </summary>
        private double CalculateExtremityScore(int swingIndex, string mode)
        {
            int lookback = Math.Min(SwingLookbackBars, m15Bars.Count);

            if (mode == "SELL")
            {
                double swingHigh = m15Bars.HighPrices[swingIndex];
                double highestHigh = m15Bars.HighPrices.Maximum(lookback);
                double avgHigh = CalculateDataSeriesAverage(m15Bars.HighPrices, lookback);

                if (highestHigh == avgHigh)
                    return 0.5; // Avoid division by zero

                // Higher swings score better
                double score = (swingHigh - avgHigh) / (highestHigh - avgHigh);
                return Math.Max(0, Math.Min(score, 1.0)); // Clamp 0-1
            }
            else // BUY mode
            {
                double swingLow = m15Bars.LowPrices[swingIndex];
                double lowestLow = m15Bars.LowPrices.Minimum(lookback);
                double avgLow = CalculateDataSeriesAverage(m15Bars.LowPrices, lookback);

                if (avgLow == lowestLow)
                    return 0.5;

                // Lower swings score better
                double score = (avgLow - swingLow) / (avgLow - lowestLow);
                return Math.Max(0, Math.Min(score, 1.0)); // Clamp 0-1
            }
        }

        /// <summary>
        /// Calculates average of a DataSeries over specified number of periods
        /// (cTrader DataSeries.Average requires a selector, not period count)
        /// </summary>
        private double CalculateDataSeriesAverage(DataSeries series, int periods)
        {
            int count = Math.Min(periods, series.Count);

            if (count <= 0)
                return 0;

            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += series.Last(i);
            }

            return sum / count;
        }

        /// <summary>
        /// Fractal Strength Score: Quality of Williams Fractal
        /// Measures how far the swing extends beyond neighboring bars
        /// </summary>
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
                double avgRange = CalculateAverageRange(20); // Last 20 bars

                if (avgRange == 0)
                    return 0.5;

                double score = strength / avgRange;
                return Math.Max(0, Math.Min(score, 1.0)); // Clamp 0-1
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

                if (avgRange == 0)
                    return 0.5;

                double score = strength / avgRange;
                return Math.Max(0, Math.Min(score, 1.0)); // Clamp 0-1
            }
        }

        /// <summary>
        /// Candle Strength Score: Quality of the swing candle
        /// Strong body candles (low wick ratio) score higher
        /// </summary>
        private double CalculateCandleStrength(int swingIndex)
        {
            double open = m15Bars.OpenPrices[swingIndex];
            double close = m15Bars.ClosePrices[swingIndex];
            double high = m15Bars.HighPrices[swingIndex];
            double low = m15Bars.LowPrices[swingIndex];

            double bodySize = Math.Abs(close - open);
            double totalSize = high - low;

            if (totalSize == 0)
                return 0.3; // Doji or error

            double bodyRatio = bodySize / totalSize;

            // Strong candle body = higher score
            if (bodyRatio >= 0.70)
                return 1.0; // Strong candle (body > 70%)
            else if (bodyRatio >= 0.50)
                return 0.6; // Medium candle (body 50-70%)
            else
                return 0.3; // Weak candle (doji, pin bar)
        }

        /// <summary>
        /// Calculates average range (High - Low) over specified bars
        /// Used for normalizing fractal strength
        /// </summary>
        private double CalculateAverageRange(int bars)
        {
            int count = Math.Min(bars, m15Bars.Count);

            // Guard against division by zero
            if (count <= 0)
                return 0;

            double totalRange = 0;

            for (int i = 0; i < count; i++)
            {
                int idx = m15Bars.Count - 1 - i;
                totalRange += m15Bars.HighPrices[idx] - m15Bars.LowPrices[idx];
            }

            return totalRange / count;
        }

        #endregion

        #region Swing Zone Management

        /// <summary>
        /// Updates the swing rectangle zone prices and draws rectangle
        /// SELL Mode: Rectangle from Close to High
        /// BUY Mode: Rectangle from Close to Low
        /// </summary>
        private void UpdateSwingZone(int swingIndex, string mode)
        {
            if (mode == "SELL")
            {
                // SELL Mode: Rectangle from Close to High
                swingBottomPrice = m15Bars.ClosePrices[swingIndex];
                swingTopPrice = m15Bars.HighPrices[swingIndex];
            }
            else // BUY Mode
            {
                // BUY Mode: Rectangle from Close to Low
                swingTopPrice = m15Bars.ClosePrices[swingIndex];
                swingBottomPrice = m15Bars.LowPrices[swingIndex];
            }

            double heightPips = (swingTopPrice - swingBottomPrice) / Symbol.PipSize;

            Print("[SwingZone] {0} Mode | Top: {1:F5} | Bottom: {2:F5} | Height: {3:F1} pips",
                mode, swingTopPrice, swingBottomPrice, heightPips);

            // Draw rectangle on chart
            if (ShowRectangles)
            {
                DrawSwingRectangle(swingIndex, mode);
            }

            // Update mode label
            if (ShowModeLabel)
            {
                UpdateModeDisplay(mode);
            }
        }

        #endregion

        #region Trade Execution

        /// <summary>
        /// Executes a SELL trade at swing HIGH zone
        /// </summary>
        private void ExecuteSellTrade()
        {
            double volume = Symbol.QuantityToVolumeInUnits(LotSize);
            double entryPrice = Symbol.Ask;
            double stopLoss = entryPrice + (StopLossPips * Symbol.PipSize);
            double takeProfit = entryPrice - (TakeProfitPips * Symbol.PipSize);

            var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, MagicNumber.ToString(),
                stopLoss, takeProfit);

            if (result.IsSuccessful)
            {
                Print("✅ SELL EXECUTED | Entry: {0:F5} | SL: {1:F5} | TP: {2:F5} | Volume: {3}",
                    entryPrice, stopLoss, takeProfit, volume);
                Print("   Swing Zone: {0:F5} - {1:F5}", swingBottomPrice, swingTopPrice);

                // Disable active swing if trading on new swing only
                if (TradeOnNewSwingOnly)
                    hasActiveSwing = false;
            }
            else
            {
                Print("❌ SELL FAILED | Error: {0}", result.Error);
            }
        }

        /// <summary>
        /// Executes a BUY trade at swing LOW zone
        /// </summary>
        private void ExecuteBuyTrade()
        {
            double volume = Symbol.QuantityToVolumeInUnits(LotSize);
            double entryPrice = Symbol.Ask;
            double stopLoss = entryPrice - (StopLossPips * Symbol.PipSize);
            double takeProfit = entryPrice + (TakeProfitPips * Symbol.PipSize);

            var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, MagicNumber.ToString(),
                stopLoss, takeProfit);

            if (result.IsSuccessful)
            {
                Print("✅ BUY EXECUTED | Entry: {0:F5} | SL: {1:F5} | TP: {2:F5} | Volume: {3}",
                    entryPrice, stopLoss, takeProfit, volume);
                Print("   Swing Zone: {0:F5} - {1:F5}", swingBottomPrice, swingTopPrice);

                // Disable active swing if trading on new swing only
                if (TradeOnNewSwingOnly)
                    hasActiveSwing = false;
            }
            else
            {
                Print("❌ BUY FAILED | Error: {0}", result.Error);
            }
        }

        #endregion

        #region Visualization Methods

        /// <summary>
        /// Draws rectangle on chart at swing point
        /// Rectangle spans from swing bar time to current time + RectangleWidthMinutes
        /// This ensures the rectangle extends forward from NOW, not from swing detection
        /// </summary>
        private void DrawSwingRectangle(int swingIndex, string mode)
        {
            rectangleCounter++;
            string rectName = string.Format("SwingRect_{0}_{1}", mode, rectangleCounter);

            // Start time: The swing bar's open time (where the fractal formed)
            DateTime startTime = m15Bars.OpenTimes[swingIndex];

            // End time: Current time + RectangleWidthMinutes (forward-looking from NOW)
            // This accounts for detection delay (swing detected after M15 bar closes)
            DateTime endTime = Server.Time.AddMinutes(RectangleWidthMinutes);

            // Parse color and add transparency
            Color baseColor = mode == "BUY" ? ParseColor(BuyColorName) : ParseColor(SellColorName);
            Color rectColor = Color.FromArgb(RectangleTransparency, baseColor);

            // Draw rectangle using native API
            var rectangle = Chart.DrawRectangle(rectName, startTime, swingTopPrice, endTime, swingBottomPrice, rectColor);
            rectangle.IsFilled = true;
            rectangle.IsInteractive = true;

            // Track for cleanup
            drawnRectangles.Add(new RectangleInfo
            {
                Name = rectName,
                CreatedAt = Server.Time
            });

            double heightPips = (swingTopPrice - swingBottomPrice) / Symbol.PipSize;

            Print("[RectangleDraw] ✅ {0} Mode Rectangle #{1}", mode, rectangleCounter);
            Print("   Start: {0} | End: {1} | Height: {2:F1} pips", startTime, endTime, heightPips);

            // Cleanup old rectangles
            CleanupOldRectangles();
        }

        /// <summary>
        /// Updates mode display label on chart
        /// </summary>
        private void UpdateModeDisplay(string mode)
        {
            // Remove old label if exists
            if (modeLabel != null)
            {
                Chart.RemoveObject(modeLabel.Name);
            }

            if (string.IsNullOrEmpty(mode))
                return;

            // Create mode text display
            string modeText = string.Format("{0} MODE", mode);
            Color labelColor = mode == "BUY" ? ParseColor(BuyColorName) : ParseColor(SellColorName);

            // Draw static text on top-right corner
            modeLabel = Chart.DrawStaticText("ModeLabel", modeText,
                VerticalAlignment.Top, HorizontalAlignment.Right, labelColor);

            Print("[ModeDisplay] Updated to: {0}", modeText);
        }

        /// <summary>
        /// Removes oldest rectangles to keep chart clean
        /// </summary>
        private void CleanupOldRectangles()
        {
            if (drawnRectangles.Count <= MaxRectangles)
                return;

            // Sort by creation time (oldest first)
            var sortedRects = drawnRectangles.OrderBy(r => r.CreatedAt).ToList();

            // Remove oldest rectangles
            int toRemove = drawnRectangles.Count - MaxRectangles;

            for (int i = 0; i < toRemove; i++)
            {
                var rect = sortedRects[i];
                Chart.RemoveObject(rect.Name);
                drawnRectangles.Remove(rect);
            }

            Print("[Cleanup] Removed {0} old rectangles | Remaining: {1}",
                toRemove, drawnRectangles.Count);
        }

        /// <summary>
        /// Parses color name string to Color object
        /// </summary>
        private Color ParseColor(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "green": return Color.Green;
                case "red": return Color.Red;
                case "blue": return Color.Blue;
                case "yellow": return Color.Yellow;
                case "orange": return Color.Orange;
                case "purple": return Color.Purple;
                case "cyan": return Color.Cyan;
                case "white": return Color.White;
                case "black": return Color.Black;
                case "lime": return Color.Lime;
                case "dodgerblue": return Color.DodgerBlue;
                default: return Color.Gray;
            }
        }

        #endregion

        #region OnStop

        protected override void OnStop()
        {
            Print("========================================");
            Print("=== JCAMP 1M SCALPING BOT STOPPED ===");
            Print("========================================");
        }

        #endregion
    }
}

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

        [Parameter("Swing Lookback Bars (M15)", DefaultValue = 30, MinValue = 10, MaxValue = 200, Group = "Trend Detection")]
        public int SwingLookbackBars { get; set; }

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

        [Parameter("Enable Trading", DefaultValue = true, Group = "Entry Filters")]
        public bool EnableTrading { get; set; }

        [Parameter("Trade on New Swing Only", DefaultValue = true, Group = "Entry Filters")]
        public bool TradeOnNewSwingOnly { get; set; }

        #endregion

        #region Parameters - Visualization

        [Parameter("=== VISUALIZATION ===", DefaultValue = "")]
        public string VisualHeader { get; set; }

        [Parameter("Show Rectangles", DefaultValue = true, Group = "Visualization")]
        public bool ShowRectangles { get; set; }

        [Parameter("Rectangle Width (Minutes)", DefaultValue = 50, MinValue = 10, MaxValue = 200, Group = "Visualization")]
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

            if (isM15Chart)
            {
                // Running on M15: use current chart's bars
                m15Bars = Bars;
                ema200_m15 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, EMAPeriod);
                Print("Chart: M15 (Direct) | Analysis: M15");
            }
            else
            {
                // Running on M1: get M15 bars via multi-timeframe access
                m15Bars = MarketData.GetBars(TimeFrame.Minute15);
                ema200_m15 = Indicators.ExponentialMovingAverage(m15Bars.ClosePrices, EMAPeriod);
                Print("Chart: M1 | Analysis: M15");
            }

            // Initialize state
            lastM15BarTime = m15Bars.OpenTimes.LastValue;

            Print("EMA Period: {0} | Swing Lookback: {1} bars", EMAPeriod, SwingLookbackBars);
            Print("Lot Size: {0} | SL: {1} pips | TP: {2} pips", LotSize, StopLossPips, TakeProfitPips);
            Print("Max Positions: {0} | Magic: {1}", MaxPositions, MagicNumber);
            Print("Trading Enabled: {0}", EnableTrading);
            Print("Visualization: Rectangles={0} | Mode Label={1}", ShowRectangles, ShowModeLabel);
            Print("========================================");
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

            // Only process when a NEW M15 bar appears
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

            // 3. Find recent swing point based on mode
            int swingIndex = FindRecentSwingPoint(currentMode);

            if (swingIndex == -1)
            {
                Print("[{0}] No valid swing point found in last {1} M15 bars",
                    currentMode, SwingLookbackBars);
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
        /// Detects trend mode using M15 price vs EMA 200
        /// </summary>
        private string DetectTrendMode()
        {
            int lastIdx = m15Bars.Count - 1;

            double currentPrice = m15Bars.ClosePrices[lastIdx];
            double emaValue = ema200_m15.Result[lastIdx];

            string mode = currentPrice > emaValue ? "BUY" : "SELL";

            Print("[TrendDetection] M15 Price: {0:F5} | EMA200: {1:F5} | Mode: {2}",
                currentPrice, emaValue, mode);

            return mode;
        }

        #endregion

        #region Swing Point Detection

        /// <summary>
        /// Finds recent swing point using Williams Fractals with candle validation
        /// SELL Mode: Swing HIGH from BULLISH candle (Close > Open)
        /// BUY Mode: Swing LOW from BEARISH candle (Close < Open)
        /// </summary>
        private int FindRecentSwingPoint(string mode)
        {
            int barsToScan = Math.Min(SwingLookbackBars, m15Bars.Count - 5);

            // Scan from most recent backwards (need 2 bars before and after for fractal)
            for (int i = 2; i < barsToScan - 2; i++)
            {
                int idx = m15Bars.Count - 1 - i;

                if (mode == "SELL")
                {
                    // Williams Fractal Up: High[i] > High[i±1] and High[i] > High[i±2]
                    bool isSwingHigh = m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 1] &&
                                       m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 2] &&
                                       m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 1] &&
                                       m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 2];

                    // Must be BULLISH candle
                    bool isBullishCandle = m15Bars.ClosePrices[idx] > m15Bars.OpenPrices[idx];

                    if (isSwingHigh && isBullishCandle)
                    {
                        Print("[SwingDetection] SELL Mode - Swing HIGH at bar {0} | High: {1:F5} | Time: {2}",
                            idx, m15Bars.HighPrices[idx], m15Bars.OpenTimes[idx]);
                        return idx;
                    }
                }
                else if (mode == "BUY")
                {
                    // Williams Fractal Down: Low[i] < Low[i±1] and Low[i] < Low[i±2]
                    bool isSwingLow = m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 1] &&
                                      m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 2] &&
                                      m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 1] &&
                                      m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 2];

                    // Must be BEARISH candle
                    bool isBearishCandle = m15Bars.LowPrices[idx] < m15Bars.OpenPrices[idx];

                    if (isSwingLow && isBearishCandle)
                    {
                        Print("[SwingDetection] BUY Mode - Swing LOW at bar {0} | Low: {1:F5} | Time: {2}",
                            idx, m15Bars.LowPrices[idx], m15Bars.OpenTimes[idx]);
                        return idx;
                    }
                }
            }

            return -1; // No swing found
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
        /// </summary>
        private void DrawSwingRectangle(int swingIndex, string mode)
        {
            rectangleCounter++;
            string rectName = string.Format("SwingRect_{0}_{1}", mode, rectangleCounter);

            // Extract swing candle data
            DateTime startTime = m15Bars.OpenTimes[swingIndex];
            DateTime endTime = startTime.AddMinutes(RectangleWidthMinutes);

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

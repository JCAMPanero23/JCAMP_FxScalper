using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo
{
    /// <summary>
    /// M15 Swing Rectangle Indicator
    /// Detects trend using EMA 200 on M15 timeframe and draws rectangles at swing points
    /// Works on both M1 and M15 charts for verification
    /// SELL Mode: Swing HIGH (bullish candle) - rectangle from Close to High
    /// BUY Mode: Swing LOW (bearish candle) - rectangle from Close to Low
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TrendModeRectangleIndicator : Indicator
    {
        #region Parameters

        [Parameter("EMA Period", DefaultValue = 200, MinValue = 50, MaxValue = 500)]
        public int EMAPeriod { get; set; }

        [Parameter("Swing Lookback Bars (M15)", DefaultValue = 30, MinValue = 10, MaxValue = 200)]
        public int SwingLookbackBars { get; set; }

        [Parameter("Rectangle Width (Minutes)", DefaultValue = 50, MinValue = 10, MaxValue = 200)]
        public int RectangleWidthMinutes { get; set; }

        [Parameter("BUY Mode Color", DefaultValue = "Green")]
        public string BuyModeColorName { get; set; }

        [Parameter("SELL Mode Color", DefaultValue = "Red")]
        public string SellModeColorName { get; set; }

        [Parameter("Rectangle Transparency (0-255)", DefaultValue = 80, MinValue = 0, MaxValue = 255)]
        public int RectangleTransparency { get; set; }

        [Parameter("Show Mode Label", DefaultValue = true)]
        public bool ShowModeLabel { get; set; }

        [Parameter("Max Rectangles to Show", DefaultValue = 10, MinValue = 1, MaxValue = 50)]
        public int MaxRectanglesToShow { get; set; }

        #endregion

        #region Private Fields

        private Bars m15Bars;
        private ExponentialMovingAverage ema200_m15;
        private bool isM15Chart;

        // State tracking
        private string currentMode = "";
        private DateTime lastM15BarTime;
        private int rectangleCounter = 0;

        // Drawn objects tracking for cleanup
        private class RectangleInfo
        {
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private List<RectangleInfo> drawnRectangles = new List<RectangleInfo>();
        private ChartStaticText modeLabel;

        #endregion

        #region Initialization

        protected override void Initialize()
        {
            // Validate chart timeframe - accept M1 or M15
            if (Chart.TimeFrame != TimeFrame.Minute && Chart.TimeFrame != TimeFrame.Minute15)
            {
                Print("ERROR: This indicator must run on M1 or M15 timeframe!");
                Print("Current timeframe: {0}", Chart.TimeFrame);
                return;
            }

            // Check if we're running on M15 chart
            isM15Chart = (Chart.TimeFrame == TimeFrame.Minute15);

            if (isM15Chart)
            {
                // Running on M15: use current chart's bars
                m15Bars = Bars;
                ema200_m15 = Indicators.ExponentialMovingAverage(Bars.ClosePrices, EMAPeriod);
                Print("=== Trend Mode Rectangle Indicator Initialized ===");
                Print("Chart: M15 (Direct) | EMA: {0} | Swing Lookback: {1} bars",
                    EMAPeriod, SwingLookbackBars);
            }
            else
            {
                // Running on M1: get M15 bars via multi-timeframe access
                m15Bars = MarketData.GetBars(TimeFrame.Minute15);
                ema200_m15 = Indicators.ExponentialMovingAverage(m15Bars.ClosePrices, EMAPeriod);
                Print("=== Trend Mode Rectangle Indicator Initialized ===");
                Print("Chart: M1 | Analysis: M15 | EMA: {0} | Swing Lookback: {1} bars",
                    EMAPeriod, SwingLookbackBars);
            }

            // Initialize state
            lastM15BarTime = m15Bars.OpenTimes.LastValue;

            Print("Rectangle Width: {0} minutes | Max Rectangles: {1}",
                RectangleWidthMinutes, MaxRectanglesToShow);
            Print("BUY Color: {0} | SELL Color: {1} | Transparency: {2}",
                BuyModeColorName, SellModeColorName, RectangleTransparency);
        }

        #endregion

        #region Calculate - Main Loop

        public override void Calculate(int index)
        {
            // Check if enough M15 bars for EMA calculation
            if (m15Bars.Count < EMAPeriod + 5)
            {
                if (index % 100 == 0) // Print occasionally to avoid spam
                {
                    Print("Waiting for {0} M15 bars for EMA calculation (current: {1})",
                        EMAPeriod + 5, m15Bars.Count);
                }
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

            // 2. Update mode display if changed
            if (newMode != currentMode && !string.IsNullOrEmpty(newMode))
            {
                currentMode = newMode;
                UpdateModeDisplay(currentMode);
            }

            // 3. Find recent swing point based on mode
            int swingIndex = FindRecentSwingPoint(currentMode);

            if (swingIndex == -1)
            {
                Print("[{0}] No valid swing point found in last {1} M15 bars",
                    currentMode, SwingLookbackBars);
                return;
            }

            // 4. Draw rectangle on chart
            DrawRectangle(swingIndex, currentMode);

            // 5. Cleanup old rectangles
            CleanupOldRectangles();
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
                    bool isBearishCandle = m15Bars.ClosePrices[idx] < m15Bars.OpenPrices[idx];

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

        #region Rectangle Drawing

        /// <summary>
        /// Draws rectangle on M1 chart using native Chart.DrawRectangle() API
        /// SELL Mode: Rectangle from Close to High
        /// BUY Mode: Rectangle from Close to Low
        /// </summary>
        private void DrawRectangle(int swingIndex, string mode)
        {
            rectangleCounter++;
            string rectName = string.Format("Rect_{0}_{1}", mode, rectangleCounter);

            // Extract swing candle data
            DateTime startTime = m15Bars.OpenTimes[swingIndex];
            DateTime endTime = startTime.AddMinutes(RectangleWidthMinutes);

            double topPrice, bottomPrice;

            if (mode == "SELL")
            {
                // SELL Mode: Rectangle from Close to High
                bottomPrice = m15Bars.ClosePrices[swingIndex];
                topPrice = m15Bars.HighPrices[swingIndex];
            }
            else // BUY Mode
            {
                // BUY Mode: Rectangle from Close to Low
                topPrice = m15Bars.ClosePrices[swingIndex];
                bottomPrice = m15Bars.LowPrices[swingIndex];
            }

            // Parse color and add transparency
            Color baseColor = mode == "BUY" ? ParseColor(BuyModeColorName) : ParseColor(SellModeColorName);
            Color rectColor = Color.FromArgb(RectangleTransparency, baseColor);

            // Draw rectangle using native API
            var rectangle = Chart.DrawRectangle(rectName, startTime, topPrice, endTime, bottomPrice, rectColor);
            rectangle.IsFilled = true; // Fill rectangle with transparent color
            rectangle.IsInteractive = true; // Make it interactive

            // Track for cleanup
            drawnRectangles.Add(new RectangleInfo
            {
                Name = rectName,
                CreatedAt = Server.Time
            });

            double heightPips = (topPrice - bottomPrice) / Symbol.PipSize;

            Print("[RectangleDraw] ✅ {0} Mode Rectangle #{1}", mode, rectangleCounter);
            Print("   Start Time: {0} | End Time: {1}", startTime, endTime);
            Print("   Top: {2:F5} | Bottom: {3:F5} | Height: {4:F1} pips",
                topPrice, bottomPrice, heightPips);
            Print("   Color: {0} | Transparency: {1}", mode == "BUY" ? BuyModeColorName : SellModeColorName, RectangleTransparency);
        }

        #endregion

        #region Mode Display

        /// <summary>
        /// Updates mode display on top-right corner using native Chart.DrawStaticText()
        /// </summary>
        private void UpdateModeDisplay(string mode)
        {
            if (!ShowModeLabel)
                return;

            // Remove old label if exists
            if (modeLabel != null)
            {
                Chart.RemoveObject(modeLabel.Name);
            }

            if (string.IsNullOrEmpty(mode))
                return;

            // Create mode text display
            string modeText = string.Format("{0} MODE", mode);
            Color labelColor = mode == "BUY" ? ParseColor(BuyModeColorName) : ParseColor(SellModeColorName);

            // Draw static text on top-right corner
            modeLabel = Chart.DrawStaticText("ModeLabel", modeText,
                VerticalAlignment.Top, HorizontalAlignment.Right, labelColor);

            Print("[ModeDisplay] Updated to: {0}", modeText);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Removes oldest rectangles to limit visual clutter
        /// Keeps only MaxRectanglesToShow rectangles on chart
        /// </summary>
        private void CleanupOldRectangles()
        {
            if (drawnRectangles.Count <= MaxRectanglesToShow)
                return;

            // Sort by creation time (oldest first)
            var sortedRects = drawnRectangles.OrderBy(r => r.CreatedAt).ToList();

            // Remove oldest rectangles
            int toRemove = drawnRectangles.Count - MaxRectanglesToShow;

            for (int i = 0; i < toRemove; i++)
            {
                var rect = sortedRects[i];
                Chart.RemoveObject(rect.Name);
                drawnRectangles.Remove(rect);
            }

            Print("[Cleanup] Removed {0} old rectangles | Remaining: {1}",
                toRemove, drawnRectangles.Count);
        }

        #endregion

        #region Helper Methods

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
                default: return Color.Gray;
            }
        }

        #endregion
    }
}

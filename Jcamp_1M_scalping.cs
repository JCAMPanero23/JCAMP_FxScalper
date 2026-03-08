using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    /// <summary>
    /// Jcamp 1M Scalping Strategy
    /// Based on M15 SMA 200 trend detection and swing rectangle entry zones
    /// SELL Mode: Enter SELL when price enters swing HIGH rectangle (Close to High)
    /// BUY Mode: Enter BUY when price enters swing LOW rectangle (Close to Low)
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Jcamp_1M_scalping : Robot
    {
        #region Parameters - Trend Detection

        [Parameter("=== TREND DETECTION ===", DefaultValue = "")]
        public string TrendHeader { get; set; }

        [Parameter("SMA Period", DefaultValue = 200, MinValue = 50, MaxValue = 500, Group = "Trend Detection")]
        public int SMAPeriod { get; set; }

        [Parameter("Swing Lookback Bars (M15)", DefaultValue = 100, MinValue = 10, MaxValue = 200, Group = "Trend Detection")]
        public int SwingLookbackBars { get; set; }

        [Parameter("Minimum Swing Score", DefaultValue = 0.60, MinValue = 0.0, MaxValue = 1.0, Group = "Trend Detection")]
        public double MinimumSwingScore { get; set; }

        #endregion

        #region Parameters - Session Management

        [Parameter("=== SESSION MANAGEMENT ===", DefaultValue = "")]
        public string SessionHeader { get; set; }

        [Parameter("Enable Session Filter", DefaultValue = true, Group = "Session Management")]
        public bool EnableSessionFilter { get; set; }

        [Parameter("Show Session Boxes", DefaultValue = false, Group = "Session Management")]
        public bool ShowSessionBoxes { get; set; }

        [Parameter("Session Weight", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Session Management")]
        public double SessionWeight { get; set; }

        #endregion

        #region Parameters - Trade Management

        [Parameter("=== TRADE MANAGEMENT ===", DefaultValue = "")]
        public string TradeHeader { get; set; }

        [Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0, Step = 0.1, Group = "Trade Management")]
        public double RiskPercent { get; set; }

        [Parameter("SL Buffer Pips", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 10.0, Step = 0.5, Group = "Trade Management")]
        public double SLBufferPips { get; set; }

        [Parameter("Minimum RR Ratio", DefaultValue = 3.0, MinValue = 2.0, MaxValue = 10.0, Step = 0.5, Group = "Trade Management")]
        public double MinimumRRRatio { get; set; }

        [Parameter("Max Positions", DefaultValue = 1, MinValue = 1, MaxValue = 10, Group = "Trade Management")]
        public int MaxPositions { get; set; }

        [Parameter("Magic Number", DefaultValue = 100001, Group = "Trade Management")]
        public int MagicNumber { get; set; }

        #endregion

        #region Parameters - TP Management

        [Parameter("=== TP MANAGEMENT ===", DefaultValue = "")]
        public string TPHeader { get; set; }

        [Parameter("Use H1 Levels for TP", DefaultValue = true, Group = "TP Management")]
        public bool UseH1LevelsForTP { get; set; }

        [Parameter("Use M15 Levels for TP", DefaultValue = true, Group = "TP Management")]
        public bool UseM15LevelsForTP { get; set; }

        [Parameter("H1 Level Proximity Pips", DefaultValue = 50, MinValue = 10, MaxValue = 200, Group = "TP Management")]
        public int H1LevelProximityPips { get; set; }

        #endregion

        #region Parameters - Entry Filters

        [Parameter("=== ENTRY FILTERS ===", DefaultValue = "")]
        public string EntryHeader { get; set; }

        [Parameter("Enable Trading", DefaultValue = false, Group = "Entry Filters")]
        public bool EnableTrading { get; set; }

        [Parameter("Entry Mode", DefaultValue = EntryMode.Breakout, Group = "Entry Filters")]
        public EntryMode EntryModeSelection { get; set; }

        [Parameter("Trade on New Swing Only", DefaultValue = true, Group = "Entry Filters")]
        public bool TradeOnNewSwingOnly { get; set; }

        [Parameter("Max Distance to Arm (pips)", DefaultValue = 10.0, MinValue = 1.0, MaxValue = 50.0, Step = 1.0, Group = "Entry Filters")]
        public double MaxDistanceToArm { get; set; }

        #endregion

        #region Entry Mode Enum

        public enum EntryMode
        {
            Breakout,       // DEFAULT: Enter when body closes beyond rectangle
            RetestConfirm   // ALTERNATIVE: Wait for retest after breakout
        }

        #endregion

        #region Session Enums and Classes

        /// <summary>
        /// Trading sessions based on UTC time
        /// Phase 2 Implementation
        /// </summary>
        public enum TradingSession
        {
            None,
            Asian,      // 00:00-09:00 UTC (Tokyo)
            London,     // 08:00-17:00 UTC
            NewYork,    // 13:00-22:00 UTC
            Overlap     // 13:00-17:00 UTC (London + NY)
        }

        /// <summary>
        /// Tracks high/low levels for a trading session
        /// Phase 2 Implementation
        /// </summary>
        private class SessionLevels
        {
            public TradingSession Session { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
        }

        /// <summary>
        /// Represents which sessions are currently active
        /// Allows independent tracking (sessions can overlap)
        /// Phase 2 Implementation
        /// </summary>
        private class SessionState
        {
            public bool IsAsian { get; set; }
            public bool IsLondon { get; set; }
            public bool IsNewYork { get; set; }
            public bool IsOverlap => IsLondon && IsNewYork;

            public override string ToString()
            {
                var active = new System.Collections.Generic.List<string>();
                if (IsAsian) active.Add("Asian");
                if (IsLondon) active.Add("London");
                if (IsNewYork) active.Add("NY");
                if (IsOverlap) active.Add("(Overlap)");
                return active.Count > 0 ? string.Join("+", active) : "Off-Session";
            }
        }

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

        #region Parameters - Score Weights

        [Parameter("=== SCORE WEIGHTS ===", DefaultValue = "")]
        public string WeightsHeader { get; set; }

        [Parameter("Weight: Validity", DefaultValue = 0.25, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightValidity { get; set; }

        [Parameter("Weight: Extremity", DefaultValue = 0.30, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightExtremity { get; set; }

        [Parameter("Weight: Fractal", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightFractal { get; set; }

        [Parameter("Weight: Session", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightSession { get; set; }

        [Parameter("Weight: Candle", DefaultValue = 0.05, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightCandle { get; set; }

        #endregion

        #region Private Fields

        private Bars m15Bars;
        private Bars h1Bars;
        private bool isM15Chart;

        // State tracking
        private string currentMode = "";
        private DateTime lastM15BarTime;
        private DateTime lastSwingTime = DateTime.MinValue;

        // Current swing rectangle zone
        private double swingTopPrice = 0;
        private double swingBottomPrice = 0;
        private bool hasActiveSwing = false;
        private bool hasValidRectangle = false;  // Rectangle exists (may or may not be armed)
        private DateTime rectangleCreatedTime = DateTime.MinValue;  // Track when rectangle was created
        private DateTime rectangleExpiryTime = DateTime.MinValue;   // Track when rectangle expires

        // Phase 1B: Entry tracking
        private bool hasBreakoutOccurred = false;
        private double breakoutPrice = 0;

        // Phase 1C: Market structure levels
        private System.Collections.Generic.List<double> h1Supports = new System.Collections.Generic.List<double>();
        private System.Collections.Generic.List<double> h1Resistances = new System.Collections.Generic.List<double>();
        private System.Collections.Generic.List<double> m15Supports = new System.Collections.Generic.List<double>();
        private System.Collections.Generic.List<double> m15Resistances = new System.Collections.Generic.List<double>();

        // Phase 2: Session tracking
        private System.Collections.Generic.List<SessionLevels> recentSessions = new System.Collections.Generic.List<SessionLevels>();
        private SessionLevels currentSession = null;
        private TradingSession lastDetectedSession = TradingSession.None;

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

            Print("SMA Period: {0} | Swing Lookback: {1} bars | Min Swing Score: {2:F2}",
                SMAPeriod, SwingLookbackBars, MinimumSwingScore);
            Print("Risk Management: {0:F1}% per trade | SL Buffer: {1:F1} pips | Min RR: 1:{2:F1}",
                RiskPercent, SLBufferPips, MinimumRRRatio);
            Print("Entry Mode: {0} | Max Distance to Arm: {1:F1} pips | Max Positions: {2} | Magic: {3}",
                EntryModeSelection, MaxDistanceToArm, MaxPositions, MagicNumber);
            Print("Trading Enabled: {0} | Trade on New Swing Only: {1}",
                EnableTrading, TradeOnNewSwingOnly);
            Print("Rectangle Width: {0} min (trading window)", RectangleWidthMinutes);
            Print("Visualization: Rectangles={0} | Mode Label={1}", ShowRectangles, ShowModeLabel);

            // Phase 1C: Initialize H1 bars for TP management
            h1Bars = MarketData.GetBars(TimeFrame.Hour);
            Print("Phase 1C TP Management: H1 Levels={0} | M15 Levels={1} | Proximity={2} pips",
                UseH1LevelsForTP, UseM15LevelsForTP, H1LevelProximityPips);

            // Update market structure levels
            UpdateH1Levels();
            UpdateM15Levels();

            Print("========================================");

            // Detect initial mode and show label immediately
            if (m15Bars.Count >= SMAPeriod + 5)
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
            // Check if enough M15 bars for SMA calculation
            if (m15Bars.Count < SMAPeriod + 5)
            {
                Print("Waiting for {0} M15 bars for SMA calculation (current: {1})",
                    SMAPeriod + 5, m15Bars.Count);
                return;
            }

            // Always update mode display on M1 bars (keeps label visible on M1 chart)
            if (!isM15Chart && ShowModeLabel && !string.IsNullOrEmpty(currentMode))
            {
                UpdateModeDisplay(currentMode);
            }

            // ============================================================
            // SWING DETECTION: Only process when a NEW M15 bar appears
            // ============================================================
            bool isNewM15Bar = (m15Bars.OpenTimes.LastValue != lastM15BarTime);

            if (isNewM15Bar)
            {
                lastM15BarTime = m15Bars.OpenTimes.LastValue;

                Print("=== NEW M15 BAR: {0} ===", lastM15BarTime);

                // Phase 1C: Update market structure levels on new M15 bar
                UpdateH1Levels();
                UpdateM15Levels();

                // Phase 2: Update session tracking
                UpdateSessionTracking();

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
                    Print("[{0}] No NEW significant swing found (score >= {1:F2}) in last {2} M15 bars",
                        currentMode, MinimumSwingScore, SwingLookbackBars);
                    // DON'T deactivate existing rectangle! It remains active until expired/invalidated/traded
                }
                else
                {
                    // 4. Update swing rectangle zone (only if we found a new swing)
                    DateTime swingTime = m15Bars.OpenTimes[swingIndex];

                    // Check if this is a new swing
                    bool isNewSwing = (swingTime != lastSwingTime);

                    if (isNewSwing)
                    {
                        lastSwingTime = swingTime;
                        hasBreakoutOccurred = false;  // Reset breakout tracking for new swing

                        // UpdateSwingZone will set hasActiveSwing based on proximity check
                        UpdateSwingZone(swingIndex, currentMode);
                    }
                }
            }

            // ============================================================
            // ENTRY DETECTION: Process on EVERY M1 bar (not just M15 bars!)
            // ============================================================

            // Check if we need to re-arm an unarmed rectangle (Issue 1 Fix: price returned)
            if (EnableTrading && hasValidRectangle && !hasActiveSwing)
            {
                TryRearmRectangle();
            }

            // Phase 1B: Entry detection on M1 bar close
            // Process breakout entry logic if trading is enabled
            if (EnableTrading && hasActiveSwing)
            {
                ProcessEntryLogic();
            }
        }

        /// <summary>
        /// Attempts to re-arm a rectangle when price returns to it
        /// Issue 1 Fix: If rectangle was created when price already moved away,
        /// we wait for price to return before arming it for trading
        /// </summary>
        private void TryRearmRectangle()
        {
            // Check if rectangle has expired
            DateTime currentTime = Bars.OpenTimes.LastValue;
            if (currentTime > rectangleExpiryTime)
            {
                Print("[RearmCheck] Rectangle expired - cannot rearm");
                hasValidRectangle = false;
                return;
            }

            // Check if price has returned close enough to rectangle
            double currentPrice = Symbol.Bid;
            double distanceToRectangle;

            if (currentMode == "SELL")
            {
                // For SELL: Check distance from current price to rectangle bottom
                distanceToRectangle = (currentPrice - swingBottomPrice) / Symbol.PipSize;
            }
            else
            {
                // For BUY: Check distance from current price to rectangle top
                distanceToRectangle = (swingTopPrice - currentPrice) / Symbol.PipSize;
            }

            // Rearm if price is now within acceptable range (above rectangle for SELL, below for BUY)
            if (distanceToRectangle >= 0 && distanceToRectangle <= MaxDistanceToArm * 2)
            {
                Print("[RearmRectangle] ✅ Price returned! Distance: {0:F1} pips - REARMED for trading", distanceToRectangle);
                hasActiveSwing = true;
            }
        }

        #endregion

        #region OnTick - Entry Logic (Phase 1B: Breakout Detection)

        protected override void OnTick()
        {
            // Phase 1B: Entry logic moved to OnBar for M1 candle close detection
            // OnTick is kept for future real-time monitoring if needed
        }

        #endregion

        #region Trend Detection

        /// <summary>
        /// Detects trend mode using M15 price vs SMA 200
        /// Uses custom SMA calculation to ensure consistency between M1 and M15 charts
        /// SMA only uses last N bars, ensuring same results regardless of chart timeframe
        /// </summary>
        private string DetectTrendMode()
        {
            int lastIdx = m15Bars.Count - 1;

            double currentPrice = m15Bars.ClosePrices[lastIdx];

            // Use custom SMA calculation (only last N bars) for consistency
            double smaValue = CalculateSMA(SMAPeriod);

            string mode = currentPrice > smaValue ? "BUY" : "SELL";

            // Debug: Show bar count and time to diagnose M1 vs M15 discrepancies
            Print("[TrendDetection] Chart: {0} | BarCount: {1} | LastBarTime: {2}",
                isM15Chart ? "M15" : "M1", m15Bars.Count, m15Bars.OpenTimes[lastIdx]);
            Print("[TrendDetection] M15 Price: {0:F5} | SMA{1}: {2:F5} | Mode: {3}",
                currentPrice, SMAPeriod, smaValue, mode);

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
            double sessionAlignment = CalculateSessionAlignment(swingIndex, mode); // Phase 2
            double candleStrength = CalculateCandleStrength(swingIndex);

            // Phase 2: Use configurable weights (must total 1.0)
            double totalScore =
                (validityScore * WeightValidity) +
                (extremityScore * WeightExtremity) +
                (fractalStrength * WeightFractal) +
                (sessionAlignment * WeightSession) +
                (candleStrength * WeightCandle);

            return totalScore;
        }

        /// <summary>
        /// Validity Score: Measures how RECENT the swing is
        /// Since rectangles extend from swing time to current+60, any swing is "valid"
        /// But more recent swings should score higher (more relevant)
        /// </summary>
        private double CalculateValidityScore(int swingIndex)
        {
            int lastIdx = m15Bars.Count - 1;
            int barsAgo = lastIdx - swingIndex;

            // How far back is this swing compared to our lookback period?
            // barsAgo = 2 (just formed) → score = 1.0
            // barsAgo = SwingLookbackBars → score = 0.0
            // Linear interpolation between

            if (barsAgo < 2)
            {
                // Too recent - fractal not confirmed yet
                return 0;
            }

            if (barsAgo >= SwingLookbackBars)
            {
                // Beyond lookback period
                return 0;
            }

            // Score: 1.0 for recent swings, decreasing linearly to 0.2 at edge of lookback
            // We use 0.2 minimum instead of 0 so older swings can still qualify if other scores are high
            double recencyRatio = 1.0 - ((double)(barsAgo - 2) / (SwingLookbackBars - 2));
            double score = 0.2 + (recencyRatio * 0.8);  // Range: 0.2 to 1.0

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

        /// <summary>
        /// Session Alignment Score: Checks if swing aligns with session high/low
        /// Swings at session extremes are more significant
        /// Phase 2 Implementation
        /// </summary>
        private double CalculateSessionAlignment(int swingIndex, string mode)
        {
            if (!EnableSessionFilter)
                return 0.5; // Neutral score if session filter disabled

            DateTime swingTime = m15Bars.OpenTimes[swingIndex];
            var session = GetSessionForTime(swingTime);

            if (session == null)
                return 0.5; // No session data, neutral score

            double swingPrice = mode == "SELL" ?
                m15Bars.HighPrices[swingIndex] :
                m15Bars.LowPrices[swingIndex];

            double distanceToSessionLevel = mode == "SELL" ?
                Math.Abs(swingPrice - session.High) :
                Math.Abs(swingPrice - session.Low);

            double threshold = 10 * Symbol.PipSize; // Within 10 pips

            if (distanceToSessionLevel <= threshold)
            {
                Print("[SessionAlign] Swing at {0} session {1} | Distance: {2:F1} pips | STRONG",
                    session.Session,
                    mode == "SELL" ? "HIGH" : "LOW",
                    distanceToSessionLevel / Symbol.PipSize);
                return 1.0; // At session level - strong signal
            }
            else
            {
                return 0.5; // Not at session level - neutral
            }
        }

        #endregion

        #region Phase 1C: Market Structure Levels

        /// <summary>
        /// Updates H1 support and resistance levels using Williams Fractals
        /// Scans last 200 H1 bars for fractal patterns
        /// Phase 1C Implementation
        /// </summary>
        private void UpdateH1Levels()
        {
            h1Supports.Clear();
            h1Resistances.Clear();

            int barsToScan = Math.Min(200, h1Bars.Count - 5);

            for (int i = 2; i < barsToScan - 2; i++)
            {
                int idx = h1Bars.Count - 1 - i;

                // Check bounds
                if (idx < 2 || idx >= h1Bars.Count - 2)
                    continue;

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

            Print("[H1 Levels] Detected {0} supports and {1} resistances",
                h1Supports.Count, h1Resistances.Count);
        }

        /// <summary>
        /// Updates M15 support and resistance levels using Williams Fractals
        /// Scans last 100 M15 bars for fractal patterns
        /// Phase 1C Implementation
        /// </summary>
        private void UpdateM15Levels()
        {
            m15Supports.Clear();
            m15Resistances.Clear();

            int barsToScan = Math.Min(100, m15Bars.Count - 5);

            for (int i = 2; i < barsToScan - 2; i++)
            {
                int idx = m15Bars.Count - 1 - i;

                // Check bounds
                if (idx < 2 || idx >= m15Bars.Count - 2)
                    continue;

                // Williams Fractal Up (Resistance)
                if (m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 1] &&
                    m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx - 2] &&
                    m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 1] &&
                    m15Bars.HighPrices[idx] > m15Bars.HighPrices[idx + 2])
                {
                    m15Resistances.Add(m15Bars.HighPrices[idx]);
                }

                // Williams Fractal Down (Support)
                if (m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 1] &&
                    m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx - 2] &&
                    m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 1] &&
                    m15Bars.LowPrices[idx] < m15Bars.LowPrices[idx + 2])
                {
                    m15Supports.Add(m15Bars.LowPrices[idx]);
                }
            }

            Print("[M15 Levels] Detected {0} supports and {1} resistances",
                m15Supports.Count, m15Resistances.Count);
        }

        /// <summary>
        /// Adjusts TP based on market structure levels
        /// Priority: H1 levels > M15 levels > Default 3R
        /// Always maintains minimum RR ratio
        /// Phase 1C Implementation
        /// </summary>
        private double AdjustTPForMarketStructure(double entryPrice, double initialTP, double slPrice, string mode)
        {
            double minTPDistance = Math.Abs(initialTP - entryPrice); // Minimum 3R distance
            double riskDistance = Math.Abs(entryPrice - slPrice);

            if (mode == "SELL")
            {
                // STEP 1: Check for H1 support level (highest priority)
                if (UseH1LevelsForTP && h1Supports.Count > 0)
                {
                    double bestH1Support = FindBestH1Support(entryPrice, initialTP);

                    if (bestH1Support > 0)
                    {
                        double h1TPDistance = entryPrice - bestH1Support;
                        double h1RR = h1TPDistance / riskDistance;

                        // Only use H1 level if it gives at least minimum RR
                        if (h1TPDistance >= minTPDistance)
                        {
                            Print("[TP-H1] Using H1 support at {0:F5} | Distance: {1:F1} pips | RR: 1:{2:F1}",
                                bestH1Support, h1TPDistance / Symbol.PipSize, h1RR);
                            return bestH1Support;
                        }
                        else
                        {
                            Print("[TP-H1] H1 support found but too close (RR: 1:{0:F1}, need ≥ 1:{1:F1})",
                                h1RR, MinimumRRRatio);
                        }
                    }
                }

                // STEP 2: Fall back to M15 support level
                if (UseM15LevelsForTP && m15Supports.Count > 0)
                {
                    double m15Support = FindM15Support(entryPrice, initialTP);

                    if (m15Support > 0)
                    {
                        double m15TPDistance = entryPrice - m15Support;
                        double m15RR = m15TPDistance / riskDistance;

                        if (m15TPDistance >= minTPDistance)
                        {
                            Print("[TP-M15] Using M15 support at {0:F5} | Distance: {1:F1} pips | RR: 1:{2:F1}",
                                m15Support, m15TPDistance / Symbol.PipSize, m15RR);
                            return m15Support;
                        }
                    }
                }

                // STEP 3: Use default 3R TP
                Print("[TP-Default] Using default {0:F1}R TP at {1:F5}", MinimumRRRatio, initialTP);
                return initialTP;
            }
            else // BUY
            {
                // STEP 1: Check for H1 resistance level (highest priority)
                if (UseH1LevelsForTP && h1Resistances.Count > 0)
                {
                    double bestH1Resistance = FindBestH1Resistance(entryPrice, initialTP);

                    if (bestH1Resistance > 0)
                    {
                        double h1TPDistance = bestH1Resistance - entryPrice;
                        double h1RR = h1TPDistance / riskDistance;

                        if (h1TPDistance >= minTPDistance)
                        {
                            Print("[TP-H1] Using H1 resistance at {0:F5} | Distance: {1:F1} pips | RR: 1:{2:F1}",
                                bestH1Resistance, h1TPDistance / Symbol.PipSize, h1RR);
                            return bestH1Resistance;
                        }
                        else
                        {
                            Print("[TP-H1] H1 resistance found but too close (RR: 1:{0:F1}, need ≥ 1:{1:F1})",
                                h1RR, MinimumRRRatio);
                        }
                    }
                }

                // STEP 2: Fall back to M15 resistance level
                if (UseM15LevelsForTP && m15Resistances.Count > 0)
                {
                    double m15Resistance = FindM15Resistance(entryPrice, initialTP);

                    if (m15Resistance > 0)
                    {
                        double m15TPDistance = m15Resistance - entryPrice;
                        double m15RR = m15TPDistance / riskDistance;

                        if (m15TPDistance >= minTPDistance)
                        {
                            Print("[TP-M15] Using M15 resistance at {0:F5} | Distance: {1:F1} pips | RR: 1:{2:F1}",
                                m15Resistance, m15TPDistance / Symbol.PipSize, m15RR);
                            return m15Resistance;
                        }
                    }
                }

                // STEP 3: Use default 3R TP
                Print("[TP-Default] Using default {0:F1}R TP at {1:F5}", MinimumRRRatio, initialTP);
                return initialTP;
            }
        }

        /// <summary>
        /// Finds best H1 support level for SELL trades
        /// Returns level below entry, within proximity, and at/beyond minimum TP
        /// </summary>
        private double FindBestH1Support(double entryPrice, double minTP)
        {
            double maxDistance = H1LevelProximityPips * Symbol.PipSize;

            // Find H1 support below entry, within proximity, and below minTP (further is better)
            var validSupports = h1Supports
                .Where(s => s < entryPrice && s <= minTP && (entryPrice - s) <= maxDistance)
                .OrderByDescending(s => s); // Closest to entry (but still profitable)

            return validSupports.FirstOrDefault();
        }

        /// <summary>
        /// Finds best H1 resistance level for BUY trades
        /// Returns level above entry, within proximity, and at/beyond minimum TP
        /// </summary>
        private double FindBestH1Resistance(double entryPrice, double minTP)
        {
            double maxDistance = H1LevelProximityPips * Symbol.PipSize;

            var validResistances = h1Resistances
                .Where(r => r > entryPrice && r >= minTP && (r - entryPrice) <= maxDistance)
                .OrderBy(r => r); // Closest to entry

            return validResistances.FirstOrDefault();
        }

        /// <summary>
        /// Finds best M15 support level for SELL trades
        /// Returns first valid support below entry and at/beyond minimum TP
        /// </summary>
        private double FindM15Support(double entryPrice, double minTP)
        {
            // Find M15 support below entry and at/beyond minimum TP
            var validSupports = m15Supports
                .Where(s => s < entryPrice && s <= minTP)
                .OrderByDescending(s => s); // Closest to entry

            return validSupports.FirstOrDefault();
        }

        /// <summary>
        /// Finds best M15 resistance level for BUY trades
        /// Returns first valid resistance above entry and at/beyond minimum TP
        /// </summary>
        private double FindM15Resistance(double entryPrice, double minTP)
        {
            var validResistances = m15Resistances
                .Where(r => r > entryPrice && r >= minTP)
                .OrderBy(r => r); // Closest to entry

            return validResistances.FirstOrDefault();
        }

        #endregion

        #region Phase 2: Session Management

        /// <summary>
        /// Gets session state for a given time
        /// Returns which sessions are active (can be multiple overlapping)
        /// Phase 2 Implementation
        /// </summary>
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

        /// <summary>
        /// Gets the PRIMARY session for a given time
        /// Overlap takes priority, then London, NY, Asian
        /// Phase 2 Implementation
        /// </summary>
        private TradingSession GetPrimarySession(DateTime time)
        {
            var state = GetSessionState(time);

            // Overlap is highest priority (most liquidity)
            if (state.IsOverlap)
                return TradingSession.Overlap;

            // Individual sessions
            if (state.IsLondon)
                return TradingSession.London;

            if (state.IsNewYork)
                return TradingSession.NewYork;

            if (state.IsAsian)
                return TradingSession.Asian;

            return TradingSession.None;
        }

        /// <summary>
        /// Updates session tracking on each M15 bar
        /// Detects session boundaries and tracks high/low
        /// Phase 2 Implementation
        /// </summary>
        private void UpdateSessionTracking()
        {
            DateTime currentTime = m15Bars.OpenTimes.LastValue;
            TradingSession currentPrimarySession = GetPrimarySession(currentTime);

            // Detect session boundary (new session started)
            if (currentPrimarySession != lastDetectedSession && currentPrimarySession != TradingSession.None)
            {
                // Save previous session if it existed
                if (currentSession != null)
                {
                    currentSession.EndTime = currentTime;
                    recentSessions.Add(currentSession);

                    Print("[Session] {0} session ended | High: {1:F5} | Low: {2:F5} | Duration: {3}",
                        currentSession.Session,
                        currentSession.High,
                        currentSession.Low,
                        currentSession.EndTime - currentSession.StartTime);

                    // Keep only last 20 sessions
                    if (recentSessions.Count > 20)
                        recentSessions.RemoveAt(0);
                }

                // Start new session
                currentSession = new SessionLevels
                {
                    Session = currentPrimarySession,
                    StartTime = currentTime,
                    High = m15Bars.HighPrices.LastValue,
                    Low = m15Bars.LowPrices.LastValue
                };

                Print("[Session] NEW {0} session started at {1}", currentPrimarySession, currentTime);

                lastDetectedSession = currentPrimarySession;
            }

            // Update current session high/low
            if (currentSession != null)
            {
                double currentHigh = m15Bars.HighPrices.LastValue;
                double currentLow = m15Bars.LowPrices.LastValue;

                if (currentHigh > currentSession.High)
                    currentSession.High = currentHigh;

                if (currentLow < currentSession.Low)
                    currentSession.Low = currentLow;
            }
        }

        /// <summary>
        /// Finds the session that a swing occurred in
        /// Returns null if swing is too old or no session found
        /// Phase 2 Implementation
        /// </summary>
        private SessionLevels GetSessionForTime(DateTime swingTime)
        {
            // Check current session first
            if (currentSession != null && swingTime >= currentSession.StartTime)
                return currentSession;

            // Check recent sessions
            foreach (var session in recentSessions)
            {
                if (swingTime >= session.StartTime && swingTime <= session.EndTime)
                    return session;
            }

            return null;
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

            // Set rectangle timing (for entry expiry logic - Issue 3 Fix)
            rectangleCreatedTime = m15Bars.OpenTimes.LastValue;
            rectangleExpiryTime = rectangleCreatedTime.AddMinutes(RectangleWidthMinutes);

            Print("[SwingZone] {0} Mode | Top: {1:F5} | Bottom: {2:F5} | Height: {3:F1} pips",
                mode, swingTopPrice, swingBottomPrice, heightPips);
            Print("[SwingZone] Created: {0} | Expires: {1} ({2} min window)",
                rectangleCreatedTime, rectangleExpiryTime, RectangleWidthMinutes);

            // Issue 1 Fix: Check if current price is close enough to rectangle to "arm" it
            double currentPrice = Symbol.Bid;
            double distanceToRectangle;

            if (mode == "SELL")
            {
                // For SELL: Check distance from current price to rectangle bottom
                // Price should be above or near the rectangle (not already broken through)
                distanceToRectangle = (currentPrice - swingBottomPrice) / Symbol.PipSize;
            }
            else
            {
                // For BUY: Check distance from current price to rectangle top
                // Price should be below or near the rectangle (not already broken through)
                distanceToRectangle = (swingTopPrice - currentPrice) / Symbol.PipSize;
            }

            // Mark that we have a valid rectangle (even if not armed yet)
            hasValidRectangle = true;

            // Check if price has already moved too far from rectangle
            if (distanceToRectangle < -MaxDistanceToArm)
            {
                Print("[SwingZone] ⚠️ Price already {0:F1} pips beyond rectangle - NOT ARMED (max: {1:F1} pips)",
                    Math.Abs(distanceToRectangle), MaxDistanceToArm);
                Print("[SwingZone] Rectangle drawn for visualization only - waiting for price to return");
                hasActiveSwing = false;  // Don't arm the rectangle for trading yet
            }
            else
            {
                Print("[SwingZone] ✅ Price {0:F1} pips from rectangle edge - ARMED for trading",
                    distanceToRectangle);
                hasActiveSwing = true;  // Arm the rectangle for trading
            }

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

        #region Phase 1B: Entry Detection Logic

        /// <summary>
        /// Processes entry logic based on selected entry mode
        /// Called on every M1 bar close (via OnBar)
        /// </summary>
        private void ProcessEntryLogic()
        {
            // Check if we already have max positions
            var positions = Positions.FindAll(MagicNumber.ToString(), SymbolName);
            if (positions.Length >= MaxPositions)
                return;

            // FIX Issue 3: Check if rectangle has expired (time-based cutoff)
            DateTime currentTime = Bars.OpenTimes.LastValue;
            if (currentTime > rectangleExpiryTime)
            {
                Print("[RectangleExpired] Rectangle expired at {0} | Current time: {1} | Disabling swing",
                    rectangleExpiryTime, currentTime);
                hasActiveSwing = false;
                hasValidRectangle = false;
                return;
            }

            // Get CLOSED M1 candle (last completed bar)
            int lastIdx = Bars.Count - 2;
            if (lastIdx < 0)
                return;

            double candleOpen = Bars.OpenPrices[lastIdx];
            double candleClose = Bars.ClosePrices[lastIdx];
            double candleHigh = Bars.HighPrices[lastIdx];
            double candleLow = Bars.LowPrices[lastIdx];

            // Process based on entry mode
            if (EntryModeSelection == EntryMode.Breakout)
            {
                ProcessBreakoutEntry(candleOpen, candleClose, candleHigh, candleLow);
            }
            else
            {
                ProcessRetestEntry(candleOpen, candleClose, candleHigh, candleLow);
            }
        }

        /// <summary>
        /// Breakout Entry Mode (DEFAULT)
        /// SELL: Candle CLOSES below rectangle bottom (bearish breakout)
        /// BUY: Candle CLOSES above rectangle top (bullish breakout)
        /// Invalidates if body closes opposite direction
        /// </summary>
        private void ProcessBreakoutEntry(double open, double close, double high, double low)
        {
            if (currentMode == "SELL")
            {
                // Invalidate if body closes ABOVE rectangle (wrong direction breakout)
                if (close > swingTopPrice && open > swingTopPrice)
                {
                    Print("[RectangleInvalid] SELL rectangle invalidated - body closed above");
                    hasActiveSwing = false;
                    hasValidRectangle = false;
                    return;
                }

                // TRIGGER: CLOSE below rectangle bottom = breakout
                // Open can be anywhere (inside rectangle is normal for a breakout candle)
                bool closesBelowRectangle = (close < swingBottomPrice);

                // Candle must have interacted with the rectangle (high touched or entered it)
                bool hadRectangleInteraction = (high >= swingBottomPrice);

                // Must be a bearish candle (close < open) for SELL
                bool isBearishCandle = (close < open);

                if (closesBelowRectangle && hadRectangleInteraction && isBearishCandle)
                {
                    Print("[BreakoutEntry] SELL trigger | Close: {0:F5} < Bottom: {1:F5} | Bearish: YES",
                        close, swingBottomPrice);
                    ExecuteSellTrade();
                }
                else if (closesBelowRectangle)
                {
                    // Debug: Why didn't we trigger?
                    Print("[BreakoutDebug] SELL almost triggered | Close below: YES | Interaction: {0} | Bearish: {1}",
                        hadRectangleInteraction ? "YES" : "NO", isBearishCandle ? "YES" : "NO");
                }
            }
            else if (currentMode == "BUY")
            {
                // Invalidate if body closes BELOW rectangle (wrong direction breakout)
                if (close < swingBottomPrice && open < swingBottomPrice)
                {
                    Print("[RectangleInvalid] BUY rectangle invalidated - body closed below");
                    hasActiveSwing = false;
                    hasValidRectangle = false;
                    return;
                }

                // TRIGGER: CLOSE above rectangle top = breakout
                // Open can be anywhere (inside rectangle is normal for a breakout candle)
                bool closesAboveRectangle = (close > swingTopPrice);

                // Candle must have interacted with the rectangle (low touched or entered it)
                bool hadRectangleInteraction = (low <= swingTopPrice);

                // Must be a bullish candle (close > open) for BUY
                bool isBullishCandle = (close > open);

                if (closesAboveRectangle && hadRectangleInteraction && isBullishCandle)
                {
                    Print("[BreakoutEntry] BUY trigger | Close: {0:F5} > Top: {1:F5} | Bullish: YES",
                        close, swingTopPrice);
                    ExecuteBuyTrade();
                }
                else if (closesAboveRectangle)
                {
                    // Debug: Why didn't we trigger?
                    Print("[BreakoutDebug] BUY almost triggered | Close above: YES | Interaction: {0} | Bullish: {1}",
                        hadRectangleInteraction ? "YES" : "NO", isBullishCandle ? "YES" : "NO");
                }
            }
        }

        /// <summary>
        /// Retest Entry Mode (ALTERNATIVE)
        /// Phase 1: Detect breakout
        /// Phase 2: Wait for retest and rejection candle
        /// </summary>
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
                        Print("[RetestEntry] SELL retest confirmed | Rejection candle detected");
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
                        Print("[RetestEntry] BUY retest confirmed | Rejection candle detected");
                        ExecuteBuyTrade();
                        hasBreakoutOccurred = false;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates position size based on risk percentage and SL distance
        /// Phase 1B: Dynamic risk-based position sizing
        /// </summary>
        private double CalculatePositionSize(double slDistancePips)
        {
            double riskAmount = Account.Balance * (RiskPercent / 100.0);
            double pipValuePerLot = Symbol.PipValue * Symbol.LotSize;
            double lotSize = riskAmount / (slDistancePips * pipValuePerLot);

            Print("[PositionSizing] Risk: {0:F2}% (${1:F2}) | SL: {2:F1} pips | Lot Size: {3:F4}",
                RiskPercent, riskAmount, slDistancePips, lotSize);

            // Normalize to broker limits
            double volumeInUnits = Symbol.QuantityToVolumeInUnits(lotSize);
            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            // Ensure minimum volume
            if (volumeInUnits < Symbol.VolumeInUnitsMin)
            {
                Print("[PositionSizing] Volume too small ({0:F2}), minimum is {1:F2}",
                    volumeInUnits, Symbol.VolumeInUnitsMin);
                return 0;
            }

            return volumeInUnits;
        }

        #endregion

        #region Trade Execution

        /// <summary>
        /// Executes a SELL trade - Phase 1B Implementation
        /// SL = Rectangle top + buffer
        /// TP = 3R minimum from entry
        /// </summary>
        private void ExecuteSellTrade()
        {
            double entryPrice = Symbol.Bid;

            // SL = rectangle top + buffer (above the zone)
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

            // TP = Minimum RR from entry (default 3R) - Phase 1C: Adjusted for market structure
            double initialTP = entryPrice - (riskPips * MinimumRRRatio * Symbol.PipSize);
            double takeProfit = AdjustTPForMarketStructure(entryPrice, initialTP, stopLoss, "SELL");

            // Calculate actual RR
            double rewardPips = (entryPrice - takeProfit) / Symbol.PipSize;
            double actualRR = rewardPips / riskPips;

            Print("[SELL] Entry Setup:");
            Print("   Entry: {0:F5} | SL: {1:F5} (+{2:F1} pips buffer)", entryPrice, stopLoss, SLBufferPips);
            Print("   TP: {0:F5} | Risk: {1:F1} pips | Reward: {2:F1} pips | RR: 1:{3:F1}",
                takeProfit, riskPips, rewardPips, actualRR);
            Print("   Volume: {0:F2} units | Rectangle: {1:F5} - {2:F5}",
                volume, swingBottomPrice, swingTopPrice);

            // Convert absolute prices to pips from entry for ExecuteMarketOrder
            double slPips = Math.Abs(stopLoss - entryPrice) / Symbol.PipSize;
            double tpPips = Math.Abs(takeProfit - entryPrice) / Symbol.PipSize;

            var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, MagicNumber.ToString(),
                slPips, tpPips);

            if (result.IsSuccessful)
            {
                Print("✅ SELL EXECUTED SUCCESSFULLY");
                Print("   Position ID: {0} | Risk Amount: ${1:F2}",
                    result.Position.Id, Account.Balance * (RiskPercent / 100.0));

                // Disable active swing if trading on new swing only
                if (TradeOnNewSwingOnly)
                {
                    hasActiveSwing = false;
                    hasValidRectangle = false;
                    hasBreakoutOccurred = false;
                }
            }
            else
            {
                Print("❌ SELL FAILED | Error: {0}", result.Error);
            }
        }

        /// <summary>
        /// Executes a BUY trade - Phase 1B Implementation
        /// SL = Rectangle bottom - buffer
        /// TP = 3R minimum from entry
        /// </summary>
        private void ExecuteBuyTrade()
        {
            double entryPrice = Symbol.Ask;

            // SL = rectangle bottom - buffer (below the zone)
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

            // TP = Minimum RR from entry (default 3R) - Phase 1C: Adjusted for market structure
            double initialTP = entryPrice + (riskPips * MinimumRRRatio * Symbol.PipSize);
            double takeProfit = AdjustTPForMarketStructure(entryPrice, initialTP, stopLoss, "BUY");

            // Calculate actual RR
            double rewardPips = (takeProfit - entryPrice) / Symbol.PipSize;
            double actualRR = rewardPips / riskPips;

            Print("[BUY] Entry Setup:");
            Print("   Entry: {0:F5} | SL: {1:F5} (-{2:F1} pips buffer)", entryPrice, stopLoss, SLBufferPips);
            Print("   TP: {0:F5} | Risk: {1:F1} pips | Reward: {2:F1} pips | RR: 1:{3:F1}",
                takeProfit, riskPips, rewardPips, actualRR);
            Print("   Volume: {0:F2} units | Rectangle: {1:F5} - {2:F5}",
                volume, swingBottomPrice, swingTopPrice);

            // Convert absolute prices to pips from entry for ExecuteMarketOrder
            double slPips = Math.Abs(stopLoss - entryPrice) / Symbol.PipSize;
            double tpPips = Math.Abs(takeProfit - entryPrice) / Symbol.PipSize;

            var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, MagicNumber.ToString(),
                slPips, tpPips);

            if (result.IsSuccessful)
            {
                Print("✅ BUY EXECUTED SUCCESSFULLY");
                Print("   Position ID: {0} | Risk Amount: ${1:F2}",
                    result.Position.Id, Account.Balance * (RiskPercent / 100.0));

                // Disable active swing if trading on new swing only
                if (TradeOnNewSwingOnly)
                {
                    hasActiveSwing = false;
                    hasValidRectangle = false;
                    hasBreakoutOccurred = false;
                }
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

            // End time: Use M15 bar time + width (more reliable than Server.Time in backtesting)
            DateTime currentM15Time = m15Bars.OpenTimes.LastValue;
            DateTime endTime = currentM15Time.AddMinutes(RectangleWidthMinutes);

            // Debug: Show all time references
            Print("[RectangleDebug] SwingIndex: {0} | SwingBarTime: {1}", swingIndex, startTime);
            Print("[RectangleDebug] Server.Time: {0} | M15 LastBar: {1} | EndTime: {2}",
                Server.Time, currentM15Time, endTime);

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

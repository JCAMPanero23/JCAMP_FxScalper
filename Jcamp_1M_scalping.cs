using System;
using System.Collections.Generic;
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

        // SMA Period: 200 is industry standard, optimize in range 100-300 with step 50
        [Parameter("SMA Period", DefaultValue = 200, MinValue = 100, MaxValue = 300, Step = 50, Group = "Trend Detection")]
        public int SMAPeriod { get; set; }

        // Swing Lookback: How far back to look for fractals. Step=10 gives 7 combinations (20-80)
        [Parameter("Swing Lookback Bars (M15)", DefaultValue = 30, MinValue = 20, MaxValue = 80, Step = 10, Group = "Trend Detection")]
        public int SwingLookbackBars { get; set; }

        // Min Swing Score: Quality threshold. Step=0.05 gives 9 combinations (0.40-0.80)
        [Parameter("Minimum Swing Score", DefaultValue = 0.60, MinValue = 0.40, MaxValue = 0.80, Step = 0.05, Group = "Trend Detection")]
        public double MinimumSwingScore { get; set; }

        #endregion

        #region Parameters - Session Management

        [Parameter("=== SESSION MANAGEMENT ===", DefaultValue = "")]
        public string SessionHeader { get; set; }

        [Parameter("Enable Session Filter", DefaultValue = true, Group = "Session Management")]
        public bool EnableSessionFilter { get; set; }

        [Parameter("Show Session Boxes", DefaultValue = false, Group = "Session Management")]
        public bool ShowSessionBoxes { get; set; }

        [Parameter("Session Box Mode", DefaultValue = SessionBoxMode.Advanced, Group = "Session Management")]
        public SessionBoxMode SessionBoxDisplayMode { get; set; }

        [Parameter("Session Weight", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Session Management")]
        public double SessionWeight { get; set; }

        #endregion

        #region Parameters - Trade Management

        [Parameter("=== TRADE MANAGEMENT ===", DefaultValue = "")]
        public string TradeHeader { get; set; }

        // Risk %: Keep conservative range 0.5-2.0%. Step=0.25 gives 7 combinations
        [Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.5, MaxValue = 2.0, Step = 0.25, Group = "Trade Management")]
        public double RiskPercent { get; set; }

        // SL Buffer: Protection pips beyond zone. Step=0.5 gives 7 combinations (1-4)
        [Parameter("SL Buffer Pips", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 4.0, Step = 0.5, Group = "Trade Management")]
        public double SLBufferPips { get; set; }

        // Min RR: Trade quality filter. Step=0.5 gives 7 combinations (2-5)
        [Parameter("Minimum RR Ratio", DefaultValue = 3.0, MinValue = 2.0, MaxValue = 5.0, Step = 0.5, Group = "Trade Management")]
        public double MinimumRRRatio { get; set; }

        // Max Positions: Usually 1-3 for scalping. Step=1 gives 3 combinations
        [Parameter("Max Positions", DefaultValue = 1, MinValue = 1, MaxValue = 3, Step = 1, Group = "Trade Management")]
        public int MaxPositions { get; set; }

        [Parameter("Magic Number", DefaultValue = 100001, Group = "Trade Management")]
        public int MagicNumber { get; set; }

        #endregion

        #region Parameters - Chandelier Stop Loss

        [Parameter("=== CHANDELIER SL ===", DefaultValue = "")]
        public string ChandelierHeader { get; set; }

        [Parameter("Enable Chandelier SL", DefaultValue = true, Group = "Chandelier SL")]
        public bool EnableChandelierSL { get; set; }

        [Parameter("Activation RR Fraction", DefaultValue = 0.75, MinValue = 0.5, MaxValue = 0.85, Step = 0.05, Group = "Chandelier SL")]
        public double ChandelierActivationRR { get; set; }

        [Parameter("Chandelier Lookback Bars", DefaultValue = 22, MinValue = 10, MaxValue = 30, Step = 2, Group = "Chandelier SL")]
        public int ChandelierLookback { get; set; }

        [Parameter("TP Mode", DefaultValue = ChandelierTPMode.TrailingTP, Group = "Chandelier SL")]
        public ChandelierTPMode ChandelierTPModeSelection { get; set; }

        [Parameter("Trailing TP Offset (pips)", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 20.0, Step = 1.0, Group = "Chandelier SL")]
        public double TrailingTPOffset { get; set; }

        #endregion

        #region Parameters - TP Management

        [Parameter("=== TP MANAGEMENT ===", DefaultValue = "")]
        public string TPHeader { get; set; }

        [Parameter("Use H1 Levels for TP", DefaultValue = true, Group = "TP Management")]
        public bool UseH1LevelsForTP { get; set; }

        [Parameter("Use M15 Levels for TP", DefaultValue = true, Group = "TP Management")]
        public bool UseM15LevelsForTP { get; set; }

        // H1 Proximity: How close to H1 level to use it. Step=10 gives 6 combinations (30-80)
        [Parameter("H1 Level Proximity Pips", DefaultValue = 50, MinValue = 30, MaxValue = 80, Step = 10, Group = "TP Management")]
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

        // Max Distance to Arm: How far price can be to arm zone. Step=2 gives 6 combinations (4-14)
        [Parameter("Max Distance to Arm (pips)", DefaultValue = 10.0, MinValue = 4.0, MaxValue = 14.0, Step = 2.0, Group = "Entry Filters")]
        public double MaxDistanceToArm { get; set; }

        #endregion

        #region Entry Mode Enum

        public enum EntryMode
        {
            Breakout,       // DEFAULT: Enter when body closes beyond rectangle
            RetestConfirm   // ALTERNATIVE: Wait for retest after breakout
        }

        #endregion

        #region Chandelier TP Mode Enum

        public enum ChandelierTPMode
        {
            KeepOriginal,   // TP stays at original level throughout
            RemoveTP,       // TP removed on activation; exit via chandelier SL only
            TrailingTP      // TP trails ahead of chandelier SL by offset
        }

        #endregion

        #region PRE-Zone Enums and Classes - Phase 4 Implementation

        /// <summary>
        /// Represents the lifecycle state of a PRE-zone
        /// Phase 4 Implementation - Zone State Management
        /// </summary>
        public enum ZoneState
        {
            Pre,          // Created from displacement + FVG, not yet confirmed
            Valid,        // Confirmed by Williams Fractal
            Armed,        // Price within proximity, ready for entry
            Expired,      // Time limit exceeded
            Invalidated   // Wrong-direction breakout
        }

        /// <summary>
        /// Represents a displacement (impulse) candle that initiates zone creation
        /// Phase 4 Implementation - Displacement Candle Tracking
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

        #region Trading Zone Class

        /// <summary>
        /// Represents a trading zone with full lifecycle management
        /// Tracks zone state, price levels, timing, source references, and scoring
        /// Phase 4 Implementation - Zone Lifecycle Management
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
                return $"Zone_{mode}_{time:yyyyMMdd_HHmmssfff}";
            }
        }

        #endregion

        #region Chandelier State Class

        /// <summary>
        /// Tracks chandelier trailing stop state for each position
        /// </summary>
        private class ChandelierState
        {
            public int PositionId { get; set; }
            public bool IsActivated { get; set; }
            public double EntryPrice { get; set; }
            public double OriginalTP { get; set; }
            public double OriginalSL { get; set; }
            public double ActivationPrice { get; set; }
            public double BreakevenPrice { get; set; }
            public double CurrentTrailingSL { get; set; }
            public double CurrentTrailingTP { get; set; }
            public double HighestTrailingSL { get; set; }
            public double HighestTrailingTP { get; set; }
            public bool TPTrailingStarted { get; set; }
            public TradeType TradeDirection { get; set; }
        }

        #endregion

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
        /// Session box display modes
        /// Advanced Mode Implementation
        /// </summary>
        public enum SessionBoxMode
        {
            Basic,      // Show all sessions (Asian/London/NY) with standard colors
            Advanced    // Show only optimal trading periods with priority colors
        }

        /// <summary>
        /// Optimal trading periods for Advanced Session Box Mode
        /// </summary>
        public enum OptimalPeriod
        {
            None,
            BestOverlap,        // 13:00-17:00 UTC - BEST (Bright Green)
            GoodLondonOpen,     // 08:00-12:00 UTC - GOOD (Yellow/Gold)
            DangerDeadZone,     // 04:00-08:00 UTC - AVOID (Red)
            DangerLateNY        // 20:00-00:00 UTC - AVOID (Red)
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

        /// <summary>
        /// Fair Value Gap (FVG) class for tracking price inefficiencies
        /// Phase 3 Implementation
        /// </summary>
        public class FairValueGap
        {
            public DateTime Time { get; set; }
            public double TopPrice { get; set; }
            public double BottomPrice { get; set; }
            public bool IsBullish { get; set; }
            public bool IsFilled { get; set; }

            // NEW fields for Phase 4
            public bool IsHighQuality { get; set; }          // Candle B meets displacement criteria
            public double GapSizeInPips { get; set; }        // For filtering
            public int DisplacementBarIndex { get; set; }    // Links to impulse candle (-1 if none)
        }

        #endregion

        #region Parameters - FVG Detection

        [Parameter("=== FVG DETECTION ===", DefaultValue = "")]
        public string FVGHeader { get; set; }

        [Parameter("Enable FVG Filter", DefaultValue = true, Group = "FVG Detection")]
        public bool EnableFVGFilter { get; set; }

        // FVG Lookback: How many bars to scan for FVGs. Step=10 gives 5 combinations (20-60)
        [Parameter("FVG Lookback Bars", DefaultValue = 30, MinValue = 20, MaxValue = 60, Step = 10, Group = "FVG Detection")]
        public int FVGLookbackBars { get; set; }

        // Min FVG Size: Filter noise gaps. Step=0.5 gives 5 combinations (0.5-2.5)
        [Parameter("Min FVG Size (pips)", DefaultValue = 1.5, MinValue = 0.5, MaxValue = 2.5, Step = 0.5, Group = "FVG Detection")]
        public double MinFVGSizePips { get; set; }

        // FVG Max Age: How old FVG can be. Step=10 gives 5 combinations (20-60)
        [Parameter("FVG Max Age (bars)", DefaultValue = 30, MinValue = 20, MaxValue = 60, Step = 10, Group = "FVG Detection")]
        public int FVGMaxAgeBars { get; set; }

        #endregion

        #region Parameters - PRE-Zone System

        [Parameter("=== PRE-ZONE SYSTEM ===", DefaultValue = "")]
        public string PreZoneHeader { get; set; }

        [Parameter("Enable PRE-Zone System", DefaultValue = true, Group = "PRE-Zone System")]
        public bool EnablePreZoneSystem { get; set; }

        // ATR Period: Standard is 14. Step=2 gives 6 combinations (10-20)
        [Parameter("ATR Period", DefaultValue = 14, MinValue = 10, MaxValue = 20, Step = 2, Group = "PRE-Zone System")]
        public int ATRPeriod { get; set; }

        // ATR Multiplier: Displacement sensitivity. Step=0.25 gives 5 combinations (1.0-2.0)
        // KEY OPTIMIZATION PARAMETER
        [Parameter("ATR Multiplier", DefaultValue = 1.5, MinValue = 1.0, MaxValue = 2.0, Step = 0.25, Group = "PRE-Zone System")]
        public double ATRMultiplier { get; set; }

        // PRE-Zone Expiry: How long zone stays active. Step=15 gives 5 combinations (30-90)
        [Parameter("PRE-Zone Expiry (minutes)", DefaultValue = 60, MinValue = 30, MaxValue = 90, Step = 15, Group = "PRE-Zone System")]
        public int PreZoneExpiryMinutes { get; set; }

        // VALID-Zone Expiry: Extended time after fractal confirms. Step=30 gives 5 combinations (60-180)
        [Parameter("VALID-Zone Expiry (minutes)", DefaultValue = 120, MinValue = 60, MaxValue = 180, Step = 30, Group = "PRE-Zone System")]
        public int ValidZoneExpiryMinutes { get; set; }

        // Fractal Tolerance: How close fractal must be to zone. Step=1 gives 5 combinations (3-7)
        [Parameter("Fractal Zone Tolerance (pips)", DefaultValue = 5.0, MinValue = 3.0, MaxValue = 7.0, Step = 1.0, Group = "PRE-Zone System")]
        public double FractalZoneTolerancePips { get; set; }

        // Min PRE-Zone Score: Quality threshold. Step=0.05 gives 7 combinations (0.40-0.70)
        // KEY OPTIMIZATION PARAMETER
        [Parameter("Min PRE-Zone Score", DefaultValue = 0.50, MinValue = 0.40, MaxValue = 0.70, Step = 0.05, Group = "PRE-Zone System")]
        public double MinPreZoneScore { get; set; }

        #endregion

        #region Parameters - Visualization

        [Parameter("=== VISUALIZATION ===", DefaultValue = "")]
        public string VisualHeader { get; set; }

        [Parameter("Show Rectangles", DefaultValue = true, Group = "Visualization")]
        public bool ShowRectangles { get; set; }

        // Rectangle Width: Trading window duration. Step=15 gives 5 combinations (30-90)
        [Parameter("Rectangle Width (Minutes)", DefaultValue = 60, MinValue = 30, MaxValue = 90, Step = 15, Group = "Visualization")]
        public int RectangleWidthMinutes { get; set; }

        [Parameter("Show Mode Label", DefaultValue = true, Group = "Visualization")]
        public bool ShowModeLabel { get; set; }

        [Parameter("BUY Color", DefaultValue = "Green", Group = "Visualization")]
        public string BuyColorName { get; set; }

        [Parameter("SELL Color", DefaultValue = "Red", Group = "Visualization")]
        public string SellColorName { get; set; }

        [Parameter("Rectangle Transparency", DefaultValue = 80, MinValue = 0, MaxValue = 255, Group = "Visualization")]
        public int RectangleTransparency { get; set; }

        [Parameter("PRE-Zone Color", DefaultValue = "Yellow", Group = "Visualization")]
        public string ColorPreZoneName { get; set; }

        [Parameter("VALID-Zone Color", DefaultValue = "Blue", Group = "Visualization")]
        public string ColorValidZoneName { get; set; }

        [Parameter("ARMED-Zone Color", DefaultValue = "Green", Group = "Visualization")]
        public string ColorArmedZoneName { get; set; }

        #endregion

        #region Parameters - Score Weights

        [Parameter("=== SCORE WEIGHTS ===", DefaultValue = "")]
        public string WeightsHeader { get; set; }

        [Parameter("Weight: Validity", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightValidity { get; set; }

        [Parameter("Weight: Extremity", DefaultValue = 0.25, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightExtremity { get; set; }

        [Parameter("Weight: Fractal", DefaultValue = 0.15, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightFractal { get; set; }

        [Parameter("Weight: Session", DefaultValue = 0.20, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightSession { get; set; }

        [Parameter("Weight: FVG", DefaultValue = 0.15, MinValue = 0.0, MaxValue = 1.0, Step = 0.05, Group = "Score Weights")]
        public double WeightFVG { get; set; }

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

        // Phase 2: Period transition tracking for live session box drawing
        private TradingSession lastDrawnSession = TradingSession.None;
        private OptimalPeriod lastDrawnPeriod = OptimalPeriod.None;

        // Phase 2: Session box colors - Basic Mode (30-40% opacity for visibility without obscuring price)
        private readonly Color ColorAsian = Color.FromArgb(30, 255, 255, 0);      // Light Yellow
        private readonly Color ColorLondon = Color.FromArgb(30, 0, 128, 255);     // Light Blue
        private readonly Color ColorNewYork = Color.FromArgb(30, 255, 128, 0);    // Light Orange
        private readonly Color ColorOverlap = Color.FromArgb(40, 128, 0, 255);    // Light Purple (higher opacity)

        // Advanced Mode: Priority-based colors for optimal trading periods
        private readonly Color ColorBestTime = Color.FromArgb(50, 0, 255, 0);     // Bright Green (BEST - Overlap 13:00-17:00)
        private readonly Color ColorGoodTime = Color.FromArgb(45, 255, 215, 0);   // Gold (GOOD - London 08:00-12:00)
        private readonly Color ColorDangerZone = Color.FromArgb(40, 255, 0, 0);   // Red (DANGER - Dead zones)

        // Phase 3: FVG tracking
        private System.Collections.Generic.List<FairValueGap> activeFVGs = new System.Collections.Generic.List<FairValueGap>();

        // Phase 4: PRE-Zone System
        private AverageTrueRange atr;                    // ATR indicator for displacement detection (M15)
        private AverageTrueRange atrM1;                  // ATR indicator for M1 displacement detection
        private TradingZone activeZone = null;           // Current active zone (or null)
        private DisplacementCandle lastDisplacement = null;  // Most recent displacement detected

        // Phase 4: Zone colors
        private readonly Color ColorPreZone = Color.FromArgb(60, 255, 255, 0);    // Yellow (PRE)
        private readonly Color ColorValidZone = Color.FromArgb(60, 0, 128, 255);  // Blue (VALID)
        private readonly Color ColorArmedZone = Color.FromArgb(60, 0, 255, 0);    // Green (ARMED)

        // Chandelier trailing stop tracking
        private Dictionary<int, ChandelierState> _chandelierStates;

        // Visualization tracking
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

            // Initialize chandelier state tracking
            _chandelierStates = new Dictionary<int, ChandelierState>();

            // Phase 4: Initialize ATR indicators for displacement detection
            if (EnablePreZoneSystem)
            {
                atr = Indicators.AverageTrueRange(m15Bars, ATRPeriod, MovingAverageType.Simple);
                atrM1 = Indicators.AverageTrueRange(Bars, ATRPeriod, MovingAverageType.Simple);
                Print("[PRE-Zone] ATR indicators initialized | Period: {0} | Multiplier: {1:F1}x | M1+M15 displacement", ATRPeriod, ATRMultiplier);
            }

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

            // Phase 2: Session Management
            Print("Phase 2 Session Management: Enabled={0} | Session Weight={1:F2}",
                EnableSessionFilter, WeightSession);
            Print("Session Boxes: {0} | Mode: {1}",
                ShowSessionBoxes ? "ON" : "OFF",
                SessionBoxDisplayMode);

            if (ShowSessionBoxes && SessionBoxDisplayMode == SessionBoxMode.Advanced)
            {
                Print("  🟢 BEST TIME (Green):   13:00-17:00 UTC (Overlap - Highest volatility)");
                Print("  🟡 GOOD TIME (Gold):    08:00-12:00 UTC (London Open)");
                Print("  🔴 DANGER ZONE (Red):   04:00-08:00 UTC (Dead zone) & 20:00-00:00 UTC (Late NY)");
                Print("  Advanced Mode: Only optimal trading periods shown on chart");
            }

            // Advanced Session Scoring Integration
            Print("Session Scoring Integration: {0}",
                EnableSessionFilter ? "ACTIVE" : "Disabled");
            if (EnableSessionFilter)
            {
                Print("  BEST periods:   Session score = +1.0 (strong positive)");
                Print("  GOOD periods:   Session score = +0.7 (good positive)");
                Print("  Neutral times:  Session score = +0.5 (neutral)");
                Print("  DANGER periods: Session score = -0.5 (NEGATIVE PENALTY!)");
                Print("  → Swings in danger zones will be AUTO-REJECTED (score too low)");
            }

            // Phase 3: FVG Detection
            Print("Phase 3 FVG Detection: Enabled={0} | Lookback={1} bars | FVG Weight={2:F2}",
                EnableFVGFilter, FVGLookbackBars, WeightFVG);

            // Phase 4: PRE-Zone System
            Print("PRE-Zone System: {0} | ATR: {1} | Multiplier: {2:F1}x | Min Score: {3:F2}",
                EnablePreZoneSystem ? "ON" : "OFF",
                ATRPeriod,
                ATRMultiplier,
                MinPreZoneScore);

            // Print weight summary
            Print("Score Weights: Validity={0:F2} | Extremity={1:F2} | Fractal={2:F2} | Session={3:F2} | FVG={4:F2} | Candle={5:F2}",
                WeightValidity, WeightExtremity, WeightFractal, WeightSession, WeightFVG, WeightCandle);
            double weightTotal = WeightValidity + WeightExtremity + WeightFractal + WeightSession + WeightFVG + WeightCandle;
            Print("Weight Total: {0:F2} {1}", weightTotal, weightTotal == 1.0 ? "✓" : "⚠ WARNING: Should be 1.0!");

            // ========== TIMEZONE DIAGNOSTIC ==========
            Print("========================================");
            Print("*** TIMEZONE DIAGNOSTIC ***");
            Print("========================================");
            Print("Robot TimeZone Setting: TimeZones.UTC");
            Print("Server Time: {0}", Server.Time.ToString("yyyy-MM-dd HH:mm:ss"));
            Print("Server Time Zone: UTC (configured in Robot attribute)");
            Print("");
            Print("✓ TIMEZONE STATUS: CORRECT");
            Print("  Sessions are configured for UTC and will trigger at:");
            Print("  - Asian Session:    00:00-09:00 UTC");
            Print("  - London Session:   08:00-17:00 UTC");
            Print("  - New York Session: 13:00-22:00 UTC");
            Print("  - Overlap Period:   13:00-17:00 UTC (London + NY)");
            Print("");
            Print("  Note: In backtesting, Server.Time uses historical backtest time,");
            Print("        not current real-world time. This is expected behavior.");
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
            // M1 FVG DETECTION: Process on every M1 bar close
            // ============================================================
            DetectFVGs();  // Scan M1 bars for Fair Value Gaps

            // ============================================================
            // M1 DISPLACEMENT DETECTION: Process on every M1 bar close
            // ============================================================
            if (EnablePreZoneSystem)
            {
                var m1Displacement = DetectM1Displacement();

                if (m1Displacement != null)
                {
                    // Use existing M1 FVGs (already detected above)
                    var matchingFVG = FindMatchingHighQualityFVG(m1Displacement);
                    if (matchingFVG != null)
                    {
                        var newZone = CreatePreZone(m1Displacement, matchingFVG);
                        if (newZone != null)
                        {
                            // Remove previous zone's visualization before replacing
                            RemoveZoneVisualization();
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

                // Note: M15 displacement detection removed - now handled on M1 bars above
                // This allows faster PRE-zone creation (within 1 minute instead of 15)

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

                        // Phase 4: Check if fractal confirms existing PRE-zone
                        bool skipFractalZoneCreation = false;
                        if (EnablePreZoneSystem && activeZone != null && activeZone.State == ZoneState.Pre)
                        {
                            double fractalPrice = currentMode == "SELL" ?
                                m15Bars.HighPrices[swingIndex] :
                                m15Bars.LowPrices[swingIndex];

                            double distanceToZone = Math.Abs(fractalPrice - activeZone.OriginPrice) / Symbol.PipSize;

                            if (distanceToZone <= FractalZoneTolerancePips)
                            {
                                // Fractal confirms PRE-zone - upgrade to VALID
                                UpgradeToValidZone(activeZone, swingIndex);
                                SyncZoneToLegacyVariables();

                                if (ShowRectangles)
                                {
                                    DrawZoneRectangle();  // Redraw with VALID state color
                                }

                                // Skip normal fractal zone creation (but continue with other M15 processing)
                                skipFractalZoneCreation = true;
                                Print("[FractalConfirm] ✅ Fractal confirms PRE-zone at {0:F5} | Distance: {1:F1} pips | UPGRADED to VALID",
                                    fractalPrice, distanceToZone);
                            }
                        }

                        // Only create fractal zone if not confirmed by PRE-zone
                        if (!skipFractalZoneCreation)
                        {
                            // UpdateSwingZone will set hasActiveSwing based on proximity check
                            UpdateSwingZone(swingIndex, currentMode);
                        }
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

            // Phase 4: Update zone states (expiry, arming, invalidation)
            if (EnablePreZoneSystem && activeZone != null)
            {
                UpdateZoneStates();
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
        /// Phase 3: Now includes 6 components with FVG alignment
        /// Validity (20%) + Extremity (25%) + Fractal (15%) + Session (20%) + FVG (15%) + Candle (5%)
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

            // Calculate all scoring components
            double extremityScore = CalculateExtremityScore(swingIndex, mode);
            double fractalStrength = CalculateFractalStrength(swingIndex, mode);
            double sessionAlignment = CalculateSessionAlignment(swingIndex, mode); // Phase 2
            double fvgAlignment = CalculateFVGAlignment(swingIndex, mode); // Phase 3
            double candleStrength = CalculateCandleStrength(swingIndex);

            // Phase 3: Use configurable weights (must total 1.0)
            double totalScore =
                (validityScore * WeightValidity) +
                (extremityScore * WeightExtremity) +
                (fractalStrength * WeightFractal) +
                (sessionAlignment * WeightSession) +
                (fvgAlignment * WeightFVG) +
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
        /// Session Alignment Score: NEW - Integrated with Advanced Session Mode
        /// BEST periods (green) = HIGH score (1.0)
        /// GOOD periods (gold) = GOOD score (0.7)
        /// NEUTRAL periods = Neutral score (0.5)
        /// DANGER periods (red) = NEGATIVE score (-0.5) ← PENALTY!
        /// Advanced Mode Implementation
        /// </summary>
        private double CalculateSessionAlignment(int swingIndex, string mode)
        {
            if (!EnableSessionFilter)
                return 0.5; // Neutral score if session filter disabled

            DateTime swingTime = m15Bars.OpenTimes[swingIndex];
            OptimalPeriod period = GetOptimalPeriod(swingTime);

            // BASE SCORE: Time-based scoring (primary factor)
            double baseScore;
            string periodLabel;

            switch (period)
            {
                case OptimalPeriod.BestOverlap:
                    baseScore = 1.0;
                    periodLabel = "🟢 BEST TIME (Overlap 13:00-17:00 UTC)";
                    break;

                case OptimalPeriod.GoodLondonOpen:
                    baseScore = 0.7;
                    periodLabel = "🟡 GOOD TIME (London 08:00-12:00 UTC)";
                    break;

                case OptimalPeriod.DangerDeadZone:
                    baseScore = -0.5; // NEGATIVE PENALTY!
                    periodLabel = "🔴 DANGER ZONE (Dead 04:00-08:00 UTC)";
                    break;

                case OptimalPeriod.DangerLateNY:
                    baseScore = -0.5; // NEGATIVE PENALTY!
                    periodLabel = "🔴 DANGER ZONE (Late NY 20:00-00:00 UTC)";
                    break;

                default: // OptimalPeriod.None
                    baseScore = 0.5;
                    periodLabel = "Neutral time";
                    break;
            }

            // BONUS: Check if swing is also at session high/low (extra confirmation)
            var session = GetSessionForTime(swingTime);
            double bonus = 0;

            if (session != null)
            {
                double swingPrice = mode == "SELL" ?
                    m15Bars.HighPrices[swingIndex] :
                    m15Bars.LowPrices[swingIndex];

                double distanceToSessionLevel = mode == "SELL" ?
                    Math.Abs(swingPrice - session.High) :
                    Math.Abs(swingPrice - session.Low);

                double threshold = 10 * Symbol.PipSize; // Within 10 pips

                if (distanceToSessionLevel <= threshold)
                {
                    // Only give bonus if base score is positive (not in danger zone)
                    if (baseScore > 0)
                    {
                        bonus = 0.3; // Extra bonus for being at session level
                        periodLabel += " + AT SESSION LEVEL";
                    }
                }
            }

            double finalScore = baseScore + bonus;

            // Cap final score between -0.5 and 1.3 (with bonus)
            finalScore = Math.Max(-0.5, Math.Min(1.3, finalScore));

            Print("[SessionAlign] {0} | Base:{1:F2} Bonus:{2:F2} Final:{3:F2}",
                periodLabel,
                baseScore,
                bonus,
                finalScore);

            return finalScore;
        }

        /// <summary>
        /// Gets session start time for a given session type
        /// Returns the start time based on current hour to handle day boundaries
        /// </summary>
        private DateTime GetSessionStartTime(TradingSession session, DateTime currentTime)
        {
            int hour = currentTime.Hour;
            DateTime today = currentTime.Date;

            switch (session)
            {
                case TradingSession.Asian:
                    // Asian: 00:00-09:00 UTC (use end time boundary)
                    return hour < 9 ? today.AddHours(0) : today.AddDays(1).AddHours(0);
                case TradingSession.London:
                    // London: 08:00-17:00 UTC (use end time boundary)
                    return hour < 17 ? today.AddHours(8) : today.AddDays(1).AddHours(8);
                case TradingSession.NewYork:
                    // NY: 13:00-22:00 UTC (use end time boundary)
                    return hour < 22 ? today.AddHours(13) : today.AddDays(1).AddHours(13);
                case TradingSession.Overlap:
                    // Overlap: 13:00-17:00 UTC (use end time boundary)
                    return hour < 17 ? today.AddHours(13) : today.AddDays(1).AddHours(13);
                default:
                    return currentTime;
            }
        }

        /// <summary>
        /// Gets session end time for a given session type
        /// Calculates based on session start + duration
        /// </summary>
        private DateTime GetSessionEndTime(TradingSession session, DateTime currentTime)
        {
            DateTime start = GetSessionStartTime(session, currentTime);

            switch (session)
            {
                case TradingSession.Asian:
                    return start.AddHours(9);  // 00:00 + 9 = 09:00
                case TradingSession.London:
                    return start.AddHours(9);  // 08:00 + 9 = 17:00
                case TradingSession.NewYork:
                    return start.AddHours(9);  // 13:00 + 9 = 22:00
                case TradingSession.Overlap:
                    return start.AddHours(4);  // 13:00 + 4 = 17:00
                default:
                    return currentTime;
            }
        }

        /// <summary>
        /// Gets optimal period start time (Advanced Mode)
        /// Returns start time based on current hour to handle day boundaries
        /// </summary>
        private DateTime GetOptimalPeriodStart(OptimalPeriod period, DateTime currentTime)
        {
            int hour = currentTime.Hour;
            DateTime today = currentTime.Date;

            switch (period)
            {
                case OptimalPeriod.BestOverlap:
                    // 13:00-17:00 UTC (use end time 17:00 as boundary)
                    return hour < 17 ? today.AddHours(13) : today.AddDays(1).AddHours(13);
                case OptimalPeriod.GoodLondonOpen:
                    // 08:00-12:00 UTC (use end time 12:00 as boundary)
                    return hour < 12 ? today.AddHours(8) : today.AddDays(1).AddHours(8);
                case OptimalPeriod.DangerDeadZone:
                    // 04:00-08:00 UTC (use end time 08:00 as boundary)
                    return hour < 8 ? today.AddHours(4) : today.AddDays(1).AddHours(4);
                case OptimalPeriod.DangerLateNY:
                    // 20:00-00:00 UTC (crosses midnight - special handling)
                    // If hour >= 20, we're in the current period (use today)
                    // If hour < 20, we're after period end (use yesterday's period)
                    return hour >= 20 ? today.AddHours(20) : today.AddDays(-1).AddHours(20);
                default:
                    return currentTime;
            }
        }

        /// <summary>
        /// Gets optimal period end time (Advanced Mode)
        /// Calculates based on period start + duration
        /// </summary>
        private DateTime GetOptimalPeriodEnd(OptimalPeriod period, DateTime currentTime)
        {
            DateTime start = GetOptimalPeriodStart(period, currentTime);

            switch (period)
            {
                case OptimalPeriod.BestOverlap:
                    return start.AddHours(4);  // 13:00 + 4 = 17:00
                case OptimalPeriod.GoodLondonOpen:
                    return start.AddHours(4);  // 08:00 + 4 = 12:00
                case OptimalPeriod.DangerDeadZone:
                    return start.AddHours(4);  // 04:00 + 4 = 08:00
                case OptimalPeriod.DangerLateNY:
                    return start.AddHours(4);  // 20:00 + 4 = 24:00 (00:00 next day)
                default:
                    return currentTime;
            }
        }

        /// <summary>
        /// Draws visual session box on chart (live at period start)
        /// Phase 2 Implementation - Session Visualization
        /// </summary>
        private void DrawSessionBox(string periodName, DateTime startTime, DateTime endTime, Color boxColor)
        {
            if (!ShowSessionBoxes)
                return;

            // Create unique name for this session box
            string boxName = string.Format("Session_{0}_{1}",
                periodName,
                startTime.ToString("yyyyMMddHH"));

            // Check if box already exists (prevents duplicates on bot restart)
            var existingBox = Chart.FindObject(boxName);
            if (existingBox != null)
                return;  // Box already drawn, skip

            // Calculate very large price range to ensure full chart coverage
            // Using current price ± large pip buffer (e.g., 10000 pips)
            double priceBuffer = 10000 * Symbol.PipSize;  // ~1.00 for 5-digit pairs like EURUSD
            double currentPrice = m15Bars.ClosePrices.LastValue;
            double chartTop = currentPrice + priceBuffer;
            double chartBottom = currentPrice - priceBuffer;

            // Draw full-height box
            var box = Chart.DrawRectangle(
                boxName,
                startTime,
                chartTop,      // Very high price (full height)
                endTime,
                chartBottom,   // Very low price (full height)
                boxColor);

            // Configure box appearance
            box.IsFilled = true;            // Filled with color
            box.IsInteractive = false;      // Don't allow manual editing
            box.ZIndex = -1;                // Behind other objects (swing rectangles on top)
            box.Comment = string.Format("{0} | {1} - {2} UTC",
                periodName,
                startTime.ToString("HH:mm"),
                endTime.ToString("HH:mm"));

            Print("[SessionBox] Drew {0} | {1} - {2}",
                periodName,
                startTime.ToString("HH:mm"),
                endTime.ToString("HH:mm"));
        }

        /// <summary>
        /// Returns color for session type
        /// Phase 2 Implementation
        /// </summary>
        private Color GetSessionColor(TradingSession session)
        {
            switch (session)
            {
                case TradingSession.Asian:
                    return ColorAsian;
                case TradingSession.London:
                    return ColorLondon;
                case TradingSession.NewYork:
                    return ColorNewYork;
                case TradingSession.Overlap:
                    return ColorOverlap;
                default:
                    return Color.Gray;
            }
        }

        /// <summary>
        /// Returns color for optimal period type (Advanced Mode)
        /// Maps periods to their priority colors (Green/Gold/Red)
        /// </summary>
        private Color GetOptimalPeriodColor(OptimalPeriod period)
        {
            switch (period)
            {
                case OptimalPeriod.BestOverlap:
                    return ColorBestTime;
                case OptimalPeriod.GoodLondonOpen:
                    return ColorGoodTime;
                case OptimalPeriod.DangerDeadZone:
                case OptimalPeriod.DangerLateNY:
                    return ColorDangerZone;
                default:
                    return Color.Gray;
            }
        }

        #endregion

        #region Phase 3: FVG Detection

        /// <summary>
        /// Detects Fair Value Gaps (FVGs) on M1 timeframe for precise entry detection
        /// ENHANCED: Scans M1 bars on every bar close for real-time FVG tracking
        /// Phase 4 Enhancement - Switched from M15 to M1 for better precision
        /// </summary>
        private void DetectFVGs()
        {
            int previousFVGCount = activeFVGs.Count;
            activeFVGs.Clear();

            // M1 lookback: Focus on RECENT FVGs only (last 30 minutes)
            // Older FVGs are likely already filled or no longer relevant for scalping entries
            int m1Lookback = Math.Min(30, Bars.Count - 3);  // Max 30 M1 bars (~30 minutes)

            // M1 minimum size: Very small threshold for M1 gaps
            // M1 FVGs are naturally smaller than M15 FVGs
            double m1MinFVGSizePips = 0.1;  // 0.1 pips minimum (~1 point on EURUSD)

            int fvgsFound = 0;
            int fvgsTooSmall = 0;
            int fvgsFilled = 0;

            // Start at i=2 to ensure all 3 candles (A, B, C) are completed
            for (int i = 2; i < m1Lookback - 1; i++)
            {
                int idx = Bars.Count - 1 - i;  // Impulse candle index (Candle B)

                if (idx - 1 < 0 || idx + 1 >= Bars.Count)
                    continue;

                double candleA_High = Bars.HighPrices[idx - 1];
                double candleA_Low = Bars.LowPrices[idx - 1];
                double candleC_High = Bars.HighPrices[idx + 1];
                double candleC_Low = Bars.LowPrices[idx + 1];

                // Check if Candle B (impulse) is a strong move (displacement-like on M1)
                double candleB_Body = Math.Abs(Bars.ClosePrices[idx] - Bars.OpenPrices[idx]);
                double candleB_Range = Bars.HighPrices[idx] - Bars.LowPrices[idx];
                bool isStrongMove = candleB_Range > 0 && (candleB_Body / candleB_Range) > 0.6;

                // BULLISH FVG: Candle A's HIGH is BELOW Candle C's LOW
                if (candleA_High < candleC_Low)
                {
                    fvgsFound++;
                    double gapSize = candleC_Low - candleA_High;
                    double gapSizePips = gapSize / Symbol.PipSize;

                    // Minimum size filter for M1
                    if (gapSizePips < m1MinFVGSizePips)
                    {
                        fvgsTooSmall++;
                        continue;
                    }

                    var fvg = new FairValueGap
                    {
                        Time = Bars.OpenTimes[idx],
                        BottomPrice = candleA_High,
                        TopPrice = candleC_Low,
                        IsBullish = true,
                        IsFilled = false,
                        IsHighQuality = isStrongMove && gapSizePips >= (m1MinFVGSizePips * 3),
                        GapSizeInPips = gapSizePips,
                        DisplacementBarIndex = isStrongMove ? idx : -1
                    };

                    fvg.IsFilled = IsFVGFilledM1(fvg, idx + 1);

                    if (fvg.IsFilled)
                    {
                        fvgsFilled++;
                    }
                    else
                    {
                        activeFVGs.Add(fvg);
                    }
                }

                // BEARISH FVG: Candle A's LOW is ABOVE Candle C's HIGH
                if (candleA_Low > candleC_High)
                {
                    fvgsFound++;
                    double gapSize = candleA_Low - candleC_High;
                    double gapSizePips = gapSize / Symbol.PipSize;

                    // Minimum size filter for M1
                    if (gapSizePips < m1MinFVGSizePips)
                    {
                        fvgsTooSmall++;
                        continue;
                    }

                    var fvg = new FairValueGap
                    {
                        Time = Bars.OpenTimes[idx],
                        TopPrice = candleA_Low,
                        BottomPrice = candleC_High,
                        IsBullish = false,
                        IsFilled = false,
                        IsHighQuality = isStrongMove && gapSizePips >= (m1MinFVGSizePips * 3),
                        GapSizeInPips = gapSizePips,
                        DisplacementBarIndex = isStrongMove ? idx : -1
                    };

                    fvg.IsFilled = IsFVGFilledM1(fvg, idx + 1);

                    if (fvg.IsFilled)
                    {
                        fvgsFilled++;
                    }
                    else
                    {
                        activeFVGs.Add(fvg);
                    }
                }
            }

            int highQualityCount = activeFVGs.Count(f => f.IsHighQuality);

            // Log every 15 M1 bars OR when FVG count changes
            bool shouldLog = (Bars.Count % 15 == 0) || (activeFVGs.Count != previousFVGCount) || (highQualityCount > 0);

            if (shouldLog)
            {
                Print("[M1 FVG] Scanned:{0} | Found:{1} | TooSmall:{2} | Filled:{3} | Active:{4} | HQ:{5}",
                    m1Lookback, fvgsFound, fvgsTooSmall, fvgsFilled, activeFVGs.Count, highQualityCount);

                // Log details of high-quality FVGs
                foreach (var fvg in activeFVGs.Where(f => f.IsHighQuality))
                {
                    Print("[M1 FVG] {0} gap | Zone: {1:F5} - {2:F5} | Size: {3:F1} pips | Time: {4:HH:mm}",
                        fvg.IsBullish ? "Bullish" : "Bearish",
                        fvg.BottomPrice, fvg.TopPrice, fvg.GapSizeInPips, fvg.Time);
                }
            }
        }

        /// <summary>
        /// Detects Fair Value Gaps (FVGs) on M1 timeframe for PRE-zone system
        /// Provides better entry precision than M15 FVGs
        /// Phase 4 Enhancement - M1 FVG Detection
        /// </summary>
        private void DetectM1FVGs()
        {
            if (!EnablePreZoneSystem)
                return;

            // Clear old M1 FVGs
            activeFVGs.Clear();

            // Scan recent M1 bars for FVG patterns
            int lookback = Math.Min(90, Bars.Count - 3); // Last 90 M1 bars = 1.5 hours

            int fvgsDetected = 0;
            int fvgsFiltered = 0;
            int fvgsFilled = 0;
            int fvgsHighQuality = 0;

            Print("[M1 FVG] Scanning {0} M1 bars | Disp: {1} @ {2}",
                lookback,
                lastDisplacement?.IsBullish == true ? "BULL" : (lastDisplacement?.IsBullish == false ? "BEAR" : "NONE"),
                lastDisplacement?.Time.ToString("HH:mm") ?? "N/A");

            // Start at i=2 to ensure all 3 candles (A, B, C) are completed
            for (int i = 2; i < lookback; i++)
            {
                int idx = Bars.Count - 1 - i;  // Candle B index

                if (idx - 1 < 0 || idx + 1 >= Bars.Count)
                    continue;

                // Check if this M1 candle falls within a recent M15 displacement window
                DateTime candleBTime = Bars.OpenTimes[idx];
                bool isWithinDisplacement = IsWithinDisplacementWindow(candleBTime);

                // Get candle prices
                double candleA_High = Bars.HighPrices[idx - 1];
                double candleA_Low = Bars.LowPrices[idx - 1];
                double candleC_High = Bars.HighPrices[idx + 1];
                double candleC_Low = Bars.LowPrices[idx + 1];

                // BULLISH FVG: Candle A's HIGH is BELOW Candle C's LOW
                if (candleA_High < candleC_Low)
                {
                    double gapSize = candleC_Low - candleA_High;
                    double gapSizePips = gapSize / Symbol.PipSize;

                    fvgsDetected++;

                    // Minimum size filter (M1 uses 1/3 of M15 threshold for smaller gaps)
                    double m1MinSize = MinFVGSizePips / 3.0;  // e.g., 1.5 / 3 = 0.5 pips
                    if (gapSizePips < m1MinSize)
                    {
                        fvgsFiltered++;
                        continue;
                    }

                    var fvg = new FairValueGap
                    {
                        Time = candleBTime,
                        BottomPrice = candleA_High,
                        TopPrice = candleC_Low,
                        IsBullish = true,
                        IsFilled = false,
                        IsHighQuality = isWithinDisplacement,
                        GapSizeInPips = gapSizePips,
                        DisplacementBarIndex = isWithinDisplacement ? idx : -1
                    };

                    if (isWithinDisplacement)
                        fvgsHighQuality++;

                    // Check if filled
                    fvg.IsFilled = IsFVGFilledM1(fvg, idx + 1);

                    if (fvg.IsFilled)
                    {
                        fvgsFilled++;
                    }
                    else
                    {
                        activeFVGs.Add(fvg);
                    }
                }

                // BEARISH FVG: Candle A's LOW is ABOVE Candle C's HIGH
                if (candleA_Low > candleC_High)
                {
                    double gapSize = candleA_Low - candleC_High;
                    double gapSizePips = gapSize / Symbol.PipSize;

                    fvgsDetected++;

                    // Minimum size filter (M1 uses 1/3 of M15 threshold for smaller gaps)
                    double m1MinSize = MinFVGSizePips / 3.0;  // e.g., 1.5 / 3 = 0.5 pips
                    if (gapSizePips < m1MinSize)
                    {
                        fvgsFiltered++;
                        continue;
                    }

                    var fvg = new FairValueGap
                    {
                        Time = candleBTime,
                        TopPrice = candleA_Low,
                        BottomPrice = candleC_High,
                        IsBullish = false,
                        IsFilled = false,
                        IsHighQuality = isWithinDisplacement,
                        GapSizeInPips = gapSizePips,
                        DisplacementBarIndex = isWithinDisplacement ? idx : -1
                    };

                    if (isWithinDisplacement)
                        fvgsHighQuality++;

                    fvg.IsFilled = IsFVGFilledM1(fvg, idx + 1);

                    if (fvg.IsFilled)
                    {
                        fvgsFilled++;
                    }
                    else
                    {
                        activeFVGs.Add(fvg);
                    }
                }
            }

            Print("[M1 FVG] Results: Detected={0} | Filtered={1} | Filled={2} | HighQuality={3} | Active={4}",
                fvgsDetected, fvgsFiltered, fvgsFilled, fvgsHighQuality, activeFVGs.Count);
        }

        /// <summary>
        /// Checks if M1 candle time falls within the last M15 displacement window
        /// Phase 4 Enhancement
        /// </summary>
        private bool IsWithinDisplacementWindow(DateTime m1Time)
        {
            if (lastDisplacement == null)
                return false;

            // Get the M15 bar time range for the displacement
            DateTime dispStart = lastDisplacement.Time;
            DateTime dispEnd = dispStart.AddMinutes(15);

            // Check if M1 candle falls within this 15-minute window
            return m1Time >= dispStart && m1Time < dispEnd;
        }

        /// <summary>
        /// Checks if M1 FVG has been FULLY filled (invalidated)
        /// FVG is only considered filled if price passes THROUGH the entire gap
        /// Partial fills (mitigations) keep the FVG active for trading
        /// </summary>
        private bool IsFVGFilledM1(FairValueGap fvg, int startIdx)
        {
            // Check bars after FVG creation up to (but not including) current bar
            int endIdx = Bars.Count - 2;

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (fvg.IsBullish)
                {
                    // Bullish FVG FULLY filled only if price drops THROUGH entire gap
                    // (price low goes below the bottom of the gap)
                    if (Bars.LowPrices[i] <= fvg.BottomPrice)
                        return true;
                }
                else
                {
                    // Bearish FVG FULLY filled only if price rises THROUGH entire gap
                    // (price high goes above the top of the gap)
                    if (Bars.HighPrices[i] >= fvg.TopPrice)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if an FVG has been filled by price action after creation
        /// FVG is filled when price returns to cover the gap
        /// Phase 3 Implementation
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

        /// <summary>
        /// Calculates how well the swing aligns with Fair Value Gaps
        /// Returns higher score if swing is within or near FVG zones
        /// Phase 3 Implementation
        /// </summary>
        private double CalculateFVGAlignment(int swingIndex, string mode)
        {
            if (!EnableFVGFilter || activeFVGs.Count == 0)
                return 0.5; // Neutral score if FVG filter disabled or no FVGs

            double swingPrice = mode == "SELL" ?
                m15Bars.HighPrices[swingIndex] :
                m15Bars.LowPrices[swingIndex];

            foreach (var fvg in activeFVGs)
            {
                // Check if swing price within FVG zone
                if (swingPrice >= fvg.BottomPrice && swingPrice <= fvg.TopPrice)
                {
                    Print("[FVGAlign] Swing at {0} FVG | Price: {1:F5} in zone {2:F5}-{3:F5} | STRONG",
                        fvg.IsBullish ? "Bullish" : "Bearish",
                        swingPrice, fvg.BottomPrice, fvg.TopPrice);
                    return 1.0; // Strong alignment - swing within FVG
                }

                // Check if near FVG (within 5 pips)
                double distanceToFVG = Math.Min(
                    Math.Abs(swingPrice - fvg.TopPrice),
                    Math.Abs(swingPrice - fvg.BottomPrice)
                );

                if (distanceToFVG <= 5 * Symbol.PipSize)
                {
                    Print("[FVGAlign] Swing near FVG | Distance: {0:F1} pips | MODERATE",
                        distanceToFVG / Symbol.PipSize);
                    return 0.7; // Near FVG
                }
            }

            return 0.3; // No FVG alignment - lower score
        }

        #endregion

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
        /// Detects displacement on M1 timeframe for faster PRE-zone creation
        /// Displacement = Body Size >= ATRMultiplier × ATR (on M1 bars)
        /// Phase 4 Implementation - M1 Fast Detection
        /// </summary>
        private DisplacementCandle DetectM1Displacement()
        {
            if (!EnablePreZoneSystem || atrM1 == null)
                return null;

            int lastIdx = Bars.Count - 2;  // Last completed M1 bar
            if (lastIdx < 1)
                return null;

            // Calculate body size on M1 bar
            double open = Bars.OpenPrices[lastIdx];
            double close = Bars.ClosePrices[lastIdx];
            double high = Bars.HighPrices[lastIdx];
            double low = Bars.LowPrices[lastIdx];
            double bodySize = Math.Abs(close - open);

            // Get M1 ATR value
            double atrValue = atrM1.Result[lastIdx];
            if (atrValue <= 0)
                return null;

            // Check displacement threshold (same multiplier as M15)
            double atrMultiple = bodySize / atrValue;
            if (atrMultiple < ATRMultiplier)
                return null;

            // M1 Displacement detected!
            bool isBullish = close > open;
            double originPrice = isBullish ? low : high;
            double bodySizePips = bodySize / Symbol.PipSize;

            var displacement = new DisplacementCandle
            {
                BarIndex = lastIdx,
                Time = Bars.OpenTimes[lastIdx],
                ImpulseSize = bodySizePips,
                ATRMultiple = atrMultiple,
                IsBullish = isBullish,
                OriginPrice = originPrice
            };

            Print("[M1 Displacement] {0} impulse at {1:HH:mm} | Size: {2:F1} pips | ATR x {3:F1}",
                isBullish ? "Bullish" : "Bearish",
                displacement.Time,
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

        /// <summary>
        /// Creates a PRE-zone from displacement + high-quality FVG
        /// Zone is created immediately (15 min faster than fractal)
        /// Phase 4 Implementation
        /// </summary>
        private TradingZone CreatePreZone(DisplacementCandle displacement, FairValueGap fvg)
        {
            // Block zone creation during danger zones
            if (EnableSessionFilter)
            {
                OptimalPeriod currentPeriod = GetOptimalPeriod(displacement.Time);
                if (currentPeriod == OptimalPeriod.DangerDeadZone || currentPeriod == OptimalPeriod.DangerLateNY)
                {
                    Print("[PRE-Zone] Blocked | Danger zone ({0}) - no zone creation allowed",
                        currentPeriod == OptimalPeriod.DangerDeadZone ? "04:00-08:00 UTC" : "20:00-00:00 UTC");
                    return null;
                }
            }

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
                    string previousState = (activeZone.State == ZoneState.Pre) ? "PRE-Zone" : "VALID-Zone";
                    activeZone.State = ZoneState.Expired;
                    Print("[Zone] Expired | No entry triggered | Was: {0} at {1:F5}",
                        previousState,
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

        /// <summary>
        /// Finds a high-quality FVG that matches the displacement direction
        /// For M1 displacement, looks for FVGs within last 5 minutes
        /// Returns the largest gap by pip size
        /// Phase 4 Implementation - Updated for M1 timing
        /// </summary>
        private FairValueGap FindMatchingHighQualityFVG(DisplacementCandle displacement)
        {
            // For M1 displacement, look for FVGs within last 5 minutes
            DateTime cutoffTime = displacement.Time.AddMinutes(-5);

            var matchingFVGs = activeFVGs
                .Where(f => f.IsBullish == displacement.IsBullish)
                .Where(f => f.IsHighQuality || f.Time >= cutoffTime)  // Recent or high-quality
                .OrderByDescending(f => f.GapSizeInPips)
                .ToList();

            if (matchingFVGs.Count == 0)
            {
                Print("[PRE-Zone] No matching FVG for {0} M1 displacement",
                    displacement.IsBullish ? "Bullish" : "Bearish");
                return null;
            }

            return matchingFVGs[0];  // Return largest gap
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
        /// Detects optimal trading period based on UTC hour
        /// Advanced Mode Implementation
        /// </summary>
        private OptimalPeriod GetOptimalPeriod(DateTime time)
        {
            // Robot is configured with TimeZone = TimeZones.UTC, so bar times are already in UTC
            // No conversion needed - use the hour directly
            int hourUTC = time.Hour;

            // BEST: Overlap period (13:00-17:00 UTC) - London + NY
            if (hourUTC >= 13 && hourUTC < 17)
                return OptimalPeriod.BestOverlap;

            // GOOD: London open to midday (08:00-12:00 UTC)
            if (hourUTC >= 8 && hourUTC < 12)
                return OptimalPeriod.GoodLondonOpen;

            // DANGER: Dead zone (04:00-08:00 UTC) - Lowest volatility
            if (hourUTC >= 4 && hourUTC < 8)
                return OptimalPeriod.DangerDeadZone;

            // DANGER: Late NY (20:00-00:00 UTC) - Dying volume
            if (hourUTC >= 20 || hourUTC < 0)
                return OptimalPeriod.DangerLateNY;

            // Other times: neutral (not optimal, not dangerous)
            return OptimalPeriod.None;
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

            // === VISUAL TRACKING (New behavior - draw at period start) ===
            if (ShowSessionBoxes)
            {
                if (SessionBoxDisplayMode == SessionBoxMode.Basic)
                {
                    // Detect current session
                    TradingSession primarySession = currentPrimarySession;

                    // If NEW session detected (and not None)
                    if (primarySession != lastDrawnSession && primarySession != TradingSession.None)
                    {
                        // Draw session box immediately
                        DateTime sessionStart = GetSessionStartTime(primarySession, currentTime);
                        DateTime sessionEnd = GetSessionEndTime(primarySession, currentTime);
                        Color sessionColor = GetSessionColor(primarySession);

                        DrawSessionBox(primarySession.ToString(), sessionStart, sessionEnd, sessionColor);

                        // Update tracking to prevent duplicate drawing
                        lastDrawnSession = primarySession;
                    }

                    // If session ends (transitions to None), reset tracking
                    if (primarySession == TradingSession.None && lastDrawnSession != TradingSession.None)
                    {
                        lastDrawnSession = TradingSession.None;
                    }
                }
                else // Advanced Mode
                {
                    // Detect current optimal period
                    OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);

                    // If NEW period detected (and not None)
                    if (currentPeriod != lastDrawnPeriod && currentPeriod != OptimalPeriod.None)
                    {
                        // Draw optimal period box immediately
                        DateTime periodStart = GetOptimalPeriodStart(currentPeriod, currentTime);
                        DateTime periodEnd = GetOptimalPeriodEnd(currentPeriod, currentTime);
                        Color periodColor = GetOptimalPeriodColor(currentPeriod);

                        DrawSessionBox(currentPeriod.ToString(), periodStart, periodEnd, periodColor);

                        // Update tracking to prevent duplicate drawing
                        lastDrawnPeriod = currentPeriod;
                    }

                    // If period ends (changes to None), reset tracking
                    if (currentPeriod == OptimalPeriod.None && lastDrawnPeriod != OptimalPeriod.None)
                    {
                        lastDrawnPeriod = OptimalPeriod.None;
                    }
                }
            }

            // === Existing session boundary detection continues below ===

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
            // Block zone creation during danger zones (check CURRENT time, not swing time)
            if (EnableSessionFilter)
            {
                DateTime currentTime = m15Bars.OpenTimes.LastValue;
                OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);
                if (currentPeriod == OptimalPeriod.DangerDeadZone || currentPeriod == OptimalPeriod.DangerLateNY)
                {
                    Print("[SwingZone] Blocked | Danger zone ({0}) at {1:HH:mm} - no zone creation allowed",
                        currentPeriod == OptimalPeriod.DangerDeadZone ? "04:00-08:00 UTC" : "20:00-00:00 UTC",
                        currentTime);
                    return;
                }
            }

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
            DateTime zoneCreatedTime = m15Bars.OpenTimes.LastValue;
            rectangleExpiryTime = zoneCreatedTime.AddMinutes(RectangleWidthMinutes);

            Print("[SwingZone] {0} Mode | Top: {1:F5} | Bottom: {2:F5} | Height: {3:F1} pips",
                mode, swingTopPrice, swingBottomPrice, heightPips);
            Print("[SwingZone] Created: {0} | Expires: {1} ({2} min window)",
                zoneCreatedTime, rectangleExpiryTime, RectangleWidthMinutes);

            // Remove previous zone's visualization before creating new one
            RemoveZoneVisualization();

            // Create activeZone for visualization (fractal-based zone)
            activeZone = new TradingZone
            {
                Id = TradingZone.GenerateId(zoneCreatedTime, mode),
                State = ZoneState.Valid,  // Fractal-based zones start as VALID
                TopPrice = swingTopPrice,
                BottomPrice = swingBottomPrice,
                OriginPrice = mode == "SELL" ? swingTopPrice : swingBottomPrice,
                CreatedTime = zoneCreatedTime,
                ExpiryTime = rectangleExpiryTime,
                Mode = mode,
                FractalBarIndex = swingIndex
            };

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

            // Draw rectangle on chart using zone system
            if (ShowRectangles)
            {
                DrawZoneRectangle();
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

            // Check if we're in a danger zone - NO TRADING during these periods
            DateTime currentTime = Bars.OpenTimes.LastValue;
            if (EnableSessionFilter)
            {
                OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);
                if (currentPeriod == OptimalPeriod.DangerDeadZone || currentPeriod == OptimalPeriod.DangerLateNY)
                {
                    // Don't log every bar - only log once when entering danger zone
                    return;
                }
            }

            // FIX Issue 3: Check if rectangle has expired (time-based cutoff)
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

        #region Chandelier Trailing Stop Methods

        /// <summary>
        /// Gets commission cost in price terms for breakeven calculation
        /// </summary>
        private double GetCommissionInPrice(Position position)
        {
            // Symbol.Commission is per-lot per-side in account currency
            double commissionPerLot = Symbol.Commission * 2;  // Round trip
            if (commissionPerLot <= 0) return 0;

            // Convert to price movement
            double commissionInPrice = (commissionPerLot / position.VolumeInUnits) * Symbol.LotSize;
            return commissionInPrice;
        }

        /// <summary>
        /// Calculates the chandelier stop loss value
        /// </summary>
        private double CalculateChandelierSL(TradeType tradeType)
        {
            int lookback = Math.Min(ChandelierLookback, Bars.Count - 1);
            if (lookback < 1) return 0;

            double atrValue = atrM1.Result.LastValue;
            double atrDistance = atrValue * ATRMultiplier;

            if (tradeType == TradeType.Buy)
            {
                // LONG: Highest High - ATR
                double highestHigh = 0;
                for (int i = 1; i <= lookback; i++)
                {
                    if (Bars.HighPrices.Last(i) > highestHigh)
                        highestHigh = Bars.HighPrices.Last(i);
                }
                return highestHigh - atrDistance;
            }
            else
            {
                // SHORT: Lowest Low + ATR
                double lowestLow = double.MaxValue;
                for (int i = 1; i <= lookback; i++)
                {
                    if (Bars.LowPrices.Last(i) < lowestLow)
                        lowestLow = Bars.LowPrices.Last(i);
                }
                return lowestLow + atrDistance;
            }
        }

        /// <summary>
        /// Processes chandelier trailing stop for all tracked positions
        /// Called from OnBar()
        /// </summary>
        private void ProcessChandelierStops()
        {
            if (!EnableChandelierSL) return;

            // Get positions opened by this bot
            var myPositions = Positions.Where(p => p.Label == MagicNumber.ToString()).ToList();

            // Clean up states for closed positions
            var closedIds = _chandelierStates.Keys.Where(id => !myPositions.Any(p => p.Id == id)).ToList();
            foreach (var id in closedIds)
            {
                _chandelierStates.Remove(id);
            }

            // Process each open position
            foreach (var position in myPositions)
            {
                if (!_chandelierStates.TryGetValue(position.Id, out var state))
                    continue;  // Position not tracked (opened before bot start)

                ProcessSinglePosition(position, state);
            }
        }

        /// <summary>
        /// Processes chandelier logic for a single position
        /// </summary>
        private void ProcessSinglePosition(Position position, ChandelierState state)
        {
            double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;

            // Phase 1: Check for activation
            if (!state.IsActivated)
            {
                bool shouldActivate = position.TradeType == TradeType.Buy
                    ? currentPrice >= state.ActivationPrice
                    : currentPrice <= state.ActivationPrice;

                if (shouldActivate)
                {
                    ActivateChandelier(position, state);
                }
                return;  // Don't trail until activated
            }

            // Phase 2 & 3: Trail the stop
            TrailChandelierStop(position, state);
        }

        /// <summary>
        /// Activates chandelier mode - moves SL to BE+commission
        /// </summary>
        private void ActivateChandelier(Position position, ChandelierState state)
        {
            state.IsActivated = true;
            state.CurrentTrailingSL = state.BreakevenPrice;
            state.HighestTrailingSL = state.BreakevenPrice;

            // Determine new TP based on mode
            double? newTP = null;
            if (ChandelierTPModeSelection == ChandelierTPMode.RemoveTP)
            {
                newTP = null;  // Remove TP
            }
            else
            {
                newTP = state.OriginalTP;  // Keep original for now
            }

            // Modify position
            ModifyPosition(position, state.CurrentTrailingSL, newTP);

            Print("[CHANDELIER] Position {0} activated at {1:F5}, SL moved to BE+comm: {2:F5}",
                position.Id, position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask, state.CurrentTrailingSL);
        }

        /// <summary>
        /// Trails the chandelier stop loss (and optionally TP)
        /// </summary>
        private void TrailChandelierStop(Position position, ChandelierState state)
        {
            double chandelierSL = CalculateChandelierSL(position.TradeType);
            if (chandelierSL <= 0) return;

            bool isBuy = position.TradeType == TradeType.Buy;
            double newSL = state.CurrentTrailingSL;
            double? newTP = position.TakeProfit;

            // Check if chandelier provides a better SL
            bool chandelierBetter = isBuy
                ? chandelierSL > state.HighestTrailingSL
                : chandelierSL < state.HighestTrailingSL;

            if (chandelierBetter)
            {
                // Check minimum movement threshold (0.5 pips)
                double movement = Math.Abs(chandelierSL - state.CurrentTrailingSL) / Symbol.PipSize;
                if (movement >= 0.5)
                {
                    double oldSL = state.CurrentTrailingSL;
                    state.CurrentTrailingSL = chandelierSL;
                    state.HighestTrailingSL = chandelierSL;
                    newSL = chandelierSL;

                    Print("[CHANDELIER] Position {0} SL trailed: {1:F5} → {2:F5}",
                        position.Id, oldSL, newSL);

                    // Start TP trailing if mode is TrailingTP and SL moved beyond BE
                    if (ChandelierTPModeSelection == ChandelierTPMode.TrailingTP && !state.TPTrailingStarted)
                    {
                        bool beyondBE = isBuy
                            ? chandelierSL > state.BreakevenPrice
                            : chandelierSL < state.BreakevenPrice;

                        if (beyondBE)
                        {
                            state.TPTrailingStarted = true;
                            Print("[CHANDELIER] Position {0} TP trailing started", position.Id);
                        }
                    }

                    // Trail TP if enabled and started
                    if (state.TPTrailingStarted && ChandelierTPModeSelection == ChandelierTPMode.TrailingTP)
                    {
                        double trailingTP = isBuy
                            ? chandelierSL + (TrailingTPOffset * Symbol.PipSize)
                            : chandelierSL - (TrailingTPOffset * Symbol.PipSize);

                        // TP only moves in favorable direction
                        bool tpBetter = isBuy
                            ? trailingTP > state.HighestTrailingTP
                            : trailingTP < state.HighestTrailingTP;

                        if (tpBetter || state.HighestTrailingTP == 0)
                        {
                            double oldTP = state.CurrentTrailingTP;
                            state.CurrentTrailingTP = trailingTP;
                            state.HighestTrailingTP = trailingTP;
                            newTP = trailingTP;

                            Print("[CHANDELIER] Position {0} TP trailed: {1:F5} → {2:F5}",
                                position.Id, oldTP, newTP);
                        }
                    }

                    // Apply modifications
                    ModifyPosition(position, newSL, newTP);
                }
            }
        }

        #endregion

        #region Visualization Methods

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

            //Print("[ModeDisplay] Updated to: {0}", modeText);
        }

        /// <summary>
        /// Removes the current zone's rectangle and label from the chart
        /// Call this before replacing activeZone with a new zone
        /// </summary>
        private void RemoveZoneVisualization()
        {
            if (activeZone == null)
                return;

            string rectName = $"ZoneRect_{activeZone.Id}";
            string labelName = $"ZoneLabel_{activeZone.Id}";

            if (Chart.FindObject(rectName) != null)
                Chart.RemoveObject(rectName);
            if (Chart.FindObject(labelName) != null)
                Chart.RemoveObject(labelName);
        }

        /// <summary>
        /// Draws zone rectangle with state-based coloring (PRE=Yellow, VALID=Blue, ARMED=Green)
        /// </summary>
        private void DrawZoneRectangle()
        {
            if (activeZone == null || !ShowRectangles)
                return;

            // Remove current zone's rectangle if it exists (for redrawing with new state)
            string oldRectName = $"ZoneRect_{activeZone.Id}";
            if (Chart.FindObject(oldRectName) != null)
            {
                Chart.RemoveObject(oldRectName);
            }
            string oldLabelName = $"ZoneLabel_{activeZone.Id}";
            if (Chart.FindObject(oldLabelName) != null)
            {
                Chart.RemoveObject(oldLabelName);
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

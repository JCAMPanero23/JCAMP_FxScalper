using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Linq;
using System.Collections.Generic;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class JCAMP_FxScalper_V2 : Robot
    {
        // ================= RISK MANAGEMENT =================

        [Parameter("Risk %", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0, Step = 0.1)]
        public double RiskPercent { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 3.0, MinValue = 1.0, MaxValue = 10.0, Step = 0.5)]
        public double MaxDailyLoss { get; set; }

        [Parameter("Max Positions", DefaultValue = 1, MinValue = 1, MaxValue = 5)]
        public int MaxGlobalPositions { get; set; }

        // ================= SESSION SETTINGS =================

        [Parameter("Trade London Session", DefaultValue = true)]
        public bool TradeLondon { get; set; }

        [Parameter("London Start (GMT)", DefaultValue = 8, MinValue = 0, MaxValue = 23)]
        public int LondonStart { get; set; }

        [Parameter("London End (GMT)", DefaultValue = 16, MinValue = 0, MaxValue = 23)]
        public int LondonEnd { get; set; }

        [Parameter("Trade New York Session", DefaultValue = false)]
        public bool TradeNewYork { get; set; }

        [Parameter("NY Start (GMT)", DefaultValue = 13, MinValue = 0, MaxValue = 23)]
        public int NewYorkStart { get; set; }

        [Parameter("NY End (GMT)", DefaultValue = 21, MinValue = 0, MaxValue = 23)]
        public int NewYorkEnd { get; set; }

        [Parameter("Trade Asian Session", DefaultValue = false)]
        public bool TradeAsian { get; set; }

        [Parameter("Asian Start (GMT)", DefaultValue = 23, MinValue = 0, MaxValue = 23)]
        public int AsianStart { get; set; }

        [Parameter("Asian End (GMT)", DefaultValue = 8, MinValue = 0, MaxValue = 23)]
        public int AsianEnd { get; set; }

        // ================= INDICATORS =================

        [Parameter("SMA1 Period", DefaultValue = 21, MinValue = 5, MaxValue = 200)]
        public int SMA1Period { get; set; }

        [Parameter("SMA2 Period", DefaultValue = 50, MinValue = 10, MaxValue = 200)]
        public int SMA2Period { get; set; }

        [Parameter("SMA3 Period", DefaultValue = 200, MinValue = 50, MaxValue = 500)]
        public int SMA3Period { get; set; }

        [Parameter("RSI Period", DefaultValue = 14, MinValue = 5, MaxValue = 50)]
        public int RSIPeriod { get; set; }

        [Parameter("ATR Period", DefaultValue = 14, MinValue = 5, MaxValue = 50)]
        public int ATRPeriod { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 1.5, MinValue = 0.5, MaxValue = 5.0, Step = 0.1)]
        public double ATRMultiplier { get; set; }

        // ================= FILTERS =================

        [Parameter("Max Spread (pips)", DefaultValue = 1.0, MinValue = 0.1, MaxValue = 5.0, Step = 0.1)]
        public double MaxSpread { get; set; }

        [Parameter("H1 Level Proximity (pips)", DefaultValue = 5, MinValue = 1, MaxValue = 50)]
        public int LevelProximity { get; set; }

        [Parameter("Enable TP Validation", DefaultValue = true)]
        public bool EnableTPValidation { get; set; }

        [Parameter("Enable SL Snapping", DefaultValue = true)]
        public bool EnableSLSnapping { get; set; }

        // ================= VISUALIZATION =================

        [Parameter("Show H1 Levels", DefaultValue = true)]
        public bool ShowH1Levels { get; set; }

        [Parameter("Max Levels to Show", DefaultValue = 5, MinValue = 1, MaxValue = 20)]
        public int MaxLevelsToShow { get; set; }

        [Parameter("Level Line Lifetime (bars)", DefaultValue = 100, MinValue = 10, MaxValue = 500)]
        public int LevelLifetimeBars { get; set; }

        [Parameter("Show SMA Lines", DefaultValue = true)]
        public bool ShowSMALines { get; set; }

        // ================= ENTRY STRATEGIES =================

        [Parameter("Enable Proximity Entry", DefaultValue = true)]
        public bool EnableProximityEntry { get; set; }

        // ================= PRIVATE INDICATORS =================

        private SimpleMovingAverage sma1;
        private SimpleMovingAverage sma2;
        private SimpleMovingAverage sma3;
        private RelativeStrengthIndex rsi;
        private AverageTrueRange atr;

        private Bars h1Bars;

        // ================= H1 LEVEL TRACKING =================

        private List<double> allSupports = new List<double>();
        private List<double> allResistances = new List<double>();
        private double nearestSupport = 0;
        private double nearestResistance = 0;
        private DateTime lastH1BarTime;

        // ================= VISUALIZATION TRACKING =================

        private class LevelLine
        {
            public string Name { get; set; }
            public double Price { get; set; }
            public int CreatedAtBar { get; set; }
            public bool IsSupport { get; set; }
        }

        private List<LevelLine> drawnLines = new List<LevelLine>();

        // ================= DAILY TRACKING =================

        private double dailyPL = 0;
        private DateTime lastResetDate;

        private const string LABEL = "JCAMP";

        protected override void OnStart()
        {
            Print("========================================");
            Print("JCAMP FxScalper v2.0 - Initializing...");
            Print("========================================");

            // Initialize indicators on M5 timeframe
            sma1 = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMA1Period);
            sma2 = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMA2Period);
            sma3 = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMA3Period);
            rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RSIPeriod);
            atr = Indicators.AverageTrueRange(ATRPeriod, MovingAverageType.Simple);

            // Get H1 bars for fractal detection
            h1Bars = MarketData.GetBars(TimeFrame.Hour);

            // Initialize tracking
            lastResetDate = Server.Time.Date;
            lastH1BarTime = h1Bars.OpenTimes.LastValue;

            // Draw SMA lines on chart
            if (ShowSMALines)
            {
                DrawSMALines();
            }

            // Log configuration
            Print("Symbol: {0} | Timeframe: M5 | Risk: {1}% | Max Daily Loss: {2}%",
                SymbolName, RiskPercent, MaxDailyLoss);
            Print("Sessions: London={0} | NY={1} | Asian={2}",
                TradeLondon, TradeNewYork, TradeAsian);
            Print("Filters: MaxSpread={0} pips | LevelProximity={1} pips | TP Validation={2} | SL Snapping={3}",
                MaxSpread, LevelProximity, EnableTPValidation, EnableSLSnapping);
            Print("Entry Strategies: Standard=TRUE | Proximity Entry={0}", EnableProximityEntry);
            Print("Visualization: H1 Levels={0} | SMA Lines={1}", ShowH1Levels, ShowSMALines);
            Print("Initialization complete - Bot ready for trading");
            Print("========================================");
        }

        protected override void OnStop()
        {
            // Cleanup all drawn lines when bot stops
            if (ShowH1Levels)
            {
                CleanupAllLevelLines();
            }

            Print("========================================");
            Print("JCAMP FxScalper v2.0 - Stopped");
            Print("========================================");
        }

        protected override void OnBar()
        {
            Print("========================================");
            Print("*** NEW M5 BAR - STARTING ANALYSIS ***");
            Print("Time: {0} | Price: {1}", Server.Time, Symbol.Bid);
            Print("========================================");

            // 1. Update H1 levels (only on new H1 bar)
            UpdateH1Levels();
            Print("[DEBUG] {0}", GetLevelStats());

            // 2. Check daily loss limit
            UpdateDailyPL();
            Print("[DEBUG] Daily P/L: {0:F2} ({1:F2}%)", dailyPL, (dailyPL / Account.Balance) * 100);
            if (!CheckDailyLossLimit())
            {
                Print("[FILTER FAILED] Daily loss limit exceeded - SKIPPING ANALYSIS");
                Print("========================================");
                return;
            }
            Print("[FILTER PASSED] Daily loss limit OK");

            // 3. Session filter
            Print("[DEBUG] Checking sessions at GMT time: {0}", Server.Time);
            if (!IsActiveSession())
            {
                Print("[FILTER FAILED] No active session - SKIPPING ANALYSIS");
                Print("========================================");
                return;
            }
            Print("[FILTER PASSED] Active session: {0}", GetActiveSessionName());

            // 4. Spread filter
            double spreadPips = Symbol.Spread / Symbol.PipSize;
            Print("[DEBUG] Current spread: {0:F1} pips (max allowed: {1:F1})", spreadPips, MaxSpread);
            if (!IsSpreadAcceptable())
            {
                Print("[FILTER FAILED] Spread too wide - SKIPPING ANALYSIS");
                Print("========================================");
                return;
            }
            Print("[FILTER PASSED] Spread acceptable");

            // 5. Position limit filter
            int currentPositions = Positions.Count(p => p.Label == LABEL);
            Print("[DEBUG] Current positions: {0} | Max allowed: {1}", currentPositions, MaxGlobalPositions);
            if (currentPositions >= MaxGlobalPositions)
            {
                Print("[FILTER FAILED] Position limit reached - SKIPPING NEW TRADES");
                ManagePartialProfits();
                Print("========================================");
                return;
            }
            Print("[FILTER PASSED] Can open new position");

            // 6. Print indicator values
            Print("--- INDICATOR VALUES ---");
            Print("SMA21: {0:F5} | SMA50: {1:F5} | SMA200: {2:F5}",
                sma1.Result.LastValue, sma2.Result.LastValue, sma3.Result.LastValue);
            Print("RSI(14): {0:F2} | ATR(14): {1:F5}", rsi.Result.LastValue, atr.Result.LastValue);
            Print("Bullish trend? {0} (SMA21>SMA50>SMA200)",
                (sma1.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma3.Result.LastValue) ? "YES" : "NO");
            Print("Bearish trend? {0} (SMA200>SMA50>SMA21)",
                (sma3.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma1.Result.LastValue) ? "YES" : "NO");
            Print("Bullish momentum? {0} (RSI>50)", rsi.Result.LastValue > 50 ? "YES" : "NO");
            Print("Bearish momentum? {0} (RSI<50)", rsi.Result.LastValue < 50 ? "YES" : "NO");

            // 7. Check for bullish signal
            Print("--- CHECKING BULLISH SIGNAL ---");
            if (IsCompleteBullishSignal())
            {
                Print("[SIGNAL DETECTED] Bullish entry conditions met!");
                ProcessBuySignal();
            }
            else
            {
                Print("[NO SIGNAL] Bullish conditions not met");
            }

            // 8. Check for bearish signal
            Print("--- CHECKING BEARISH SIGNAL ---");
            if (IsCompleteBearishSignal())
            {
                Print("[SIGNAL DETECTED] Bearish entry conditions met!");
                ProcessSellSignal();
            }
            else
            {
                Print("[NO SIGNAL] Bearish conditions not met");
            }

            // 9. Check for PROXIMITY-BASED entries (if enabled)
            if (EnableProximityEntry)
            {
                Print("--- CHECKING PROXIMITY ENTRY ---");

                // Proximity BUY: Price near support + Bullish engulfing + Bullish trend
                if (IsProximityBuySignal())
                {
                    Print("[PROXIMITY SIGNAL] BUY - Price near support with bullish setup!");
                    ProcessBuySignal();
                }
                // Proximity SELL: Price near resistance + Bearish engulfing + Bearish trend
                else if (IsProximitySellSignal())
                {
                    Print("[PROXIMITY SIGNAL] SELL - Price near resistance with bearish setup!");
                    ProcessSellSignal();
                }
                else
                {
                    Print("[NO PROXIMITY SIGNAL] Conditions not met");
                }
            }

            // 10. Manage existing positions
            ManagePartialProfits();

            Print("========================================");
            Print("*** M5 BAR ANALYSIS COMPLETE ***");
            Print("========================================");
        }

        // ================= H1 FRACTAL DETECTION (Williams Fractals) =================

        private void UpdateH1Levels()
        {
            // Only update on new H1 bar
            if (h1Bars.OpenTimes.LastValue == lastH1BarTime)
                return;

            lastH1BarTime = h1Bars.OpenTimes.LastValue;

            // Reset level arrays
            allSupports.Clear();
            allResistances.Clear();

            double currentPrice = Symbol.Bid;

            // Scan last 200 H1 bars for fractal levels
            int barsToScan = Math.Min(200, h1Bars.Count - 5);

            for (int i = 2; i < barsToScan - 2; i++)
            {
                int idx = h1Bars.Count - 1 - i;

                // Williams Fractal Up (Resistance): High[i] > High[i±1] and High[i] > High[i±2]
                if (h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx - 1] &&
                    h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx - 2] &&
                    h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx + 1] &&
                    h1Bars.HighPrices[idx] > h1Bars.HighPrices[idx + 2])
                {
                    allResistances.Add(h1Bars.HighPrices[idx]);
                }

                // Williams Fractal Down (Support): Low[i] < Low[i±1] and Low[i] < Low[i±2]
                if (h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx - 1] &&
                    h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx - 2] &&
                    h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx + 1] &&
                    h1Bars.LowPrices[idx] < h1Bars.LowPrices[idx + 2])
                {
                    allSupports.Add(h1Bars.LowPrices[idx]);
                }
            }

            // Find nearest levels to current price
            nearestSupport = FindNearestSupportBelow(currentPrice);
            nearestResistance = FindNearestResistanceAbove(currentPrice);

            Print("[MarketStructure] H1 levels updated | Supports: {0} | Resistances: {1} | Nearest Support: {2:F5} | Nearest Resistance: {3:F5}",
                allSupports.Count, allResistances.Count, nearestSupport, nearestResistance);

            // Update visualization
            if (ShowH1Levels)
            {
                UpdateH1LevelVisualization(currentPrice);
            }
        }

        private double FindNearestSupportBelow(double price)
        {
            double nearest = 0;
            double minDistance = double.MaxValue;

            foreach (var support in allSupports)
            {
                if (support > 0 && support < price)
                {
                    double distance = price - support;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = support;
                    }
                }
            }

            return nearest;
        }

        private double FindNearestResistanceAbove(double price)
        {
            double nearest = 0;
            double minDistance = double.MaxValue;

            foreach (var resistance in allResistances)
            {
                if (resistance > 0 && resistance > price)
                {
                    double distance = resistance - price;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = resistance;
                    }
                }
            }

            return nearest;
        }

        private string GetLevelStats()
        {
            return string.Format("H1 Levels | Supports: {0} | Resistances: {1} | Nearest S: {2:F5} | Nearest R: {3:F5}",
                allSupports.Count, allResistances.Count, nearestSupport, nearestResistance);
        }

        // ================= H1 LEVEL VISUALIZATION =================

        private void UpdateH1LevelVisualization(double currentPrice)
        {
            // FIRST: Remove ALL existing level lines to ensure only top N are shown
            foreach (var line in drawnLines.ToList())
            {
                Chart.RemoveObject(line.Name);
            }
            drawnLines.Clear();

            // Get the closest levels to current price (limited to MaxLevelsToShow)
            var nearestSupports = allSupports
                .Where(s => s > 0 && s < currentPrice)
                .OrderByDescending(s => s)
                .Take(MaxLevelsToShow)
                .ToList();

            var nearestResistances = allResistances
                .Where(r => r > 0 && r > currentPrice)
                .OrderBy(r => r)
                .Take(MaxLevelsToShow)
                .ToList();

            // Draw support lines (green)
            foreach (var support in nearestSupports)
            {
                DrawLevelLine(support, true);
            }

            // Draw resistance lines (red)
            foreach (var resistance in nearestResistances)
            {
                DrawLevelLine(resistance, false);
            }

            Print("[Visualization] Drew {0} support lines and {1} resistance lines (Max: {2})",
                nearestSupports.Count, nearestResistances.Count, MaxLevelsToShow);
        }

        private void DrawLevelLine(double price, bool isSupport)
        {
            string lineName = string.Format("H1_{0}_{1:F5}", isSupport ? "Support" : "Resistance", price);

            // Check if line already exists
            if (drawnLines.Any(l => l.Name == lineName))
                return;

            // Draw the line
            var color = isSupport ? Color.Green : Color.Red;
            var lineStyle = LineStyle.Solid;
            var thickness = isSupport && price == nearestSupport ? 2 : ((!isSupport && price == nearestResistance) ? 2 : 1);

            Chart.DrawHorizontalLine(lineName, price, color, thickness, lineStyle);

            // Track the line
            drawnLines.Add(new LevelLine
            {
                Name = lineName,
                Price = price,
                CreatedAtBar = Bars.Count,
                IsSupport = isSupport
            });
        }

        private void CleanupOldLevelLines()
        {
            int currentBar = Bars.Count;
            var linesToRemove = new List<LevelLine>();

            foreach (var line in drawnLines)
            {
                // Remove if line is older than lifetime
                int lineAge = currentBar - line.CreatedAtBar;
                if (lineAge > LevelLifetimeBars)
                {
                    Chart.RemoveObject(line.Name);
                    linesToRemove.Add(line);
                }
                // Remove if price has moved significantly away from level
                else
                {
                    double currentPrice = Symbol.Bid;
                    double distancePips = Math.Abs(currentPrice - line.Price) / Symbol.PipSize;

                    // Remove if price is more than 200 pips away
                    if (distancePips > 200)
                    {
                        Chart.RemoveObject(line.Name);
                        linesToRemove.Add(line);
                    }
                }
            }

            // Remove from tracking list
            foreach (var line in linesToRemove)
            {
                drawnLines.Remove(line);
            }

            if (linesToRemove.Count > 0)
            {
                Print("[Visualization] Cleaned up {0} old level lines", linesToRemove.Count);
            }
        }

        private void CleanupAllLevelLines()
        {
            foreach (var line in drawnLines)
            {
                Chart.RemoveObject(line.Name);
            }
            drawnLines.Clear();
            Print("[Visualization] Removed all level lines");
        }

        // ================= SMA VISUALIZATION =================

        private void DrawSMALines()
        {
            // Note: In cTrader, to display SMA indicators on the chart, you need to manually add them
            // from the Indicators menu in the platform. The SMA values are used internally for calculations.
            // The Chart.DrawIndicator method does not exist in cTrader API.

            Print("[Visualization] SMA indicators initialized for internal calculations");
            Print("[Visualization] To view SMAs on chart, manually add SimpleMovingAverage indicators with periods: {0}, {1}, {2}",
                SMA1Period, SMA2Period, SMA3Period);
        }

        // ================= ENTRY LOGIC =================

        private bool IsCompleteBullishSignal()
        {
            // 1. Bullish trend (SMA 21 > 50 > 200)
            if (!(sma1.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma3.Result.LastValue))
            {
                Print("[EntryLogic] Bullish signal rejected - No bullish trend");
                return false;
            }

            // 2. Bullish momentum (RSI > 50)
            if (!(rsi.Result.LastValue > 50))
            {
                Print("[EntryLogic] Bullish signal rejected - No bullish momentum");
                return false;
            }

            // 3. Bullish trigger pattern
            if (!DetectBullishTrigger())
            {
                Print("[EntryLogic] Bullish signal rejected - No bullish trigger pattern");
                return false;
            }

            Print("[EntryLogic] *** COMPLETE BULLISH SIGNAL CONFIRMED ***");
            return true;
        }

        private bool IsCompleteBearishSignal()
        {
            // 1. Bearish trend (SMA 200 > 50 > 21)
            if (!(sma3.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma1.Result.LastValue))
            {
                Print("[EntryLogic] Bearish signal rejected - No bearish trend");
                return false;
            }

            // 2. Bearish momentum (RSI < 50)
            if (!(rsi.Result.LastValue < 50))
            {
                Print("[EntryLogic] Bearish signal rejected - No bearish momentum");
                return false;
            }

            // 3. Bearish trigger pattern
            if (!DetectBearishTrigger())
            {
                Print("[EntryLogic] Bearish signal rejected - No bearish trigger pattern");
                return false;
            }

            Print("[EntryLogic] *** COMPLETE BEARISH SIGNAL CONFIRMED ***");
            return true;
        }

        // ================= PROXIMITY ENTRY LOGIC =================

        private bool IsProximityBuySignal()
        {
            // Proximity BUY entry: Price near support + Bullish engulfing + Bullish trend + RSI confirmation

            // 1. Check if we have a valid support level
            if (nearestSupport == 0)
            {
                Print("[ProximityEntry] No support level detected");
                return false;
            }

            // 2. Check if price is near support (within proximity pips)
            double currentPrice = Symbol.Bid;
            double distancePips = Math.Abs(currentPrice - nearestSupport) / Symbol.PipSize;

            if (distancePips > LevelProximity)
            {
                Print("[ProximityEntry] BUY rejected - Price too far from support ({0:F1} pips, max {1})",
                    distancePips, LevelProximity);
                return false;
            }

            Print("[ProximityEntry] Price near support: {0:F5} | Distance: {1:F1} pips", nearestSupport, distancePips);

            // 3. Bullish trend (SMA 21 > 50 > 200)
            if (!(sma1.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma3.Result.LastValue))
            {
                Print("[ProximityEntry] BUY rejected - No bullish trend");
                return false;
            }

            Print("[ProximityEntry] Bullish trend confirmed");

            // 4. RSI confirmation (RSI > 50)
            if (!(rsi.Result.LastValue > 50))
            {
                Print("[ProximityEntry] BUY rejected - RSI too low ({0:F2}, need >50)", rsi.Result.LastValue);
                return false;
            }

            Print("[ProximityEntry] Bullish momentum confirmed (RSI: {0:F2})", rsi.Result.LastValue);

            // 5. Bullish engulfing pattern (main trigger)
            if (!IsBullishEngulfing())
            {
                Print("[ProximityEntry] BUY rejected - No bullish engulfing");
                return false;
            }

            Print("[ProximityEntry] *** PROXIMITY BUY SIGNAL CONFIRMED ***");
            Print("[ProximityEntry] Support: {0:F5} | Price: {1:F5} | Distance: {2:F1} pips | RSI: {3:F2}",
                nearestSupport, currentPrice, distancePips, rsi.Result.LastValue);

            return true;
        }

        private bool IsProximitySellSignal()
        {
            // Proximity SELL entry: Price near resistance + Bearish engulfing + Bearish trend + RSI confirmation

            // 1. Check if we have a valid resistance level
            if (nearestResistance == 0)
            {
                Print("[ProximityEntry] No resistance level detected");
                return false;
            }

            // 2. Check if price is near resistance (within proximity pips)
            double currentPrice = Symbol.Bid;
            double distancePips = Math.Abs(currentPrice - nearestResistance) / Symbol.PipSize;

            if (distancePips > LevelProximity)
            {
                Print("[ProximityEntry] SELL rejected - Price too far from resistance ({0:F1} pips, max {1})",
                    distancePips, LevelProximity);
                return false;
            }

            Print("[ProximityEntry] Price near resistance: {0:F5} | Distance: {1:F1} pips", nearestResistance, distancePips);

            // 3. Bearish trend (SMA 200 > 50 > 21)
            if (!(sma3.Result.LastValue > sma2.Result.LastValue && sma2.Result.LastValue > sma1.Result.LastValue))
            {
                Print("[ProximityEntry] SELL rejected - No bearish trend");
                return false;
            }

            Print("[ProximityEntry] Bearish trend confirmed");

            // 4. RSI confirmation (RSI < 50)
            if (!(rsi.Result.LastValue < 50))
            {
                Print("[ProximityEntry] SELL rejected - RSI too high ({0:F2}, need <50)", rsi.Result.LastValue);
                return false;
            }

            Print("[ProximityEntry] Bearish momentum confirmed (RSI: {0:F2})", rsi.Result.LastValue);

            // 5. Bearish engulfing pattern (main trigger)
            if (!IsBearishEngulfing())
            {
                Print("[ProximityEntry] SELL rejected - No bearish engulfing");
                return false;
            }

            Print("[ProximityEntry] *** PROXIMITY SELL SIGNAL CONFIRMED ***");
            Print("[ProximityEntry] Resistance: {0:F5} | Price: {1:F5} | Distance: {2:F1} pips | RSI: {3:F2}",
                nearestResistance, currentPrice, distancePips, rsi.Result.LastValue);

            return true;
        }

        // ================= PATTERN DETECTION =================

        private bool DetectBullishTrigger()
        {
            return IsBullishEngulfing() || IsComplexBullishPattern();
        }

        private bool DetectBearishTrigger()
        {
            return IsBearishEngulfing() || IsComplexBearishPattern();
        }

        private bool IsBullishEngulfing()
        {
            // Check COMPLETED bars [1] and [2]
            int idx1 = Bars.Count - 2; // Bar that just closed
            int idx2 = Bars.Count - 3; // Bar before that

            if (idx2 < 0) return false;

            double pipBuffer = 3.0 * Symbol.PipSize;

            bool prevBearish = Bars.ClosePrices[idx2] < Bars.OpenPrices[idx2];
            bool currBullish = Bars.ClosePrices[idx1] > Bars.OpenPrices[idx1];
            bool engulfs = (Bars.OpenPrices[idx1] <= Bars.ClosePrices[idx2] + pipBuffer) &&
                           (Bars.ClosePrices[idx1] >= Bars.OpenPrices[idx2]);

            Print("[DEBUG] Bullish Engulfing check | Prev[2]: {0} | Curr[1]: {1} | Engulfs: {2}",
                prevBearish ? "BEARISH" : "bullish",
                currBullish ? "BULLISH" : "bearish",
                engulfs ? "YES" : "no");

            if (prevBearish && currBullish && engulfs)
            {
                Print("[EntryLogic] Bullish Engulfing DETECTED | Prev[2] O:{0:F5} C:{1:F5} | Curr[1] O:{2:F5} C:{3:F5}",
                    Bars.OpenPrices[idx2], Bars.ClosePrices[idx2], Bars.OpenPrices[idx1], Bars.ClosePrices[idx1]);
                return true;
            }

            return false;
        }

        private bool IsBearishEngulfing()
        {
            int idx1 = Bars.Count - 2;
            int idx2 = Bars.Count - 3;

            if (idx2 < 0) return false;

            double pipBuffer = 3.0 * Symbol.PipSize;

            bool prevBullish = Bars.ClosePrices[idx2] > Bars.OpenPrices[idx2];
            bool currBearish = Bars.ClosePrices[idx1] < Bars.OpenPrices[idx1];
            bool engulfs = (Bars.OpenPrices[idx1] >= Bars.ClosePrices[idx2] - pipBuffer) &&
                           (Bars.ClosePrices[idx1] <= Bars.OpenPrices[idx2]);

            Print("[DEBUG] Bearish Engulfing check | Prev[2]: {0} | Curr[1]: {1} | Engulfs: {2}",
                prevBullish ? "BULLISH" : "bearish",
                currBearish ? "BEARISH" : "bullish",
                engulfs ? "YES" : "no");

            if (prevBullish && currBearish && engulfs)
            {
                Print("[EntryLogic] Bearish Engulfing DETECTED | Prev[2] O:{0:F5} C:{1:F5} | Curr[1] O:{2:F5} C:{3:F5}",
                    Bars.OpenPrices[idx2], Bars.ClosePrices[idx2], Bars.OpenPrices[idx1], Bars.ClosePrices[idx1]);
                return true;
            }

            return false;
        }

        private bool IsComplexBullishPattern()
        {
            // Pattern: [5]=bull, [4]=bull, [3]=bull, [2]=bear, [1]=bull
            int idx1 = Bars.Count - 2;
            int idx2 = Bars.Count - 3;
            int idx3 = Bars.Count - 4;
            int idx4 = Bars.Count - 5;
            int idx5 = Bars.Count - 6;

            if (idx5 < 0) return false;

            bool bull5 = Bars.ClosePrices[idx5] > Bars.OpenPrices[idx5];
            bool bull4 = Bars.ClosePrices[idx4] > Bars.OpenPrices[idx4];
            bool bull3 = Bars.ClosePrices[idx3] > Bars.OpenPrices[idx3];
            bool bear2 = Bars.ClosePrices[idx2] < Bars.OpenPrices[idx2];
            bool bull1 = Bars.ClosePrices[idx1] > Bars.OpenPrices[idx1];

            Print("[DEBUG] Complex bullish pattern check | [5]:{0} [4]:{1} [3]:{2} [2]:{3} [1]:{4}",
                bull5 ? "BULL" : "bear",
                bull4 ? "BULL" : "bear",
                bull3 ? "BULL" : "bear",
                bear2 ? "BEAR" : "bull",
                bull1 ? "BULL" : "bear");

            if (bull5 && bull4 && bull3 && bear2 && bull1)
            {
                Print("[EntryLogic] Complex bullish pattern DETECTED | 3 bulls + 1 bear + 1 bull");
                return true;
            }

            return false;
        }

        private bool IsComplexBearishPattern()
        {
            // Pattern: [5]=bear, [4]=bear, [3]=bear, [2]=bull, [1]=bear
            int idx1 = Bars.Count - 2;
            int idx2 = Bars.Count - 3;
            int idx3 = Bars.Count - 4;
            int idx4 = Bars.Count - 5;
            int idx5 = Bars.Count - 6;

            if (idx5 < 0) return false;

            bool bear5 = Bars.ClosePrices[idx5] < Bars.OpenPrices[idx5];
            bool bear4 = Bars.ClosePrices[idx4] < Bars.OpenPrices[idx4];
            bool bear3 = Bars.ClosePrices[idx3] < Bars.OpenPrices[idx3];
            bool bull2 = Bars.ClosePrices[idx2] > Bars.OpenPrices[idx2];
            bool bear1 = Bars.ClosePrices[idx1] < Bars.OpenPrices[idx1];

            Print("[DEBUG] Complex bearish pattern check | [5]:{0} [4]:{1} [3]:{2} [2]:{3} [1]:{4}",
                bear5 ? "BEAR" : "bull",
                bear4 ? "BEAR" : "bull",
                bear3 ? "BEAR" : "bull",
                bull2 ? "BULL" : "bear",
                bear1 ? "BEAR" : "bull");

            if (bear5 && bear4 && bear3 && bull2 && bear1)
            {
                Print("[EntryLogic] Complex bearish pattern DETECTED | 3 bears + 1 bull + 1 bear");
                return true;
            }

            return false;
        }

        // ================= TRADE EXECUTION =================

        private void ProcessBuySignal()
        {
            Print("*** PROCESSING BUY SIGNAL ***");

            double atrValue = atr.Result.LastValue;
            double prevLow = Bars.LowPrices[Bars.Count - 2];
            double entryPrice = Symbol.Ask;

            // Calculate SL: Previous candle low - (ATR × Multiplier)
            double calculatedSL = prevLow - (atrValue * ATRMultiplier);

            Print("[BUY] Initial SL calculation | Prev Low: {0:F5} | ATR: {1:F5} | Multiplier: {2:F1} | SL: {3:F5}",
                prevLow, atrValue, ATRMultiplier, calculatedSL);

            // Apply SL snapping if enabled
            double finalSL = SnapSLToLevel(calculatedSL, entryPrice, true);

            // Recalculate TP to maintain 2:1 R:R
            double slDistance = entryPrice - finalSL;
            double tpPrice = entryPrice + (slDistance * 2.0);

            Print("[BUY] TP calculation | Entry: {0:F5} | SL: {1:F5} | Distance: {2:F5} | TP: {3:F5} (2:1 R:R)",
                entryPrice, finalSL, slDistance, tpPrice);

            // Validate TP doesn't cross H1 resistance
            if (!ValidateTPLevel(entryPrice, tpPrice, true))
            {
                Print("[BUY] Trade ABORTED - TP crosses H1 resistance");
                return;
            }

            // Calculate lot size
            double slPips = Math.Abs(entryPrice - finalSL) / Symbol.PipSize;
            double volumeInLots = CalculateLotSize(slPips);

            if (volumeInLots <= 0)
            {
                Print("[BUY] Trade ABORTED - Lot size calculation failed");
                return;
            }

            double volumeInUnits = Symbol.QuantityToVolumeInUnits(volumeInLots);
            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            if (volumeInUnits < Symbol.VolumeInUnitsMin)
            {
                Print("[BUY] Trade ABORTED - Volume too small");
                return;
            }

            // Execute BUY order
            Print("[BUY] Executing order | Volume: {0:F2} | Entry: {1:F5} | SL: {2:F5} | TP: {3:F5}",
                volumeInUnits, entryPrice, finalSL, tpPrice);

            var result = PlaceMarketOrder(TradeType.Buy, SymbolName, volumeInUnits, LABEL, finalSL, tpPrice);

            if (result.IsSuccessful)
            {
                Print("[BUY] *** ORDER EXECUTED SUCCESSFULLY ***");
            }
            else
            {
                Print("[BUY] *** ORDER EXECUTION FAILED: {0} ***", result.Error);
            }
        }

        private void ProcessSellSignal()
        {
            Print("*** PROCESSING SELL SIGNAL ***");

            double atrValue = atr.Result.LastValue;
            double prevHigh = Bars.HighPrices[Bars.Count - 2];
            double entryPrice = Symbol.Bid;

            // Calculate SL: Previous candle high + (ATR × Multiplier)
            double calculatedSL = prevHigh + (atrValue * ATRMultiplier);

            Print("[SELL] Initial SL calculation | Prev High: {0:F5} | ATR: {1:F5} | Multiplier: {2:F1} | SL: {3:F5}",
                prevHigh, atrValue, ATRMultiplier, calculatedSL);

            // Apply SL snapping if enabled
            double finalSL = SnapSLToLevel(calculatedSL, entryPrice, false);

            // Recalculate TP to maintain 2:1 R:R
            double slDistance = finalSL - entryPrice;
            double tpPrice = entryPrice - (slDistance * 2.0);

            Print("[SELL] TP calculation | Entry: {0:F5} | SL: {1:F5} | Distance: {2:F5} | TP: {3:F5} (2:1 R:R)",
                entryPrice, finalSL, slDistance, tpPrice);

            // Validate TP doesn't cross H1 support
            if (!ValidateTPLevel(entryPrice, tpPrice, false))
            {
                Print("[SELL] Trade ABORTED - TP crosses H1 support");
                return;
            }

            // Calculate lot size
            double slPips = Math.Abs(finalSL - entryPrice) / Symbol.PipSize;
            double volumeInLots = CalculateLotSize(slPips);

            if (volumeInLots <= 0)
            {
                Print("[SELL] Trade ABORTED - Lot size calculation failed");
                return;
            }

            double volumeInUnits = Symbol.QuantityToVolumeInUnits(volumeInLots);
            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            if (volumeInUnits < Symbol.VolumeInUnitsMin)
            {
                Print("[SELL] Trade ABORTED - Volume too small");
                return;
            }

            // Execute SELL order
            Print("[SELL] Executing order | Volume: {0:F2} | Entry: {1:F5} | SL: {2:F5} | TP: {3:F5}",
                volumeInUnits, entryPrice, finalSL, tpPrice);

            var result = PlaceMarketOrder(TradeType.Sell, SymbolName, volumeInUnits, LABEL, finalSL, tpPrice);

            if (result.IsSuccessful)
            {
                Print("[SELL] *** ORDER EXECUTED SUCCESSFULLY ***");
            }
            else
            {
                Print("[SELL] *** ORDER EXECUTION FAILED: {0} ***", result.Error);
            }
        }

        private TradeResult PlaceMarketOrder(TradeType tradeType, string symbol, double volume,
            string label, double? stopLossPrice, double? takeProfitPrice)
        {
            double? slPips = stopLossPrice.HasValue ? Math.Abs(Symbol.Bid - stopLossPrice.Value) / Symbol.PipSize : (double?)null;
            double? tpPips = takeProfitPrice.HasValue ? Math.Abs(Symbol.Bid - takeProfitPrice.Value) / Symbol.PipSize : (double?)null;

            return ExecuteMarketOrder(tradeType, symbol, volume, label, slPips, tpPips);
        }

        // ================= SL SNAPPING & TP VALIDATION =================

        private double SnapSLToLevel(double calculatedSL, double entryPrice, bool isBuy)
        {
            if (!EnableSLSnapping)
                return calculatedSL;

            double atrValue = atr.Result.LastValue;

            if (isBuy)
            {
                // For BUY: Look for support level near calculated SL
                double targetLevel = 0;
                double minDistance = double.MaxValue;

                foreach (var support in allSupports)
                {
                    if (support > 0 && support < entryPrice)
                    {
                        double distance = Math.Abs(support - calculatedSL);
                        if (distance <= atrValue && distance < minDistance)
                        {
                            minDistance = distance;
                            targetLevel = support;
                        }
                    }
                }

                if (targetLevel > 0)
                {
                    double bufferDistance = 2 * Symbol.PipSize;
                    double snappedSL = targetLevel - bufferDistance;

                    // Never widen SL
                    if (snappedSL < calculatedSL)
                    {
                        Print("[MarketStructure] SL snapped to H1 support (BUY) | Original SL: {0:F5} | Support: {1:F5} | Snapped SL: {2:F5}",
                            calculatedSL, targetLevel, snappedSL);
                        return snappedSL;
                    }
                }
            }
            else
            {
                // For SELL: Look for resistance level near calculated SL
                double targetLevel = 0;
                double minDistance = double.MaxValue;

                foreach (var resistance in allResistances)
                {
                    if (resistance > 0 && resistance > entryPrice)
                    {
                        double distance = Math.Abs(resistance - calculatedSL);
                        if (distance <= atrValue && distance < minDistance)
                        {
                            minDistance = distance;
                            targetLevel = resistance;
                        }
                    }
                }

                if (targetLevel > 0)
                {
                    double bufferDistance = 2 * Symbol.PipSize;
                    double snappedSL = targetLevel + bufferDistance;

                    // Never widen SL
                    if (snappedSL > calculatedSL)
                    {
                        Print("[MarketStructure] SL snapped to H1 resistance (SELL) | Original SL: {0:F5} | Resistance: {1:F5} | Snapped SL: {2:F5}",
                            calculatedSL, targetLevel, snappedSL);
                        return snappedSL;
                    }
                }
            }

            Print("[MarketStructure] No suitable H1 level for SL snapping - using calculated SL");
            return calculatedSL;
        }

        private bool ValidateTPLevel(double entryPrice, double tpPrice, bool isBuy)
        {
            if (!EnableTPValidation)
                return true;

            double distanceToTP = Math.Abs(tpPrice - entryPrice);
            double threshold75Percent = 0.75 * distanceToTP;

            if (isBuy)
            {
                // Check if any resistance level between entry and TP
                foreach (var resistance in allResistances)
                {
                    if (resistance > entryPrice && resistance < tpPrice)
                    {
                        double distanceToResistance = resistance - entryPrice;
                        double percentToTP = (distanceToResistance / distanceToTP) * 100.0;

                        if (distanceToResistance >= threshold75Percent)
                        {
                            Print("[MarketStructure] TP validation: Resistance at {0:F1}% to target | ALLOWING TRADE",
                                percentToTP);
                            return true;
                        }
                        else
                        {
                            Print("[MarketStructure] TP VALIDATION FAILED (BUY) | Resistance at {0:F1}% to target (need ≥75%) | TRADE ABORTED",
                                percentToTP);
                            return false;
                        }
                    }
                }
            }
            else
            {
                // Check if any support level between entry and TP
                foreach (var support in allSupports)
                {
                    if (support < entryPrice && support > tpPrice)
                    {
                        double distanceToSupport = entryPrice - support;
                        double percentToTP = (distanceToSupport / distanceToTP) * 100.0;

                        if (distanceToSupport >= threshold75Percent)
                        {
                            Print("[MarketStructure] TP validation: Support at {0:F1}% to target | ALLOWING TRADE",
                                percentToTP);
                            return true;
                        }
                        else
                        {
                            Print("[MarketStructure] TP VALIDATION FAILED (SELL) | Support at {0:F1}% to target (need ≥75%) | TRADE ABORTED",
                                percentToTP);
                            return false;
                        }
                    }
                }
            }

            Print("[MarketStructure] TP validation passed | TP clear of structural levels");
            return true;
        }

        // ================= RISK MANAGEMENT =================

        private double CalculateLotSize(double stopPips)
        {
            double riskAmount = Account.Balance * (RiskPercent / 100.0);

            // Symbol.PipValue is the pip value for 1 UNIT of volume
            // For 1 standard lot (100,000 units), multiply by LotSize
            double pipValuePerLot = Symbol.PipValue * Symbol.LotSize;

            double lotSize = riskAmount / (stopPips * pipValuePerLot);

            Print("[RiskManager] Position sizing | Risk: ${0:F2} | SL: {1:F1} pips | PipValue/Lot: ${2:F2} | Lot size: {3:F4}",
                riskAmount, stopPips, pipValuePerLot, lotSize);

            return lotSize;
        }

        private void UpdateDailyPL()
        {
            // Reset daily P/L at midnight
            if (Server.Time.Date != lastResetDate)
            {
                dailyPL = 0;
                lastResetDate = Server.Time.Date;
                Print("[RiskManager] Daily P/L reset - New trading day");
            }

            // Calculate daily P/L from closed positions
            dailyPL = 0;
            foreach (var trade in History.Where(t => t.ClosingTime.Date == Server.Time.Date))
            {
                dailyPL += trade.NetProfit;
            }
        }

        private bool CheckDailyLossLimit()
        {
            double dailyLossPercent = (dailyPL / Account.Balance) * 100;
            return dailyLossPercent > -MaxDailyLoss;
        }

        // ================= SESSION MANAGEMENT =================

        private bool IsActiveSession()
        {
            int hour = Server.Time.Hour;

            bool londonActive = TradeLondon && IsInRange(hour, LondonStart, LondonEnd);
            bool nyActive = TradeNewYork && IsInRange(hour, NewYorkStart, NewYorkEnd);
            bool asianActive = TradeAsian && IsInRange(hour, AsianStart, AsianEnd);

            return londonActive || nyActive || asianActive;
        }

        private bool IsInRange(int hour, int start, int end)
        {
            if (start <= end)
                return hour >= start && hour <= end;
            else
                return hour >= start || hour <= end; // Handles overnight ranges
        }

        private string GetActiveSessionName()
        {
            int hour = Server.Time.Hour;
            var sessions = new List<string>();

            if (TradeLondon && IsInRange(hour, LondonStart, LondonEnd))
                sessions.Add("London");
            if (TradeNewYork && IsInRange(hour, NewYorkStart, NewYorkEnd))
                sessions.Add("NY");
            if (TradeAsian && IsInRange(hour, AsianStart, AsianEnd))
                sessions.Add("Asian");

            return sessions.Count > 0 ? string.Join(", ", sessions) : "None";
        }

        private bool IsSpreadAcceptable()
        {
            return Symbol.Spread / Symbol.PipSize <= MaxSpread;
        }

        // ================= TRADE MANAGEMENT =================

        protected override void OnTick()
        {
            ManagePartialProfits();
        }

        private void ManagePartialProfits()
        {
            foreach (var position in Positions.Where(p => p.Label == LABEL && p.SymbolName == SymbolName))
            {
                double rr = GetRR(position);

                if (rr >= 2.0)
                {
                    // Close 80% at 2:1 RR
                    double volumeToClose = position.VolumeInUnits * 0.8;
                    volumeToClose = Symbol.NormalizeVolumeInUnits(volumeToClose, RoundingMode.Down);

                    if (volumeToClose >= Symbol.VolumeInUnitsMin)
                    {
                        Print("[TradeManager] Closing 80% at 2:1 RR | Position: {0} | Volume: {1:F2}",
                            position.Id, volumeToClose);

                        ClosePosition(position, volumeToClose);

                        // Move SL to breakeven
                        ModifyPosition(position, position.EntryPrice, position.TakeProfit, ProtectionType.Absolute);
                    }
                }
            }
        }

        private double GetRR(Position position)
        {
            if (!position.StopLoss.HasValue)
                return 0;

            double risk = Math.Abs(position.EntryPrice - position.StopLoss.Value);
            double reward = position.TradeType == TradeType.Buy
                ? Symbol.Bid - position.EntryPrice
                : position.EntryPrice - Symbol.Ask;

            return risk > 0 ? reward / risk : 0;
        }
    }
}

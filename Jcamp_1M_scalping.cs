using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    /// <summary>
    /// Jcamp 1M Scalping Strategy - MTF SMA Alignment v4.1
    /// Entry: Trade when price > SMA on ALL configured timeframes (M1 + TF2 + TF3)
    /// Trigger: M1 SMA crossover while higher TFs already aligned
    /// Filter: ADX trend strength filter to avoid ranging markets
    /// Exit: Exhaustion detection via swing pattern + RSI divergence
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Jcamp_1M_scalping : Robot
    {
        #region Version Info
        private const string BOT_VERSION = "4.1.3";
        private const string VERSION_DATE = "2026-03-29";
        private const string VERSION_NOTES = "Consecutive Loss Limit + Monthly DD + ADX Flip";
        #endregion

        #region Parameters - MTF SMA Alignment

        [Parameter("=== MTF SMA ALIGNMENT ===", DefaultValue = "")]
        public string MTFHeader { get; set; }

        [Parameter("Enable MTF SMA Entry", DefaultValue = true, Group = "MTF SMA Alignment")]
        public bool EnableMTFSMAEntry { get; set; }

        [Parameter("MTF SMA Period", DefaultValue = 275, MinValue = 50, MaxValue = 350, Step = 25, Group = "MTF SMA Alignment")]
        public int MTFSMAPeriod { get; set; }

        [Parameter("Timeframe 2", DefaultValue = "Minute4", Group = "MTF SMA Alignment")]
        public TimeFrame Timeframe2 { get; set; }

        [Parameter("Timeframe 3", DefaultValue = "Minute15", Group = "MTF SMA Alignment")]
        public TimeFrame Timeframe3 { get; set; }

        [Parameter("Require All TFs Aligned", DefaultValue = true, Group = "MTF SMA Alignment")]
        public bool RequireAllTFsAligned { get; set; }

        [Parameter("ATR Period", DefaultValue = 14, MinValue = 10, MaxValue = 20, Step = 2, Group = "MTF SMA Alignment")]
        public int ATRPeriod { get; set; }

        [Parameter("SL ATR Multiplier", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 3.0, Step = 0.25, Group = "MTF SMA Alignment")]
        public double SLATRMultiplier { get; set; }

        [Parameter("Minimum SL (pips)", DefaultValue = 5.0, MinValue = 3.0, MaxValue = 15.0, Step = 1.0, Group = "MTF SMA Alignment")]
        public double MinimumSLPips { get; set; }

        #endregion

        #region Parameters - ADX Filter

        [Parameter("=== ADX FILTER ===", DefaultValue = "")]
        public string ADXHeader { get; set; }

        [Parameter("Enable ADX Filter", DefaultValue = false, Group = "ADX Filter")]
        public bool EnableADXFilter { get; set; }

        [Parameter("ADX Mode", DefaultValue = ADXFilterMode.BlockEntry, Group = "ADX Filter")]
        public ADXFilterMode ADXMode { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 7, MaxValue = 28, Step = 1, Group = "ADX Filter")]
        public int ADXPeriod { get; set; }

        [Parameter("ADX Min Threshold", DefaultValue = 20.0, MinValue = 15.0, MaxValue = 35.0, Step = 5.0, Group = "ADX Filter")]
        public double ADXMinThreshold { get; set; }

        #endregion

        #region Parameters - Trade Management

        [Parameter("=== TRADE MANAGEMENT ===", DefaultValue = "")]
        public string TradeHeader { get; set; }

        [Parameter("Enable Trading", DefaultValue = false, Group = "Trade Management")]
        public bool EnableTrading { get; set; }

        [Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.5, MaxValue = 3.0, Step = 0.25, Group = "Trade Management")]
        public double RiskPercent { get; set; }

        [Parameter("SL Buffer Pips", DefaultValue = 4.0, MinValue = 1.0, MaxValue = 6.0, Step = 0.5, Group = "Trade Management")]
        public double SLBufferPips { get; set; }

        [Parameter("Minimum RR Ratio", DefaultValue = 5.0, MinValue = 1.5, MaxValue = 7.0, Step = 0.5, Group = "Trade Management")]
        public double MinimumRRRatio { get; set; }

        [Parameter("Max Positions", DefaultValue = 1, MinValue = 1, MaxValue = 3, Step = 1, Group = "Trade Management")]
        public int MaxPositions { get; set; }

        [Parameter("Magic Number", DefaultValue = 100001, Group = "Trade Management")]
        public int MagicNumber { get; set; }

        #endregion

        #region Parameters - Session Management

        [Parameter("=== SESSION MANAGEMENT ===", DefaultValue = "")]
        public string SessionHeader { get; set; }

        [Parameter("Enable Session Filter", DefaultValue = true, Group = "Session Management")]
        public bool EnableSessionFilter { get; set; }

        [Parameter("Show Session Boxes", DefaultValue = false, Group = "Session Management")]
        public bool ShowSessionBoxes { get; set; }

        #endregion

        #region Parameters - Chandelier Trailing Stop

        [Parameter("=== CHANDELIER SL ===", DefaultValue = "")]
        public string ChandelierHeader { get; set; }

        [Parameter("Enable Chandelier SL", DefaultValue = true, Group = "Chandelier SL")]
        public bool EnableChandelierSL { get; set; }

        [Parameter("Activation RR Fraction", DefaultValue = 0.75, MinValue = 0.5, MaxValue = 1.0, Step = 0.05, Group = "Chandelier SL")]
        public double ChandelierActivationRR { get; set; }

        [Parameter("Trail Increment (pips)", DefaultValue = 5.0, MinValue = 3.0, MaxValue = 20.0, Step = 1.0, Group = "Chandelier SL")]
        public double TrailIncrementPips { get; set; }

        [Parameter("TP Mode", DefaultValue = ChandelierTPMode.RemoveTP, Group = "Chandelier SL")]
        public ChandelierTPMode TPModeSelection { get; set; }

        [Parameter("Min SL Distance (pips)", DefaultValue = 5.0, MinValue = 3.0, MaxValue = 10.0, Step = 1.0, Group = "Chandelier SL")]
        public double MinChandelierDistance { get; set; }

        #endregion

        #region Parameters - Exhaustion Exit

        [Parameter("=== EXHAUSTION EXIT ===", DefaultValue = "")]
        public string ExhaustionHeader { get; set; }

        [Parameter("Enable Exhaustion Exit", DefaultValue = false, Group = "Exhaustion Exit")]
        public bool EnableExhaustionExit { get; set; }

        [Parameter("Min Chandelier Moves", DefaultValue = 2, MinValue = 1, MaxValue = 5, Step = 1, Group = "Exhaustion Exit")]
        public int MinChandelierMovesBeforeExit { get; set; }

        [Parameter("Swing Lookback Bars", DefaultValue = 8, MinValue = 3, MaxValue = 15, Step = 1, Group = "Exhaustion Exit")]
        public int ExhaustionSwingBars { get; set; }

        [Parameter("RSI Period", DefaultValue = 14, MinValue = 6, MaxValue = 21, Step = 1, Group = "Exhaustion Exit")]
        public int ExhaustionRSIPeriod { get; set; }

        #endregion

        #region Parameters - Risk Management

        [Parameter("=== RISK MANAGEMENT ===", DefaultValue = "")]
        public string RiskHeader { get; set; }

        [Parameter("Enable Daily Loss Limit", DefaultValue = true, Group = "Risk Management")]
        public bool EnableDailyLossLimit { get; set; }

        [Parameter("Max Daily R Loss", DefaultValue = -3.0, MinValue = -10.0, MaxValue = -1.0, Step = 0.5, Group = "Risk Management")]
        public double MaxDailyRLoss { get; set; }

        [Parameter("Max Daily Losing Trades", DefaultValue = 5, MinValue = 1, MaxValue = 20, Step = 1, Group = "Risk Management")]
        public int MaxDailyLosingTrades { get; set; }

        [Parameter("Enable Consecutive Loss Limit", DefaultValue = false, Group = "Risk Management")]
        public bool EnableConsecutiveLossLimit { get; set; }

        [Parameter("Max Consecutive Losses", DefaultValue = 9, MinValue = 5, MaxValue = 20, Step = 1, Group = "Risk Management")]
        public int MaxConsecutiveLosses { get; set; }

        [Parameter("Enable Monthly DD Limit", DefaultValue = true, Group = "Risk Management")]
        public bool EnableMonthlyDrawdownLimit { get; set; }

        [Parameter("Max Monthly DD %", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 20.0, Step = 1.0, Group = "Risk Management")]
        public double MaxMonthlyDrawdownPercent { get; set; }

        #endregion

        #region Parameters - Diagnostics

        [Parameter("=== DIAGNOSTICS ===", DefaultValue = "")]
        public string DiagnosticsHeader { get; set; }

        [Parameter("Enable Diagnostics", DefaultValue = false, Group = "Diagnostics")]
        public bool EnableDiagnostics { get; set; }

        [Parameter("Diagnostic Interval (bars)", DefaultValue = 60, MinValue = 5, MaxValue = 240, Step = 5, Group = "Diagnostics")]
        public int DiagnosticIntervalBars { get; set; }

        #endregion

        #region Enums

        public enum ChandelierTPMode
        {
            KeepOriginal,
            RemoveTP,
            TrailingTP
        }

        public enum ADXFilterMode
        {
            BlockEntry,      // Low ADX = skip entry (original)
            FlipDirection    // Low ADX = reverse direction (contrarian)
        }

        public enum OptimalPeriod
        {
            None,
            BestOverlap,
            GoodLondonOpen,
            DangerDeadZone,
            DangerLateNY
        }

        public enum ExhaustionState
        {
            Monitoring,
            PatternDetected,
            Confirmed,
            Invalidated
        }

        #endregion

        #region Swing Point Class

        private class SwingPoint
        {
            public double Price { get; set; }
            public double RSIValue { get; set; }
            public int BarIndex { get; set; }
        }

        #endregion

        #region Private Fields

        // MTF Bar Data
        private Bars m1Bars;
        private Bars tf2Bars;
        private Bars tf3Bars;

        // Indicators
        private AverageTrueRange atrM1;
        private DirectionalMovementSystem adxIndicator;
        private RelativeStrengthIndex exhaustionRSI;

        // MTF SMA tracking
        private string _previousM1Alignment = "";

        // Chandelier state tracking
        private Dictionary<int, ChandelierState> _chandelierStates;

        // Daily loss tracking
        private double _dailyRLoss = 0.0;
        private int _dailyLosingTrades = 0;
        private DateTime _lastTradingDay = DateTime.MinValue;
        private bool _dailyLimitReached = false;

        // Consecutive loss tracking
        private int _consecutiveLosses = 0;
        private bool _consecutiveLimitReached = false;

        // Monthly drawdown tracking
        private double _monthStartEquity = 0.0;
        private int _lastTradingMonth = 0;
        private int _lastTradingYear = 0;
        private bool _monthlyLimitReached = false;

        // Session box tracking
        private OptimalPeriod _lastDrawnPeriod = OptimalPeriod.None;

        // Session colors
        private readonly Color ColorBestTime = Color.FromArgb(50, 0, 255, 0);
        private readonly Color ColorGoodTime = Color.FromArgb(45, 255, 215, 0);
        private readonly Color ColorDangerZone = Color.FromArgb(40, 255, 0, 0);

        #endregion

        #region Chandelier State Class

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
            public double PriceWatermark { get; set; }
            public int LastIncrementCount { get; set; }
            public TradeType TradeDirection { get; set; }

            // Exhaustion Exit tracking (v4.1)
            public int ChandelierMoveCount { get; set; }
            public List<SwingPoint> SwingHistory { get; set; }
            public ExhaustionState ExhaustionStatus { get; set; }
            public double ConfirmationPrice { get; set; }
            public int ConfirmationBarIndex { get; set; }
        }

        #endregion

        #region Initialization

        protected override void OnStart()
        {
            Print("========================================");
            Print("Jcamp 1M Scalping v{0} ({1})", BOT_VERSION, VERSION_DATE);
            Print("Notes: {0}", VERSION_NOTES);
            Print("========================================");

            // Validate timeframe
            if (TimeFrame != TimeFrame.Minute && TimeFrame != TimeFrame.Minute15)
            {
                Print("ERROR: This cBot must run on M1 or M15 timeframe!");
                Stop();
                return;
            }

            // Initialize MTF bar data
            m1Bars = MarketData.GetBars(TimeFrame.Minute);
            tf2Bars = MarketData.GetBars(Timeframe2);
            tf3Bars = MarketData.GetBars(Timeframe3);

            Print("[MTF-SMA] Initialized | M1 + {0} + {1} | SMA Period: {2}",
                Timeframe2, Timeframe3, MTFSMAPeriod);

            // Initialize ATR indicator
            atrM1 = Indicators.AverageTrueRange(m1Bars, ATRPeriod, MovingAverageType.Simple);
            Print("[ATR] Period: {0} | Multiplier: {1:F2}x", ATRPeriod, SLATRMultiplier);

            // Initialize ADX indicator
            if (EnableADXFilter)
            {
                adxIndicator = Indicators.DirectionalMovementSystem(m1Bars, ADXPeriod);
                Print("[ADX] Enabled | Period: {0} | Threshold: {1:F0}", ADXPeriod, ADXMinThreshold);
            }

            // Initialize Exhaustion RSI indicator
            if (EnableExhaustionExit)
            {
                exhaustionRSI = Indicators.RelativeStrengthIndex(m1Bars.ClosePrices, ExhaustionRSIPeriod);
                Print("[EXHAUSTION] Enabled | RSI Period: {0} | Swing Bars: {1} | Min Moves: {2}",
                    ExhaustionRSIPeriod, ExhaustionSwingBars, MinChandelierMovesBeforeExit);
            }

            // Initialize chandelier tracking
            _chandelierStates = new Dictionary<int, ChandelierState>();

            // Initialize monthly drawdown tracking
            if (EnableMonthlyDrawdownLimit)
            {
                _monthStartEquity = Account.Equity;
                _lastTradingMonth = Server.Time.Month;
                _lastTradingYear = Server.Time.Year;
                Print("[MONTHLY-DD] Enabled | Max DD: {0:F0}% | Start Equity: {1:F2}",
                    MaxMonthlyDrawdownPercent, _monthStartEquity);
            }

            // Initialize daily loss limit
            if (EnableDailyLossLimit)
            {
                Print("[DAILY-LIMIT] Enabled | Max R Loss: {0:F1} | Max Losing Trades: {1}",
                    MaxDailyRLoss, MaxDailyLosingTrades);
            }

            // Initialize consecutive loss limit
            if (EnableConsecutiveLossLimit)
            {
                Print("[CONSECUTIVE-LOSS] Enabled | Max Consecutive Losses: {0} (Warning: Re-optimize or switch pair)",
                    MaxConsecutiveLosses);
            }

            // Subscribe to position events
            Positions.Closed += OnPositionClosedHandler;

            Print("Trading Enabled: {0} | Session Filter: {1}", EnableTrading, EnableSessionFilter);
            Print("ADX Filter: {0} | Exhaustion Exit: {1}", EnableADXFilter, EnableExhaustionExit);
            Print("Daily Limit: {0} | Consecutive Loss Limit: {1} | Monthly DD Limit: {2}", EnableDailyLossLimit, EnableConsecutiveLossLimit, EnableMonthlyDrawdownLimit);
            Print("Risk: {0:F1}% | Min RR: {1:F1} | Max Positions: {2}", RiskPercent, MinimumRRRatio, MaxPositions);
            Print("Diagnostics: {0} | Interval: {1} bars ({2} min on M1)", EnableDiagnostics, DiagnosticIntervalBars, DiagnosticIntervalBars);
            Print("========================================");
        }

        #endregion

        #region OnBar - Main Loop

        protected override void OnBar()
        {
            // DIAGNOSTIC: Log heartbeat to verify cBot is running
            if (EnableDiagnostics && Bars.Count % DiagnosticIntervalBars == 0)
            {
                Print("[DIAGNOSTIC] cBot is running | Time: {0} | Equity: {1:F2}",
                    Server.Time, Account.Equity);
            }

            // Check daily reset
            CheckDailyReset();

            // Draw session boxes
            if (ShowSessionBoxes)
            {
                DrawSessionBoxes();
            }

            // Process MTF SMA entry
            if (EnableMTFSMAEntry)
            {
                ProcessMTFSMAEntry();
            }
            else if (EnableDiagnostics && Bars.Count % DiagnosticIntervalBars == 0)
            {
                Print("[DIAGNOSTIC] MTF SMA Entry is DISABLED in parameters");
            }

            // Process chandelier trailing stops
            if (EnableChandelierSL)
            {
                ProcessChandelierStops();
            }
        }

        #endregion

        #region MTF SMA Entry System

        private double CalculateSMAForBars(Bars bars, int period)
        {
            int count = Math.Min(period, bars.Count);
            if (count <= 0) return 0;

            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += bars.ClosePrices.Last(i);
            }
            return sum / count;
        }

        private string GetSMAAlignment(Bars bars)
        {
            if (bars == null || bars.Count < MTFSMAPeriod)
                return "NONE";

            double price = bars.ClosePrices.LastValue;
            double sma = CalculateSMAForBars(bars, MTFSMAPeriod);

            if (sma <= 0) return "NONE";

            return price > sma ? "BUY" : "SELL";
        }

        private bool CheckMTFAlignment(out string direction)
        {
            direction = "NONE";

            if (m1Bars == null || tf2Bars == null || tf3Bars == null)
                return false;

            string m1 = GetSMAAlignment(m1Bars);
            string tf2 = GetSMAAlignment(tf2Bars);
            string tf3 = GetSMAAlignment(tf3Bars);

            bool allBuy = (m1 == "BUY" && tf2 == "BUY" && tf3 == "BUY");
            bool allSell = (m1 == "SELL" && tf2 == "SELL" && tf3 == "SELL");

            if (RequireAllTFsAligned)
            {
                if (allBuy) { direction = "BUY"; return true; }
                if (allSell) { direction = "SELL"; return true; }
                return false;
            }
            else
            {
                int buyCount = (m1 == "BUY" ? 1 : 0) + (tf2 == "BUY" ? 1 : 0) + (tf3 == "BUY" ? 1 : 0);
                int sellCount = (m1 == "SELL" ? 1 : 0) + (tf2 == "SELL" ? 1 : 0) + (tf3 == "SELL" ? 1 : 0);

                if (buyCount >= 2) { direction = "BUY"; return true; }
                if (sellCount >= 2) { direction = "SELL"; return true; }
                return false;
            }
        }

        private bool DetectM1Crossover(out string direction)
        {
            direction = "NONE";

            if (m1Bars == null) return false;

            string current = GetSMAAlignment(m1Bars);

            bool crossed = !string.IsNullOrEmpty(_previousM1Alignment)
                           && _previousM1Alignment != "NONE"
                           && _previousM1Alignment != current
                           && current != "NONE";

            direction = current;
            _previousM1Alignment = current;

            return crossed;
        }

        private void ProcessMTFSMAEntry()
        {
            // DIAGNOSTIC: Show why entries are blocked
            bool showDiagnostics = EnableDiagnostics && (Bars.Count % DiagnosticIntervalBars == 0);

            if (showDiagnostics)
            {
                Print("[DIAGNOSTIC] ProcessMTFSMAEntry called - checking entry conditions...");
            }

            // Skip if trading disabled or position open
            if (!EnableTrading)
            {
                if (showDiagnostics) Print("[DIAGNOSTIC] Trading is DISABLED");
                return;
            }

            if (HasOpenPosition())
            {
                if (showDiagnostics) Print("[DIAGNOSTIC] Position already open");
                return;
            }

            // Check daily loss limit
            if (IsDailyLimitReached())
            {
                if (showDiagnostics) Print("[DIAGNOSTIC] Daily limit reached | R Loss: {0:F2} | Losses: {1}",
                    _dailyRLoss, _dailyLosingTrades);
                return;
            }

            // Check monthly drawdown limit
            if (IsMonthlyLimitReached())
            {
                if (showDiagnostics) Print("[DIAGNOSTIC] Monthly limit reached");
                return;
            }

            // Check session filter
            if (EnableSessionFilter)
            {
                var period = GetOptimalPeriod(Server.Time);
                if (period != OptimalPeriod.BestOverlap && period != OptimalPeriod.GoodLondonOpen)
                {
                    if (showDiagnostics) Print("[DIAGNOSTIC] Outside trading hours | Current: {0}", period);
                    return;
                }
            }

            // Check ADX filter
            bool flipDirection = false;
            double adxValue = 0;
            if (EnableADXFilter && adxIndicator != null)
            {
                adxValue = adxIndicator.ADX.LastValue;
                if (adxValue < ADXMinThreshold)
                {
                    if (ADXMode == ADXFilterMode.BlockEntry)
                    {
                        // Ranging market, skip entry
                        if (showDiagnostics) Print("[DIAGNOSTIC] ADX filter blocking entry | ADX: {0:F1} < Threshold: {1:F1} | Mode: BlockEntry",
                            adxValue, ADXMinThreshold);
                        return;
                    }
                    else if (ADXMode == ADXFilterMode.FlipDirection)
                    {
                        // Ranging market, flip direction (contrarian)
                        flipDirection = true;
                        if (showDiagnostics) Print("[DIAGNOSTIC] ADX filter will flip direction | ADX: {0:F1} < Threshold: {1:F1}",
                            adxValue, ADXMinThreshold);
                    }
                }
                else if (showDiagnostics)
                {
                    Print("[DIAGNOSTIC] ADX filter passed | ADX: {0:F1} >= Threshold: {1:F1}", adxValue, ADXMinThreshold);
                }
            }

            // Check MTF alignment
            if (!CheckMTFAlignment(out string alignmentDirection))
            {
                if (showDiagnostics)
                {
                    string m1 = GetSMAAlignment(m1Bars);
                    string tf2 = GetSMAAlignment(tf2Bars);
                    string tf3 = GetSMAAlignment(tf3Bars);
                    Print("[DIAGNOSTIC] MTF not aligned | M1: {0} | TF2: {1} | TF3: {2}", m1, tf2, tf3);
                }
                return;
            }
            else if (showDiagnostics)
            {
                string m1 = GetSMAAlignment(m1Bars);
                string tf2 = GetSMAAlignment(tf2Bars);
                string tf3 = GetSMAAlignment(tf3Bars);
                Print("[DIAGNOSTIC] MTF aligned {0} | M1: {1} | TF2: {2} | TF3: {3} | Waiting for M1 crossover...",
                    alignmentDirection, m1, tf2, tf3);
            }

            // Check for M1 crossover
            if (DetectM1Crossover(out string m1Direction))
            {
                if (m1Direction == alignmentDirection)
                {
                    // Apply flip if ADX is low and mode is FlipDirection
                    string tradeDirection = alignmentDirection;
                    if (flipDirection)
                    {
                        tradeDirection = (alignmentDirection == "BUY") ? "SELL" : "BUY";
                        Print("[MTF-SMA] All TFs aligned {0} | ADX: {1:F1} (low) | FLIP → {2}",
                            alignmentDirection, adxValue, tradeDirection);
                    }
                    else
                    {
                        Print("[MTF-SMA] All TFs aligned {0} | M1 crossover | ADX: {1:F1} | ENTRY",
                            tradeDirection, adxValue);
                    }

                    if (tradeDirection == "BUY")
                        ExecuteBuyTrade();
                    else
                        ExecuteSellTrade();
                }
            }
        }

        #endregion

        #region Trade Execution

        private void ExecuteBuyTrade()
        {
            double entryPrice = Symbol.Ask;

            // Calculate SL using ATR
            double atrValue = atrM1.Result.LastValue;
            double slPips = (atrValue * SLATRMultiplier) / Symbol.PipSize;

            // Enforce minimum SL
            if (slPips < MinimumSLPips)
                slPips = MinimumSLPips;

            double stopLoss = entryPrice - (slPips * Symbol.PipSize) - (SLBufferPips * Symbol.PipSize);

            // Calculate risk in pips
            double riskPips = (entryPrice - stopLoss) / Symbol.PipSize;

            // Calculate position size
            double volume = CalculatePositionSize(riskPips);
            if (volume <= 0)
            {
                Print("[BUY] Position size too small - skipping");
                return;
            }

            // Calculate TP based on minimum RR
            double takeProfit = entryPrice + (riskPips * MinimumRRRatio * Symbol.PipSize);

            Print("[BUY] Entry: {0:F5} | SL: {1:F5} ({2:F1} pips) | TP: {3:F5} | Vol: {4:F2}",
                entryPrice, stopLoss, riskPips, takeProfit, volume);

            var result = ExecuteMarketOrder(TradeType.Buy, SymbolName, volume, MagicNumber.ToString(),
                riskPips, riskPips * MinimumRRRatio);

            if (result.IsSuccessful)
            {
                Print("[BUY] SUCCESS | Position ID: {0}", result.Position.Id);
                InitializeChandelierState(result.Position, entryPrice, stopLoss, takeProfit);
            }
            else
            {
                Print("[BUY] FAILED | Error: {0}", result.Error);
            }
        }

        private void ExecuteSellTrade()
        {
            double entryPrice = Symbol.Bid;

            // Calculate SL using ATR
            double atrValue = atrM1.Result.LastValue;
            double slPips = (atrValue * SLATRMultiplier) / Symbol.PipSize;

            // Enforce minimum SL
            if (slPips < MinimumSLPips)
                slPips = MinimumSLPips;

            double stopLoss = entryPrice + (slPips * Symbol.PipSize) + (SLBufferPips * Symbol.PipSize);

            // Calculate risk in pips
            double riskPips = (stopLoss - entryPrice) / Symbol.PipSize;

            // Calculate position size
            double volume = CalculatePositionSize(riskPips);
            if (volume <= 0)
            {
                Print("[SELL] Position size too small - skipping");
                return;
            }

            // Calculate TP based on minimum RR
            double takeProfit = entryPrice - (riskPips * MinimumRRRatio * Symbol.PipSize);

            Print("[SELL] Entry: {0:F5} | SL: {1:F5} ({2:F1} pips) | TP: {3:F5} | Vol: {4:F2}",
                entryPrice, stopLoss, riskPips, takeProfit, volume);

            var result = ExecuteMarketOrder(TradeType.Sell, SymbolName, volume, MagicNumber.ToString(),
                riskPips, riskPips * MinimumRRRatio);

            if (result.IsSuccessful)
            {
                Print("[SELL] SUCCESS | Position ID: {0}", result.Position.Id);
                InitializeChandelierState(result.Position, entryPrice, stopLoss, takeProfit);
            }
            else
            {
                Print("[SELL] FAILED | Error: {0}", result.Error);
            }
        }

        private double CalculatePositionSize(double riskPips)
        {
            double riskAmount = Account.Balance * (RiskPercent / 100.0);
            double pipValue = Symbol.PipValue;
            double volumeInUnits = riskAmount / (riskPips * pipValue);

            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            if (volumeInUnits < Symbol.VolumeInUnitsMin)
                return 0;

            return volumeInUnits;
        }

        #endregion

        #region Chandelier Trailing Stop

        private void InitializeChandelierState(Position position, double entryPrice, double stopLoss, double takeProfit)
        {
            if (!EnableChandelierSL) return;

            double activationDistance = Math.Abs(takeProfit - entryPrice) * ChandelierActivationRR;
            double activationPrice = position.TradeType == TradeType.Buy
                ? entryPrice + activationDistance
                : entryPrice - activationDistance;

            var state = new ChandelierState
            {
                PositionId = position.Id,
                IsActivated = false,
                EntryPrice = entryPrice,
                OriginalTP = takeProfit,
                OriginalSL = stopLoss,
                ActivationPrice = activationPrice,
                BreakevenPrice = entryPrice,
                CurrentTrailingSL = stopLoss,
                PriceWatermark = entryPrice,
                LastIncrementCount = 0,
                TradeDirection = position.TradeType,
                // Exhaustion tracking
                ChandelierMoveCount = 0,
                SwingHistory = new List<SwingPoint>(),
                ExhaustionStatus = ExhaustionState.Monitoring,
                ConfirmationPrice = 0,
                ConfirmationBarIndex = 0
            };

            _chandelierStates[position.Id] = state;
            Print("[CHANDELIER] Tracking position {0} | Activation: {1:F5}", position.Id, activationPrice);
        }

        private void ProcessChandelierStops()
        {
            var positionsToRemove = new List<int>();

            foreach (var kvp in _chandelierStates)
            {
                var state = kvp.Value;
                var position = Positions.FirstOrDefault(p => p.Id == state.PositionId);

                if (position == null)
                {
                    positionsToRemove.Add(kvp.Key);
                    continue;
                }

                double currentPrice = state.TradeDirection == TradeType.Buy ? Symbol.Bid : Symbol.Ask;

                // Check activation
                if (!state.IsActivated)
                {
                    bool shouldActivate = state.TradeDirection == TradeType.Buy
                        ? currentPrice >= state.ActivationPrice
                        : currentPrice <= state.ActivationPrice;

                    if (shouldActivate)
                    {
                        state.IsActivated = true;
                        state.PriceWatermark = currentPrice;
                        Print("[CHANDELIER] ACTIVATED | Position {0} | Price: {1:F5}", position.Id, currentPrice);

                        // Remove TP if configured
                        if (TPModeSelection == ChandelierTPMode.RemoveTP)
                        {
                            ModifyPosition(position, position.StopLoss, null);
                        }
                    }
                }

                // Trail SL if activated
                if (state.IsActivated)
                {
                    // Update watermark
                    if (state.TradeDirection == TradeType.Buy)
                        state.PriceWatermark = Math.Max(state.PriceWatermark, currentPrice);
                    else
                        state.PriceWatermark = Math.Min(state.PriceWatermark, currentPrice);

                    // Calculate trailing increments
                    double profitFromEntry = state.TradeDirection == TradeType.Buy
                        ? state.PriceWatermark - state.EntryPrice
                        : state.EntryPrice - state.PriceWatermark;

                    double incrementSize = TrailIncrementPips * Symbol.PipSize;
                    int currentIncrements = (int)(profitFromEntry / incrementSize);

                    if (currentIncrements > state.LastIncrementCount)
                    {
                        // Calculate new SL
                        double newSL;
                        if (state.TradeDirection == TradeType.Buy)
                        {
                            newSL = state.EntryPrice + ((currentIncrements - 1) * incrementSize);
                            newSL = Math.Max(newSL, state.CurrentTrailingSL);
                        }
                        else
                        {
                            newSL = state.EntryPrice - ((currentIncrements - 1) * incrementSize);
                            newSL = Math.Min(newSL, state.CurrentTrailingSL);
                        }

                        // Check minimum distance from price
                        double distanceFromPrice = Math.Abs(currentPrice - newSL) / Symbol.PipSize;
                        if (distanceFromPrice >= MinChandelierDistance)
                        {
                            ModifyPosition(position, newSL, position.TakeProfit);
                            state.CurrentTrailingSL = newSL;
                            state.LastIncrementCount = currentIncrements;
                            state.ChandelierMoveCount++;
                            Print("[CHANDELIER] Trail #{0} | New SL: {1:F5} | Move Count: {2}",
                                currentIncrements, newSL, state.ChandelierMoveCount);
                        }
                    }

                    // Check exhaustion exit if enabled
                    if (EnableExhaustionExit && state.ChandelierMoveCount >= MinChandelierMovesBeforeExit)
                    {
                        CheckExhaustionExit(position, state);
                    }
                }
            }

            // Cleanup closed positions
            foreach (var id in positionsToRemove)
            {
                _chandelierStates.Remove(id);
            }
        }

        #endregion

        #region Exhaustion Exit

        private void CheckExhaustionExit(Position position, ChandelierState state)
        {
            // Safety checks
            if (exhaustionRSI == null || m1Bars.Count < ExhaustionSwingBars + 5)
                return;

            if (double.IsNaN(exhaustionRSI.Result.LastValue))
                return;

            int currentBarIndex = m1Bars.Count - 1;

            // Handle confirmation state from previous bar
            if (state.ExhaustionStatus == ExhaustionState.PatternDetected)
            {
                // Check if we're on a new bar after pattern detection
                if (currentBarIndex > state.ConfirmationBarIndex)
                {
                    CheckConfirmation(position, state);
                    return;
                }
            }

            // Update swing history
            UpdateSwingHistory(state, currentBarIndex);

            // Need at least 3 swings for pattern detection
            if (state.SwingHistory.Count < 3)
                return;

            // Check for exhaustion pattern
            bool patternDetected = state.TradeDirection == TradeType.Sell
                ? CheckBullishDivergence(state)  // SELL exhaustion = bullish divergence
                : CheckBearishDivergence(state); // BUY exhaustion = bearish divergence

            if (patternDetected)
            {
                state.ExhaustionStatus = ExhaustionState.PatternDetected;
                state.ConfirmationBarIndex = currentBarIndex;

                // Set confirmation price based on the most recent swing
                state.ConfirmationPrice = state.SwingHistory[state.SwingHistory.Count - 1].Price;

                Print("[EXHAUSTION] Pattern detected | Position {0} | Confirmation Level: {1:F5}",
                    position.Id, state.ConfirmationPrice);
            }
        }

        private void UpdateSwingHistory(ChandelierState state, int currentBarIndex)
        {
            SwingPoint newSwing = null;

            if (state.TradeDirection == TradeType.Sell)
            {
                // For SELL positions, detect swing lows (higher lows = bullish exhaustion)
                if (DetectSwingLow(out double swingPrice, out double rsiValue))
                {
                    newSwing = new SwingPoint
                    {
                        Price = swingPrice,
                        RSIValue = rsiValue,
                        BarIndex = currentBarIndex
                    };
                }
            }
            else
            {
                // For BUY positions, detect swing highs (lower highs = bearish exhaustion)
                if (DetectSwingHigh(out double swingPrice, out double rsiValue))
                {
                    newSwing = new SwingPoint
                    {
                        Price = swingPrice,
                        RSIValue = rsiValue,
                        BarIndex = currentBarIndex
                    };
                }
            }

            if (newSwing != null)
            {
                // Avoid duplicate swings on same bar
                if (state.SwingHistory.Count > 0 &&
                    state.SwingHistory[state.SwingHistory.Count - 1].BarIndex == currentBarIndex)
                {
                    state.SwingHistory[state.SwingHistory.Count - 1] = newSwing;
                }
                else
                {
                    state.SwingHistory.Add(newSwing);
                    // Keep only last 3 swings
                    while (state.SwingHistory.Count > 3)
                    {
                        state.SwingHistory.RemoveAt(0);
                    }
                }
            }
        }

        private bool DetectSwingLow(out double swingPrice, out double rsiValue)
        {
            swingPrice = 0;
            rsiValue = 0;

            // Find lowest low in the lookback window
            double lowestLow = double.MaxValue;
            int lowestIndex = 0;

            for (int i = 0; i < ExhaustionSwingBars; i++)
            {
                double low = m1Bars.LowPrices.Last(i);
                if (low < lowestLow)
                {
                    lowestLow = low;
                    lowestIndex = i;
                }
            }

            // Swing low is valid if it's in the middle (not at edges)
            if (lowestIndex > 0 && lowestIndex < ExhaustionSwingBars - 1)
            {
                swingPrice = lowestLow;
                rsiValue = exhaustionRSI.Result.Last(lowestIndex);
                return true;
            }

            return false;
        }

        private bool DetectSwingHigh(out double swingPrice, out double rsiValue)
        {
            swingPrice = 0;
            rsiValue = 0;

            // Find highest high in the lookback window
            double highestHigh = double.MinValue;
            int highestIndex = 0;

            for (int i = 0; i < ExhaustionSwingBars; i++)
            {
                double high = m1Bars.HighPrices.Last(i);
                if (high > highestHigh)
                {
                    highestHigh = high;
                    highestIndex = i;
                }
            }

            // Swing high is valid if it's in the middle (not at edges)
            if (highestIndex > 0 && highestIndex < ExhaustionSwingBars - 1)
            {
                swingPrice = highestHigh;
                rsiValue = exhaustionRSI.Result.Last(highestIndex);
                return true;
            }

            return false;
        }

        private bool CheckBullishDivergence(ChandelierState state)
        {
            // For SELL positions: Price making Higher Lows BUT RSI making Lower Lows
            if (state.SwingHistory.Count < 3) return false;

            var swing0 = state.SwingHistory[0]; // Oldest
            var swing1 = state.SwingHistory[1];
            var swing2 = state.SwingHistory[2]; // Most recent

            // Price making higher lows (bullish structure)
            bool priceHL1 = swing1.Price > swing0.Price;
            bool priceHL2 = swing2.Price > swing1.Price;

            // RSI making lower lows (momentum weakening)
            bool rsiLL1 = swing1.RSIValue < swing0.RSIValue;
            bool rsiLL2 = swing2.RSIValue < swing1.RSIValue;

            if (priceHL1 && priceHL2 && rsiLL1 && rsiLL2)
            {
                Print("[EXHAUSTION] Bullish divergence | Price HL: {0:F5} → {1:F5} → {2:F5} | RSI LL: {3:F1} → {4:F1} → {5:F1}",
                    swing0.Price, swing1.Price, swing2.Price,
                    swing0.RSIValue, swing1.RSIValue, swing2.RSIValue);
                return true;
            }

            return false;
        }

        private bool CheckBearishDivergence(ChandelierState state)
        {
            // For BUY positions: Price making Lower Highs BUT RSI making Higher Highs
            if (state.SwingHistory.Count < 3) return false;

            var swing0 = state.SwingHistory[0]; // Oldest
            var swing1 = state.SwingHistory[1];
            var swing2 = state.SwingHistory[2]; // Most recent

            // Price making lower highs (bearish structure)
            bool priceLH1 = swing1.Price < swing0.Price;
            bool priceLH2 = swing2.Price < swing1.Price;

            // RSI making higher highs (momentum weakening)
            bool rsiHH1 = swing1.RSIValue > swing0.RSIValue;
            bool rsiHH2 = swing2.RSIValue > swing1.RSIValue;

            if (priceLH1 && priceLH2 && rsiHH1 && rsiHH2)
            {
                Print("[EXHAUSTION] Bearish divergence | Price LH: {0:F5} → {1:F5} → {2:F5} | RSI HH: {3:F1} → {4:F1} → {5:F1}",
                    swing0.Price, swing1.Price, swing2.Price,
                    swing0.RSIValue, swing1.RSIValue, swing2.RSIValue);
                return true;
            }

            return false;
        }

        private void CheckConfirmation(Position position, ChandelierState state)
        {
            double currentLow = m1Bars.LowPrices.LastValue;
            double currentHigh = m1Bars.HighPrices.LastValue;

            if (state.TradeDirection == TradeType.Sell)
            {
                // SELL: Invalidate if price breaks below HL2 (downtrend resumes)
                if (currentLow < state.ConfirmationPrice)
                {
                    state.ExhaustionStatus = ExhaustionState.Invalidated;
                    state.SwingHistory.Clear();
                    Print("[EXHAUSTION] INVALIDATED | Price broke below {0:F5} | Current Low: {1:F5}",
                        state.ConfirmationPrice, currentLow);
                    state.ExhaustionStatus = ExhaustionState.Monitoring;
                    return;
                }
            }
            else
            {
                // BUY: Invalidate if price breaks above LH2 (uptrend resumes)
                if (currentHigh > state.ConfirmationPrice)
                {
                    state.ExhaustionStatus = ExhaustionState.Invalidated;
                    state.SwingHistory.Clear();
                    Print("[EXHAUSTION] INVALIDATED | Price broke above {0:F5} | Current High: {1:F5}",
                        state.ConfirmationPrice, currentHigh);
                    state.ExhaustionStatus = ExhaustionState.Monitoring;
                    return;
                }
            }

            // Pattern confirmed - close position
            state.ExhaustionStatus = ExhaustionState.Confirmed;
            double closePrice = state.TradeDirection == TradeType.Buy ? Symbol.Bid : Symbol.Ask;

            Print("[EXHAUSTION] CONFIRMED | Closing {0} position {1} at {2:F5} | Profit: {3:F1} pips",
                state.TradeDirection, position.Id, closePrice, position.Pips);

            ClosePosition(position);
        }

        #endregion

        #region Session Management

        private OptimalPeriod GetOptimalPeriod(DateTime time)
        {
            int hour = time.Hour;

            if (hour >= 13 && hour < 17)
                return OptimalPeriod.BestOverlap;

            if (hour >= 8 && hour < 12)
                return OptimalPeriod.GoodLondonOpen;

            if (hour >= 4 && hour < 8)
                return OptimalPeriod.DangerDeadZone;

            if (hour >= 20 || hour < 4)
                return OptimalPeriod.DangerLateNY;

            return OptimalPeriod.None;
        }

        private void DrawSessionBoxes()
        {
            DateTime currentTime = m1Bars.OpenTimes.LastValue;
            OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);

            if (currentPeriod != _lastDrawnPeriod && currentPeriod != OptimalPeriod.None)
            {
                DateTime periodStart = GetPeriodStart(currentPeriod, currentTime);
                DateTime periodEnd = GetPeriodEnd(currentPeriod, currentTime);
                Color periodColor = GetPeriodColor(currentPeriod);

                // Calculate Y-range from recent price action to span the visible chart
                // Get high/low from last 1000 bars to ensure box spans entire visible area
                int lookback = Math.Min(1000, m1Bars.Count - 1);
                double highest = m1Bars.HighPrices.Maximum(lookback);
                double lowest = m1Bars.LowPrices.Minimum(lookback);
                double range = highest - lowest;

                // Extend the range significantly for full chart coverage
                double yTop = highest + (range * 10);
                double yBottom = lowest - (range * 10);

                string name = string.Format("Session_{0}_{1}", currentPeriod, periodStart.ToString("yyyyMMdd_HHmm"));
                var rect = Chart.DrawRectangle(name, periodStart, yBottom, periodEnd, yTop, periodColor);
                rect.IsFilled = true;
                rect.Thickness = 0;

                _lastDrawnPeriod = currentPeriod;
            }

            if (currentPeriod == OptimalPeriod.None)
                _lastDrawnPeriod = OptimalPeriod.None;
        }

        private DateTime GetPeriodStart(OptimalPeriod period, DateTime currentTime)
        {
            DateTime date = currentTime.Date;
            switch (period)
            {
                case OptimalPeriod.BestOverlap: return date.AddHours(13);
                case OptimalPeriod.GoodLondonOpen: return date.AddHours(8);
                case OptimalPeriod.DangerDeadZone: return date.AddHours(4);
                case OptimalPeriod.DangerLateNY: return date.AddHours(20);
                default: return currentTime;
            }
        }

        private DateTime GetPeriodEnd(OptimalPeriod period, DateTime currentTime)
        {
            DateTime date = currentTime.Date;
            switch (period)
            {
                case OptimalPeriod.BestOverlap: return date.AddHours(17);
                case OptimalPeriod.GoodLondonOpen: return date.AddHours(12);
                case OptimalPeriod.DangerDeadZone: return date.AddHours(8);
                case OptimalPeriod.DangerLateNY: return date.AddDays(1).AddHours(4);
                default: return currentTime;
            }
        }

        private Color GetPeriodColor(OptimalPeriod period)
        {
            switch (period)
            {
                case OptimalPeriod.BestOverlap: return ColorBestTime;
                case OptimalPeriod.GoodLondonOpen: return ColorGoodTime;
                case OptimalPeriod.DangerDeadZone:
                case OptimalPeriod.DangerLateNY: return ColorDangerZone;
                default: return Color.Gray;
            }
        }

        #endregion

        #region Risk Management

        private bool HasOpenPosition()
        {
            return Positions.Count(p => p.Label == MagicNumber.ToString()) >= MaxPositions;
        }

        private bool IsDailyLimitReached()
        {
            // Check daily loss limit
            if (EnableDailyLossLimit && _dailyLimitReached)
                return true;

            // Check consecutive loss limit
            if (EnableConsecutiveLossLimit && _consecutiveLimitReached)
                return true;

            return false;
        }

        private void CheckDailyReset()
        {
            DateTime today = Server.Time.Date;
            if (today != _lastTradingDay)
            {
                _dailyRLoss = 0;
                _dailyLosingTrades = 0;
                _dailyLimitReached = false;
                _lastTradingDay = today;
            }

            // Check monthly reset
            CheckMonthlyReset();
        }

        private void CheckMonthlyReset()
        {
            if (!EnableMonthlyDrawdownLimit) return;

            int currentMonth = Server.Time.Month;
            int currentYear = Server.Time.Year;

            // New month started
            if (currentMonth != _lastTradingMonth || currentYear != _lastTradingYear)
            {
                _monthStartEquity = Account.Equity;
                _monthlyLimitReached = false;
                _lastTradingMonth = currentMonth;
                _lastTradingYear = currentYear;
                Print("[MONTHLY-DD] New month started | Reset equity baseline: {0:F2}", _monthStartEquity);
            }

            // Check if monthly drawdown limit reached
            if (!_monthlyLimitReached && _monthStartEquity > 0)
            {
                double currentDrawdown = (_monthStartEquity - Account.Equity) / _monthStartEquity * 100;
                if (currentDrawdown >= MaxMonthlyDrawdownPercent)
                {
                    _monthlyLimitReached = true;
                    Print("[MONTHLY-DD] LIMIT REACHED | DD: {0:F1}% | Start: {1:F2} | Current: {2:F2}",
                        currentDrawdown, _monthStartEquity, Account.Equity);
                    Print("[MONTHLY-DD] Trading paused until next month. Re-optimize parameters.");
                }
            }
        }

        private bool IsMonthlyLimitReached()
        {
            if (!EnableMonthlyDrawdownLimit) return false;
            return _monthlyLimitReached;
        }

        private void OnPositionClosedHandler(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (position.Label != MagicNumber.ToString()) return;

            // Remove chandelier state
            if (_chandelierStates.ContainsKey(position.Id))
                _chandelierStates.Remove(position.Id);

            // Track consecutive losses
            if (EnableConsecutiveLossLimit)
            {
                if (position.NetProfit < 0)
                {
                    _consecutiveLosses++;
                    if (_consecutiveLosses >= MaxConsecutiveLosses)
                    {
                        _consecutiveLimitReached = true;
                        Print("[RISK] CONSECUTIVE LOSS LIMIT | {0} losses in a row | Re-optimize parameters or switch pair", _consecutiveLosses);
                    }
                }
                else if (position.NetProfit > 0)
                {
                    // Reset on winning trade
                    _consecutiveLosses = 0;
                }
            }

            // Track daily losses
            if (EnableDailyLossLimit && position.NetProfit < 0)
            {
                double riskAmount = Account.Balance * (RiskPercent / 100.0);
                double rLoss = position.NetProfit / riskAmount;
                _dailyRLoss += rLoss;
                _dailyLosingTrades++;

                if (_dailyRLoss <= MaxDailyRLoss || _dailyLosingTrades >= MaxDailyLosingTrades)
                {
                    _dailyLimitReached = true;
                    Print("[RISK] Daily limit reached | R Loss: {0:F2} | Losses: {1}", _dailyRLoss, _dailyLosingTrades);
                }
            }
        }

        #endregion

        #region OnStop

        protected override void OnStop()
        {
            Print("========================================");
            Print("Jcamp 1M Scalping v{0} STOPPED", BOT_VERSION);
            Print("========================================");
        }

        #endregion
    }
}

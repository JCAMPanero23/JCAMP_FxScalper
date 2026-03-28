using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    /// <summary>
    /// Jcamp 1M Scalping Strategy - MTF SMA Alignment v4.0
    /// Entry: Trade when price > SMA on ALL configured timeframes (M1 + TF2 + TF3)
    /// Trigger: M1 SMA crossover while higher TFs already aligned
    /// </summary>
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Jcamp_1M_scalping : Robot
    {
        #region Version Info
        private const string BOT_VERSION = "4.0.0";
        private const string VERSION_DATE = "2026-03-28";
        private const string VERSION_NOTES = "MTF SMA Alignment - Clean implementation";
        #endregion

        #region Parameters - MTF SMA Alignment

        [Parameter("=== MTF SMA ALIGNMENT ===", DefaultValue = "")]
        public string MTFHeader { get; set; }

        [Parameter("Enable MTF SMA Entry", DefaultValue = true, Group = "MTF SMA Alignment")]
        public bool EnableMTFSMAEntry { get; set; }

        [Parameter("MTF SMA Period", DefaultValue = 200, MinValue = 50, MaxValue = 300, Step = 50, Group = "MTF SMA Alignment")]
        public int MTFSMAPeriod { get; set; }

        [Parameter("Timeframe 2", DefaultValue = "Minute3", Group = "MTF SMA Alignment")]
        public TimeFrame Timeframe2 { get; set; }

        [Parameter("Timeframe 3", DefaultValue = "Minute5", Group = "MTF SMA Alignment")]
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

        #region Parameters - Trade Management

        [Parameter("=== TRADE MANAGEMENT ===", DefaultValue = "")]
        public string TradeHeader { get; set; }

        [Parameter("Enable Trading", DefaultValue = false, Group = "Trade Management")]
        public bool EnableTrading { get; set; }

        [Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.5, MaxValue = 3.0, Step = 0.25, Group = "Trade Management")]
        public double RiskPercent { get; set; }

        [Parameter("SL Buffer Pips", DefaultValue = 2.0, MinValue = 1.0, MaxValue = 5.0, Step = 0.5, Group = "Trade Management")]
        public double SLBufferPips { get; set; }

        [Parameter("Minimum RR Ratio", DefaultValue = 2.0, MinValue = 1.5, MaxValue = 5.0, Step = 0.5, Group = "Trade Management")]
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

        #region Parameters - Risk Management

        [Parameter("=== RISK MANAGEMENT ===", DefaultValue = "")]
        public string RiskHeader { get; set; }

        [Parameter("Enable Daily Loss Limit", DefaultValue = false, Group = "Risk Management")]
        public bool EnableDailyLossLimit { get; set; }

        [Parameter("Max Daily R Loss", DefaultValue = -3.0, MinValue = -10.0, MaxValue = -1.0, Step = 0.5, Group = "Risk Management")]
        public double MaxDailyRLoss { get; set; }

        [Parameter("Max Daily Losing Trades", DefaultValue = 5, MinValue = 1, MaxValue = 20, Step = 1, Group = "Risk Management")]
        public int MaxDailyLosingTrades { get; set; }

        #endregion

        #region Enums

        public enum ChandelierTPMode
        {
            KeepOriginal,
            RemoveTP,
            TrailingTP
        }

        public enum OptimalPeriod
        {
            None,
            BestOverlap,
            GoodLondonOpen,
            DangerDeadZone,
            DangerLateNY
        }

        #endregion

        #region Private Fields

        // MTF Bar Data
        private Bars m1Bars;
        private Bars tf2Bars;
        private Bars tf3Bars;

        // Indicators
        private AverageTrueRange atrM1;

        // MTF SMA tracking
        private string _previousM1Alignment = "";

        // Chandelier state tracking
        private Dictionary<int, ChandelierState> _chandelierStates;

        // Daily loss tracking
        private double _dailyRLoss = 0.0;
        private int _dailyLosingTrades = 0;
        private DateTime _lastTradingDay = DateTime.MinValue;
        private bool _dailyLimitReached = false;

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

            // Initialize chandelier tracking
            _chandelierStates = new Dictionary<int, ChandelierState>();

            // Subscribe to position events
            Positions.Closed += OnPositionClosedHandler;

            Print("Trading Enabled: {0} | Session Filter: {1}", EnableTrading, EnableSessionFilter);
            Print("Risk: {0:F1}% | Min RR: {1:F1} | Max Positions: {2}", RiskPercent, MinimumRRRatio, MaxPositions);
            Print("========================================");
        }

        #endregion

        #region OnBar - Main Loop

        protected override void OnBar()
        {
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
            // Skip if trading disabled or position open
            if (!EnableTrading || HasOpenPosition())
                return;

            // Check daily loss limit
            if (IsDailyLimitReached())
                return;

            // Check session filter
            if (EnableSessionFilter)
            {
                var period = GetOptimalPeriod(Server.Time);
                if (period != OptimalPeriod.BestOverlap && period != OptimalPeriod.GoodLondonOpen)
                    return;
            }

            // Check MTF alignment
            if (!CheckMTFAlignment(out string alignmentDirection))
                return;

            // Check for M1 crossover
            if (DetectM1Crossover(out string m1Direction))
            {
                if (m1Direction == alignmentDirection)
                {
                    Print("[MTF-SMA] All TFs aligned {0} | M1 crossover | ENTRY", alignmentDirection);

                    if (alignmentDirection == "BUY")
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
                TradeDirection = position.TradeType
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
                            Print("[CHANDELIER] Trail #{0} | New SL: {1:F5}", currentIncrements, newSL);
                        }
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

                string name = string.Format("Session_{0}_{1}", currentPeriod, periodStart.ToString("yyyyMMdd_HHmm"));
                Chart.DrawRectangle(name, periodStart, Symbol.Bid * 0.999, periodEnd, Symbol.Bid * 1.001, periodColor);

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
            if (!EnableDailyLossLimit) return false;
            return _dailyLimitReached;
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
        }

        private void OnPositionClosedHandler(PositionClosedEventArgs args)
        {
            var position = args.Position;
            if (position.Label != MagicNumber.ToString()) return;

            // Remove chandelier state
            if (_chandelierStates.ContainsKey(position.Id))
                _chandelierStates.Remove(position.Id);

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

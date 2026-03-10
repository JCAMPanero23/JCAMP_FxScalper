using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
using System.Linq;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class JCAMP_FxScalper_V3_Aggressive : Robot
    {
        // ===================== RISK =====================

        [Parameter("Risk %", DefaultValue = 0.75)]
        public double RiskPercent { get; set; }

        [Parameter("Max Daily Loss %", DefaultValue = 2.5)]
        public double MaxDailyLoss { get; set; }

        // ===================== SESSIONS =====================

        [Parameter("Trade London", DefaultValue = true)]
        public bool TradeLondon { get; set; }

        [Parameter("London Start", DefaultValue = 8)]
        public int LondonStart { get; set; }

        [Parameter("London End", DefaultValue = 16)]
        public int LondonEnd { get; set; }

        [Parameter("Trade NY", DefaultValue = true)]
        public bool TradeNY { get; set; }

        [Parameter("NY Start", DefaultValue = 13)]
        public int NYStart { get; set; }

        [Parameter("NY End", DefaultValue = 21)]
        public int NYEnd { get; set; }

        // ===================== INDICATORS =====================

        [Parameter("SMA Fast", DefaultValue = 21)]
        public int SMAFast { get; set; }

        [Parameter("SMA Mid", DefaultValue = 50)]
        public int SMAMid { get; set; }

        [Parameter("SMA Slow", DefaultValue = 200)]
        public int SMASlow { get; set; }

        [Parameter("RSI Period", DefaultValue = 14)]
        public int RSIPeriod { get; set; }

        [Parameter("ADX Period", DefaultValue = 14)]
        public int ADXPeriod { get; set; }

        [Parameter("Min ADX", DefaultValue = 23)]
        public double MinADX { get; set; }

        [Parameter("ATR Period", DefaultValue = 14)]
        public int ATRPeriod { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 1.8)]
        public double ATRMultiplier { get; set; }

        [Parameter("Min ATR Expansion %", DefaultValue = 105)]
        public double MinATRExpansionPercent { get; set; }

        [Parameter("Max Spread (pips)", DefaultValue = 1.2)]
        public double MaxSpread { get; set; }

        // ===================== PRIVATE =====================

        private SimpleMovingAverage smaFast;
        private SimpleMovingAverage smaMid;
        private SimpleMovingAverage smaSlow;
        private RelativeStrengthIndex rsi;
        private AverageTrueRange atr;
        private AverageDirectionalMovementIndex adx;

        private const string LABEL = "JCAMP_V3";
        private bool tradePlacedThisBar;
        private double dailyPL;
        private DateTime lastResetDate;

        protected override void OnStart()
        {
            smaFast = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMAFast);
            smaMid = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMAMid);
            smaSlow = Indicators.SimpleMovingAverage(Bars.ClosePrices, SMASlow);
            rsi = Indicators.RelativeStrengthIndex(Bars.ClosePrices, RSIPeriod);
            atr = Indicators.AverageTrueRange(ATRPeriod, MovingAverageType.Simple);
            adx = Indicators.AverageDirectionalMovementIndex(ADXPeriod);

            lastResetDate = Server.Time.Date;
        }

        protected override void OnBar()
        {
            tradePlacedThisBar = false;

            UpdateDailyPL();
            if (!CheckDailyLossLimit())
                return;

            if (!IsActiveSession())
                return;

            if (!IsSpreadOK())
                return;

            if (Positions.Any(p => p.Label == LABEL && p.SymbolName == SymbolName))
                return;

            if (IsBullishSignal())
                ExecuteBuy();

            if (IsBearishSignal())
                ExecuteSell();
        }

        // ===================== ENTRY LOGIC =====================

        private bool IsBullishSignal()
        {
            if (!(smaFast.Result.LastValue > smaMid.Result.LastValue &&
                  smaMid.Result.LastValue > smaSlow.Result.LastValue))
                return false;

            if (rsi.Result.LastValue <= 56)
                return false;

            if (adx.Result.LastValue < MinADX)
                return false;

            if (adx.Result.Last(1) <= adx.Result.Last(3))
                return false;

            double atrNow = atr.Result.LastValue;
            double atrPrev = atr.Result.Last(5);

            if ((atrNow / atrPrev) * 100 < MinATRExpansionPercent)
                return false;

            double slope = smaFast.Result.Last(1) - smaFast.Result.Last(4);
            if (slope <= 0)
                return false;

            return IsBullishEngulfing();
        }

        private bool IsBearishSignal()
        {
            if (!(smaSlow.Result.LastValue > smaMid.Result.LastValue &&
                  smaMid.Result.LastValue > smaFast.Result.LastValue))
                return false;

            if (rsi.Result.LastValue >= 44)
                return false;

            if (adx.Result.LastValue < MinADX)
                return false;

            if (adx.Result.Last(1) <= adx.Result.Last(3))
                return false;

            double atrNow = atr.Result.LastValue;
            double atrPrev = atr.Result.Last(5);

            if ((atrNow / atrPrev) * 100 < MinATRExpansionPercent)
                return false;

            double slope = smaFast.Result.Last(1) - smaFast.Result.Last(4);
            if (slope >= 0)
                return false;

            return IsBearishEngulfing();
        }

        // ===================== EXECUTION =====================

        private void ExecuteBuy()
        {
            if (tradePlacedThisBar) return;

            double entry = Symbol.Ask;
            double sl = Bars.LowPrices.Last(1) - atr.Result.LastValue * ATRMultiplier;
            double risk = entry - sl;
            double tp = entry + risk * 3.0;

            PlaceTrade(TradeType.Buy, entry, sl, tp);
        }

        private void ExecuteSell()
        {
            if (tradePlacedThisBar) return;

            double entry = Symbol.Bid;
            double sl = Bars.HighPrices.Last(1) + atr.Result.LastValue * ATRMultiplier;
            double risk = sl - entry;
            double tp = entry - risk * 3.0;

            PlaceTrade(TradeType.Sell, entry, sl, tp);
        }

        private void PlaceTrade(TradeType type, double entry, double sl, double tp)
        {
            double slPips = Math.Abs(entry - sl) / Symbol.PipSize;
            double volume = CalculateVolume(slPips);

            if (volume <= 0) return;

            var result = ExecuteMarketOrder(type, SymbolName, volume, LABEL, slPips,
                Math.Abs(tp - entry) / Symbol.PipSize);

            if (result.IsSuccessful)
                tradePlacedThisBar = true;
        }

        // ===================== TRADE MANAGEMENT =====================

        protected override void OnTick()
        {
            ManagePositions();
        }

        private void ManagePositions()
        {
            foreach (var pos in Positions.Where(p => p.Label == LABEL))
            {
                double rr = GetRR(pos);

                if (rr >= 1.2 && pos.StopLoss != pos.EntryPrice)
                    ModifyPosition(pos, pos.EntryPrice, pos.TakeProfit);

                if (rr >= 2.0)
                {
                    double risk = Math.Abs(pos.EntryPrice - pos.StopLoss.Value);
                    double newSL = pos.TradeType == TradeType.Buy
                        ? Symbol.Bid - risk * 0.75
                        : Symbol.Ask + risk * 0.75;

                    if ((pos.TradeType == TradeType.Buy && newSL > pos.StopLoss) ||
                        (pos.TradeType == TradeType.Sell && newSL < pos.StopLoss))
                        ModifyPosition(pos, newSL, pos.TakeProfit);
                }
            }
        }

        private double GetRR(Position pos)
        {
            double risk = Math.Abs(pos.EntryPrice - pos.StopLoss.Value);
            double reward = pos.TradeType == TradeType.Buy
                ? Symbol.Bid - pos.EntryPrice
                : pos.EntryPrice - Symbol.Ask;

            return risk > 0 ? reward / risk : 0;
        }

        // ===================== HELPERS =====================

        private double CalculateVolume(double stopPips)
        {
            double riskAmount = Account.Balance * (RiskPercent / 100);
            double pipValuePerLot = Symbol.PipValue * Symbol.LotSize;
            double lot = riskAmount / (stopPips * pipValuePerLot);
            double units = Symbol.QuantityToVolumeInUnits(lot);
            return Symbol.NormalizeVolumeInUnits(units, RoundingMode.Down);
        }

        private bool IsSpreadOK()
        {
            return Symbol.Spread / Symbol.PipSize <= MaxSpread;
        }

        private bool IsActiveSession()
        {
            int hour = Server.Time.Hour;
            bool london = TradeLondon && hour >= LondonStart && hour <= LondonEnd;
            bool ny = TradeNY && hour >= NYStart && hour <= NYEnd;
            return london || ny;
        }

        private void UpdateDailyPL()
        {
            if (Server.Time.Date != lastResetDate)
            {
                dailyPL = 0;
                lastResetDate = Server.Time.Date;
            }

            dailyPL = History
                .Where(h => h.ClosingTime.Date == Server.Time.Date)
                .Sum(h => h.NetProfit);
        }

        private bool CheckDailyLossLimit()
        {
            double lossPercent = (dailyPL / Account.Balance) * 100;
            return lossPercent > -MaxDailyLoss;
        }

        private bool IsBullishEngulfing()
        {
            int i1 = Bars.Count - 2;
            int i2 = Bars.Count - 3;
            if (i2 < 0) return false;

            return Bars.ClosePrices[i2] < Bars.OpenPrices[i2] &&
                   Bars.ClosePrices[i1] > Bars.OpenPrices[i1] &&
                   Bars.OpenPrices[i1] <= Bars.ClosePrices[i2] &&
                   Bars.ClosePrices[i1] >= Bars.OpenPrices[i2];
        }

        private bool IsBearishEngulfing()
        {
            int i1 = Bars.Count - 2;
            int i2 = Bars.Count - 3;
            if (i2 < 0) return false;

            return Bars.ClosePrices[i2] > Bars.OpenPrices[i2] &&
                   Bars.ClosePrices[i1] < Bars.OpenPrices[i1] &&
                   Bars.OpenPrices[i1] >= Bars.ClosePrices[i2] &&
                   Bars.ClosePrices[i1] <= Bars.OpenPrices[i2];
        }
    }
}
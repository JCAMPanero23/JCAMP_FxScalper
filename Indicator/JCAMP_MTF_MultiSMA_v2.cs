using System;
using System.IO;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Indicators
{
    /// <summary>
    /// JCAMP Multi-Timeframe Multi-SMA Indicator v3 (cBot-aligned)
    ///
    /// Three MTF SMA lines:
    ///   SMA 0  - chart execution TF (M1)
    ///   SMA 1  - TF1 (e.g. M4)
    ///   SMA 2  - TF2 (e.g. M15)  <- macro bias filter
    ///
    /// Signal layer - matches cBot entry logic:
    ///   Entry trigger: M1 price crosses M1 SMA (SMA0) when higher TFs already aligned
    ///
    ///   BUY  signal : TF1+TF2 aligned BULL → M1 price crosses ABOVE M1 SMA
    ///   SELL signal : TF1+TF2 aligned BEAR → M1 price crosses BELOW M1 SMA
    ///
    ///   Arrows painted on the chart at the confirmed M1 crossover.
    ///   Bar-close confirmation eliminates repaints.
    /// </summary>
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class JCAMP_MTF_MultiSMA_v2 : Indicator
    {
        // =====================================================================
        // GLOBAL SETTINGS
        // =====================================================================

        [Parameter("SMA Period (all MTF lines)", DefaultValue = 275, MinValue = 2, MaxValue = 1000, Group = "Global Settings")]
        public int SmaPeriod { get; set; }

        [Parameter("Show Info Panel", DefaultValue = true, Group = "Global Settings")]
        public bool ShowInfoPanel { get; set; }

        [Parameter("Alert On Signal", DefaultValue = true, Group = "Global Settings")]
        public bool AlertOnSignal { get; set; }

        [Parameter("Alert Sound File", DefaultValue = "alert.wav", Group = "Global Settings")]
        public string AlertSoundFile { get; set; }

        [Parameter("Enable CSV Debug Export", DefaultValue = false, Group = "Global Settings")]
        public bool EnableCSVExport { get; set; }

        [Parameter("Show SMA Stacking Status", DefaultValue = true, Group = "Global Settings")]
        public bool ShowStackingStatus { get; set; }

        [Parameter("Show ADX Status", DefaultValue = true, Group = "Global Settings")]
        public bool ShowADXStatus { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, MinValue = 7, MaxValue = 28, Group = "Global Settings")]
        public int ADXPeriod { get; set; }

        [Parameter("ADX Min Threshold", DefaultValue = 20, MinValue = 10, MaxValue = 35, Group = "Global Settings")]
        public double ADXMinThreshold { get; set; }

        [Parameter("ADX Max Threshold", DefaultValue = 40, MinValue = 35, MaxValue = 50, Group = "Global Settings")]
        public double ADXMaxThreshold { get; set; }

        [Parameter("Show Session Info", DefaultValue = true, Group = "Global Settings")]
        public bool ShowSessionInfo { get; set; }

        // =====================================================================
        // SMA 0 - CHART EXECUTION TF
        // =====================================================================

        [Parameter("Enable SMA 0 (Chart TF)", DefaultValue = true, Group = "SMA 0 - Chart TF")]
        public bool EnableSma0 { get; set; }

        [Parameter("SMA 0 Color", DefaultValue = "Red", Group = "SMA 0 - Chart TF")]
        public string Sma0Color { get; set; }

        [Parameter("SMA 0 Thickness", DefaultValue = 1, MinValue = 1, MaxValue = 5, Group = "SMA 0 - Chart TF")]
        public int Sma0Thickness { get; set; }

        [Parameter("SMA 0 Style", DefaultValue = LineStyle.Solid, Group = "SMA 0 - Chart TF")]
        public LineStyle Sma0Style { get; set; }

        // =====================================================================
        // SMA 1 - TF1
        // =====================================================================

        [Parameter("Enable SMA 1 (TF1)", DefaultValue = true, Group = "SMA 1 - TF1")]
        public bool EnableSma1 { get; set; }

        [Parameter("TF1 Timeframe", DefaultValue = "Minute5", Group = "SMA 1 - TF1")]
        public TimeFrame Tf1 { get; set; }

        [Parameter("SMA 1 Color", DefaultValue = "LimeGreen", Group = "SMA 1 - TF1")]
        public string Sma1Color { get; set; }

        [Parameter("SMA 1 Thickness", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "SMA 1 - TF1")]
        public int Sma1Thickness { get; set; }

        [Parameter("SMA 1 Style", DefaultValue = LineStyle.Solid, Group = "SMA 1 - TF1")]
        public LineStyle Sma1Style { get; set; }

        // =====================================================================
        // SMA 2 - TF2 (macro filter)
        // =====================================================================

        [Parameter("Enable SMA 2 (TF2)", DefaultValue = true, Group = "SMA 2 - TF2")]
        public bool EnableSma2 { get; set; }

        [Parameter("TF2 Timeframe", DefaultValue = "Minute10", Group = "SMA 2 - TF2")]
        public TimeFrame Tf2 { get; set; }

        [Parameter("SMA 2 Color", DefaultValue = "Blue", Group = "SMA 2 - TF2")]
        public string Sma2Color { get; set; }

        [Parameter("SMA 2 Thickness", DefaultValue = 2, MinValue = 1, MaxValue = 5, Group = "SMA 2 - TF2")]
        public int Sma2Thickness { get; set; }

        [Parameter("SMA 2 Style", DefaultValue = LineStyle.Solid, Group = "SMA 2 - TF2")]
        public LineStyle Sma2Style { get; set; }

        // =====================================================================
        // SMA 3 - TF0 (entry trigger) - v4.6.0 4TF System
        // =====================================================================

        [Parameter("Enable SMA 3 (TF0)", DefaultValue = true, Group = "SMA 3 - TF0")]
        public bool EnableSma3 { get; set; }

        [Parameter("TF0 Timeframe", DefaultValue = "Minute3", Group = "SMA 3 - TF0")]
        public TimeFrame Tf0 { get; set; }

        [Parameter("SMA 3 Color", DefaultValue = "Gold", Group = "SMA 3 - TF0")]
        public string Sma3Color { get; set; }

        [Parameter("SMA 3 Thickness", DefaultValue = 1, MinValue = 1, MaxValue = 5, Group = "SMA 3 - TF0")]
        public int Sma3Thickness { get; set; }

        [Parameter("SMA 3 Style", DefaultValue = LineStyle.Dots, Group = "SMA 3 - TF0")]
        public LineStyle Sma3Style { get; set; }

        // =====================================================================
        // SIGNAL / ENTRY CROSS (TF0 Crossover) - v4.6.0
        // =====================================================================

        [Parameter("Enable Signal Layer", DefaultValue = true, Group = "Signal - M1 Crossover")]
        public bool EnableSignalLayer { get; set; }

        [Parameter("Require All TFs Aligned", DefaultValue = false, Group = "Signal - M1 Crossover")]
        public bool RequireAllTFsAligned { get; set; }

        [Parameter("BUY Arrow Color", DefaultValue = "LimeGreen", Group = "Signal - M1 Crossover")]
        public string BuyArrowColor { get; set; }

        [Parameter("SELL Arrow Color", DefaultValue = "Red", Group = "Signal - M1 Crossover")]
        public string SellArrowColor { get; set; }

        [Parameter("Arrow Offset (pips)", DefaultValue = 5, MinValue = 0, MaxValue = 50, Group = "Signal - M1 Crossover")]
        public int ArrowOffsetPips { get; set; }

        [Parameter("Require Bar Close Confirmation", DefaultValue = true, Group = "Signal - M1 Crossover")]
        public bool RequireBarClose { get; set; }

        // =====================================================================
        // OUTPUTS
        // =====================================================================

        [Output("SMA 0 (M1)", LineColor = "Red", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Sma0Result { get; set; }

        [Output("SMA 1 (TF1)", LineColor = "LimeGreen", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries Sma1Result { get; set; }

        [Output("SMA 2 (TF2)", LineColor = "Blue", PlotType = PlotType.Line, Thickness = 2)]
        public IndicatorDataSeries Sma2Result { get; set; }

        [Output("SMA 3 (TF0)", LineColor = "Gold", PlotType = PlotType.Line, Thickness = 1)]
        public IndicatorDataSeries Sma3Result { get; set; }

        // =====================================================================
        // PRIVATE FIELDS
        // =====================================================================

        // MTF SMAs
        private SimpleMovingAverage _sma0;
        private Bars                _tf1Bars;
        private SimpleMovingAverage _sma1;
        private Bars                _tf2Bars;
        private SimpleMovingAverage _sma2;
        private Bars                _tf0Bars;  // v4.6.0 4TF system
        private SimpleMovingAverage _sma3;     // TF0 SMA

        // ADX
        private DirectionalMovementSystem _adx;

        // Colors
        private Color _color0, _color1, _color2, _color3;
        private Color _colorBuy, _colorSell;

        // M1 crossover tracking (like cBot's DetectM1Crossover)
        private string _previousM1Alignment = null;
        private int    _lastSignalIndex     = -1;

        // Alignment state for panel
        private bool _bullAligned;
        private bool _bearAligned;
        private bool _htfAligned;
        private string _htfDirection;

        // Pip value for arrow offset
        private double _pipSize;

        // CSV export for debugging
        private StreamWriter _csvWriter;
        private string _csvFilePath;

        // =====================================================================
        // INITIALIZE
        // =====================================================================

        protected override void Initialize()
        {
            // SMA 0 - chart TF (M1)
            if (EnableSma0)
                _sma0 = Indicators.SimpleMovingAverage(Bars.ClosePrices, SmaPeriod);

            // SMA 1 - TF1
            if (EnableSma1)
            {
                _tf1Bars = MarketData.GetBars(Tf1);
                _sma1 = Indicators.SimpleMovingAverage(_tf1Bars.ClosePrices, SmaPeriod);
            }

            // SMA 2 - TF2
            if (EnableSma2)
            {
                _tf2Bars = MarketData.GetBars(Tf2);
                _sma2 = Indicators.SimpleMovingAverage(_tf2Bars.ClosePrices, SmaPeriod);
            }

            // SMA 3 - TF0 (v4.6.0 4TF system - entry trigger)
            if (EnableSma3)
            {
                _tf0Bars = MarketData.GetBars(Tf0);
                _sma3 = Indicators.SimpleMovingAverage(_tf0Bars.ClosePrices, SmaPeriod);
            }

            // ADX indicator
            if (ShowADXStatus)
            {
                _adx = Indicators.DirectionalMovementSystem(ADXPeriod);
            }

            // Parse colors
            _color0    = ParseColor(Sma0Color,     Color.Red);
            _color1    = ParseColor(Sma1Color,     Color.LimeGreen);
            _color2    = ParseColor(Sma2Color,     Color.Blue);
            _color3    = ParseColor(Sma3Color,     Color.Gold);  // TF0
            _colorBuy  = ParseColor(BuyArrowColor, Color.LimeGreen);
            _colorSell = ParseColor(SellArrowColor, Color.Red);

            // Pip size for arrow offset
            _pipSize = Symbol.PipSize;

            // Initialize CSV export for debugging
            if (EnableCSVExport)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _csvFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    string.Format("JCAMP_Indicator_SMA_Debug_{0}_{1}.csv", Symbol.Name, timestamp));

                _csvWriter = new StreamWriter(_csvFilePath, false);
                _csvWriter.WriteLine("Timestamp,BarIndex,Price,SMA0_M1,SMA1_TF1,SMA2_TF2,SMA3_TF0,M1_Alignment,HTF_Aligned,HTF_Direction,Signal");
                _csvWriter.Flush();

                Print(string.Format("[CSV] Debug export enabled: {0}", _csvFilePath));
            }

            DrawStaticLabel();

            Print(string.Format(
                "JCAMP MTF MultiSMA v4 (4TF System v4.6.0) | Period:{0} | M1:{1} | TF0:{2} | TF1:{3} | TF2:{4} | Entry: TF0 Crossover",
                SmaPeriod, TimeFrame, Tf0, Tf1, Tf2));
        }

        // =====================================================================
        // CALCULATE
        // =====================================================================

        public override void Calculate(int index)
        {
            DateTime barTime = Bars.OpenTimes[index];

            // ------------------------------------------------------------------
            // SMA 0 - chart TF (M1)
            // ------------------------------------------------------------------
            double s0 = double.NaN;
            if (EnableSma0 && _sma0 != null)
            {
                double v = _sma0.Result[index];
                s0 = double.IsNaN(v) ? double.NaN : v;
                Sma0Result[index] = s0;
            }
            else
            {
                Sma0Result[index] = double.NaN;
            }

            // ------------------------------------------------------------------
            // SMA 1 - TF1 (mapped)
            // ------------------------------------------------------------------
            double s1 = double.NaN;
            if (EnableSma1 && _sma1 != null)
            {
                int htfIdx = GetHTFIndex(_tf1Bars, barTime);
                if (htfIdx >= SmaPeriod - 1)
                {
                    double v = _sma1.Result[htfIdx];
                    s1 = double.IsNaN(v) ? double.NaN : v;
                }
                Sma1Result[index] = s1;
            }
            else
            {
                Sma1Result[index] = double.NaN;
            }

            // ------------------------------------------------------------------
            // SMA 2 - TF2 (mapped)
            // ------------------------------------------------------------------
            double s2 = double.NaN;
            if (EnableSma2 && _sma2 != null)
            {
                int htfIdx = GetHTFIndex(_tf2Bars, barTime);
                if (htfIdx >= SmaPeriod - 1)
                {
                    double v = _sma2.Result[htfIdx];
                    s2 = double.IsNaN(v) ? double.NaN : v;
                }
                Sma2Result[index] = s2;
            }
            else
            {
                Sma2Result[index] = double.NaN;
            }

            // ------------------------------------------------------------------
            // SMA 3 - TF0 (mapped) - v4.6.0 4TF system entry trigger
            // ------------------------------------------------------------------
            double s3 = double.NaN;
            if (EnableSma3 && _sma3 != null)
            {
                int htfIdx = GetHTFIndex(_tf0Bars, barTime);
                if (htfIdx >= SmaPeriod - 1)
                {
                    double v = _sma3.Result[htfIdx];
                    s3 = double.IsNaN(v) ? double.NaN : v;
                }
                Sma3Result[index] = s3;
            }
            else
            {
                Sma3Result[index] = double.NaN;
            }

            // ------------------------------------------------------------------
            // CSV DEBUG EXPORT (log ALL bars for historical comparison)
            // ------------------------------------------------------------------
            if (EnableCSVExport && _csvWriter != null)
            {
                // Log current bar values
                double price = Bars.ClosePrices[index];
                double s0Log = s0;
                double s1Log = s1;
                double s2Log = s2;
                double s3Log = s3;  // TF0

                string m1Align = GetSMAAlignment(price, s0Log);
                bool htfAlign = CheckHigherTFAlignment(price, s1Log, s2Log, out string htfDir);
                string signal = (_lastSignalIndex == index) ?
                    (m1Align == "BUY" ? "BUY_ARROW" : "SELL_ARROW") : "NONE";

                _csvWriter.WriteLine(string.Format("{0},{1},{2:F5},{3},{4},{5},{6},{7},{8},{9},{10}",
                    Bars.OpenTimes[index].ToString("yyyy-MM-dd HH:mm:ss"),
                    index,
                    price,
                    double.IsNaN(s0Log) ? "NaN" : s0Log.ToString("F5"),
                    double.IsNaN(s1Log) ? "NaN" : s1Log.ToString("F5"),
                    double.IsNaN(s2Log) ? "NaN" : s2Log.ToString("F5"),
                    double.IsNaN(s3Log) ? "NaN" : s3Log.ToString("F5"),  // TF0
                    m1Align,
                    htfAlign ? "TRUE" : "FALSE",
                    htfDir,
                    signal));

                // Flush periodically (every 100 bars) to avoid memory issues
                if (index % 100 == 0)
                    _csvWriter.Flush();
            }

            // ------------------------------------------------------------------
            // M1 PRICE CROSSOVER DETECTION (cBot-style)
            // Trigger: M1 price crosses M1 SMA when higher TFs already aligned
            // ------------------------------------------------------------------
            if (EnableSignalLayer)
            {
                // Use current bar for signal detection (match cBot logic)
                double price = Bars.ClosePrices[index];
                string currentM1Alignment = GetSMAAlignment(price, s0);

                // Detect M1 crossover (alignment changed)
                bool m1Crossed = !string.IsNullOrEmpty(_previousM1Alignment)
                               && _previousM1Alignment != "NONE"
                               && _previousM1Alignment != currentM1Alignment
                               && currentM1Alignment != "NONE";

                if (m1Crossed)
                {
                    // Check if higher TFs (TF1 + TF2) are aligned
                    bool htfAligned = CheckHigherTFAlignment(price, s1, s2, out string htfDirection);

                    // Signal when: M1 crossover + HTF aligned + directions match
                    if (htfAligned && currentM1Alignment == htfDirection)
                    {
                        // Place arrow on current bar
                        if (currentM1Alignment == "BUY")
                        {
                            PlaceBuyArrow(index);
                            FireAlert("BUY", price, index, htfDirection);
                            _lastSignalIndex = index;
                        }
                        else if (currentM1Alignment == "SELL")
                        {
                            PlaceSellArrow(index);
                            FireAlert("SELL", price, index, htfDirection);
                            _lastSignalIndex = index;
                        }
                    }
                }

                // Update previous alignment
                _previousM1Alignment = currentM1Alignment;
            }

            // ------------------------------------------------------------------
            // INFO PANEL + LEGEND (last bar only for performance)
            // ------------------------------------------------------------------
            if (IsLastBar)
            {
                double price = Bars.ClosePrices[index];
                _bullAligned = IsBullAligned(price, s0, s1, s2);
                _bearAligned = IsBearAligned(price, s0, s1, s2);
                _htfAligned = CheckHigherTFAlignment(price, s1, s2, out _htfDirection);

                if (ShowInfoPanel)
                    DrawInfoPanel(price, s0, s1, s2);

                DrawStaticLabel();
            }
        }

        // =====================================================================
        // ALIGNMENT HELPERS (cBot-style)
        // =====================================================================

        /// <summary>
        /// Get M1 SMA alignment direction (like cBot's GetSMAAlignment)
        /// Returns: "BUY" if price > SMA, "SELL" if price < SMA, "NONE" otherwise
        /// </summary>
        private string GetSMAAlignment(double price, double sma)
        {
            if (double.IsNaN(price) || double.IsNaN(sma))
                return "NONE";

            if (price > sma)
                return "BUY";
            else if (price < sma)
                return "SELL";
            else
                return "NONE";
        }

        /// <summary>
        /// Check if higher timeframes (TF1 + TF2) are aligned
        /// Like cBot's CheckMTFAlignment but only for TF1+TF2
        /// </summary>
        private bool CheckHigherTFAlignment(double price, double s1, double s2, out string direction)
        {
            direction = "NONE";

            // Get TF1 and TF2 alignments
            string tf1Dir = GetSMAAlignment(price, s1);
            string tf2Dir = GetSMAAlignment(price, s2);

            if (RequireAllTFsAligned)
            {
                // Require both TF1 and TF2 aligned same direction
                bool allBuy = (tf1Dir == "BUY" && tf2Dir == "BUY");
                bool allSell = (tf1Dir == "SELL" && tf2Dir == "SELL");

                if (allBuy) { direction = "BUY"; return true; }
                if (allSell) { direction = "SELL"; return true; }
                return false;
            }
            else
            {
                // 2 out of 2 alignment (both TFs must agree)
                int buyCount = (tf1Dir == "BUY" ? 1 : 0) + (tf2Dir == "BUY" ? 1 : 0);
                int sellCount = (tf1Dir == "SELL" ? 1 : 0) + (tf2Dir == "SELL" ? 1 : 0);

                if (buyCount >= 2) { direction = "BUY"; return true; }
                if (sellCount >= 2) { direction = "SELL"; return true; }
                return false;
            }
        }

        /// <summary>
        /// Bull alignment: price is ABOVE all three enabled MTF SMAs.
        /// </summary>
        private bool IsBullAligned(double price, double s0, double s1, double s2)
        {
            bool ok = true;
            if (EnableSma0 && !double.IsNaN(s0)) ok &= price > s0;
            if (EnableSma1 && !double.IsNaN(s1)) ok &= price > s1;
            if (EnableSma2 && !double.IsNaN(s2)) ok &= price > s2;
            return ok;
        }

        /// <summary>
        /// Bear alignment: price is BELOW all three enabled MTF SMAs.
        /// </summary>
        private bool IsBearAligned(double price, double s0, double s1, double s2)
        {
            bool ok = true;
            if (EnableSma0 && !double.IsNaN(s0)) ok &= price < s0;
            if (EnableSma1 && !double.IsNaN(s1)) ok &= price < s1;
            if (EnableSma2 && !double.IsNaN(s2)) ok &= price < s2;
            return ok;
        }

        // =====================================================================
        // ARROW DRAWING
        // =====================================================================

        private void PlaceBuyArrow(int index)
        {
            double price = Bars.LowPrices[index] - (ArrowOffsetPips * _pipSize);
            string name  = "JCAMP_BUY_" + index;

            Chart.DrawText(
                name,
                "\u25B2",   // filled up triangle
                index,
                price,
                _colorBuy);
        }

        private void PlaceSellArrow(int index)
        {
            double price = Bars.HighPrices[index] + (ArrowOffsetPips * _pipSize);
            string name  = "JCAMP_SELL_" + index;

            Chart.DrawText(
                name,
                "\u25BC",   // filled down triangle
                index,
                price,
                _colorSell);
        }

        // =====================================================================
        // ALERT
        // =====================================================================

        private void FireAlert(string direction, double price, int index, string htfDirection)
        {
            if (!AlertOnSignal) return;

            string msg = string.Format(
                "JCAMP MTF SMA Signal | {0} {1} | {2} | Entry ~{3:F5} | HTF Aligned {4} | M1 Price Crossed SMA({5})",
                Symbol.Name, TimeFrame, direction, price, htfDirection, SmaPeriod);

            Print(msg);

            if (!string.IsNullOrEmpty(AlertSoundFile))
                Notifications.PlaySound(AlertSoundFile);
        }

        // =====================================================================
        // INFO PANEL
        // =====================================================================

        private void DrawInfoPanel(double price, double s0, double s1, double s2)
        {
            // Get s3 (TF0) from the result series
            double s3 = Sma3Result[Sma3Result.Count - 1];

            string ValStr(double v) => double.IsNaN(v) ? "  ---   " : v.ToString("F5");

            string alignStatus;
            Color  panelColor;

            if (_bullAligned)
            {
                alignStatus = ">>> ALL TFs BULL ALIGNED";
                panelColor  = Color.LimeGreen;
            }
            else if (_bearAligned)
            {
                alignStatus = "<<< ALL TFs BEAR ALIGNED";
                panelColor  = Color.OrangeRed;
            }
            else if (_htfAligned)
            {
                alignStatus = ">>> HTF Aligned " + _htfDirection + " - waiting M1 cross";
                panelColor  = (_htfDirection == "BUY") ? Color.LimeGreen : Color.OrangeRed;
            }
            else
            {
                alignStatus = "--- NOT ALIGNED";
                panelColor  = Color.Gray;
            }

            // Session info
            string sessionInfo = "";
            if (ShowSessionInfo)
            {
                DateTime currentTime = Server.Time;
                string currentSession = GetCurrentSession(currentTime);
                string countdown = GetSessionCountdown(currentTime);
                sessionInfo = string.Format("Session: {0} | Next: {1}\n", currentSession, countdown);
            }

            // SMA Stacking info
            string stackingInfo = "";
            if (ShowStackingStatus)
            {
                string direction = _htfAligned ? _htfDirection : "NONE";
                bool isStacked = CheckSMAStacking(s0, s1, s2, s3, direction, out string stackDetails);
                string stackStatus = isStacked ? "✓ STACKED" : (direction != "NONE" ? "✗ NOT STACKED" : "N/A");
                stackingInfo = string.Format("SMA Stacking: {0}\n", stackStatus);
            }

            // ADX info
            string adxInfo = "";
            if (ShowADXStatus && _adx != null)
            {
                double adxValue = _adx.ADX.LastValue;
                string adxTrend;

                if (adxValue > ADXMaxThreshold)
                {
                    adxTrend = "TOO HIGH - NO TRADES";
                }
                else if (adxValue < ADXMinThreshold)
                {
                    adxTrend = "TOO LOW - RANGING";
                }
                else if (adxValue >= 25)
                {
                    adxTrend = "TRENDING";
                }
                else
                {
                    adxTrend = "WEAK TREND";
                }

                adxInfo = string.Format("ADX: {0:F1} ({1})\n", adxValue, adxTrend);
            }

            string m1Status = GetSMAAlignment(price, s0);
            string m1Cross = (m1Status == "BUY") ? "ABOVE M1" : (m1Status == "SELL") ? "BELOW M1" : "AT M1";

            string panel =
                "=== JCAMP MTF v4 (4TF) ===\n" +
                sessionInfo +
                "\n" +
                "Price: " + price.ToString("F5") + " (" + m1Cross + ")\n" +
                "\n" +
                "M1  [Red]:   " + ValStr(s0) + "\n" +
                "TF0 [Gold]:  " + ValStr(s3) + "\n" +
                "TF1 [Lime]:  " + ValStr(s1) + "\n" +
                "TF2 [Blue]:  " + ValStr(s2) + "\n" +
                "\n" +
                stackingInfo +
                adxInfo +
                "\n" +
                alignStatus;

            Chart.DrawStaticText("jcamp_multisma_panel", panel, VerticalAlignment.Top, HorizontalAlignment.Left, panelColor);
        }

        private string PadTF(string tf) { return tf.PadRight(8); }

        // =====================================================================
        // LEGEND LABEL
        // =====================================================================

        private void DrawStaticLabel()
        {
            string label =
                "JCAMP Multi-SMA v3 (cBot)\n" +
                "Period: " + SmaPeriod + "\n" +
                (EnableSma0 ? "o SMA0  " + TimeFrame + " (M1)\n" : "") +
                (EnableSma1 ? "o SMA1  " + Tf1       + "\n" : "") +
                (EnableSma2 ? "o SMA2  " + Tf2       + "\n" : "") +
                (EnableSignalLayer ? "* Entry: M1 Price x SMA0" : "");

            Chart.DrawStaticText("jcamp_multisma_legend", label, VerticalAlignment.Top, HorizontalAlignment.Right, _color1);
        }

        // =====================================================================
        // HTF INDEX - BINARY SEARCH (no repaint, no look-ahead)
        // =====================================================================

        private int GetHTFIndex(Bars bars, DateTime chartBarTime)
        {
            int lo = 0, hi = bars.OpenTimes.Count - 1, result = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (bars.OpenTimes[mid] <= chartBarTime)
                { result = mid; lo = mid + 1; }
                else
                { hi = mid - 1; }
            }
            return result;
        }

        // =====================================================================
        // SMA STACKING CHECK
        // =====================================================================

        private bool CheckSMAStacking(double s0, double s1, double s2, double s3, string direction, out string stackInfo)
        {
            stackInfo = string.Format("M1:{0:F5} TF0:{1:F5} TF1:{2:F5} TF2:{3:F5}",
                double.IsNaN(s0) ? 0 : s0,
                double.IsNaN(s3) ? 0 : s3,
                double.IsNaN(s1) ? 0 : s1,
                double.IsNaN(s2) ? 0 : s2);

            if (double.IsNaN(s0) || double.IsNaN(s1) || double.IsNaN(s2) || double.IsNaN(s3))
                return false;

            if (direction == "BUY")
            {
                // For BUY: M1 > TF0 > TF1 > TF2 (faster SMAs above slower ones)
                return (s0 > s3 && s3 > s1 && s1 > s2);
            }
            else if (direction == "SELL")
            {
                // For SELL: M1 < TF0 < TF1 < TF2 (faster SMAs below slower ones)
                return (s0 < s3 && s3 < s1 && s1 < s2);
            }

            return false;
        }

        // =====================================================================
        // SESSION HELPERS
        // =====================================================================

        private string GetCurrentSession(DateTime time)
        {
            int hour = time.Hour;

            // Asian session: 00:00-09:00 UTC
            if (hour >= 0 && hour < 9)
                return "Asian";

            // London session: 08:00-17:00 UTC
            if (hour >= 8 && hour < 17)
                return "London";

            // New York session: 13:00-22:00 UTC
            if (hour >= 13 && hour < 22)
                return "NY";

            // Overlap: London + NY (13:00-17:00 UTC)
            if (hour >= 13 && hour < 17)
                return "London+NY";

            return "Off-Hours";
        }

        private string GetSessionCountdown(DateTime time)
        {
            int hour = time.Hour;
            int minute = time.Minute;

            DateTime nextSession;
            string sessionName;

            if (hour < 8)
            {
                // Next: London at 08:00
                nextSession = new DateTime(time.Year, time.Month, time.Day, 8, 0, 0);
                sessionName = "London";
            }
            else if (hour < 13)
            {
                // Next: NY at 13:00
                nextSession = new DateTime(time.Year, time.Month, time.Day, 13, 0, 0);
                sessionName = "NY";
            }
            else if (hour < 22)
            {
                // Next: Asian at 00:00 (next day)
                nextSession = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0).AddDays(1);
                sessionName = "Asian";
            }
            else
            {
                // Next: Asian at 00:00 (next day)
                nextSession = new DateTime(time.Year, time.Month, time.Day, 0, 0, 0).AddDays(1);
                sessionName = "Asian";
            }

            TimeSpan remaining = nextSession - time;
            return string.Format("{0} in {1:D2}h {2:D2}m", sessionName, (int)remaining.TotalHours, remaining.Minutes);
        }

        // =====================================================================
        // COLOR PARSER
        // =====================================================================

        private Color ParseColor(string name, Color fallback)
        {
            try
            {
                return Color.FromName(name);
            }
            catch
            {
                return fallback;
            }
        }
    }
}

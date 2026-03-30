// ============================================================================
// ENHANCED TRADE LOGGER FOR WFO ANALYSIS
// Add this code to Jcamp_1M_scalping.cs
// ============================================================================

// ADD TO: #region Private Fields (around line 240)

private string _tradeLogPath;
private bool _logHeaderWritten = false;

// Track entry context for each position
private class TradeContext
{
    public DateTime EntryTime { get; set; }
    public OptimalPeriod Session { get; set; }
    public double ADXValue { get; set; }
    public ADXFilterMode ADXMode { get; set; }
    public bool FlipDirectionUsed { get; set; }
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
    public string MTFAlignment { get; set; }
    public TradeType Direction { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public string DayOfWeek { get; set; }
}

private Dictionary<long, TradeContext> _tradeContexts = new Dictionary<long, TradeContext>();

// ============================================================================
// ADD TO: OnStart() method (after line 385)
// ============================================================================

// Initialize trade log file
_tradeLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "cAlgo", "Trade_Logs", string.Format("TradeLog_{0}_{1}_{2}.csv",
    SymbolName, Account.Number, DateTime.Now.ToString("yyyyMMdd_HHmmss")));

// Create directory if it doesn't exist
string logDir = Path.GetDirectoryName(_tradeLogPath);
if (!Directory.Exists(logDir))
    Directory.CreateDirectory(logDir);

Print("Trade log initialized: {0}", _tradeLogPath);
WriteLogHeader();

// ============================================================================
// ADD NEW METHODS: (Add to end of class, before final closing brace)
// ============================================================================

#region Trade Logging

private void WriteLogHeader()
{
    if (_logHeaderWritten) return;

    var header = string.Join(",", new string[]
    {
        // Trade Identification
        "TradeID", "PositionID",

        // Time Dimensions
        "EntryDate", "EntryTime", "EntryHour", "EntryMinute", "EntryDayOfWeek",
        "ExitDate", "ExitTime", "ExitHour", "ExitMinute",
        "DurationMinutes",

        // Session & Market Context
        "Session", "IsLondonSession", "IsNYSession", "IsAsianSession",

        // Trade Details
        "Direction", "EntryPrice", "ExitPrice", "StopLoss", "TakeProfit",
        "Volume", "ProfitPips", "ProfitCurrency", "ProfitPercent",

        // ADX Context
        "ADXValue", "ADXPeriod", "ADXThreshold", "ADXMode",
        "FlipDirectionUsed", "ADXTrending",

        // MTF Alignment
        "MTFAlignment", "M1Direction", "TF2Direction", "TF3Direction",

        // Parameters Snapshot
        "SMAPeriod", "Timeframe2", "Timeframe3", "MinRR", "RiskPercent",

        // Risk Context
        "DailyRLoss", "ConsecutiveLosses", "AccountBalance", "AccountEquity",

        // Outcome
        "Result", "RMultiple", "WinningTrade"
    });

    File.WriteAllText(_tradeLogPath, header + Environment.NewLine);
    _logHeaderWritten = true;
}

private void LogTradeEntry(Position position, TradeContext context)
{
    // Store context for when position closes
    if (!_tradeContexts.ContainsKey(position.Id))
        _tradeContexts.Add(position.Id, context);

    Print("[LOG] Trade #{0} logged | {1} | Session: {2} | ADX: {3:F1} | Flip: {4}",
        position.Id, context.Direction, context.Session, context.ADXValue, context.FlipDirectionUsed);
}

private void LogTradeExit(Position position)
{
    if (!_tradeContexts.ContainsKey(position.Id))
    {
        Print("[LOG] Warning: No entry context found for position {0}", position.Id);
        return;
    }

    TradeContext ctx = _tradeContexts[position.Id];
    DateTime exitTime = Server.Time;

    // Calculate metrics
    double profitPips = position.Pips;
    double profitPercent = (position.NetProfit / Account.Balance) * 100;
    double riskAmount = Account.Balance * (RiskPercent / 100.0);
    double rMultiple = position.NetProfit / riskAmount;
    bool isWin = position.NetProfit > 0;

    // Session flags
    bool isLondon = (ctx.Session == OptimalPeriod.GoodLondonOpen);
    bool isNY = (ctx.Session == OptimalPeriod.BestOverlap);
    bool isAsian = (ctx.Session == OptimalPeriod.DangerDeadZone || ctx.Session == OptimalPeriod.DangerLateNY);

    // ADX trending flag
    bool adxTrending = ctx.ADXValue >= ADXMinThreshold;

    // Duration
    double durationMinutes = (exitTime - ctx.EntryTime).TotalMinutes;

    // Build CSV row
    var row = string.Join(",", new object[]
    {
        // Trade Identification
        History.Count, position.Id,

        // Time Dimensions
        ctx.EntryTime.ToString("yyyy-MM-dd"), ctx.EntryTime.ToString("HH:mm:ss"),
        ctx.Hour, ctx.Minute, ctx.DayOfWeek,
        exitTime.ToString("yyyy-MM-dd"), exitTime.ToString("HH:mm:ss"),
        exitTime.Hour, exitTime.Minute,
        Math.Round(durationMinutes, 1),

        // Session & Market Context
        ctx.Session, isLondon, isNY, isAsian,

        // Trade Details
        ctx.Direction, ctx.EntryPrice, position.EntryPrice, // Use actual filled price
        ctx.StopLoss, ctx.TakeProfit,
        position.VolumeInUnits,
        Math.Round(profitPips, 1),
        Math.Round(position.NetProfit, 2),
        Math.Round(profitPercent, 4),

        // ADX Context
        Math.Round(ctx.ADXValue, 2), ADXPeriod, ADXMinThreshold,
        ctx.ADXMode, ctx.FlipDirectionUsed, adxTrending,

        // MTF Alignment
        ctx.MTFAlignment,
        GetSMAAlignment(m1Bars), GetSMAAlignment(tf2Bars), GetSMAAlignment(tf3Bars),

        // Parameters Snapshot
        MTFSMAPeriod, Timeframe2, Timeframe3, MinimumRRRatio, RiskPercent,

        // Risk Context
        Math.Round(_dailyRLoss, 2), _consecutiveLosses,
        Math.Round(Account.Balance, 2), Math.Round(Account.Equity, 2),

        // Outcome
        isWin ? "WIN" : "LOSS", Math.Round(rMultiple, 2), isWin
    });

    // Append to CSV file
    try
    {
        File.AppendAllText(_tradeLogPath, row + Environment.NewLine);
        Print("[LOG] Trade #{0} exit logged | Profit: {1:F2} ({2}R) | Duration: {3:F0}m",
            position.Id, position.NetProfit, rMultiple, durationMinutes);
    }
    catch (Exception ex)
    {
        Print("[LOG] Error writing to log: {0}", ex.Message);
    }

    // Clean up context
    _tradeContexts.Remove(position.Id);
}

#endregion

// ============================================================================
// MODIFY: ExecuteBuyOrder() method (around line 673)
// Add this AFTER successful ExecuteMarketOrder but BEFORE the closing brace:
// ============================================================================

// Log trade entry context
var context = new TradeContext
{
    EntryTime = Server.Time,
    Session = GetOptimalPeriod(Server.Time),
    ADXValue = adxIndicator != null ? adxIndicator.ADX.LastValue : 0,
    ADXMode = ADXMode,
    FlipDirectionUsed = flipDirection,
    EntryPrice = Bars.ClosePrices.LastValue,
    StopLoss = stopLoss,
    TakeProfit = takeProfit,
    MTFAlignment = alignmentDirection,
    Direction = TradeType.Buy,
    Hour = Server.Time.Hour,
    Minute = Server.Time.Minute,
    DayOfWeek = Server.Time.DayOfWeek.ToString()
};
LogTradeEntry(result.Position, context);

// ============================================================================
// MODIFY: ExecuteSellOrder() method (around line 718)
// Add the same code as above but with Direction = TradeType.Sell:
// ============================================================================

// Log trade entry context
var context = new TradeContext
{
    EntryTime = Server.Time,
    Session = GetOptimalPeriod(Server.Time),
    ADXValue = adxIndicator != null ? adxIndicator.ADX.LastValue : 0,
    ADXMode = ADXMode,
    FlipDirectionUsed = flipDirection,
    EntryPrice = Bars.ClosePrices.LastValue,
    StopLoss = stopLoss,
    TakeProfit = takeProfit,
    MTFAlignment = alignmentDirection,
    Direction = TradeType.Sell,
    Hour = Server.Time.Hour,
    Minute = Server.Time.Minute,
    DayOfWeek = Server.Time.DayOfWeek.ToString()
};
LogTradeEntry(result.Position, context);

// ============================================================================
// MODIFY: OnPositionClosedHandler() method (line 1311)
// Add this at the VERY BEGINNING of the method (after the label check):
// ============================================================================

// Log trade exit
LogTradeExit(position);

// ============================================================================
// That's it! Your bot will now create detailed CSV logs for Python analysis
// ============================================================================

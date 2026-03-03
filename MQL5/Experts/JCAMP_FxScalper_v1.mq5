//+------------------------------------------------------------------+
//|                                         JCAMP_FxScalper_v1.mq5 |
//|                                             JCAMP Trading System |
//|                    Automated scalping EA with trend following    |
//+------------------------------------------------------------------+
#property copyright "JCAMP Trading"
#property link      ""
#property version   "1.00"
#property strict
#property description "Phase 1: Single-pair (EURUSD) scalping EA"
#property description "Strategy: Trend-following scalping with H1 structural levels"
#property description "Risk: Dynamic 1-2% per trade with partial profit management"

//+------------------------------------------------------------------+
//| Include common inputs (must be first)                            |
//+------------------------------------------------------------------+
#include <JC_Inputs.mqh>

//+------------------------------------------------------------------+
//| Include custom libraries                                         |
//+------------------------------------------------------------------+
#include <JC_Utils.mqh>
#include <JC_RiskManager.mqh>
#include <JC_MarketStructure.mqh>
#include <JC_EntryLogic.mqh>
#include <JC_TradeManager.mqh>

//+------------------------------------------------------------------+
//| Global Variables - Indicator Handles                             |
//+------------------------------------------------------------------+
int g_SMA1_Handle;
int g_SMA2_Handle;
int g_SMA3_Handle;
int g_RSI_Handle;
int g_ATR_Handle;

//+------------------------------------------------------------------+
//| Global Variables - Bar Tracking                                  |
//+------------------------------------------------------------------+
datetime g_LastM5BarTime = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                    |
//+------------------------------------------------------------------+
int OnInit()
{
   // Print EA initialization info
   LogTrade("========================================");
   LogTrade("JCAMP_FxScalper v1.00 - Initializing...");
   LogTrade("========================================");

   // Validate symbol
   if(!IsSymbolValid(_Symbol))
   {
      Alert("ERROR: Symbol ", _Symbol, " is not valid or not tradeable");
      return INIT_FAILED;
   }

   // Create indicator handles
   g_SMA1_Handle = iMA(_Symbol, PERIOD_M5, SMA1_Period, 0, MODE_SMA, PRICE_CLOSE);
   g_SMA2_Handle = iMA(_Symbol, PERIOD_M5, SMA2_Period, 0, MODE_SMA, PRICE_CLOSE);
   g_SMA3_Handle = iMA(_Symbol, PERIOD_M5, SMA3_Period, 0, MODE_SMA, PRICE_CLOSE);
   g_RSI_Handle = iRSI(_Symbol, PERIOD_M5, RSI_Period, PRICE_CLOSE);
   g_ATR_Handle = iATR(_Symbol, PERIOD_M5, ATR_Period);

   // Validate indicator handles
   if(g_SMA1_Handle == INVALID_HANDLE || g_SMA2_Handle == INVALID_HANDLE ||
      g_SMA3_Handle == INVALID_HANDLE || g_RSI_Handle == INVALID_HANDLE ||
      g_ATR_Handle == INVALID_HANDLE)
   {
      Alert("ERROR: Failed to create indicator handles");
      return INIT_FAILED;
   }

   // Initialize modules
   InitializeMarketStructure();
   InitializeTradeManager();
   InitializeDailyTracking();

   // Log configuration
   LogTrade(StringFormat("Symbol: %s | Timeframe: M5 | Risk: %.1f%% | Max Daily Loss: %.1f%%",
            _Symbol, RiskPercent, MaxDailyLoss));

   LogTrade(StringFormat("Sessions: London=%s | NY=%s | Asian=%s | Tokyo=%s",
            TradeLondon ? "ON" : "OFF",
            TradeNewYork ? "ON" : "OFF",
            TradeAsian ? "ON" : "OFF",
            TradeTokyo ? "ON" : "OFF"));

   LogTrade(StringFormat("Filters: MaxSpread=%.1f pips | LevelProximity=%d pips | TP Validation=%s | SL Snapping=%s",
            MaxSpread, LevelProximity,
            EnableTPValidation ? "ON" : "OFF",
            EnableSLSnapping ? "ON" : "OFF"));

   LogTrade("Initialization complete - EA ready for trading");
   LogTrade("========================================");

   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   // Release indicator handles
   IndicatorRelease(g_SMA1_Handle);
   IndicatorRelease(g_SMA2_Handle);
   IndicatorRelease(g_SMA3_Handle);
   IndicatorRelease(g_RSI_Handle);
   IndicatorRelease(g_ATR_Handle);

   LogTrade("========================================");
   LogTrade(StringFormat("JCAMP_FxScalper stopped | Reason: %d", reason));
   LogTrade("========================================");
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   // 1. Check for new M5 bar (only trade on bar close)
   if(!IsNewBar(_Symbol, PERIOD_M5, g_LastM5BarTime))
      return;

   LogTrade("========================================");
   LogTrade("*** NEW M5 BAR - STARTING ANALYSIS ***");
   LogTrade(StringFormat("Time: %s | Price: %.5f", GMTTimeToString(), SymbolInfoDouble(_Symbol, SYMBOL_BID)));
   LogTrade("========================================");

   // 2. Update H1 levels (only on H1 bar close, cached internally)
   UpdateH1Levels(_Symbol);
   LogTrade(StringFormat("[DEBUG] %s", GetLevelStats()));

   // 3. Session filter
   LogTrade(StringFormat("[DEBUG] Checking sessions at GMT time: %s", GMTTimeToString()));
   LogTrade(StringFormat("[DEBUG] London=%s | NY=%s | Asian=%s | Tokyo=%s",
            TradeLondon ? "ENABLED" : "disabled",
            TradeNewYork ? "ENABLED" : "disabled",
            TradeAsian ? "ENABLED" : "disabled",
            TradeTokyo ? "ENABLED" : "disabled"));

   if(!IsActiveSession())
   {
      LogTrade("[FILTER FAILED] No active session - SKIPPING ANALYSIS");
      LogTrade("========================================");
      return;
   }

   LogTrade(StringFormat("[FILTER PASSED] Active sessions: %s", GetActiveSessionName()));

   // 4. Spread filter
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   double spreadPips = ((ask - bid) / point) / 10.0;
   LogTrade(StringFormat("[DEBUG] Current spread: %.1f pips (max allowed: %.1f)", spreadPips, MaxSpread));

   if(!IsSpreadAcceptable(_Symbol))
   {
      LogTrade("[FILTER FAILED] Spread too wide - SKIPPING ANALYSIS");
      LogTrade("========================================");
      return;
   }
   LogTrade("[FILTER PASSED] Spread acceptable");

   // 5. Daily loss filter
   LogTrade(StringFormat("[DEBUG] %s", GetDailyPLStats()));
   if(!CheckDailyLossLimit())
   {
      LogTrade("[FILTER FAILED] Daily loss limit exceeded - SKIPPING ANALYSIS");
      LogTrade("========================================");
      return;
   }
   LogTrade("[FILTER PASSED] Daily loss limit OK");

   // 6. Position limit filter
   int currentPositions = GetPositionCount(_Symbol);
   LogTrade(StringFormat("[DEBUG] Current positions: %d | Max allowed: %d", currentPositions, MaxGlobalPositions));

   if(!CanOpenPosition(_Symbol))
   {
      LogTrade("[FILTER FAILED] Position limit reached - SKIPPING NEW TRADES");
      // Still manage existing positions
      ManagePartialProfits(_Symbol);
      CheckRSIDivergence(_Symbol, g_RSI_Handle);
      LogTrade("========================================");
      return;
   }
   LogTrade("[FILTER PASSED] Can open new position");

   // 7. Get indicator values for debugging
   double sma1[], sma2[], sma3[], rsi[], atr[];
   ArraySetAsSeries(sma1, true);
   ArraySetAsSeries(sma2, true);
   ArraySetAsSeries(sma3, true);
   ArraySetAsSeries(rsi, true);
   ArraySetAsSeries(atr, true);

   CopyBuffer(g_SMA1_Handle, 0, 0, 1, sma1);
   CopyBuffer(g_SMA2_Handle, 0, 0, 1, sma2);
   CopyBuffer(g_SMA3_Handle, 0, 0, 1, sma3);
   CopyBuffer(g_RSI_Handle, 0, 0, 1, rsi);
   CopyBuffer(g_ATR_Handle, 0, 0, 1, atr);

   LogTrade("--- INDICATOR VALUES ---");
   LogTrade(StringFormat("SMA21: %.5f | SMA50: %.5f | SMA200: %.5f", sma1[0], sma2[0], sma3[0]));
   LogTrade(StringFormat("RSI(14): %.2f | ATR(14): %.5f", rsi[0], atr[0]));
   LogTrade(StringFormat("Bullish trend? %s (SMA21>SMA50>SMA200)",
            (sma1[0] > sma2[0] && sma2[0] > sma3[0]) ? "YES" : "NO"));
   LogTrade(StringFormat("Bearish trend? %s (SMA200>SMA50>SMA21)",
            (sma3[0] > sma2[0] && sma2[0] > sma1[0]) ? "YES" : "NO"));
   LogTrade(StringFormat("Bullish momentum? %s (RSI>50)", rsi[0] > 50 ? "YES" : "NO"));
   LogTrade(StringFormat("Bearish momentum? %s (RSI<50)", rsi[0] < 50 ? "YES" : "NO"));

   // 8. Check for bullish signal
   LogTrade("--- CHECKING BULLISH SIGNAL ---");
   if(IsCompleteBullishSignal(_Symbol, g_SMA1_Handle, g_SMA2_Handle, g_SMA3_Handle, g_RSI_Handle))
   {
      LogTrade("[SIGNAL DETECTED] Bullish entry conditions met!");
      LogTrade("[PROCESSING BUY] Proximity filter REMOVED - Processing trade");
      ProcessBuySignal();
   }
   else
   {
      LogTrade("[NO SIGNAL] Bullish conditions not met");
   }

   // 9. Check for bearish signal
   LogTrade("--- CHECKING BEARISH SIGNAL ---");
   if(IsCompleteBearishSignal(_Symbol, g_SMA1_Handle, g_SMA2_Handle, g_SMA3_Handle, g_RSI_Handle))
   {
      LogTrade("[SIGNAL DETECTED] Bearish entry conditions met!");
      LogTrade("[PROCESSING SELL] Proximity filter REMOVED - Processing trade");
      ProcessSellSignal();
   }
   else
   {
      LogTrade("[NO SIGNAL] Bearish conditions not met");
   }

   // 10. Manage existing positions
   ManagePartialProfits(_Symbol);
   CheckRSIDivergence(_Symbol, g_RSI_Handle);

   LogTrade("========================================");
   LogTrade("*** M5 BAR ANALYSIS COMPLETE ***");
   LogTrade("========================================");
}

//+------------------------------------------------------------------+
//| Process BUY signal and execute order                             |
//+------------------------------------------------------------------+
void ProcessBuySignal()
{
   LogTrade("*** PROCESSING BUY SIGNAL ***");

   // Get ATR value for SL calculation
   double atrBuffer[];
   ArraySetAsSeries(atrBuffer, true);
   if(CopyBuffer(g_ATR_Handle, 0, 0, 1, atrBuffer) <= 0)
   {
      LogTrade("[ERROR] Failed to copy ATR buffer");
      return;
   }

   double atrValue = atrBuffer[0];

   // Get previous candle low for SL calculation
   double prevLow = iLow(_Symbol, PERIOD_M5, 1);
   double entryPrice = SymbolInfoDouble(_Symbol, SYMBOL_ASK);

   // Calculate SL: Previous candle low - (ATR × Multiplier)
   double calculatedSL = prevLow - (atrValue * ATR_Multiplier);

   LogTrade(StringFormat("[BUY] Initial SL calculation | Prev Low: %.5f | ATR: %.5f | Multiplier: %.1f | SL: %.5f",
            prevLow, atrValue, ATR_Multiplier, calculatedSL));

   // Apply SL snapping if enabled
   double finalSL = SnapSLToLevel(_Symbol, calculatedSL, entryPrice, true, g_ATR_Handle);

   // Recalculate TP to maintain 2:1 R:R (after potential SL adjustment)
   double slDistance = entryPrice - finalSL;
   double tpPrice = entryPrice + (slDistance * 2.0);

   LogTrade(StringFormat("[BUY] TP calculation | Entry: %.5f | SL: %.5f | Distance: %.5f | TP: %.5f (2:1 R:R)",
            entryPrice, finalSL, slDistance, tpPrice));

   // Validate TP doesn't cross H1 resistance
   if(!ValidateTPLevel(_Symbol, entryPrice, tpPrice, true))
   {
      LogTrade("[BUY] Trade ABORTED - TP crosses H1 resistance");
      return;
   }

   // Calculate lot size
   double accountEquity = AccountInfoDouble(ACCOUNT_EQUITY);
   double lotSize = CalculateLotSize(_Symbol, entryPrice, finalSL, RiskPercent, accountEquity);

   if(lotSize <= 0)
   {
      LogTrade("[BUY] Trade ABORTED - Lot size calculation failed (SL too wide or insufficient equity)");
      return;
   }

   // Check margin
   if(!HasSufficientMargin(_Symbol, lotSize, ORDER_TYPE_BUY))
   {
      LogTrade("[BUY] Trade ABORTED - Insufficient margin");
      return;
   }

   // Execute BUY order
   LogTrade(StringFormat("[BUY] Executing order | Lot: %.2f | Entry: %.5f | SL: %.5f | TP: %.5f",
            lotSize, entryPrice, finalSL, tpPrice));

   if(ExecuteBuyOrder(_Symbol, lotSize, finalSL, tpPrice))
   {
      LogTrade("[BUY] *** ORDER EXECUTED SUCCESSFULLY ***");
   }
   else
   {
      LogTrade("[BUY] *** ORDER EXECUTION FAILED ***");
   }
}

//+------------------------------------------------------------------+
//| Process SELL signal and execute order                            |
//+------------------------------------------------------------------+
void ProcessSellSignal()
{
   LogTrade("*** PROCESSING SELL SIGNAL ***");

   // Get ATR value for SL calculation
   double atrBuffer[];
   ArraySetAsSeries(atrBuffer, true);
   if(CopyBuffer(g_ATR_Handle, 0, 0, 1, atrBuffer) <= 0)
   {
      LogTrade("[ERROR] Failed to copy ATR buffer");
      return;
   }

   double atrValue = atrBuffer[0];

   // Get previous candle high for SL calculation
   double prevHigh = iHigh(_Symbol, PERIOD_M5, 1);
   double entryPrice = SymbolInfoDouble(_Symbol, SYMBOL_BID);

   // Calculate SL: Previous candle high + (ATR × Multiplier)
   double calculatedSL = prevHigh + (atrValue * ATR_Multiplier);

   LogTrade(StringFormat("[SELL] Initial SL calculation | Prev High: %.5f | ATR: %.5f | Multiplier: %.1f | SL: %.5f",
            prevHigh, atrValue, ATR_Multiplier, calculatedSL));

   // Apply SL snapping if enabled
   double finalSL = SnapSLToLevel(_Symbol, calculatedSL, entryPrice, false, g_ATR_Handle);

   // Recalculate TP to maintain 2:1 R:R (after potential SL adjustment)
   double slDistance = finalSL - entryPrice;
   double tpPrice = entryPrice - (slDistance * 2.0);

   LogTrade(StringFormat("[SELL] TP calculation | Entry: %.5f | SL: %.5f | Distance: %.5f | TP: %.5f (2:1 R:R)",
            entryPrice, finalSL, slDistance, tpPrice));

   // Validate TP doesn't cross H1 support
   if(!ValidateTPLevel(_Symbol, entryPrice, tpPrice, false))
   {
      LogTrade("[SELL] Trade ABORTED - TP crosses H1 support");
      return;
   }

   // Calculate lot size
   double accountEquity = AccountInfoDouble(ACCOUNT_EQUITY);
   double lotSize = CalculateLotSize(_Symbol, entryPrice, finalSL, RiskPercent, accountEquity);

   if(lotSize <= 0)
   {
      LogTrade("[SELL] Trade ABORTED - Lot size calculation failed (SL too wide or insufficient equity)");
      return;
   }

   // Check margin
   if(!HasSufficientMargin(_Symbol, lotSize, ORDER_TYPE_SELL))
   {
      LogTrade("[SELL] Trade ABORTED - Insufficient margin");
      return;
   }

   // Execute SELL order
   LogTrade(StringFormat("[SELL] Executing order | Lot: %.2f | Entry: %.5f | SL: %.5f | TP: %.5f",
            lotSize, entryPrice, finalSL, tpPrice));

   if(ExecuteSellOrder(_Symbol, lotSize, finalSL, tpPrice))
   {
      LogTrade("[SELL] *** ORDER EXECUTED SUCCESSFULLY ***");
   }
   else
   {
      LogTrade("[SELL] *** ORDER EXECUTION FAILED ***");
   }
}

//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|                                              JC_EntryLogic.mqh |
//|                                             JCAMP Trading System |
//|                     SMA trend, RSI momentum, pattern detection    |
//+------------------------------------------------------------------+
#property copyright "JCAMP Trading"
#property link      ""
#property version   "1.00"
#property strict

#include "JC_Utils.mqh"

//+------------------------------------------------------------------+
//| Include common inputs                                            |
//+------------------------------------------------------------------+
#include <JC_Inputs.mqh>

//+------------------------------------------------------------------+
//| Check if bullish trend exists (SMA 21 > 50 > 200)                |
//+------------------------------------------------------------------+
bool IsBullishTrend(int sma1Handle, int sma2Handle, int sma3Handle)
{
   double sma1[], sma2[], sma3[];
   ArraySetAsSeries(sma1, true);
   ArraySetAsSeries(sma2, true);
   ArraySetAsSeries(sma3, true);

   // Copy latest SMA values
   if(CopyBuffer(sma1Handle, 0, 0, 1, sma1) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy SMA1 buffer");
      return false;
   }

   if(CopyBuffer(sma2Handle, 0, 0, 1, sma2) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy SMA2 buffer");
      return false;
   }

   if(CopyBuffer(sma3Handle, 0, 0, 1, sma3) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy SMA3 buffer");
      return false;
   }

   // Check alignment: SMA(21) > SMA(50) > SMA(200)
   bool isBullish = (sma1[0] > sma2[0]) && (sma2[0] > sma3[0]);

   if(isBullish)
   {
      LogTrade(StringFormat("[EntryLogic] Bullish trend confirmed | SMA21: %.5f > SMA50: %.5f > SMA200: %.5f",
               sma1[0], sma2[0], sma3[0]));
   }

   return isBullish;
}

//+------------------------------------------------------------------+
//| Check if bearish trend exists (SMA 200 > 50 > 21)                |
//+------------------------------------------------------------------+
bool IsBearishTrend(int sma1Handle, int sma2Handle, int sma3Handle)
{
   double sma1[], sma2[], sma3[];
   ArraySetAsSeries(sma1, true);
   ArraySetAsSeries(sma2, true);
   ArraySetAsSeries(sma3, true);

   // Copy latest SMA values
   if(CopyBuffer(sma1Handle, 0, 0, 1, sma1) <= 0 ||
      CopyBuffer(sma2Handle, 0, 0, 1, sma2) <= 0 ||
      CopyBuffer(sma3Handle, 0, 0, 1, sma3) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy SMA buffers");
      return false;
   }

   // Check alignment: SMA(200) > SMA(50) > SMA(21)
   bool isBearish = (sma3[0] > sma2[0]) && (sma2[0] > sma1[0]);

   if(isBearish)
   {
      LogTrade(StringFormat("[EntryLogic] Bearish trend confirmed | SMA200: %.5f > SMA50: %.5f > SMA21: %.5f",
               sma3[0], sma2[0], sma1[0]));
   }

   return isBearish;
}

//+------------------------------------------------------------------+
//| Check if bullish momentum exists (RSI > 50)                      |
//+------------------------------------------------------------------+
bool IsBullishMomentum(int rsiHandle)
{
   double rsi[];
   ArraySetAsSeries(rsi, true);

   if(CopyBuffer(rsiHandle, 0, 0, 1, rsi) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy RSI buffer");
      return false;
   }

   bool isBullish = rsi[0] > 50.0;

   if(isBullish)
   {
      LogTrade(StringFormat("[EntryLogic] Bullish momentum confirmed | RSI: %.1f > 50", rsi[0]));
   }

   return isBullish;
}

//+------------------------------------------------------------------+
//| Check if bearish momentum exists (RSI < 50)                      |
//+------------------------------------------------------------------+
bool IsBearishMomentum(int rsiHandle)
{
   double rsi[];
   ArraySetAsSeries(rsi, true);

   if(CopyBuffer(rsiHandle, 0, 0, 1, rsi) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy RSI buffer");
      return false;
   }

   bool isBearish = rsi[0] < 50.0;

   if(isBearish)
   {
      LogTrade(StringFormat("[EntryLogic] Bearish momentum confirmed | RSI: %.1f < 50", rsi[0]));
   }

   return isBearish;
}

//+------------------------------------------------------------------+
//| Detect bullish engulfing pattern                                 |
//+------------------------------------------------------------------+
bool IsBullishEngulfing(string symbol)
{
   double open[], high[], low[], close[];
   ArraySetAsSeries(open, true);
   ArraySetAsSeries(high, true);
   ArraySetAsSeries(low, true);
   ArraySetAsSeries(close, true);

   // Copy last 3 candles (need [2] and [1] - both COMPLETED bars)
   if(CopyOpen(symbol, PERIOD_M5, 0, 3, open) <= 0 ||
      CopyHigh(symbol, PERIOD_M5, 0, 3, high) <= 0 ||
      CopyLow(symbol, PERIOD_M5, 0, 3, low) <= 0 ||
      CopyClose(symbol, PERIOD_M5, 0, 3, close) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy price data for pattern detection");
      return false;
   }

   // FIXED: Check COMPLETED bars [1] and [2], not [0] and [1]
   // [0] = new bar just started (incomplete)
   // [1] = bar that just closed (current for pattern)
   // [2] = bar before that (previous for pattern)

   // Bullish Engulfing:
   // 1. Previous candle [2] is bearish (close < open)
   // 2. Current candle [1] is bullish (close > open)
   // 3. Current [1] engulfs previous [2] (with 3 pip buffer for flexibility)

   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
   double pipBuffer = 3.0 * 10.0 * point; // 3 pips buffer (for 5-digit quotes)

   bool prevBearish = close[2] < open[2];
   bool currBullish = close[1] > open[1];
   // Engulfing with buffer: bull can open up to 3 pips above bear's close
   bool engulfs = (open[1] <= close[2] + pipBuffer) && (close[1] >= open[2]);

   LogTrade(StringFormat("[DEBUG] Bullish Engulfing check | Prev[2]: %s | Curr[1]: %s | Engulfs: %s",
            prevBearish ? "BEARISH" : "bullish",
            currBullish ? "BULLISH" : "bearish",
            engulfs ? "YES" : "no"));

   if(prevBearish && currBullish && engulfs)
   {
      LogTrade(StringFormat("[EntryLogic] Bullish Engulfing DETECTED | Prev[2] O:%.5f C:%.5f | Curr[1] O:%.5f C:%.5f",
               open[2], close[2], open[1], close[1]));
      return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//| Detect bearish engulfing pattern                                 |
//+------------------------------------------------------------------+
bool IsBearishEngulfing(string symbol)
{
   double open[], high[], low[], close[];
   ArraySetAsSeries(open, true);
   ArraySetAsSeries(high, true);
   ArraySetAsSeries(low, true);
   ArraySetAsSeries(close, true);

   // Copy last 3 candles (need [2] and [1] - both COMPLETED bars)
   if(CopyOpen(symbol, PERIOD_M5, 0, 3, open) <= 0 ||
      CopyHigh(symbol, PERIOD_M5, 0, 3, high) <= 0 ||
      CopyLow(symbol, PERIOD_M5, 0, 3, low) <= 0 ||
      CopyClose(symbol, PERIOD_M5, 0, 3, close) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy price data for pattern detection");
      return false;
   }

   // FIXED: Check COMPLETED bars [1] and [2], not [0] and [1]
   // Bearish Engulfing:
   // 1. Previous candle [2] is bullish (close > open)
   // 2. Current candle [1] is bearish (close < open)
   // 3. Current [1] engulfs previous [2] (with 3 pip buffer for flexibility)

   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
   double pipBuffer = 3.0 * 10.0 * point; // 3 pips buffer (for 5-digit quotes)

   bool prevBullish = close[2] > open[2];
   bool currBearish = close[1] < open[1];
   // Engulfing with buffer: bear can open up to 3 pips below bull's close
   bool engulfs = (open[1] >= close[2] - pipBuffer) && (close[1] <= open[2]);

   LogTrade(StringFormat("[DEBUG] Bearish Engulfing check | Prev[2]: %s | Curr[1]: %s | Engulfs: %s",
            prevBullish ? "BULLISH" : "bearish",
            currBearish ? "BEARISH" : "bullish",
            engulfs ? "YES" : "no"));

   if(prevBullish && currBearish && engulfs)
   {
      LogTrade(StringFormat("[EntryLogic] Bearish Engulfing DETECTED | Prev[2] O:%.5f C:%.5f | Curr[1] O:%.5f C:%.5f",
               open[2], close[2], open[1], close[1]));
      return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//| Detect complex bullish pattern (3 bulls + 1 bear + bull)         |
//| Pattern: 3 consecutive bullish, then 1 bearish, then bullish     |
//| that closes above bearish candle's open                          |
//+------------------------------------------------------------------+
bool IsComplexBullishPattern(string symbol)
{
   double open[], close[];
   ArraySetAsSeries(open, true);
   ArraySetAsSeries(close, true);

   // Copy last 6 candles (need [1] through [5] - all COMPLETED bars)
   if(CopyOpen(symbol, PERIOD_M5, 0, 6, open) <= 0 ||
      CopyClose(symbol, PERIOD_M5, 0, 6, close) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy price data for complex pattern");
      return false;
   }

   // FIXED: Check COMPLETED bars [1] through [5], not [0] through [4]
   // [0] = new bar just started (incomplete)
   // [1] = bar that just closed (most recent in pattern)
   // [5] = oldest bar in pattern

   // Check pattern: [5]=bull, [4]=bull, [3]=bull, [2]=bear, [1]=bull
   bool bull5 = close[5] > open[5];
   bool bull4 = close[4] > open[4];
   bool bull3 = close[3] > open[3];
   bool bear2 = close[2] < open[2];
   bool bull1 = close[1] > open[1];

   LogTrade(StringFormat("[DEBUG] Complex bullish pattern check | [5]:%s [4]:%s [3]:%s [2]:%s [1]:%s",
            bull5 ? "BULL" : "bear",
            bull4 ? "BULL" : "bear",
            bull3 ? "BULL" : "bear",
            bear2 ? "BEAR" : "bull",
            bull1 ? "BULL" : "bear"));

   if(bull5 && bull4 && bull3 && bear2 && bull1)
   {
      LogTrade("[EntryLogic] Complex bullish pattern DETECTED | 3 bulls + 1 bear + 1 bull");
      return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//| Detect complex bearish pattern (3 bears + 1 bull + bear)         |
//+------------------------------------------------------------------+
bool IsComplexBearishPattern(string symbol)
{
   double open[], close[];
   ArraySetAsSeries(open, true);
   ArraySetAsSeries(close, true);

   // Copy last 6 candles (need [1] through [5] - all COMPLETED bars)
   if(CopyOpen(symbol, PERIOD_M5, 0, 6, open) <= 0 ||
      CopyClose(symbol, PERIOD_M5, 0, 6, close) <= 0)
   {
      LogTrade("[EntryLogic] ERROR: Failed to copy price data for complex pattern");
      return false;
   }

   // FIXED: Check COMPLETED bars [1] through [5], not [0] through [4]
   // Check pattern: [5]=bear, [4]=bear, [3]=bear, [2]=bull, [1]=bear
   bool bear5 = close[5] < open[5];
   bool bear4 = close[4] < open[4];
   bool bear3 = close[3] < open[3];
   bool bull2 = close[2] > open[2];
   bool bear1 = close[1] < open[1];

   LogTrade(StringFormat("[DEBUG] Complex bearish pattern check | [5]:%s [4]:%s [3]:%s [2]:%s [1]:%s",
            bear5 ? "BEAR" : "bull",
            bear4 ? "BEAR" : "bull",
            bear3 ? "BEAR" : "bull",
            bull2 ? "BULL" : "bear",
            bear1 ? "BEAR" : "bull"));

   if(bear5 && bear4 && bear3 && bull2 && bear1)
   {
      LogTrade("[EntryLogic] Complex bearish pattern DETECTED | 3 bears + 1 bull + 1 bear");
      return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//| Main bullish trigger detection (checks both patterns)            |
//+------------------------------------------------------------------+
bool DetectBullishTrigger(string symbol)
{
   // Check Pattern 1: Bullish Engulfing
   if(IsBullishEngulfing(symbol))
      return true;

   // Check Pattern 2: Complex bullish pattern
   if(IsComplexBullishPattern(symbol))
      return true;

   return false;
}

//+------------------------------------------------------------------+
//| Main bearish trigger detection (checks both patterns)            |
//+------------------------------------------------------------------+
bool DetectBearishTrigger(string symbol)
{
   // Check Pattern 1: Bearish Engulfing
   if(IsBearishEngulfing(symbol))
      return true;

   // Check Pattern 2: Complex bearish pattern
   if(IsComplexBearishPattern(symbol))
      return true;

   return false;
}

//+------------------------------------------------------------------+
//| Complete bullish entry signal validation                         |
//+------------------------------------------------------------------+
bool IsCompleteBullishSignal(string symbol, int sma1Handle, int sma2Handle, int sma3Handle, int rsiHandle)
{
   // All conditions must be true:
   // 1. Bullish trend (SMA 21 > 50 > 200)
   // 2. Bullish momentum (RSI > 50)
   // 3. Bullish trigger pattern

   if(!IsBullishTrend(sma1Handle, sma2Handle, sma3Handle))
   {
      LogTrade("[EntryLogic] Bullish signal rejected - No bullish trend");
      return false;
   }

   if(!IsBullishMomentum(rsiHandle))
   {
      LogTrade("[EntryLogic] Bullish signal rejected - No bullish momentum");
      return false;
   }

   if(!DetectBullishTrigger(symbol))
   {
      LogTrade("[EntryLogic] Bullish signal rejected - No bullish trigger pattern");
      return false;
   }

   LogTrade("[EntryLogic] *** COMPLETE BULLISH SIGNAL CONFIRMED ***");
   return true;
}

//+------------------------------------------------------------------+
//| Complete bearish entry signal validation                         |
//+------------------------------------------------------------------+
bool IsCompleteBearishSignal(string symbol, int sma1Handle, int sma2Handle, int sma3Handle, int rsiHandle)
{
   // All conditions must be true:
   // 1. Bearish trend (SMA 200 > 50 > 21)
   // 2. Bearish momentum (RSI < 50)
   // 3. Bearish trigger pattern

   if(!IsBearishTrend(sma1Handle, sma2Handle, sma3Handle))
   {
      LogTrade("[EntryLogic] Bearish signal rejected - No bearish trend");
      return false;
   }

   if(!IsBearishMomentum(rsiHandle))
   {
      LogTrade("[EntryLogic] Bearish signal rejected - No bearish momentum");
      return false;
   }

   if(!DetectBearishTrigger(symbol))
   {
      LogTrade("[EntryLogic] Bearish signal rejected - No bearish trigger pattern");
      return false;
   }

   LogTrade("[EntryLogic] *** COMPLETE BEARISH SIGNAL CONFIRMED ***");
   return true;
}

//+------------------------------------------------------------------+

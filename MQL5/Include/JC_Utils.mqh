//+------------------------------------------------------------------+
//|                                                     JC_Utils.mqh |
//|                                             JCAMP Trading System |
//|                        Session filters, time validation, logging |
//+------------------------------------------------------------------+
#property copyright "JCAMP Trading"
#property link      ""
#property version   "1.00"
#property strict

//+------------------------------------------------------------------+
//| Session time constants (GMT)                                      |
//+------------------------------------------------------------------+
#define LONDON_START_HOUR      8
#define LONDON_START_MINUTE    0
#define LONDON_END_HOUR        16
#define LONDON_END_MINUTE      30

#define NEWYORK_START_HOUR     13
#define NEWYORK_START_MINUTE   0
#define NEWYORK_END_HOUR       21
#define NEWYORK_END_MINUTE     0

#define ASIAN_START_HOUR       23
#define ASIAN_START_MINUTE     0
#define ASIAN_END_HOUR         8
#define ASIAN_END_MINUTE       0

#define TOKYO_START_HOUR       0
#define TOKYO_START_MINUTE     0
#define TOKYO_END_HOUR         9
#define TOKYO_END_MINUTE       0

//+------------------------------------------------------------------+
//| Include common inputs                                            |
//+------------------------------------------------------------------+
#include <JC_Inputs.mqh>

//+------------------------------------------------------------------+
//| Check if current time falls within London session                |
//+------------------------------------------------------------------+
bool IsLondonSession()
{
   datetime currentTime = TimeGMT();
   MqlDateTime timeStruct;
   TimeToStruct(currentTime, timeStruct);

   int currentHour = timeStruct.hour;
   int currentMinute = timeStruct.min;

   // London: 08:00 - 16:30 GMT
   int startTime = LONDON_START_HOUR * 60 + LONDON_START_MINUTE;
   int endTime = LONDON_END_HOUR * 60 + LONDON_END_MINUTE;
   int nowTime = currentHour * 60 + currentMinute;

   return (nowTime >= startTime && nowTime <= endTime);
}

//+------------------------------------------------------------------+
//| Check if current time falls within New York session              |
//+------------------------------------------------------------------+
bool IsNewYorkSession()
{
   datetime currentTime = TimeGMT();
   MqlDateTime timeStruct;
   TimeToStruct(currentTime, timeStruct);

   int currentHour = timeStruct.hour;
   int currentMinute = timeStruct.min;

   // New York: 13:00 - 21:00 GMT
   int startTime = NEWYORK_START_HOUR * 60 + NEWYORK_START_MINUTE;
   int endTime = NEWYORK_END_HOUR * 60 + NEWYORK_END_MINUTE;
   int nowTime = currentHour * 60 + currentMinute;

   return (nowTime >= startTime && nowTime <= endTime);
}

//+------------------------------------------------------------------+
//| Check if current time falls within Asian session                 |
//+------------------------------------------------------------------+
bool IsAsianSession()
{
   datetime currentTime = TimeGMT();
   MqlDateTime timeStruct;
   TimeToStruct(currentTime, timeStruct);

   int currentHour = timeStruct.hour;
   int currentMinute = timeStruct.min;

   // Asian: 23:00 - 08:00 GMT (spans midnight)
   int startTime = ASIAN_START_HOUR * 60 + ASIAN_START_MINUTE;
   int endTime = ASIAN_END_HOUR * 60 + ASIAN_END_MINUTE;
   int nowTime = currentHour * 60 + currentMinute;

   // Handle midnight crossover
   if(startTime > endTime)
      return (nowTime >= startTime || nowTime <= endTime);
   else
      return (nowTime >= startTime && nowTime <= endTime);
}

//+------------------------------------------------------------------+
//| Check if current time falls within Tokyo session                 |
//+------------------------------------------------------------------+
bool IsTokyoSession()
{
   datetime currentTime = TimeGMT();
   MqlDateTime timeStruct;
   TimeToStruct(currentTime, timeStruct);

   int currentHour = timeStruct.hour;
   int currentMinute = timeStruct.min;

   // Tokyo: 00:00 - 09:00 GMT
   int startTime = TOKYO_START_HOUR * 60 + TOKYO_START_MINUTE;
   int endTime = TOKYO_END_HOUR * 60 + TOKYO_END_MINUTE;
   int nowTime = currentHour * 60 + currentMinute;

   return (nowTime >= startTime && nowTime <= endTime);
}

//+------------------------------------------------------------------+
//| Check if ANY enabled session is currently active                 |
//+------------------------------------------------------------------+
bool IsActiveSession()
{
   bool isActive = false;

   if(TradeLondon && IsLondonSession())
   {
      isActive = true;
   }

   if(TradeNewYork && IsNewYorkSession())
   {
      isActive = true;
   }

   if(TradeAsian && IsAsianSession())
   {
      isActive = true;
   }

   if(TradeTokyo && IsTokyoSession())
   {
      isActive = true;
   }

   return isActive;
}

//+------------------------------------------------------------------+
//| Check if current spread is acceptable for trading                |
//+------------------------------------------------------------------+
bool IsSpreadAcceptable(string symbol)
{
   double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);
   double bid = SymbolInfoDouble(symbol, SYMBOL_BID);
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   if(point == 0)
   {
      LogTrade("[Utils] ERROR: Point value is zero for " + symbol);
      return false;
   }

   double spreadPoints = (ask - bid) / point;
   double spreadPips = spreadPoints / 10.0; // Convert to pips (for 5-digit quotes)

   if(spreadPips <= MaxSpread)
   {
      return true;
   }
   else
   {
      LogTrade(StringFormat("[Utils] Spread too wide: %.1f pips (max %.1f)", spreadPips, MaxSpread));
      return false;
   }
}

//+------------------------------------------------------------------+
//| Structured logging with timestamps                               |
//+------------------------------------------------------------------+
void LogTrade(string message)
{
   datetime currentTime = TimeGMT();
   string timestamp = TimeToString(currentTime, TIME_DATE|TIME_SECONDS);

   Print("[JCAMP_FxScalper] ", timestamp, " GMT | ", message);
}

//+------------------------------------------------------------------+
//| Convert GMT time to readable string                              |
//+------------------------------------------------------------------+
string GMTTimeToString()
{
   datetime currentTime = TimeGMT();
   return TimeToString(currentTime, TIME_DATE|TIME_SECONDS) + " GMT";
}

//+------------------------------------------------------------------+
//| Check if a new bar has formed on specified timeframe             |
//+------------------------------------------------------------------+
bool IsNewBar(string symbol, ENUM_TIMEFRAMES timeframe, datetime &lastBarTime)
{
   datetime currentBarTime = iTime(symbol, timeframe, 0);

   if(currentBarTime != lastBarTime)
   {
      lastBarTime = currentBarTime;
      return true;
   }

   return false;
}

//+------------------------------------------------------------------+
//| Get session name for logging purposes                            |
//+------------------------------------------------------------------+
string GetActiveSessionName()
{
   string sessions = "";

   if(TradeLondon && IsLondonSession())
      sessions += "London ";

   if(TradeNewYork && IsNewYorkSession())
      sessions += "NewYork ";

   if(TradeAsian && IsAsianSession())
      sessions += "Asian ";

   if(TradeTokyo && IsTokyoSession())
      sessions += "Tokyo ";

   if(sessions == "")
      return "None";

   return sessions;
}

//+------------------------------------------------------------------+
//| Validate symbol exists and is tradeable                          |
//+------------------------------------------------------------------+
bool IsSymbolValid(string symbol)
{
   if(!SymbolInfoInteger(symbol, SYMBOL_SELECT))
   {
      // Try to select symbol in Market Watch
      if(!SymbolSelect(symbol, true))
      {
         LogTrade(StringFormat("[Utils] ERROR: Symbol %s not found in Market Watch", symbol));
         return false;
      }
   }

   // Check if symbol allows trading
   if(!SymbolInfoInteger(symbol, SYMBOL_TRADE_MODE))
   {
      LogTrade(StringFormat("[Utils] ERROR: Trading disabled for symbol %s", symbol));
      return false;
   }

   return true;
}

//+------------------------------------------------------------------+

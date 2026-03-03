//+------------------------------------------------------------------+
//|                                               JC_RiskManager.mqh |
//|                                             JCAMP Trading System |
//|                   Position limits, lot sizing, daily loss tracking |
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
//| Global variables for daily loss tracking                         |
//+------------------------------------------------------------------+
double g_StartingDailyEquity = 0;
int g_LastTrackedDay = 0;

//+------------------------------------------------------------------+
//| Calculate lot size based on SL distance and risk percentage      |
//+------------------------------------------------------------------+
double CalculateLotSize(string symbol, double entryPrice, double slPrice, double riskPercent, double accountEquity)
{
   // Risk amount in account currency
   double riskAmount = accountEquity * (riskPercent / 100.0);

   // SL distance in points
   double slDistance = MathAbs(entryPrice - slPrice);
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   if(point == 0)
   {
      LogTrade("[RiskManager] ERROR: Point value is zero");
      return 0;
   }

   double slDistancePoints = slDistance / point;

   // Pip value calculation
   double contractSize = SymbolInfoDouble(symbol, SYMBOL_TRADE_CONTRACT_SIZE);
   double tickValue = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);
   double tickSize = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);

   if(tickSize == 0)
   {
      LogTrade("[RiskManager] ERROR: Tick size is zero");
      return 0;
   }

   // Calculate lot size
   // Risk Amount = Lot Size × SL Distance × Tick Value / Tick Size
   double lotSize = riskAmount / (slDistancePoints * tickValue / tickSize);

   // Get symbol lot constraints
   double minLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

   // Round DOWN to lot step (safety first - never risk more than intended)
   lotSize = MathFloor(lotSize / lotStep) * lotStep;

   // Validate lot size
   if(lotSize < minLot)
   {
      LogTrade(StringFormat("[RiskManager] Calculated lot %.2f below minimum %.2f | SL too wide for account size | Risk=$%.2f | SL Distance=%.1f pips",
               lotSize, minLot, riskAmount, slDistancePoints / 10.0));
      return 0; // Abort trade - SL too wide for risk amount
   }

   if(lotSize > maxLot)
   {
      LogTrade(StringFormat("[RiskManager] WARNING: Calculated lot %.2f exceeds maximum %.2f | Capping at max", lotSize, maxLot));
      lotSize = maxLot;
   }

   // Log final calculation
   LogTrade(StringFormat("[RiskManager] Lot size calculated | Risk=%.1f%% ($%.2f) | SL Distance=%.1f pips | Lot=%.2f",
            riskPercent, riskAmount, slDistancePoints / 10.0, lotSize));

   return lotSize;
}

//+------------------------------------------------------------------+
//| Check if EA can open new position (position limit check)         |
//+------------------------------------------------------------------+
bool CanOpenPosition(string symbol)
{
   int totalPositions = PositionsTotal();
   int symbolPositions = 0;

   // Count positions for current symbol
   for(int i = 0; i < totalPositions; i++)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket > 0)
      {
         if(PositionGetString(POSITION_SYMBOL) == symbol)
            symbolPositions++;
      }
   }

   // Phase 1: Max 1 position for EURUSD
   if(symbolPositions >= MaxGlobalPositions)
   {
      LogTrade(StringFormat("[RiskManager] Position limit reached | Current: %d | Max: %d", symbolPositions, MaxGlobalPositions));
      return false;
   }

   // Check total global positions (for Phase 2 multi-pair)
   if(totalPositions >= MaxGlobalPositions)
   {
      LogTrade(StringFormat("[RiskManager] Global position limit reached | Total: %d | Max: %d", totalPositions, MaxGlobalPositions));
      return false;
   }

   return true;
}

//+------------------------------------------------------------------+
//| Initialize daily loss tracking (call on new day)                 |
//+------------------------------------------------------------------+
void InitializeDailyTracking()
{
   MqlDateTime timeStruct;
   TimeToStruct(TimeGMT(), timeStruct);

   if(timeStruct.day != g_LastTrackedDay)
   {
      g_StartingDailyEquity = AccountInfoDouble(ACCOUNT_EQUITY);
      g_LastTrackedDay = timeStruct.day;

      LogTrade(StringFormat("[RiskManager] New trading day | Starting equity: $%.2f | Max daily loss: %.1f%% ($%.2f)",
               g_StartingDailyEquity, MaxDailyLoss, g_StartingDailyEquity * (MaxDailyLoss / 100.0)));
   }
}

//+------------------------------------------------------------------+
//| Check if daily loss limit has been exceeded                      |
//+------------------------------------------------------------------+
bool CheckDailyLossLimit()
{
   // Initialize if not done yet
   if(g_StartingDailyEquity == 0)
   {
      InitializeDailyTracking();
      return true; // First check of the day, allow trading
   }

   // Check if new day started
   InitializeDailyTracking();

   // Calculate current daily P&L
   double currentEquity = AccountInfoDouble(ACCOUNT_EQUITY);
   double dailyPL = currentEquity - g_StartingDailyEquity;
   double dailyPLPercent = (dailyPL / g_StartingDailyEquity) * 100.0;

   // Check if loss limit exceeded
   if(dailyPLPercent <= -MaxDailyLoss)
   {
      LogTrade(StringFormat("[RiskManager] DAILY LOSS LIMIT EXCEEDED | Daily P&L: $%.2f (%.2f%%) | Limit: -%.1f%% | Trading halted",
               dailyPL, dailyPLPercent, MaxDailyLoss));
      return false;
   }

   return true;
}

//+------------------------------------------------------------------+
//| Calculate break-even price including commission                  |
//+------------------------------------------------------------------+
double CalculateBreakEvenPrice(string symbol, double entryPrice, double lotSize, bool isBuy, double commissionPerLot = 3.0)
{
   // FP Markets: $3 per lot per side = $6 round-trip
   double totalCommission = commissionPerLot * 2.0; // Round-trip

   double contractSize = SymbolInfoDouble(symbol, SYMBOL_TRADE_CONTRACT_SIZE);
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   if(contractSize == 0 || point == 0 || lotSize == 0)
   {
      LogTrade("[RiskManager] ERROR: Invalid parameters for BE calculation");
      return entryPrice;
   }

   // Commission in price terms
   double commissionAdjustment = (totalCommission / lotSize / contractSize);

   double bePrice;
   if(isBuy)
      bePrice = entryPrice + commissionAdjustment;
   else
      bePrice = entryPrice - commissionAdjustment;

   // Log for verification
   LogTrade(StringFormat("[RiskManager] Break-even calculated | Entry=%.5f | BE=%.5f | Adjustment=%.5f (%.1f pips) | Lot=%.2f",
            entryPrice, bePrice, commissionAdjustment, (commissionAdjustment / point) / 10.0, lotSize));

   return bePrice;
}

//+------------------------------------------------------------------+
//| Check if sufficient margin available for new position            |
//+------------------------------------------------------------------+
bool HasSufficientMargin(string symbol, double lotSize, ENUM_ORDER_TYPE orderType)
{
   double freeMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   double price = (orderType == ORDER_TYPE_BUY) ?
                  SymbolInfoDouble(symbol, SYMBOL_ASK) :
                  SymbolInfoDouble(symbol, SYMBOL_BID);

   // Calculate required margin
   double requiredMargin = 0;
   if(!OrderCalcMargin(orderType, symbol, lotSize, price, requiredMargin))
   {
      LogTrade(StringFormat("[RiskManager] ERROR: Failed to calculate margin | Error=%d", GetLastError()));
      return false;
   }

   if(freeMargin < requiredMargin)
   {
      LogTrade(StringFormat("[RiskManager] Insufficient margin | Required: $%.2f | Available: $%.2f", requiredMargin, freeMargin));
      return false;
   }

   // Log margin check (for monitoring)
   double marginPercent = (requiredMargin / freeMargin) * 100.0;
   LogTrade(StringFormat("[RiskManager] Margin check OK | Required: $%.2f (%.1f%% of free margin)", requiredMargin, marginPercent));

   return true;
}

//+------------------------------------------------------------------+
//| Get daily P&L statistics for logging                             |
//+------------------------------------------------------------------+
string GetDailyPLStats()
{
   if(g_StartingDailyEquity == 0)
      return "Daily tracking not initialized";

   double currentEquity = AccountInfoDouble(ACCOUNT_EQUITY);
   double dailyPL = currentEquity - g_StartingDailyEquity;
   double dailyPLPercent = (dailyPL / g_StartingDailyEquity) * 100.0;

   return StringFormat("Daily P&L: $%.2f (%.2f%%) | Start: $%.2f | Current: $%.2f",
                       dailyPL, dailyPLPercent, g_StartingDailyEquity, currentEquity);
}

//+------------------------------------------------------------------+
//| Validate if position size allows for partial profits             |
//+------------------------------------------------------------------+
bool CanExecutePartials(double lotSize)
{
   // For 80/20 split: Need minimum 0.05 lot
   // 80% of 0.05 = 0.04 (valid)
   // 20% of 0.05 = 0.01 (valid)

   double minLotForPartials = 0.05;

   if(lotSize < minLotForPartials)
   {
      LogTrade(StringFormat("[RiskManager] Position too small for partials | Lot: %.2f | Minimum: %.2f | Will exit 100%% at TP",
               lotSize, minLotForPartials));
      return false;
   }

   return true;
}

//+------------------------------------------------------------------+
//| Calculate partial position sizes (80% / 20%)                     |
//+------------------------------------------------------------------+
bool CalculatePartialSizes(double originalLot, double &lot80Percent, double &lot20Percent)
{
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);

   // Calculate 80% and 20%
   lot80Percent = MathFloor((originalLot * 0.80) / lotStep) * lotStep;
   lot20Percent = MathFloor((originalLot * 0.20) / lotStep) * lotStep;

   // Validate both parts meet minimum
   if(lot80Percent < minLot || lot20Percent < minLot)
   {
      LogTrade(StringFormat("[RiskManager] Partial calculation failed | 80%%: %.2f | 20%%: %.2f | Min: %.2f",
               lot80Percent, lot20Percent, minLot));
      return false;
   }

   // Ensure total equals original (adjust for rounding)
   double total = lot80Percent + lot20Percent;
   if(MathAbs(total - originalLot) > lotStep)
   {
      LogTrade(StringFormat("[RiskManager] WARNING: Partial rounding error | Original: %.2f | Total: %.2f",
               originalLot, total));
   }

   LogTrade(StringFormat("[RiskManager] Partials calculated | Original: %.2f | 80%%: %.2f | 20%%: %.2f",
            originalLot, lot80Percent, lot20Percent));

   return true;
}

//+------------------------------------------------------------------+

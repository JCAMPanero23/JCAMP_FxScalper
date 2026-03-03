//+------------------------------------------------------------------+
//|                                          JC_MarketStructure.mqh |
//|                                             JCAMP Trading System |
//|          H1 fractal levels, TP validation, SL snapping, proximity |
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
//| Global variables for cached H1 levels                            |
//+------------------------------------------------------------------+
double g_NearestSupport = 0;
double g_NearestResistance = 0;
double g_AllSupports[50];      // Cache up to 50 support levels
double g_AllResistances[50];   // Cache up to 50 resistance levels
int g_SupportsCount = 0;
int g_ResistancesCount = 0;
datetime g_LastH1BarTime = 0;

//+------------------------------------------------------------------+
//| Initialize H1 level arrays                                        |
//+------------------------------------------------------------------+
void InitializeMarketStructure()
{
   ArrayInitialize(g_AllSupports, 0);
   ArrayInitialize(g_AllResistances, 0);
   g_SupportsCount = 0;
   g_ResistancesCount = 0;

   LogTrade("[MarketStructure] Initialized - Ready to detect H1 levels");
}

//+------------------------------------------------------------------+
//| Update H1 levels using Williams Fractals (5-bar pattern)         |
//+------------------------------------------------------------------+
void UpdateH1Levels(string symbol)
{
   // Only update on new H1 bar
   datetime currentH1BarTime = iTime(symbol, PERIOD_H1, 0);
   if(currentH1BarTime == g_LastH1BarTime)
      return;

   g_LastH1BarTime = currentH1BarTime;

   // Reset level arrays
   ArrayInitialize(g_AllSupports, 0);
   ArrayInitialize(g_AllResistances, 0);
   g_SupportsCount = 0;
   g_ResistancesCount = 0;

   // Get current price for reference
   double currentPrice = SymbolInfoDouble(symbol, SYMBOL_BID);

   // Scan last 200 H1 bars for fractal levels (approximately 1 month of H1 data)
   int barsToScan = 200;
   int supportIdx = 0;
   int resistanceIdx = 0;

   for(int i = 2; i < barsToScan - 2; i++) // Need 2 bars on each side for fractal
   {
      double high1 = iHigh(symbol, PERIOD_H1, i - 2);
      double high2 = iHigh(symbol, PERIOD_H1, i - 1);
      double highCenter = iHigh(symbol, PERIOD_H1, i);
      double high3 = iHigh(symbol, PERIOD_H1, i + 1);
      double high4 = iHigh(symbol, PERIOD_H1, i + 2);

      double low1 = iLow(symbol, PERIOD_H1, i - 2);
      double low2 = iLow(symbol, PERIOD_H1, i - 1);
      double lowCenter = iLow(symbol, PERIOD_H1, i);
      double low3 = iLow(symbol, PERIOD_H1, i + 1);
      double low4 = iLow(symbol, PERIOD_H1, i + 2);

      // Williams Fractal Up (Resistance): High[i] > High[i±1] and High[i] > High[i±2]
      if(highCenter > high1 && highCenter > high2 && highCenter > high3 && highCenter > high4)
      {
         if(resistanceIdx < ArraySize(g_AllResistances))
         {
            g_AllResistances[resistanceIdx] = highCenter;
            resistanceIdx++;
         }
      }

      // Williams Fractal Down (Support): Low[i] < Low[i±1] and Low[i] < Low[i±2]
      if(lowCenter < low1 && lowCenter < low2 && lowCenter < low3 && lowCenter < low4)
      {
         if(supportIdx < ArraySize(g_AllSupports))
         {
            g_AllSupports[supportIdx] = lowCenter;
            supportIdx++;
         }
      }
   }

   g_SupportsCount = supportIdx;
   g_ResistancesCount = resistanceIdx;

   // Find nearest levels to current price
   g_NearestSupport = FindNearestSupportBelow(currentPrice, symbol);
   g_NearestResistance = FindNearestResistanceAbove(currentPrice, symbol);

   // Log update
   LogTrade(StringFormat("[MarketStructure] H1 levels updated | Supports: %d | Resistances: %d | Nearest Support: %.5f | Nearest Resistance: %.5f",
            g_SupportsCount, g_ResistancesCount, g_NearestSupport, g_NearestResistance));
}

//+------------------------------------------------------------------+
//| Find nearest support level below given price                     |
//+------------------------------------------------------------------+
double FindNearestSupportBelow(double price, string symbol)
{
   double nearestSupport = 0;
   double minDistance = 999999;

   for(int i = 0; i < g_SupportsCount; i++)
   {
      if(g_AllSupports[i] > 0 && g_AllSupports[i] < price)
      {
         double distance = price - g_AllSupports[i];
         if(distance < minDistance)
         {
            minDistance = distance;
            nearestSupport = g_AllSupports[i];
         }
      }
   }

   return nearestSupport;
}

//+------------------------------------------------------------------+
//| Find nearest resistance level above given price                  |
//+------------------------------------------------------------------+
double FindNearestResistanceAbove(double price, string symbol)
{
   double nearestResistance = 0;
   double minDistance = 999999;

   for(int i = 0; i < g_ResistancesCount; i++)
   {
      if(g_AllResistances[i] > 0 && g_AllResistances[i] > price)
      {
         double distance = g_AllResistances[i] - price;
         if(distance < minDistance)
         {
            minDistance = distance;
            nearestResistance = g_AllResistances[i];
         }
      }
   }

   return nearestResistance;
}

//+------------------------------------------------------------------+
//| Get nearest H1 support level (public interface)                  |
//+------------------------------------------------------------------+
double GetNearestH1Support()
{
   return g_NearestSupport;
}

//+------------------------------------------------------------------+
//| Get nearest H1 resistance level (public interface)               |
//+------------------------------------------------------------------+
double GetNearestH1Resistance()
{
   return g_NearestResistance;
}

//+------------------------------------------------------------------+
//| Check if price is within proximity of H1 level                   |
//+------------------------------------------------------------------+
bool IsPriceNearLevel(string symbol, bool isBuy)
{
   double currentPrice = SymbolInfoDouble(symbol, SYMBOL_BID);
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
   double proximityDistance = LevelProximity * 10 * point; // Convert pips to price

   double relevantLevel = isBuy ? g_NearestSupport : g_NearestResistance;

   if(relevantLevel == 0)
   {
      LogTrade("[MarketStructure] No H1 level detected - cannot validate proximity");
      return false;
   }

   double distance = MathAbs(currentPrice - relevantLevel);

   if(distance <= proximityDistance)
   {
      LogTrade(StringFormat("[MarketStructure] Price near H1 level | Price: %.5f | Level: %.5f | Distance: %.1f pips | Max: %d pips",
               currentPrice, relevantLevel, (distance / point) / 10.0, LevelProximity));
      return true;
   }
   else
   {
      LogTrade(StringFormat("[MarketStructure] Price NOT near H1 level | Distance: %.1f pips | Required: <%d pips",
               (distance / point) / 10.0, LevelProximity));
      return false;
   }
}

//+------------------------------------------------------------------+
//| Validate that TP doesn't cross H1 resistance/support             |
//| Returns TRUE if TP is clear (no levels blocking target)          |
//| Returns FALSE if TP crosses a level (trade should be aborted)    |
//+------------------------------------------------------------------+
bool ValidateTPLevel(string symbol, double entryPrice, double tpPrice, bool isBuy)
{
   if(!EnableTPValidation)
      return true; // Validation disabled

   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   // Calculate 75% threshold - if level is beyond this point, allow trade
   double distanceToTP = MathAbs(tpPrice - entryPrice);
   double threshold75Percent = 0.75 * distanceToTP;

   if(isBuy)
   {
      // For BUY: Check if any resistance level between entry and TP
      // SMART RULE: Allow if resistance is at 75% or more of the way to TP
      for(int i = 0; i < g_ResistancesCount; i++)
      {
         double resistance = g_AllResistances[i];
         if(resistance > entryPrice && resistance < tpPrice)
         {
            // Calculate how far resistance is from entry
            double distanceToResistance = resistance - entryPrice;
            double percentToTP = (distanceToResistance / distanceToTP) * 100.0;

            if(distanceToResistance >= threshold75Percent)
            {
               // Resistance is at 75% or more - ALLOW TRADE, just adjust TP
               LogTrade(StringFormat("[MarketStructure] TP validation: Resistance at %.1f%% to target | Entry: %.5f | Resistance: %.5f | Original TP: %.5f | ALLOWING TRADE",
                        percentToTP, entryPrice, resistance, tpPrice));
               return true;
            }
            else
            {
               // Resistance is too close (less than 75%) - REJECT TRADE
               LogTrade(StringFormat("[MarketStructure] TP VALIDATION FAILED (BUY) | Resistance at %.1f%% to target (need ≥75%%) | Entry: %.5f | Resistance: %.5f | TP: %.5f | TRADE ABORTED",
                        percentToTP, entryPrice, resistance, tpPrice));
               return false;
            }
         }
      }
   }
   else
   {
      // For SELL: Check if any support level between entry and TP
      // SMART RULE: Allow if support is at 75% or more of the way to TP
      for(int i = 0; i < g_SupportsCount; i++)
      {
         double support = g_AllSupports[i];
         if(support < entryPrice && support > tpPrice)
         {
            // Calculate how far support is from entry
            double distanceToSupport = entryPrice - support;
            double percentToTP = (distanceToSupport / distanceToTP) * 100.0;

            if(distanceToSupport >= threshold75Percent)
            {
               // Support is at 75% or more - ALLOW TRADE
               LogTrade(StringFormat("[MarketStructure] TP validation: Support at %.1f%% to target | Entry: %.5f | Support: %.5f | Original TP: %.5f | ALLOWING TRADE",
                        percentToTP, entryPrice, support, tpPrice));
               return true;
            }
            else
            {
               // Support is too close (less than 75%) - REJECT TRADE
               LogTrade(StringFormat("[MarketStructure] TP VALIDATION FAILED (SELL) | Support at %.1f%% to target (need ≥75%%) | Entry: %.5f | Support: %.5f | TP: %.5f | TRADE ABORTED",
                        percentToTP, entryPrice, support, tpPrice));
               return false;
            }
         }
      }
   }

   LogTrade(StringFormat("[MarketStructure] TP validation passed | TP: %.5f clear of structural levels", tpPrice));
   return true;
}

//+------------------------------------------------------------------+
//| Snap SL to nearby H1 level if within ATR range                   |
//| Returns adjusted SL or original if no snap needed                |
//+------------------------------------------------------------------+
double SnapSLToLevel(string symbol, double calculatedSL, double entryPrice, bool isBuy, int atrHandle)
{
   if(!EnableSLSnapping)
      return calculatedSL; // Snapping disabled

   // Get ATR value
   double atrBuffer[];
   ArraySetAsSeries(atrBuffer, true);
   if(CopyBuffer(atrHandle, 0, 0, 1, atrBuffer) <= 0)
   {
      LogTrade("[MarketStructure] ERROR: Failed to copy ATR buffer for SL snapping");
      return calculatedSL;
   }

   double atrValue = atrBuffer[0];
   double point = SymbolInfoDouble(symbol, SYMBOL_POINT);

   if(isBuy)
   {
      // For BUY: Look for support level near calculated SL
      double targetLevel = 0;
      double minDistance = 999999;

      for(int i = 0; i < g_SupportsCount; i++)
      {
         double support = g_AllSupports[i];
         if(support > 0 && support < entryPrice) // Support must be below entry
         {
            double distance = MathAbs(support - calculatedSL);
            if(distance <= atrValue && distance < minDistance)
            {
               minDistance = distance;
               targetLevel = support;
            }
         }
      }

      if(targetLevel > 0)
      {
         // Add 2-pip buffer below support (avoid exact level stop hunts)
         double bufferDistance = 2 * 10 * point;
         double snappedSL = targetLevel - bufferDistance;

         // CRITICAL: Never widen SL beyond calculated SL
         if(snappedSL < calculatedSL)
         {
            LogTrade(StringFormat("[MarketStructure] SL snapped to H1 support (BUY) | Original SL: %.5f | Support: %.5f | Snapped SL: %.5f (-%d pips buffer)",
                     calculatedSL, targetLevel, snappedSL, 2));
            return snappedSL;
         }
         else
         {
            LogTrade(StringFormat("[MarketStructure] SL snap rejected - would widen SL | Original: %.5f | Snapped: %.5f",
                     calculatedSL, snappedSL));
            return calculatedSL;
         }
      }
   }
   else
   {
      // For SELL: Look for resistance level near calculated SL
      double targetLevel = 0;
      double minDistance = 999999;

      for(int i = 0; i < g_ResistancesCount; i++)
      {
         double resistance = g_AllResistances[i];
         if(resistance > 0 && resistance > entryPrice) // Resistance must be above entry
         {
            double distance = MathAbs(resistance - calculatedSL);
            if(distance <= atrValue && distance < minDistance)
            {
               minDistance = distance;
               targetLevel = resistance;
            }
         }
      }

      if(targetLevel > 0)
      {
         // Add 2-pip buffer above resistance
         double bufferDistance = 2 * 10 * point;
         double snappedSL = targetLevel + bufferDistance;

         // CRITICAL: Never widen SL beyond calculated SL
         if(snappedSL > calculatedSL)
         {
            LogTrade(StringFormat("[MarketStructure] SL snapped to H1 resistance (SELL) | Original SL: %.5f | Resistance: %.5f | Snapped SL: %.5f (+%d pips buffer)",
                     calculatedSL, targetLevel, snappedSL, 2));
            return snappedSL;
         }
         else
         {
            LogTrade(StringFormat("[MarketStructure] SL snap rejected - would widen SL | Original: %.5f | Snapped: %.5f",
                     calculatedSL, snappedSL));
            return calculatedSL;
         }
      }
   }

   // No suitable level found for snapping
   LogTrade("[MarketStructure] No suitable H1 level for SL snapping - using calculated SL");
   return calculatedSL;
}

//+------------------------------------------------------------------+
//| Get next resistance level above given price (for TP validation)  |
//+------------------------------------------------------------------+
double GetNextResistanceAbove(double price)
{
   return FindNearestResistanceAbove(price, _Symbol);
}

//+------------------------------------------------------------------+
//| Get next support level below given price (for TP validation)     |
//+------------------------------------------------------------------+
double GetNextSupportBelow(double price)
{
   return FindNearestSupportBelow(price, _Symbol);
}

//+------------------------------------------------------------------+
//| Get statistics on H1 levels for logging/debugging                |
//+------------------------------------------------------------------+
string GetLevelStats()
{
   return StringFormat("H1 Levels | Supports: %d | Resistances: %d | Nearest S: %.5f | Nearest R: %.5f",
                       g_SupportsCount, g_ResistancesCount, g_NearestSupport, g_NearestResistance);
}

//+------------------------------------------------------------------+

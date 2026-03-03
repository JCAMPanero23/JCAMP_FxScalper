//+------------------------------------------------------------------+
//|                                            JC_TradeManager.mqh |
//|                                             JCAMP Trading System |
//|              Order execution, partial profits, RSI divergence     |
//+------------------------------------------------------------------+
#property copyright "JCAMP Trading"
#property link      ""
#property version   "1.00"
#property strict

#include "JC_Utils.mqh"
#include "JC_RiskManager.mqh"

#include <Trade\Trade.mqh>

//+------------------------------------------------------------------+
//| Global trade object                                              |
//+------------------------------------------------------------------+
CTrade g_Trade;

//+------------------------------------------------------------------+
//| Structure to track runner positions                              |
//+------------------------------------------------------------------+
struct RunnerPosition
{
   ulong ticket;
   double entryPrice;
   bool isActive;
   datetime openTime;
   double highestPrice;  // For divergence tracking
   double lowestPrice;
};

RunnerPosition g_Runners[10]; // Support up to 10 concurrent runners
int g_RunnerCount = 0;

//+------------------------------------------------------------------+
//| Initialize trade manager                                          |
//+------------------------------------------------------------------+
void InitializeTradeManager()
{
   // Set trade execution parameters
   g_Trade.SetExpertMagicNumber(123456); // Unique magic number for JCAMP EA
   g_Trade.SetDeviationInPoints(10);     // Allow 1 pip slippage
   g_Trade.SetTypeFilling(ORDER_FILLING_FOK); // Fill or Kill
   g_Trade.SetAsyncMode(false);          // Synchronous execution

   // Initialize runner array
   for(int i = 0; i < ArraySize(g_Runners); i++)
   {
      g_Runners[i].ticket = 0;
      g_Runners[i].entryPrice = 0;
      g_Runners[i].isActive = false;
      g_Runners[i].openTime = 0;
      g_Runners[i].highestPrice = 0;
      g_Runners[i].lowestPrice = 0;
   }

   LogTrade("[TradeManager] Initialized - Ready for order execution");
}

//+------------------------------------------------------------------+
//| Execute BUY order with calculated SL and TP                       |
//+------------------------------------------------------------------+
bool ExecuteBuyOrder(string symbol, double lotSize, double slPrice, double tpPrice)
{
   double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);

   // Validate prices
   if(ask == 0 || slPrice == 0 || tpPrice == 0)
   {
      LogTrade("[TradeManager] ERROR: Invalid price parameters for BUY order");
      return false;
   }

   // Normalize prices to symbol digits
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   slPrice = NormalizeDouble(slPrice, digits);
   tpPrice = NormalizeDouble(tpPrice, digits);

   // Execute market buy
   bool result = g_Trade.Buy(lotSize, symbol, ask, slPrice, tpPrice, "JCAMP BUY");

   if(result)
   {
      ulong ticket = g_Trade.ResultOrder();
      LogTrade(StringFormat("[TradeManager] BUY order executed | Ticket: %d | Lot: %.2f | Entry: %.5f | SL: %.5f | TP: %.5f",
               ticket, lotSize, ask, slPrice, tpPrice));
      return true;
   }
   else
   {
      int errorCode = GetLastError();
      LogTrade(StringFormat("[TradeManager] BUY order FAILED | Error: %d - %s", errorCode, g_Trade.ResultRetcodeDescription()));
      return false;
   }
}

//+------------------------------------------------------------------+
//| Execute SELL order with calculated SL and TP                     |
//+------------------------------------------------------------------+
bool ExecuteSellOrder(string symbol, double lotSize, double slPrice, double tpPrice)
{
   double bid = SymbolInfoDouble(symbol, SYMBOL_BID);

   // Validate prices
   if(bid == 0 || slPrice == 0 || tpPrice == 0)
   {
      LogTrade("[TradeManager] ERROR: Invalid price parameters for SELL order");
      return false;
   }

   // Normalize prices to symbol digits
   int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
   slPrice = NormalizeDouble(slPrice, digits);
   tpPrice = NormalizeDouble(tpPrice, digits);

   // Execute market sell
   bool result = g_Trade.Sell(lotSize, symbol, bid, slPrice, tpPrice, "JCAMP SELL");

   if(result)
   {
      ulong ticket = g_Trade.ResultOrder();
      LogTrade(StringFormat("[TradeManager] SELL order executed | Ticket: %d | Lot: %.2f | Entry: %.5f | SL: %.5f | TP: %.5f",
               ticket, lotSize, bid, slPrice, tpPrice));
      return true;
   }
   else
   {
      int errorCode = GetLastError();
      LogTrade(StringFormat("[TradeManager] SELL order FAILED | Error: %d - %s", errorCode, g_Trade.ResultRetcodeDescription()));
      return false;
   }
}

//+------------------------------------------------------------------+
//| Manage partial profits for open positions                        |
//| Monitors positions that hit TP and executes 80/20 split          |
//+------------------------------------------------------------------+
void ManagePartialProfits(string symbol)
{
   int totalPositions = PositionsTotal();

   for(int i = totalPositions - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket <= 0)
         continue;

      // Check if position belongs to this EA and symbol
      if(PositionGetString(POSITION_SYMBOL) != symbol)
         continue;

      if(PositionGetInteger(POSITION_MAGIC) != 123456)
         continue;

      // Check if position already managed as runner
      if(IsRunnerPosition(ticket))
         continue;

      // Get position details
      double positionOpenPrice = PositionGetDouble(POSITION_PRICE_OPEN);
      double positionTP = PositionGetDouble(POSITION_TP);
      double positionSL = PositionGetDouble(POSITION_SL);
      double positionLot = PositionGetDouble(POSITION_VOLUME);
      long positionType = PositionGetInteger(POSITION_TYPE);

      // Check if price has reached TP (within 2 pips tolerance)
      double currentPrice = (positionType == POSITION_TYPE_BUY) ?
                            SymbolInfoDouble(symbol, SYMBOL_BID) :
                            SymbolInfoDouble(symbol, SYMBOL_ASK);

      double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
      double tpTolerance = 2 * 10 * point; // 2 pips

      bool tpReached = false;
      if(positionType == POSITION_TYPE_BUY)
         tpReached = (currentPrice >= positionTP - tpTolerance);
      else
         tpReached = (currentPrice <= positionTP + tpTolerance);

      if(tpReached)
      {
         // Check if position size allows partials
         if(!CanExecutePartials(positionLot))
         {
            // Close entire position at TP
            LogTrade(StringFormat("[TradeManager] Position too small for partials - closing 100%% | Ticket: %d", ticket));
            g_Trade.PositionClose(ticket);
            continue;
         }

         // Calculate 80% and 20% lot sizes
         double lot80, lot20;
         if(!CalculatePartialSizes(positionLot, lot80, lot20))
         {
            LogTrade(StringFormat("[TradeManager] ERROR: Failed to calculate partial sizes | Ticket: %d", ticket));
            continue;
         }

         // Close 80% at current price
         bool closeResult = g_Trade.PositionClosePartial(ticket, lot80);

         if(closeResult)
         {
            LogTrade(StringFormat("[TradeManager] Partial close 80%% executed | Ticket: %d | Closed: %.2f | Remaining: %.2f",
                     ticket, lot80, lot20));

            // Move SL to break-even + commission on remaining 20%
            double bePrice = CalculateBreakEvenPrice(symbol, positionOpenPrice, lot20, (positionType == POSITION_TYPE_BUY));
            int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
            bePrice = NormalizeDouble(bePrice, digits);

            bool modifyResult = g_Trade.PositionModify(ticket, bePrice, positionTP);

            if(modifyResult)
            {
               LogTrade(StringFormat("[TradeManager] Runner SL moved to BE | Ticket: %d | BE Price: %.5f", ticket, bePrice));

               // Register position as runner for divergence monitoring
               AddRunnerPosition(ticket, positionOpenPrice);
            }
            else
            {
               LogTrade(StringFormat("[TradeManager] WARNING: Failed to modify runner SL | Ticket: %d | Error: %d",
                        ticket, GetLastError()));
            }
         }
         else
         {
            LogTrade(StringFormat("[TradeManager] ERROR: Partial close failed | Ticket: %d | Error: %d - %s",
                     ticket, GetLastError(), g_Trade.ResultRetcodeDescription()));
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Check RSI divergence for runner positions                        |
//+------------------------------------------------------------------+
void CheckRSIDivergence(string symbol, int rsiHandle)
{
   double rsi[];
   ArraySetAsSeries(rsi, true);

   // Copy last 10 RSI values for divergence detection
   if(CopyBuffer(rsiHandle, 0, 0, 10, rsi) <= 0)
   {
      LogTrade("[TradeManager] ERROR: Failed to copy RSI buffer for divergence check");
      return;
   }

   // Copy last 10 price highs/lows
   double high[], low[];
   ArraySetAsSeries(high, true);
   ArraySetAsSeries(low, true);

   if(CopyHigh(symbol, PERIOD_M5, 0, 10, high) <= 0 ||
      CopyLow(symbol, PERIOD_M5, 0, 10, low) <= 0)
   {
      LogTrade("[TradeManager] ERROR: Failed to copy price data for divergence check");
      return;
   }

   // Check each active runner
   for(int i = 0; i < ArraySize(g_Runners); i++)
   {
      if(!g_Runners[i].isActive)
         continue;

      ulong ticket = g_Runners[i].ticket;

      // Verify position still exists
      if(!PositionSelectByTicket(ticket))
      {
         g_Runners[i].isActive = false;
         LogTrade(StringFormat("[TradeManager] Runner position closed externally | Ticket: %d", ticket));
         continue;
      }

      long positionType = PositionGetInteger(POSITION_TYPE);

      // Detect divergence based on position type
      bool divergenceDetected = false;

      if(positionType == POSITION_TYPE_BUY)
      {
         // Bearish divergence: Higher highs in price, lower highs in RSI
         // Find last 2 swing highs in price
         double priceHigh1 = high[0];
         double priceHigh2 = 0;
         double rsiAtHigh1 = rsi[0];
         double rsiAtHigh2 = 0;

         // Simple detection: Compare current high vs high 5 bars ago
         for(int j = 1; j < 10; j++)
         {
            if(high[j] > priceHigh2)
            {
               priceHigh2 = high[j];
               rsiAtHigh2 = rsi[j];
            }
         }

         // Check divergence condition
         if(priceHigh1 > priceHigh2 && rsiAtHigh1 < rsiAtHigh2)
         {
            divergenceDetected = true;
            LogTrade(StringFormat("[TradeManager] Bearish RSI divergence detected | Price: %.5f > %.5f | RSI: %.1f < %.1f",
                     priceHigh1, priceHigh2, rsiAtHigh1, rsiAtHigh2));
         }
      }
      else // POSITION_TYPE_SELL
      {
         // Bullish divergence: Lower lows in price, higher lows in RSI
         double priceLow1 = low[0];
         double priceLow2 = 999999;
         double rsiAtLow1 = rsi[0];
         double rsiAtLow2 = 0;

         // Simple detection: Compare current low vs low 5 bars ago
         for(int j = 1; j < 10; j++)
         {
            if(low[j] < priceLow2)
            {
               priceLow2 = low[j];
               rsiAtLow2 = rsi[j];
            }
         }

         // Check divergence condition
         if(priceLow1 < priceLow2 && rsiAtLow1 > rsiAtLow2)
         {
            divergenceDetected = true;
            LogTrade(StringFormat("[TradeManager] Bullish RSI divergence detected | Price: %.5f < %.5f | RSI: %.1f > %.1f",
                     priceLow1, priceLow2, rsiAtLow1, rsiAtLow2));
         }
      }

      // Close runner if divergence detected
      if(divergenceDetected)
      {
         LogTrade(StringFormat("[TradeManager] Closing runner due to RSI divergence | Ticket: %d", ticket));
         g_Trade.PositionClose(ticket);
         g_Runners[i].isActive = false;
      }
   }
}

//+------------------------------------------------------------------+
//| Add position to runner tracking array                            |
//+------------------------------------------------------------------+
void AddRunnerPosition(ulong ticket, double entryPrice)
{
   // Find empty slot
   for(int i = 0; i < ArraySize(g_Runners); i++)
   {
      if(!g_Runners[i].isActive)
      {
         g_Runners[i].ticket = ticket;
         g_Runners[i].entryPrice = entryPrice;
         g_Runners[i].isActive = true;
         g_Runners[i].openTime = TimeCurrent();
         g_Runners[i].highestPrice = entryPrice;
         g_Runners[i].lowestPrice = entryPrice;

         LogTrade(StringFormat("[TradeManager] Runner registered for divergence monitoring | Ticket: %d | Entry: %.5f",
                  ticket, entryPrice));
         return;
      }
   }

   LogTrade(StringFormat("[TradeManager] WARNING: Runner array full - cannot track position %d", ticket));
}

//+------------------------------------------------------------------+
//| Check if position is already tracked as runner                   |
//+------------------------------------------------------------------+
bool IsRunnerPosition(ulong ticket)
{
   for(int i = 0; i < ArraySize(g_Runners); i++)
   {
      if(g_Runners[i].isActive && g_Runners[i].ticket == ticket)
         return true;
   }
   return false;
}

//+------------------------------------------------------------------+
//| Get count of active positions for symbol                         |
//+------------------------------------------------------------------+
int GetPositionCount(string symbol)
{
   int count = 0;
   int totalPositions = PositionsTotal();

   for(int i = 0; i < totalPositions; i++)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket > 0 && PositionGetString(POSITION_SYMBOL) == symbol)
      {
         if(PositionGetInteger(POSITION_MAGIC) == 123456)
            count++;
      }
   }

   return count;
}

//+------------------------------------------------------------------+
//| Close all positions for symbol (emergency use)                   |
//+------------------------------------------------------------------+
void CloseAllPositions(string symbol)
{
   int totalPositions = PositionsTotal();

   for(int i = totalPositions - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket > 0 && PositionGetString(POSITION_SYMBOL) == symbol)
      {
         if(PositionGetInteger(POSITION_MAGIC) == 123456)
         {
            g_Trade.PositionClose(ticket);
            LogTrade(StringFormat("[TradeManager] Emergency close | Ticket: %d", ticket));
         }
      }
   }
}

//+------------------------------------------------------------------+

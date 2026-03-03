//+------------------------------------------------------------------+
//|                                                    JC_Inputs.mqh |
//|                                             JCAMP Trading System |
//|              Common input parameters - include in all files      |
//+------------------------------------------------------------------+
#property copyright "JCAMP Trading"
#property link      ""
#property version   "1.00"
#property strict

#ifndef __JC_INPUTS_MQH__
#define __JC_INPUTS_MQH__

//+------------------------------------------------------------------+
//| Input Parameters - Risk Management                               |
//+------------------------------------------------------------------+
input group "=== Risk Management ==="
input double RiskPercent = 1.0;              // Risk per trade (1.0-2.0%)
input double MaxDailyLoss = 3.0;             // Max daily loss (%)
input int MaxGlobalPositions = 1;            // Max positions (Phase 1: 1 for EURUSD)

//+------------------------------------------------------------------+
//| Input Parameters - Session Settings                              |
//+------------------------------------------------------------------+
input group "=== Session Settings ==="
input bool TradeLondon = true;               // Trade London session (08:00-16:30 GMT)
input bool TradeNewYork = false;             // Trade New York session (13:00-21:00 GMT)
input bool TradeAsian = false;               // Trade Asian session (23:00-08:00 GMT)
input bool TradeTokyo = false;               // Trade Tokyo session (00:00-09:00 GMT)

//+------------------------------------------------------------------+
//| Input Parameters - Indicators                                    |
//+------------------------------------------------------------------+
input group "=== Indicators ==="
input int SMA1_Period = 21;                  // Fast SMA period
input int SMA2_Period = 50;                  // Medium SMA period
input int SMA3_Period = 200;                 // Slow SMA period
input int RSI_Period = 14;                   // RSI period
input int ATR_Period = 14;                   // ATR period
input double ATR_Multiplier = 1.5;           // ATR multiplier for SL (EURUSD: 1.5)

//+------------------------------------------------------------------+
//| Input Parameters - Filters                                       |
//+------------------------------------------------------------------+
input group "=== Filters ==="
input double MaxSpread = 1.0;                // Max spread in pips
input int LevelProximity = 5;                // H1 level proximity (pips)
input bool EnableTPValidation = true;        // Abort trade if TP crosses H1 level
input bool EnableSLSnapping = true;          // Snap SL to nearby H1 levels

#endif // __JC_INPUTS_MQH__
//+------------------------------------------------------------------+

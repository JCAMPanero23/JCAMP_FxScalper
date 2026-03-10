# JCAMP FxScalper - Backtest Configuration Guide

## ✅ FIXED ISSUES
- **Position sizing bug**: Now calculates correctly for $500 accounts
- **Expected position size**: 0.01 - 0.10 lots (not 50 lots!)

## 🎯 RECOMMENDED BACKTEST SETTINGS FOR $500 ACCOUNT

### Basic Settings
```
Starting Capital: $500 USD
Timeframe: M5 (5-minute)
Symbol: EURUSD
Data Quality: Tick data from server (accurate)
Commission: 6 per lot (realistic)
```

### Date Range Options

**Option 1: Quick Test (1 month)**
- Period: February 2025 - March 2025
- Expected trades: 5-15 trades
- Purpose: Verify bot works correctly

**Option 2: Standard Test (3 months)**
- Period: December 2024 - March 2025
- Expected trades: 20-50 trades
- Purpose: See performance across different conditions

**Option 3: Full Test (6-12 months)**
- Period: Full 2024 or last 12 months
- Expected trades: 50-150 trades
- Purpose: Statistical significance

### Bot Parameters

**For MORE trades (testing purposes):**
```
Risk %: 1.0% (safe for $500)
Max Daily Loss: 3.0%
Max Positions: 1

Sessions:
- Trade London: TRUE ✓
- Trade New York: TRUE ✓ (enables more hours)
- Trade Asian: FALSE

Filters:
- Max Spread: 2.0 pips (increased from 1.0 to allow more trades)
- Enable TP Validation: TRUE
- Enable SL Snapping: TRUE
- H1 Level Proximity: 10 pips (increased from 5 for more trades)

Indicators: (keep defaults)
- SMA1: 21
- SMA2: 50
- SMA3: 200
- RSI: 14
- ATR: 14
- ATR Multiplier: 1.5
```

**For FEWER trades (conservative/realistic):**
```
Sessions:
- Trade London: TRUE ✓
- Trade New York: FALSE
- Trade Asian: FALSE

Filters:
- Max Spread: 1.0 pips (strict)
- H1 Level Proximity: 5 pips (strict)
```

## 🔍 TROUBLESHOOTING: Still Only Getting 1-2 Trades?

### Step 1: Check the Logs
Look in the "Logs" tab during backtest for messages like:
```
[FILTER FAILED] No active session
[FILTER FAILED] Spread too wide
[NO SIGNAL] Bullish conditions not met
[MarketStructure] TP VALIDATION FAILED
```

This tells you WHY trades are being rejected.

### Step 2: Verify Session Times
Make sure your backtest includes London session hours:
- London: 08:00 - 16:00 GMT
- New York: 13:00 - 21:00 GMT
- Overlap: 13:00 - 16:00 GMT (best period)

### Step 3: Check Chart for Signals
The bot needs ALL of these at the same time:
1. ✓ Trend: SMA21 > SMA50 > SMA200 (bullish) OR SMA200 > SMA50 > SMA21 (bearish)
2. ✓ Momentum: RSI > 50 (bullish) OR RSI < 50 (bearish)
3. ✓ Pattern: Engulfing OR Complex 5-bar pattern
4. ✓ Session: Active trading session
5. ✓ Spread: <= max spread setting
6. ✓ H1 Level: TP doesn't cross structural levels

## 📈 EXPECTED POSITION SIZES WITH $500

With proper calculation:
```
Account: $500
Risk: 1% = $5 per trade
SL: 20 pips

Calculation:
- Risk amount: $5
- Pip value for 1 lot EURUSD: ~$10
- Lot size = $5 / (20 pips × $10/pip) = 0.025 lots

Expected range: 0.01 - 0.10 lots (NOT 50 lots!)
```

## 🎯 QUICK BACKTEST PLAN

### Phase 1: Verify Bot Works (30 minutes)
```
Date: Last 3 months
Sessions: London + New York (both enabled)
Max Spread: 2.0 pips
Expected: 15-40 trades
Goal: Make sure bot executes correctly
```

### Phase 2: Realistic Settings (1-2 hours)
```
Date: Last 6-12 months
Sessions: London only
Max Spread: 1.0 pips
Expected: 30-80 trades
Goal: See realistic performance
```

### Phase 3: Optimization (optional)
```
Use cTrader's Optimization tab to test:
- Different ATR multipliers (1.0 - 2.5)
- Different session combinations
- Different spread limits
Goal: Find best parameters for your broker
```

## ⚠️ IMPORTANT NOTES

1. **This is a SELECTIVE strategy** - It's designed for quality over quantity
2. **Low trade frequency is NORMAL** - 2-5 trades per week is expected
3. **Pattern + Trend + Structure** = Very specific entry conditions
4. **Not a scalping machine** - Despite the name, it's more of a precision swing/day trading strategy
5. **$500 account** - You'll trade 0.01-0.05 lots, perfect for learning

## 🚀 NEXT STEPS AFTER BACKTEST

1. Review trade history - check entry/exit logic
2. Check drawdown - should stay within max daily loss
3. Look at win rate - expect 40-60% (with 2:1 R:R this is profitable)
4. Verify partial profits work - 80% closed at 2:1, 20% runs to TP
5. Check logs for filter activity - understand what's blocking trades

---

**If you still only get 1-2 trades**, share:
1. Date range tested
2. Session settings (London/NY/Asian)
3. Screenshots from the Logs tab

Product Requirements Document (PRD)
Project Name: JCAMP_FxScalper 
EAPlatform: MetaTrader 5 (MQL5)
Target Account: FP Markets Raw (ECN/Commission-based)
Starting Equity: $500 USD

1. ObjectiveTo build a fully automated MT5 Expert Advisor that executes a trend-following scalping strategy on the M5 timeframe, using H1 horizontal levels for context, strictly during the London trading session.

2. Operational Constraints & Risk ManagementWith a $500 starting equity, strict risk and exposure parameters are mandatory to prevent ruin. 
Max Open Positions (Global): Strictly limited to 2 active trades at any given time across all pairs.
Max Open Positions (Per Pair): Strictly limited to 1 active trade per currency pair.
Risk Per Trade: Variable input (Default: 1%). On a $500 account, this equals $5 risk.  The EA must auto-calculate lot sizes (often relying on 0.01 micro-lots) based on the dynamic Stop Loss distance.
Spread Filter: Max spread allowed for entry = 1.0 pips.

3. Core Inputs & Parameters
The EA must have these customizable inputs via the MT5 settings menu:
Active Session: London Session (e.g., 08:00 to 16:30 GMT—developer must allow manual time shifts).
Primary Pairs: AUDUSD, EURUSD, GBPUSD.

Indicators:
SMA 1: 21 Periods 
SMA 2: 50 Periods
SMA 3: 200 Periods
RSI: 14 Periods
ATR: 14 Periods
ATR Multiplier: Variable input (Default: 1.5 for AUDUSD, EURUSD, 2.0 for GBPUSD).

4. Market Structure & Execution Logic
The EA operates on two timeframes: H1 (Context) and M5 (Execution).

A. H1 Setup (The Filter)
The EA must identify the nearest H1 Support and Resistance levels (developer can use fractal logic or a defined lookback period to establish these horizontal levels).
Condition: M5 price must be within a defined pip-range (e.g., 5 pips) of the H1 horizontal level to enable execution.

B. M5 Entry Logic (Buy Conditions)
Trend: SMAs must be sequentially aligned upward: 21 > 50 > 200.

Momentum: RSI(14) > 50.

Trigger: * Bullish Engulfing Candle, OR
3 consecutive Bullish candles followed by 1 Bearish candle, immediately followed by a Bullish candle that closes higher than the Bearish candle's open.

Action: Execute BUY at the open of the next M5 candle.
Note: Sell conditions are the exact inverse.

5. Trade Management & ExitsThis section must account for FP Markets' commission structure ($3 per side, per standard lot).
Stop Loss (SL): Dynamic, based on volatility.
Buy SL Formula: SL = Previous Candle Low - (ATR Multiplier X ATR 14 )
Sell SL Formula: SL = Previous Candle High + (ATR Multiplier X ATR 14 )
Target 1 (Partial Profit): $1:2$ Risk-to-Reward ratio.
Action: Close 80% of the position volume. Immediately move the Stop Loss on the remaining 20% to Break-Even + Commission costs.
Target 2 (The Runner): 
Action: Hold the remaining 20% until a bearish RSI Divergence is detected on the M5 chart, or the trailing break-even SL is hit.

6. Developer Edge-Cases to HandleRounding to 0.01: If the risk calculation demands a lot size of 0.014, the EA must round down to 0.01 to protect the $500 equity.
Insufficient Margin: If $5 risk on a very wide stop requires a lot size smaller than the broker's minimum (0.01), the EA must abort the trade.
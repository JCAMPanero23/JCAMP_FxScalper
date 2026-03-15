# M1 Scalping System – Enhanced Architecture Upgrade

## Overview
This document proposes a structural upgrade to the existing M1 scalping system. The goal is to improve:

- Early zone detection
- Entry precision
- Signal quality
- Scalability for EA automation

The improvements maintain the original 6-phase architecture while adding institutional trading concepts such as displacement, liquidity sweeps, and multi‑stage zone validation.

---

# System Architecture (Enhanced)

1. Bar Processing (M1 loop)
2. Higher Timeframe Processing (M15 / H1)
3. Liquidity + Displacement Detection
4. FVG Engine
5. Swing Scoring Engine
6. Zone Lifecycle Engine
7. Entry Engine
8. Trade Execution Engine

---

# Phase 1 — Bar Processing (Every M1 Bar)

Responsibilities:

- Update current M1 candle
- Detect new M15 candle
- Maintain session state
- Update rectangle proximity checks

Key checks:

- Spread filter
- Max trades filter
- Active trade management

---

# Phase 2 — Higher Timeframe Processing (Every M15 Bar)

Components:

## Market Structure Engine

Tracks:

- Higher High
- Higher Low
- Lower High
- Lower Low

Structure is determined using swing pivots.

Outputs:

- Trend Bias
- Key liquidity levels

---

## Trend Mode Detection

Trend filter using:

SMA(200)

Modes:

- Bullish Bias
- Bearish Bias
- Neutral Range

---

## Session Tracking Engine

Sessions detected:

- Asian
- London
- New York
- London–NY Overlap

Outputs:

- Session High
- Session Low
- Session Range

These become **liquidity targets**.

---

# Phase 3 — Liquidity Sweep Detection

Before strong moves, markets frequently sweep liquidity.

Types detected:

1. Session High Sweep
2. Session Low Sweep
3. Previous Swing Sweep

Rules:

Bullish Sweep

Price wicks below a previous low
Then closes back above

Bearish Sweep

Price wicks above a previous high
Then closes back below

Sweep events increase probability score.

---

# Phase 4 — Displacement Detection

Displacement identifies institutional momentum.

Conditions:

Impulse Candle Size >= 1.8 × Average Candle Size

OR

Impulse Candle Size >= 1.5 × ATR(14)

Outputs:

- Displacement flag
- Impulse origin price

The impulse origin becomes a **candidate zone anchor**.

---

# Phase 5 — FVG Engine

Fair Value Gap detection uses a standard 3‑candle pattern.

Bullish FVG:

Candle1 High < Candle3 Low

Bearish FVG:

Candle1 Low > Candle3 High

Filters:

Minimum gap size

>= 1.5 pips

Age limit

<= 30 candles

Only FVGs formed during displacement are considered **high quality**.

---

# Phase 6 — Swing Scoring Engine (Existing System)

Your existing scoring engine remains largely unchanged.

## Components

Validity (20%)
Time since swing formed

Extremity (25%)
Distance from SMA200

Fractal Strength (15%)
Quality of swing pattern

Session Alignment (20%)
Distance from session high/low

FVG Alignment (15%)
Proximity to imbalance

Candle Quality (5%)
Body/wick ratio

Scores determine **zone priority**.

---

# Phase 7 — Zone Lifecycle Engine

This replaces the old rectangle creation logic.

## Three Stage Zone Model

Stage 1 — Pre‑Zone

Created immediately after displacement + FVG.

Purpose:

Catch early retracements.

State:

PRE

---

Stage 2 — Confirmed Zone

Activated once swing confirmation occurs.

State:

VALID

---

Stage 3 — Armed Zone

Triggered when price approaches zone.

State:

ARMED

---

Zone width rule:

ZoneWidth = max(4 pips , 0.25 × impulse candle size)

---

Zone expiry:

Expiry = 2 × timeframe

For M15 zones:

120 minutes

---

# Phase 8 — Entry Engine

Two modes remain available.

## Breakout Mode

Entry when M1 candle body closes beyond rectangle.

## Retest Mode

Steps:

1 Breakout

2 Retest of zone

3 Rejection candle

Entry executed.

---

# Advanced Entry Model (Recommended)

For highest precision:

1 Price enters zone

2 M1 displacement candle forms

3 M1 FVG appears

4 Entry placed at 50% of FVG

Formula:

Entry = (FVG_high + FVG_low) / 2

Stop loss:

Below FVG low or zone boundary.

---

# Trade Execution Engine

Risk model:

Default risk

1% per trade

Dynamic SL calculation:

SL = zone boundary + buffer

TP calculation hierarchy:

1 Nearest liquidity

2 M15 structure level

3 H1 structure level

---

# Additional Filters

## Spread Filter

Spread <= 1.2 pips

---

## Time Filter

Preferred sessions:

London

New York

Overlap

Asian session optional.

---

## Maximum Zone Distance

Entry allowed only if price is within:

<= 30% of target distance

---

# Suggested Data Structures

Zone Object

Properties:

ID

State

Origin Price

Upper Boundary

Lower Boundary

Expiry Time

Score

FVG Reference

Sweep Reference

---

# Logging System

Recommended logs:

Zone Created

Zone Armed

Entry Triggered

Trade Result

Each trade should record:

Session

Zone Score

FVG Size

Displacement Strength

Result in R

---

# Expected Improvements

With these upgrades:

Earlier zone creation

Higher entry precision

Better filtering of weak signals

Improved risk‑to‑reward

Better compatibility with automated trading.

---

# Future Enhancements

Possible additional modules:

Liquidity Map Engine

Multi‑pair scanning

Adaptive volatility filters

Machine learning score optimizer

Portfolio‑level risk manager

---

End of Document


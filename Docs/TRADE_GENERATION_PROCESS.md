# Trade Generation Process - Complete Flow

**Bot**: Jcamp 1M Scalping with M15 Swing Detection
**Version**: Phase 3 (All Features Implemented)
**Last Updated**: 2026-03-12

---

## High-Level Overview

```
M1 Bar Close → M15 Detection → Swing Scoring → Rectangle Drawing → Entry Detection → Trade Execution
     ↓              ↓                ↓                  ↓                  ↓              ↓
  Every bar    On new M15      Score 6 factors    Draw zone on chart   M1 breakout   Dynamic sizing
```

---

## Detailed Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                    EVERY M1 BAR CLOSE (OnBar)                       │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
                    ┌────────────────────┐
                    │ Check M15 Bar Data │
                    │ (Need SMA 200 + 5) │
                    └────────┬───────────┘
                             │
                             ▼
                    ┌────────────────────┐
                    │ Is NEW M15 Bar?    │
                    └────┬────────┬──────┘
                         │ YES    │ NO
                         │        │
                         │        └──────────┐
                         ▼                   │
    ┌────────────────────────────────────┐   │
    │    M15 BAR PROCESSING              │   │
    │                                    │   │
    │ 1. Update H1 Levels                │   │
    │ 2. Update M15 Levels               │   │
    │ 3. Update Session Tracking         │   │
    │    (Phase 2)                       │   │
    │ 4. Detect Fair Value Gaps (FVGs)   │   │
    │    (Phase 3)                       │   │
    │ 5. Detect Trend Mode               │   │
    │    (BUY/SELL via SMA 200)          │   │
    │ 6. Find Significant Swing          │   │
    │    (Multi-Factor Scoring)          │   │
    │ 7. Update Swing Rectangle          │   │
    └────────────────┬───────────────────┘   │
                     │                       │
                     ▼                       │
    ┌────────────────────────────────────┐   │
    │   SWING DETECTION & SCORING        │   │
    │                                    │   │
    │ Step 1: Find Williams Fractals    │   │
    │ └─ Look back 20 M15 bars           │   │
    │ └─ Mode-specific (BUY/SELL)        │   │
    │                                    │   │
    │ Step 2: Score Each Swing           │   │
    │ ┌─────────────────────────────┐    │   │
    │ │ 1. Validity (20%)           │    │   │
    │ │    - Time since formed      │    │   │
    │ │    - Recency bonus          │    │   │
    │ │                             │    │   │
    │ │ 2. Extremity (25%)          │    │   │
    │ │    - Distance from SMA 200  │    │   │
    │ │    - How "extreme" the swing│    │   │
    │ │                             │    │   │
    │ │ 3. Fractal Strength (15%)   │    │   │
    │ │    - Pattern quality        │    │   │
    │ │    - Bar configuration      │    │   │
    │ │                             │    │   │
    │ │ 4. Session Alignment (20%)  │    │   │
    │ │    - At session high/low?   │    │   │
    │ │    - 10 pip tolerance       │    │   │
    │ │                             │    │   │
    │ │ 5. FVG Alignment (15%)      │    │   │
    │ │    - In FVG zone?           │    │   │
    │ │    - Near FVG (5 pips)?     │    │   │
    │ │                             │    │   │
    │ │ 6. Candle Quality (5%)      │    │   │
    │ │    - Body/wick ratio        │    │   │
    │ │    - Rejection quality      │    │   │
    │ └─────────────────────────────┘    │   │
    │                                    │   │
    │ Step 3: Select Best Swing          │   │
    │ └─ Highest score ≥ threshold       │   │
    │ └─ Default: 0.60 minimum           │   │
    │                                    │   │
    │ Step 4: Draw Rectangle Zone        │   │
    │ └─ Check proximity to price        │   │
    │ └─ Set 60-minute expiry            │   │
    │ └─ Arm for trading if close enough │   │
    └────────────────┬───────────────────┘   │
                     │                       │
                     └───────┬───────────────┘
                             │
                             ▼
                    ┌────────────────────┐
                    │ Entry Detection    │
                    │ (EVERY M1 Bar)     │
                    └────────┬───────────┘
                             │
                             ▼
                    ┌────────────────────┐
                    │ Trading Enabled?   │
                    └────┬────────┬──────┘
                         │ YES    │ NO → Exit
                         │        │
                         ▼        │
                ┌──────────────────────┐ │
                │ Check Rectangle      │ │
                │ Status               │ │
                └────┬────────┬────────┘ │
                     │        │          │
          ┌──────────┘        └──────────┤
          │                              │
Has Valid Rectangle?              Has Active Swing?
But NOT Armed?                    (Armed for trading)
          │                              │
          ▼                              ▼
┌─────────────────────┐      ┌──────────────────────────┐
│ TryRearmRectangle   │      │  ENTRY LOGIC PROCESSING  │
│                     │      │                          │
│ - Check expiry      │      │ Check expiry first       │
│ - Check proximity   │      │ └─ 60 min from drawing   │
│ - Rearm if price    │      │                          │
│   returned          │      │ Get M1 closed candle     │
└─────────────────────┘      │ └─ Last completed bar    │
                             │                          │
                             │ Entry Mode?              │
                             └──────┬──────┬────────────┘
                                    │      │
                         ┌──────────┘      └──────────┐
                         │                            │
                    BREAKOUT                       RETEST
                    (Default)                   (Alternative)
                         │                            │
                         ▼                            ▼
         ┌───────────────────────────┐    ┌──────────────────────────┐
         │ BREAKOUT ENTRY LOGIC      │    │ RETEST ENTRY LOGIC       │
         │                           │    │                          │
         │ SELL Mode:                │    │ Phase 1: Detect Breakout │
         │ ├─ Body closes BELOW      │    │ └─ Body below rectangle  │
         │ │  rectangle bottom       │    │                          │
         │ ├─ Had interaction with   │    │ Phase 2: Wait for Retest │
         │ │  rectangle (high touch) │    │ └─ Price returns         │
         │ ├─ Bearish candle         │    │                          │
         │ │  (close < open)         │    │ Phase 3: Rejection       │
         │ └─ Execute SELL           │    │ └─ Bearish rejection     │
         │                           │    │ └─ Execute SELL          │
         │ BUY Mode:                 │    │                          │
         │ ├─ Body closes ABOVE      │    │ (Similar for BUY mode)   │
         │ │  rectangle top          │    │                          │
         │ ├─ Had interaction with   │    │                          │
         │ │  rectangle (low touch)  │    │                          │
         │ ├─ Bullish candle         │    │                          │
         │ │  (close > open)         │    │                          │
         │ └─ Execute BUY            │    │                          │
         │                           │    │                          │
         │ Invalidation:             │    │                          │
         │ └─ Body closes opposite   │    │                          │
         │    direction              │    │                          │
         └───────────┬───────────────┘    └────────────┬─────────────┘
                     │                                 │
                     └────────────┬────────────────────┘
                                  │
                                  ▼
                      ┌────────────────────────┐
                      │  TRADE EXECUTION       │
                      │                        │
                      │ Calculate SL:          │
                      │ SELL: Top + buffer     │
                      │ BUY: Bottom - buffer   │
                      │                        │
                      │ Calculate Risk (pips): │
                      │ Distance to SL         │
                      │                        │
                      │ Calculate Position Size│
                      │ └─ Risk % * Balance    │
                      │ └─ / Risk in pips      │
                      │                        │
                      │ Calculate TP:          │
                      │ ├─ Initial: 3R minimum │
                      │ └─ Adjusted for:       │
                      │    ├─ H1 levels        │
                      │    └─ M15 levels       │
                      │                        │
                      │ Execute Market Order   │
                      │ └─ With SL & TP        │
                      │                        │
                      │ If Trade on New Swing: │
                      │ └─ Disable rectangle   │
                      └────────────────────────┘
```

---

## Detailed Breakdown by Phase

### Phase 1: Bar Processing

**Trigger**: Every M1 bar close

**Actions**:
1. Check if enough M15 data (SMA 200 + 5 bars)
2. Update mode display label
3. Detect if NEW M15 bar formed

---

### Phase 2: M15 Bar Processing (New Bar Only)

**Trigger**: New M15 bar detected

**Actions**:
1. **Update Market Structure**:
   - Detect H1 swing highs/lows (Phase 1C)
   - Detect M15 swing highs/lows (Phase 1C)

2. **Update Session Tracking** (Phase 2):
   - Track Asian/London/NY/Overlap sessions
   - Update session highs/lows
   - **Draw live session boxes** (NEW!)
   - Calculate session alignment scores

3. **Detect Fair Value Gaps** (Phase 3):
   - Scan last 50 M15 bars for 3-candle gaps
   - Identify Bullish FVGs (A.High < C.Low)
   - Identify Bearish FVGs (A.Low > C.High)
   - Track which FVGs are filled
   - Calculate FVG alignment scores

4. **Detect Trend Mode**:
   - Compare M15 close to SMA 200
   - Above = BUY mode
   - Below = SELL mode

5. **Find Significant Swing**:
   - Scan last 20 M15 bars for Williams Fractals
   - Score each fractal (6 components)
   - Select highest scoring swing ≥ 0.60

6. **Update Rectangle**:
   - Draw/update swing rectangle zone
   - Check proximity to current price
   - Arm for trading if close enough
   - Set 60-minute expiry

---

### Phase 3: Swing Scoring System

**6-Component Scoring (Total = 1.0)**:

#### 1. Validity Score (Weight: 20%)
```
Formula: Based on time since swing formed
- Recently formed (0-5 bars): 1.0
- Medium age (6-10 bars): 0.8
- Older (11-15 bars): 0.6
- Very old (16-20 bars): 0.4

Multiplied by weight: Score × 0.20
```

#### 2. Extremity Score (Weight: 25%)
```
Formula: Distance from SMA 200
- Very far (50+ pips): 1.0
- Far (30-50 pips): 0.8
- Medium (20-30 pips): 0.6
- Close (10-20 pips): 0.4
- Too close (<10 pips): 0.3

Multiplied by weight: Score × 0.25
```

#### 3. Fractal Strength (Weight: 15%)
```
Formula: Pattern quality
- Clear 5-bar fractal: 1.0
- 4-bar pattern: 0.8
- 3-bar pattern: 0.6
- Weak pattern: 0.4

Multiplied by weight: Score × 0.15
```

#### 4. Session Alignment (Weight: 20%)
```
Formula: Distance to session high/low
- AT session level (±0 pips): 1.0
- Near session level (±5 pips): 0.85
- Close to session level (±10 pips): 0.7
- Not aligned: 0.5

Multiplied by weight: Score × 0.20
```

#### 5. FVG Alignment (Weight: 15%)
```
Formula: Position relative to FVG zones
- INSIDE FVG zone: 1.0
- NEAR FVG (within 5 pips): 0.7
- Far from FVG: 0.3

Multiplied by weight: Score × 0.15
```

#### 6. Candle Quality (Weight: 5%)
```
Formula: Rejection candle quality
- Strong rejection (70%+ body): 1.0
- Good rejection (50-70% body): 0.8
- Medium rejection (30-50%): 0.6
- Weak (pin bar, <30% body): 0.4

Multiplied by weight: Score × 0.05
```

**Total Score Calculation**:
```
Total = (Validity × 0.20) +
        (Extremity × 0.25) +
        (Fractal × 0.15) +
        (Session × 0.20) +
        (FVG × 0.15) +
        (Candle × 0.05)

Must be ≥ 0.60 (default) to qualify for trading
```

---

### Phase 4: Rectangle Management

**When Swing Found**:
1. Calculate rectangle zone:
   - **SELL**: High + 2 pips to High + 6 pips
   - **BUY**: Low - 6 pips to Low - 2 pips

2. Check proximity to current price:
   - **SELL**: Price should be above rectangle (max 50 pips)
   - **BUY**: Price should be below rectangle (max 50 pips)

3. Set status:
   - `hasValidRectangle = true` (rectangle exists)
   - `hasActiveSwing = true/false` (armed based on proximity)

4. Set expiry:
   - 60 minutes from drawing time
   - Rectangle invalidates after expiry

**Rectangle States**:
```
Valid but Unarmed:
├─ Rectangle drawn but price too far away
├─ Wait for price to return
└─ Check every M1 bar (TryRearmRectangle)

Active (Armed):
├─ Rectangle drawn and price is close
├─ Ready for breakout detection
└─ Monitor every M1 bar

Expired:
├─ 60 minutes passed since drawing
└─ Disable and wait for new swing

Invalidated:
├─ Wrong-direction breakout occurred
└─ Disable and wait for new swing
```

---

### Phase 5: Entry Detection (Every M1 Bar)

**Mode: Breakout (Default)**

**SELL Entry Requirements**:
- ✅ M1 candle body closes BELOW rectangle bottom
- ✅ Candle had interaction with rectangle (high touched it)
- ✅ Bearish candle (close < open)
- ❌ Invalidate if body closes ABOVE rectangle

**BUY Entry Requirements**:
- ✅ M1 candle body closes ABOVE rectangle top
- ✅ Candle had interaction with rectangle (low touched it)
- ✅ Bullish candle (close > open)
- ❌ Invalidate if body closes BELOW rectangle

**Mode: Retest (Alternative)**

**Phase 1 - Detect Breakout**:
- Body closes beyond rectangle
- Flag breakout occurred
- Store breakout price

**Phase 2 - Wait for Retest**:
- Price returns to rectangle level
- Wick touches breakout level (±2 pips)

**Phase 3 - Confirm Rejection**:
- Rejection candle forms (bearish for SELL, bullish for BUY)
- Body closes away from retest level
- Execute trade

---

### Phase 6: Trade Execution

**Step 1: Calculate Stop Loss**
```
SELL: SL = Rectangle Top + Buffer (default 5 pips)
BUY:  SL = Rectangle Bottom - Buffer (default 5 pips)
```

**Step 2: Calculate Risk**
```
Risk (pips) = |Entry - SL| / PipSize
```

**Step 3: Calculate Position Size**
```
Risk Amount = Account Balance × (Risk % / 100)
              Default: 1% of balance

Position Size = Risk Amount / (Risk Pips × Pip Value)

Constraints:
├─ Minimum: 0.01 lots
├─ Maximum: Based on broker limits
└─ Round to broker's step size
```

**Step 4: Calculate Take Profit**
```
Initial TP = Entry ± (Risk × MinRR × PipSize)
             Default MinRR: 3.0 (3R minimum)

Adjusted TP = AdjustTPForMarketStructure()
├─ Check H1 levels first (priority)
├─ Check M15 levels if no H1
├─ Extend to level if beyond 3R
└─ Keep 3R minimum if no levels found

Actual RR = Reward Pips / Risk Pips
```

**Step 5: Execute Order**
```
ExecuteMarketOrder(
    Direction: Buy/Sell,
    Symbol: EURUSD,
    Volume: Calculated lots,
    SL: In pips from entry,
    TP: In pips from entry
)
```

**Step 6: Post-Execution**
```
If Trade on New Swing Only:
├─ hasActiveSwing = false
├─ hasValidRectangle = false
└─ Wait for new M15 swing

Else:
└─ Rectangle stays active for multiple trades
```

---

## Key Parameters

### Swing Detection
```
Swing Lookback Bars: 20 M15 bars
Minimum Swing Score: 0.60 (adjustable)
```

### Score Weights
```
Validity:  0.20 (20%)
Extremity: 0.25 (25%)
Fractal:   0.15 (15%)
Session:   0.20 (20%)
FVG:       0.15 (15%)
Candle:    0.05 (5%)
Total:     1.00 (100%)
```

### Rectangle Settings
```
SELL Rectangle:
├─ Top: Swing High + 6 pips
└─ Bottom: Swing High + 2 pips

BUY Rectangle:
├─ Top: Swing Low - 2 pips
└─ Bottom: Swing Low - 6 pips

Expiry: 60 minutes from drawing
Max Distance to Arm: 50 pips
```

### Trade Management
```
Risk Per Trade: 1.0% (adjustable)
Minimum RR Ratio: 3.0 (adjustable)
SL Buffer: 5 pips (adjustable)
Max Positions: 1 (adjustable)
Trade on New Swing Only: TRUE (adjustable)
```

### Entry Modes
```
Breakout (Default):
└─ Body closes beyond rectangle

Retest (Alternative):
├─ Detect breakout
├─ Wait for retest
└─ Enter on rejection
```

---

## Flow Timing

**M15 Processing**: Every 15 minutes
- Swing detection
- Rectangle drawing
- Market structure updates
- Session tracking
- FVG detection

**M1 Processing**: Every 1 minute
- Entry detection
- Rectangle rearming checks
- Mode display updates

**Trade Execution**: Immediate
- As soon as entry conditions met
- No delay or confirmation needed

---

## Success Conditions

**For a Trade to Execute**:
1. ✅ Trading enabled
2. ✅ Valid swing found (score ≥ 0.60)
3. ✅ Rectangle drawn and active
4. ✅ Rectangle not expired (<60 minutes)
5. ✅ Entry conditions met (breakout or retest)
6. ✅ Below max position limit
7. ✅ Position size valid (not too small)
8. ✅ TP/SL calculations successful

**If Any Step Fails**:
- Log reason for rejection
- Continue monitoring
- Wait for next opportunity

---

## Example Trade Flow

**Timeline**:
```
13:00 - M15 bar closes
      └─ SELL mode detected (price below SMA)
      └─ Fractal found at 13:00
      └─ Score: 0.82 (excellent!)
          ├─ Validity: 1.0 × 0.20 = 0.20
          ├─ Extremity: 0.9 × 0.25 = 0.23
          ├─ Fractal: 0.8 × 0.15 = 0.12
          ├─ Session: 1.0 × 0.20 = 0.20 (at London high!)
          ├─ FVG: 1.0 × 0.15 = 0.15 (in FVG zone!)
          └─ Candle: 0.8 × 0.05 = 0.04
      └─ Rectangle drawn: 1.10500 - 1.10540
      └─ Price at 1.10600 (60 pips away)
      └─ Status: Valid but UNARMED (too far)

13:05 - M1 bar closes
      └─ Price at 1.10580
      └─ TryRearmRectangle() checks proximity
      └─ Still too far (40 pips)

13:12 - M1 bar closes
      └─ Price at 1.10555
      └─ TryRearmRectangle() checks proximity
      └─ Distance: 15 pips ✅
      └─ Status: ARMED for trading

13:14 - M1 bar closes
      └─ Bearish breakout candle
      └─ Close: 1.10480 (below 1.10500) ✅
      └─ High: 1.10520 (touched rectangle) ✅
      └─ Bearish: Close < Open ✅

      └─ EXECUTE SELL TRADE:
          ├─ Entry: 1.10480
          ├─ SL: 1.10590 (top + 5 pips)
          ├─ Risk: 11.0 pips
          ├─ Position: 0.18 lots (1% risk)
          ├─ Initial TP: 1.10150 (3R = 33 pips)
          ├─ Adjusted TP: 1.10100 (H1 level at 1.10100)
          ├─ Final RR: 1:3.5
          └─ ✅ Trade executed successfully!

13:15 - Post-execution
      └─ hasActiveSwing = false (Trade on New Swing = TRUE)
      └─ Wait for next M15 swing
```

---

## Summary Statistics

**Processing Frequency**:
- M15 Analysis: Every 15 minutes
- M1 Entry Check: Every 1 minute
- Trade Execution: Instant (when triggered)

**Typical Timings**:
- Swing Detection: <1 second
- Scoring Calculation: <1 second
- Rectangle Drawing: Instant
- Entry Detection: <1 second
- Trade Execution: 1-2 seconds

**Expected Trade Frequency**:
- Good swings: 2-5 per day
- Qualified entries: 1-3 per day
- Executed trades: 0-2 per day (depends on breakouts)

---

## Next Steps

1. **Phase 3 Validation**: Test FVG detection (see `docs/FVG_VALIDATION_GUIDE.md`)
2. **Optimization**: Fine-tune score weights based on backtest results
3. **Multi-Pair Testing**: Test on GBPUSD, USDJPY, etc.
4. **Live Testing**: Forward test on demo account

---

**Last Updated**: 2026-03-12
**All Phases**: 1A, 1B, 1C, 2, 3 ✅ Complete

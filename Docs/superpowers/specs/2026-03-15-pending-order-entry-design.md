# Pending Order Entry System Design Specification

## Goal
Replace market order execution with pending STOP orders to reduce entry slippage and dramatically decrease SL size from ~9 pips to ~4-5 pips.

## Architecture
Add optional pending order entry mode to existing breakout system. Maintain backward compatibility with current market order approach through parameter selection.

## Tech Stack
- cTrader cAlgo C# API
- Existing Jcamp_1M_scalping bot framework
- PlaceStopOrder/PlaceStopLimitOrder API methods
- PendingOrder tracking and management

---

## Problem Statement

### Current System Issues:
1. **Breakout slippage**: Market orders execute 2-3.4 pips away from zone boundary
2. **Large SL**: Entry distance + zone width + buffer = 6.2-9.4 pips
3. **Distant TP**: Large SL requires proportionally far TP (24-29 pips for 2.5-3.1 RR)
4. **Lower win rate**: Distant TPs have lower probability of being hit

### Example Trade (Current System):
```
Zone: 1.10363 - 1.10403 (4 pips wide)
Breakout at: 01:06 → Market order @ 1.10436 (3.3 pips slippage)
SL: 1.10343 (2 pips below zone)
Total SL: 9.3 pips
TP: 1.10724 (28.8 pips for 3.1 RR)
```

---

## Proposed Solution

### Pending STOP Order Entry

**When zone becomes ARMED:**
1. Calculate entry price at zone boundary (+ small offset)
2. Place pending STOP order at that price
3. Set order expiry based on zone lifecycle
4. If price breaks through → Order executes near zone boundary
5. If zone expires → Order auto-cancels

### Expected Outcome:
```
Zone: 1.10363 - 1.10403 (4 pips wide)
Pending BUY STOP at: 1.10405 (2 pips above zone)
If triggered: Entry fills @ ~1.10405 (minimal slippage)
SL: 1.10361 (2 pips below zone)
Total SL: 4.4 pips (53% reduction!)
TP: 1.10541 (13.6 pips for 3.1 RR - 53% closer!)
```

---

## Functional Requirements

### 1. Entry Execution Mode Parameter

```csharp
public enum EntryExecutionMode
{
    Market,         // Current: Immediate market order on breakout
    PendingStop,    // New: Pending STOP order at zone boundary
    PendingLimit    // Future: Limit order for retest entries
}

[Parameter("Entry Execution Mode", DefaultValue = EntryExecutionMode.Market, Group = "Entry System")]
public EntryExecutionMode EntryExecution { get; set; }
```

### 2. Entry Offset Parameter

```csharp
[Parameter("Pending Entry Offset (pips)", DefaultValue = 2.0, MinValue = 0.5, MaxValue = 5.0, Step = 0.5, Group = "Entry System")]
public double PendingEntryOffsetPips { get; set; }
```

**Purpose:** Distance from zone boundary to place pending order
- **BUY:** Entry = zoneTop + offset
- **SELL:** Entry = zoneBottom - offset

### 3. Pending Order Expiry Parameter

```csharp
[Parameter("Pending Order Expiry (minutes)", DefaultValue = 60, MinValue = 30, MaxValue = 240, Step = 30, Group = "Entry System")]
public int PendingOrderExpiryMinutes { get; set; }
```

**Purpose:** Auto-cancel pending orders after N minutes if not filled

---

## Technical Design

### Data Structures

#### Pending Order Tracking
```csharp
private class ZonePendingOrder
{
    public string ZoneId { get; set; }
    public PendingOrder Order { get; set; }
    public DateTime PlacedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public double EntryPrice { get; set; }
    public double StopLoss { get; set; }
    public double TakeProfit { get; set; }
}

private Dictionary<string, ZonePendingOrder> _zonePendingOrders = new Dictionary<string, ZonePendingOrder>();
```

### Core Logic Flow

#### 1. Zone ARMED → Place Pending Order

**Trigger:** When zone transitions to ARMED state

**BUY Setup:**
```csharp
private void PlaceBuyPendingOrder(Zone zone)
{
    // Calculate entry at zone boundary + offset
    double entryPrice = zone.TopPrice + (PendingEntryOffsetPips * Symbol.PipSize);

    // SL at zone bottom - buffer
    double slPrice = zone.BottomPrice - (SLBufferPips * Symbol.PipSize);
    double slPips = (entryPrice - slPrice) / Symbol.PipSize;

    // Calculate TP using existing TP logic
    double tpPrice = CalculateTP(TradeType.Buy, entryPrice, slPips);
    double tpPips = (tpPrice - entryPrice) / Symbol.PipSize;

    // Calculate position size
    double volume = CalculatePositionSize(slPips);

    // Place pending STOP order
    DateTime expiry = Server.Time.AddMinutes(PendingOrderExpiryMinutes);
    var result = PlaceStopOrder(
        tradeType: TradeType.Buy,
        symbolName: Symbol.Name,
        volume: volume,
        targetPrice: entryPrice,
        label: $"BUY_ZONE_{zone.Id}",
        stopLossPips: slPips,
        takeProfitPips: tpPips,
        expiration: expiry
    );

    if (result.IsSuccessful)
    {
        _zonePendingOrders[zone.Id] = new ZonePendingOrder
        {
            ZoneId = zone.Id,
            Order = result.PendingOrder,
            PlacedAt = Server.Time,
            ExpiresAt = expiry,
            EntryPrice = entryPrice,
            StopLoss = slPrice,
            TakeProfit = tpPrice
        };

        Print($"[PENDING BUY] Zone {zone.Id} | Entry: {entryPrice} | SL: {slPrice} ({slPips:F1} pips) | TP: {tpPrice} ({tpPips:F1} pips)");
    }
    else
    {
        Print($"[ERROR] Failed to place pending BUY order: {result.Error}");
    }
}
```

**SELL Setup:**
```csharp
private void PlaceSellPendingOrder(Zone zone)
{
    // Calculate entry at zone boundary - offset
    double entryPrice = zone.BottomPrice - (PendingEntryOffsetPips * Symbol.PipSize);

    // SL at zone top + buffer
    double slPrice = zone.TopPrice + (SLBufferPips * Symbol.PipSize);
    double slPips = (slPrice - entryPrice) / Symbol.PipSize;

    // Calculate TP using existing TP logic
    double tpPrice = CalculateTP(TradeType.Sell, entryPrice, slPips);
    double tpPips = (entryPrice - tpPrice) / Symbol.PipSize;

    // Calculate position size
    double volume = CalculatePositionSize(slPips);

    // Place pending STOP order
    DateTime expiry = Server.Time.AddMinutes(PendingOrderExpiryMinutes);
    var result = PlaceStopOrder(
        tradeType: TradeType.Sell,
        symbolName: Symbol.Name,
        volume: volume,
        targetPrice: entryPrice,
        label: $"SELL_ZONE_{zone.Id}",
        stopLossPips: slPips,
        takeProfitPips: tpPips,
        expiration: expiry
    );

    if (result.IsSuccessful)
    {
        _zonePendingOrders[zone.Id] = new ZonePendingOrder
        {
            ZoneId = zone.Id,
            Order = result.PendingOrder,
            PlacedAt = Server.Time,
            ExpiresAt = expiry,
            EntryPrice = entryPrice,
            StopLoss = slPrice,
            TakeProfit = tpPrice
        };

        Print($"[PENDING SELL] Zone {zone.Id} | Entry: {entryPrice} | SL: {slPrice} ({slPips:F1} pips) | TP: {tpPrice} ({tpPips:F1} pips)");
    }
    else
    {
        Print($"[ERROR] Failed to place pending SELL order: {result.Error}");
    }
}
```

#### 2. Order Management

**Cancel Pending Order (Multiple Scenarios):**

```csharp
private void CancelZonePendingOrder(string zoneId, string reason)
{
    if (_zonePendingOrders.TryGetValue(zoneId, out var pendingOrder))
    {
        if (pendingOrder.Order != null && pendingOrder.Order.IsActive)
        {
            var result = CancelPendingOrder(pendingOrder.Order);
            if (result.IsSuccessful)
            {
                Print($"[PENDING CANCEL] Zone {zoneId} | Reason: {reason}");
            }
        }
        _zonePendingOrders.Remove(zoneId);
    }
}
```

**Cancellation Triggers:**

1. **Zone Expires:**
```csharp
// In zone cleanup logic
if (zone.IsExpired)
{
    CancelZonePendingOrder(zone.Id, "Zone expired");
}
```

2. **Higher-Scored Zone Replaces Existing:**
```csharp
// When new zone is created with higher score
if (newZone.Score > existingZone.Score)
{
    CancelZonePendingOrder(existingZone.Id, "Replaced by higher-scored zone");
    PlacePendingOrder(newZone);
}
```

3. **Order Expiry Reached:**
```csharp
// Check on each bar
private void CheckExpiredPendingOrders()
{
    var expired = _zonePendingOrders.Values
        .Where(x => Server.Time >= x.ExpiresAt && x.Order.IsActive)
        .ToList();

    foreach (var pendingOrder in expired)
    {
        CancelZonePendingOrder(pendingOrder.ZoneId, "Order expiry reached");
    }
}
```

4. **Max Positions Reached:**
```csharp
// Before placing new pending order
if (Positions.Count >= MaxPositions)
{
    Print("[PENDING ORDER] Skipped - max positions reached");
    return;
}
```

#### 3. Order Filled Handling

**Detect when pending order becomes position:**

```csharp
protected override void OnPositionOpened(PositionOpenedEventArgs args)
{
    base.OnPositionOpened(args);

    // Check if this was from a pending order
    var pendingOrder = _zonePendingOrders.Values
        .FirstOrDefault(x => x.Order != null && x.Order.Id == args.Position.Id);

    if (pendingOrder != null)
    {
        Print($"[PENDING FILLED] Zone {pendingOrder.ZoneId} | Entry: {args.Position.EntryPrice}");

        // Clean up tracking
        _zonePendingOrders.Remove(pendingOrder.ZoneId);

        // Continue with normal position management (Chandelier SL, etc.)
    }
}
```

---

## Entry Mode Comparison

### Market Order Mode (Current)
**Flow:**
1. Zone becomes ARMED
2. Wait for price to CLOSE beyond zone boundary
3. Execute market order immediately
4. Entry fills 2-3 pips away from zone

**SL Calculation:**
- From entry price to opposite zone boundary + buffer
- Includes breakout slippage
- Result: 6-9 pips

### Pending STOP Mode (New)
**Flow:**
1. Zone becomes ARMED
2. Place STOP order at zone boundary + offset
3. Wait for price to trigger order
4. Entry fills at/near pending order price

**SL Calculation:**
- From entry price (zone boundary) to opposite boundary + buffer
- Minimal slippage
- Result: 4-5 pips

---

## Edge Cases & Error Handling

### 1. Order Placement Fails
```csharp
if (!result.IsSuccessful)
{
    Print($"[ERROR] Pending order failed: {result.Error}");
    // Fall back to market order mode for this zone?
    // Or skip entry and wait for next zone?
    // Decision: Log and skip - maintain consistency
}
```

### 2. Price Gaps Through Entry Level
- STOP orders convert to market orders
- Accept slippage in this scenario
- Still likely better than current market order approach

### 3. Multiple Zones Active Simultaneously
```csharp
// Limit pending orders
private const int MAX_PENDING_ORDERS = 2;

if (_zonePendingOrders.Count >= MAX_PENDING_ORDERS)
{
    Print("[PENDING ORDER] Max pending orders reached - skipping");
    return;
}
```

### 4. Order Partially Filled
- cTrader typically fills orders atomically
- If partial fill occurs, treat as regular position
- Position management handles any size

### 5. Connection Loss During Order Lifecycle
- cTrader maintains pending orders on server
- Orders will execute/expire regardless of connection
- On reconnection, sync state from PendingOrders collection

---

## Testing Requirements

### Unit Testing Scenarios

1. **Pending order placement**
   - Verify correct entry price calculation
   - Verify SL/TP distances
   - Verify order expiry time

2. **Order cancellation**
   - Zone expires → Order cancelled
   - Higher-scored zone → Old order cancelled, new placed
   - Max positions → No new orders placed

3. **Order execution**
   - Order fills → Position created correctly
   - SL/TP applied correctly
   - Chandelier tracking starts

4. **Edge cases**
   - Placement failure → Graceful handling
   - Multiple zones → Proper tracking
   - Expiry reached → Auto-cancel

### Backtest Comparison

**Test A: Market Order Mode (Baseline)**
- EntryExecution = Market
- April-June 2024 backtest
- Record: Win rate, avg SL, avg TP, profit factor

**Test B: Pending STOP Mode (New)**
- EntryExecution = PendingStop
- Same period (April-June 2024)
- Compare metrics

**Expected Improvements:**
- SL reduction: 40-50%
- Win rate: +5-10%
- Profit factor: +20-40%

---

## Success Criteria

### Minimum Viable Success:
- ✅ Pending orders place correctly when zones arm
- ✅ Orders execute at or near intended price
- ✅ SL reduced by at least 30%
- ✅ No increase in errors/failed trades
- ✅ Backward compatibility maintained (Market mode still works)

### Optimal Success:
- ✅ SL reduced by 40-50%
- ✅ Win rate increases by 5-10%
- ✅ Profit factor improves by 20-40%
- ✅ No significant gap-fill issues
- ✅ Clean order lifecycle management (no orphaned orders)

---

## Implementation Phases

### Phase 1: Core Pending Order Logic
- Add parameters (EntryExecution, PendingEntryOffset, PendingOrderExpiry)
- Implement PlaceBuyPendingOrder() / PlaceSellPendingOrder()
- Add pending order tracking dictionary
- Integrate into zone ARMED logic

### Phase 2: Order Management
- Implement order cancellation logic
- Handle zone expiry → cancel order
- Handle zone replacement → cancel old, place new
- Add OnPositionOpened handling

### Phase 3: Testing & Validation
- Backtest comparison (Market vs Pending)
- Verify SL/TP calculations
- Test edge cases
- Optimize parameters (offset, expiry)

### Phase 4: Documentation
- Update user guide
- Document parameter recommendations
- Create troubleshooting guide

---

## Parameter Recommendations

### Initial Testing Values:
```
EntryExecution: PendingStop
PendingEntryOffsetPips: 2.0
PendingOrderExpiryMinutes: 60
SLBufferPips: 1.0 (reduced from 2.0)
MaxDynamicRR: 3.5 (reduced from 5.0)
```

### Optimization Ranges:
```
PendingEntryOffsetPips: 0.5 - 3.0 (step 0.5) → 6 values
PendingOrderExpiryMinutes: 30, 60, 90, 120 → 4 values
SLBufferPips: 0.5, 1.0, 1.5 → 3 values
```

**Total combinations:** 6 × 4 × 3 = 72 (~30-40 minutes optimization)

---

## Risks & Mitigations

### Risk 1: Increased false signals
**Issue:** Orders might fill on brief spikes that reverse
**Mitigation:** Offset parameter allows tuning sensitivity
**Monitoring:** Track filled-then-stopped trades

### Risk 2: Gap fills in fast markets
**Issue:** Order fills far from intended price during volatility
**Mitigation:** STOP orders become market orders - same as current approach
**Monitoring:** Track actual fill price vs intended price

### Risk 3: Order management complexity
**Issue:** Orphaned orders, memory leaks, tracking errors
**Mitigation:** Robust cleanup logic, testing, logging
**Monitoring:** Log all order lifecycle events

### Risk 4: Broker execution differences
**Issue:** Pending order behavior may vary by broker
**Mitigation:** Test with actual broker before live trading
**Monitoring:** Compare backtest vs paper trading results

---

## Future Enhancements

### 1. Pending LIMIT Orders (Zone Retest Mode)
- Place limit order at zone center
- Wait for pullback/retest
- Higher quality entries but fewer trades

### 2. Smart Order Sizing
- Adjust volume based on order price vs zone quality
- Scale into positions on retest

### 3. Trailing Entry Orders
- Move pending order as zone quality updates
- Cancel and replace if better entry becomes available

### 4. Multiple Order Strategies
- Place both STOP (breakout) and LIMIT (retest) simultaneously
- First fill wins, cancel the other

---

## Conclusion

Pending order entry mode addresses the core SL sizing issue by eliminating breakout slippage. Expected 40-50% SL reduction should significantly improve:
- Risk per trade
- TP hit rate (closer targets)
- Overall profitability

Implementation maintains backward compatibility through parameter selection, allowing direct comparison between Market and Pending modes.

**Recommendation:** Implement and test on April-June 2024 data before considering live deployment.

# Live Session Box Drawing - Design Specification

**Date:** 2026-03-11
**Status:** Approved
**Implementation:** Pending

---

## Overview

### Problem Statement

Currently, session boxes are drawn **only after a session ends**. This means:
- Traders see the session indicator too late (after the trading opportunity has passed)
- The visual guidance arrives when the session is already over
- The "BEST TIME" or "DANGER ZONE" indicators are not visible during the actual session

### Solution

Change session boxes to draw **immediately when a session/period starts**, similar to how swing rectangles work. This provides real-time visual guidance while trading opportunities are active.

### Key Benefits

1. **Immediate Visual Feedback:** See colored bands the moment an optimal period begins
2. **Real-Time Trading Guidance:** Know instantly if you should trade (green/gold) or avoid (red)
3. **Cleaner Chart:** Full-height time zone bands are cleaner than dynamic high/low boxes
4. **Simpler Code:** Remove complex end-of-session drawing logic, active box tracking, and updates

---

## Requirements Summary

### Functional Requirements

1. **Draw at Period Start:** Session/period boxes must appear immediately when the period begins
2. **Full-Height Bands:** Boxes span entire chart visible range (vertical time zone indicators)
3. **No Dynamic Updates:** Boxes are static once drawn (no high/low updates)
4. **Internal Tracking:** System continues tracking actual session high/low for scoring (decoupled from visuals)
5. **Both Modes:** Same behavior for Basic Mode (sessions) and Advanced Mode (optimal periods)
6. **Mode Switching:** Users can switch between Basic/Advanced modes via parameter
7. **Replace Old Behavior:** Completely remove "draw at session end" logic

### Visual Requirements

1. **Basic Mode Colors:** Yellow (Asian), Blue (London), Orange (NY), Purple (Overlap)
2. **Advanced Mode Colors:** Bright Green (BEST), Gold (GOOD), Red (DANGER)
3. **Transparency:** 30-40% opacity (existing values)
4. **Z-Index:** -1 (behind swing rectangles and price action)
5. **Multiple Bands:** Can have multiple active bands simultaneously (e.g., London + NY overlap)

---

## Design

### Architecture Changes

#### 1. Drawing Trigger Change

**Before:**
```csharp
// Line 1797 in UpdateSessionTracking()
if (currentSession != null)
{
    currentSession.EndTime = currentTime;
    recentSessions.Add(currentSession);

    DrawSessionBox(currentSession);  // ← Draw at END
}
```

**After:**
```csharp
// In UpdateSessionTracking() - when NEW period detected
if (newPeriodDetected)
{
    DrawSessionBoxAtStart(period, startTime, endTime);  // ← Draw at START
    activeSessionBoxes[boxName] = box;  // Track active box
}
```

#### 2. Box Height Calculation

**Before:**
```csharp
// Used actual session high/low
Chart.DrawRectangle(name, startTime, session.High, endTime, session.Low, color);
```

**After:**
```csharp
// Use full chart visible range
Chart.DrawRectangle(name, startTime, Chart.TopY, endTime, Chart.BottomY, color);
```

#### 3. Active Box Tracking

**New Field:**
```csharp
// Track currently visible session boxes
private Dictionary<string, ChartRectangle> activeSessionBoxes = new Dictionary<string, ChartRectangle>();
```

**Purpose:**
- Know which boxes are currently active
- Clean up tracking when period ends (box stays visible, just stop tracking)
- Prevent duplicate box creation

#### 4. Decoupled Tracking

**Visual Tracking:**
- Session boxes: Full-height, static, time zone indicators
- Drawn at period start
- Never updated after creation

**Internal Tracking:**
- Session high/low: Continues updating every bar (lines 1818-1828)
- Used for scoring and session alignment calculations
- Completely independent of visual boxes

---

### Basic Mode Behavior

#### Session Detection

Uses existing `GetSessionState()` method to detect:
- **Asian:** 00:00-09:00 UTC
- **London:** 08:00-17:00 UTC
- **NewYork:** 13:00-22:00 UTC
- **Overlap:** 13:00-17:00 UTC (London + NY)

#### Drawing Logic

```
Each M15 bar in UpdateSessionTracking():
1. Call GetSessionState(currentTime)
2. Determine primary session (Overlap > NewYork > London > Asian)
3. If NEW session detected:
   a. Draw full-height box (startTime to endTime)
   b. Use session-specific color
   c. Add to activeSessionBoxes tracking
4. If session ended:
   a. Remove from activeSessionBoxes tracking
   b. Box remains visible on chart (historical)
```

#### Visual Timeline Example

```
UTC Time  Action
--------  ------
00:00     Yellow band appears (Asian session starts)
08:00     Blue band appears (London session starts, Asian still visible)
09:00     Yellow band ends (Asian session over)
13:00     Orange band appears (NY starts)
          Purple band appears (Overlap starts)
17:00     Blue band ends (London over)
          Purple band ends (Overlap over)
22:00     Orange band ends (NY over)
```

#### Colors (Existing)

- `ColorAsian`: `Color.FromArgb(30, 255, 255, 0)` - Light Yellow
- `ColorLondon`: `Color.FromArgb(30, 0, 128, 255)` - Light Blue
- `ColorNewYork`: `Color.FromArgb(30, 255, 128, 0)` - Light Orange
- `ColorOverlap`: `Color.FromArgb(40, 128, 0, 255)` - Light Purple

---

### Advanced Mode Behavior

#### Optimal Period Detection

Uses existing `GetOptimalPeriod()` method to detect:
- **BEST:** 13:00-17:00 UTC (Overlap - highest volatility)
- **GOOD:** 08:00-12:00 UTC (London Open)
- **DANGER Dead Zone:** 04:00-08:00 UTC (avoid)
- **DANGER Late NY:** 20:00-00:00 UTC (avoid)
- **None:** Neutral times (no box drawn)

#### Drawing Logic

```
Each M15 bar in UpdateSessionTracking():
1. Call GetOptimalPeriod(currentTime)
2. If NEW optimal period detected (and != None):
   a. Draw full-height box (startTime to endTime)
   b. Use priority color (Green/Gold/Red)
   c. Add to activeSessionBoxes tracking
3. If period ended OR changed to None:
   a. Remove from activeSessionBoxes tracking
   b. Box remains visible on chart (historical)
```

#### Visual Timeline Example

```
UTC Time  Action
--------  ------
00:00     No box (neutral period)
04:00     Red band appears (DANGER - Dead Zone)
08:00     Red disappears, Gold band appears (GOOD TIME)
12:00     Gold disappears, no box (neutral hour)
13:00     Green band appears (BEST TIME)
17:00     Green disappears, no box (neutral hours)
20:00     Red band appears (DANGER - Late NY)
00:00     Red disappears, no box (neutral hours)
```

#### Colors (Existing)

- `ColorBestTime`: `Color.FromArgb(50, 0, 255, 0)` - Bright Green
- `ColorGoodTime`: `Color.FromArgb(40, 255, 215, 0)` - Gold
- `ColorDangerZone`: `Color.FromArgb(40, 255, 0, 0)` - Red

---

### Implementation Details

#### 1. Modified DrawSessionBox() Method

**Before:**
```csharp
private void DrawSessionBox(SessionLevels session)
{
    // Used session.High and session.Low
    // Drew box after session ended
}
```

**After:**
```csharp
private void DrawSessionBox(string periodName, DateTime startTime, DateTime endTime, Color boxColor)
{
    // Create unique name
    string boxName = string.Format("Session_{0}_{1}",
        periodName,
        startTime.ToString("yyyyMMddHH"));

    // Get full chart height
    double chartTop = Chart.TopY;
    double chartBottom = Chart.BottomY;

    // Draw full-height box
    var box = Chart.DrawRectangle(
        boxName,
        startTime,
        chartTop,      // ← Full height
        endTime,
        chartBottom,   // ← Full height
        boxColor);

    // Configure
    box.IsFilled = true;
    box.IsInteractive = false;
    box.ZIndex = -1;  // Behind everything
    box.Comment = string.Format("{0} | {1} - {2} UTC",
        periodName,
        startTime.ToString("HH:mm"),
        endTime.ToString("HH:mm"));

    // Track active box
    activeSessionBoxes[boxName] = box;

    Print("[SessionBox] Drew {0} | {1} - {2}",
        periodName,
        startTime.ToString("HH:mm"),
        endTime.ToString("HH:mm"));
}
```

#### 2. Modified UpdateSessionTracking() Method

**Key Changes:**

```csharp
private void UpdateSessionTracking()
{
    DateTime currentTime = m15Bars.OpenTimes.LastValue;

    // === VISUAL TRACKING (New behavior) ===
    if (ShowSessionBoxes)
    {
        if (SessionBoxDisplayMode == SessionBoxMode.Basic)
        {
            // Detect current session
            var currentState = GetSessionState(currentTime);
            TradingSession primarySession = DeterminePrimarySession(currentState);

            // If NEW session detected
            if (primarySession != lastDetectedSession)
            {
                // Draw session box immediately
                DateTime sessionStart = GetSessionStartTime(primarySession, currentTime);
                DateTime sessionEnd = GetSessionEndTime(primarySession, currentTime);
                Color sessionColor = GetSessionColor(primarySession);

                DrawSessionBox(primarySession.ToString(), sessionStart, sessionEnd, sessionColor);

                lastDetectedSession = primarySession;
            }
        }
        else // Advanced Mode
        {
            // Detect current optimal period
            OptimalPeriod currentPeriod = GetOptimalPeriod(currentTime);

            // If NEW period detected (and not None)
            if (currentPeriod != lastOptimalPeriod && currentPeriod != OptimalPeriod.None)
            {
                // Draw optimal period box immediately
                DateTime periodStart = GetOptimalPeriodStart(currentPeriod, currentTime);
                DateTime periodEnd = GetOptimalPeriodEnd(currentPeriod, currentTime);
                Color periodColor = GetOptimalPeriodColor(currentPeriod);

                DrawSessionBox(currentPeriod.ToString(), periodStart, periodEnd, periodColor);

                lastOptimalPeriod = currentPeriod;
            }
        }
    }

    // === INTERNAL TRACKING (Unchanged) ===
    // Continue tracking actual session high/low for scoring
    if (currentSession != null)
    {
        double currentHigh = m15Bars.HighPrices.LastValue;
        double currentLow = m15Bars.LowPrices.LastValue;

        if (currentHigh > currentSession.High)
            currentSession.High = currentHigh;

        if (currentLow < currentSession.Low)
            currentSession.Low = currentLow;
    }
}
```

#### 3. New Helper Methods

```csharp
/// <summary>
/// Gets session start time for a given session type
/// </summary>
private DateTime GetSessionStartTime(TradingSession session, DateTime currentTime)
{
    int hour = currentTime.Hour;
    DateTime today = currentTime.Date;

    switch (session)
    {
        case TradingSession.Asian:
            return hour < 9 ? today : today.AddDays(1);
        case TradingSession.London:
            return today.AddHours(8);
        case TradingSession.NewYork:
            return today.AddHours(13);
        case TradingSession.Overlap:
            return today.AddHours(13);
        default:
            return currentTime;
    }
}

/// <summary>
/// Gets session end time for a given session type
/// </summary>
private DateTime GetSessionEndTime(TradingSession session, DateTime currentTime)
{
    DateTime start = GetSessionStartTime(session, currentTime);

    switch (session)
    {
        case TradingSession.Asian:
            return start.AddHours(9);
        case TradingSession.London:
            return start.AddHours(9);
        case TradingSession.NewYork:
            return start.AddHours(9);
        case TradingSession.Overlap:
            return start.AddHours(4);
        default:
            return currentTime;
    }
}

// Similar methods for GetOptimalPeriodStart() and GetOptimalPeriodEnd()
```

#### 4. Box Cleanup

```csharp
/// <summary>
/// Removes box from active tracking when period ends
/// Box stays visible on chart, just stops being tracked
/// </summary>
private void CleanupActiveSessionBox(string boxName)
{
    if (activeSessionBoxes.ContainsKey(boxName))
    {
        activeSessionBoxes.Remove(boxName);
        Print("[SessionBox] Cleanup: {0}", boxName);
    }
}
```

---

### Code Removal

#### Delete Entire Methods

1. **DrawAdvancedSessionBoxes() (lines 1142-1174)**
   - Hour-by-hour iteration logic
   - No longer needed (draw at start, not at end)

#### Delete Specific Lines

1. **Line 1797:** `DrawSessionBox(currentSession);`
   - Drawing at session end

2. **Duplicate box checking logic:**
   ```csharp
   var existingBox = Chart.FindObject(boxName);
   if (existingBox != null)
       return;
   ```
   - Not needed (deterministic drawing at start)

---

### Code Preservation

#### Keep Unchanged

1. **Session detection methods:**
   - `GetSessionState()`
   - `GetOptimalPeriod()`
   - All session/period detection logic

2. **Internal session tracking (lines 1818-1828):**
   ```csharp
   if (currentSession != null)
   {
       double currentHigh = m15Bars.HighPrices.LastValue;
       double currentLow = m15Bars.LowPrices.LastValue;

       if (currentHigh > currentSession.High)
           currentSession.High = currentHigh;

       if (currentLow < currentSession.Low)
           currentSession.Low = currentLow;
   }
   ```
   - Used for scoring, completely separate from visuals

3. **Color definitions (lines 308-315):**
   - All existing session and optimal period colors

4. **Parameters:**
   - `ShowSessionBoxes`
   - `SessionBoxDisplayMode`
   - All existing parameters

---

## Visual Comparison

### Before (Current)

```
08:00 UTC - London session starts
├─ [Trading happens...]
├─ [No visual indicator]
├─ [User doesn't know it's optimal time]
│
17:00 UTC - London session ends
└─ Blue box appears (08:00-17:00) ← TOO LATE!
```

### After (New)

```
08:00 UTC - London session starts
├─ GOLD BAND APPEARS IMMEDIATELY ← Visual guidance!
├─ [Trading happens with clear visual context]
├─ [User sees "GOOD TIME" indicator live]
│
17:00 UTC - London session ends
└─ Gold band remains visible (historical)
```

---

## User Experience

### Trading Workflow

**1. Chart Monitoring:**
```
Trader looks at chart at 09:00 UTC:
- Sees gold vertical band spanning chart
- Knows: "This is GOOD TIME to trade"
- Trades with confidence
```

**2. Risk Avoidance:**
```
Trader looks at chart at 05:00 UTC:
- Sees red vertical band spanning chart
- Knows: "This is DANGER ZONE - avoid trading"
- Stays out, saves capital
```

**3. Optimal Timing:**
```
Trader looks at chart at 14:00 UTC:
- Sees bright green vertical band
- Knows: "This is BEST TIME - trade aggressively"
- Increases position size appropriately
```

### Mode Switching

**Basic Mode:**
- Use case: Learning session boundaries, understanding overlaps
- Shows: All sessions (Asian/London/NY/Overlap)
- Visual: Yellow/Blue/Orange/Purple bands

**Advanced Mode:**
- Use case: Live trading, actionable guidance
- Shows: Only optimal periods (BEST/GOOD/DANGER)
- Visual: Green/Gold/Red bands

**Switching:**
```
Parameters → Session Management → Session Box Mode:
  - Basic (informational)
  - Advanced (actionable)
```

---

## Migration

### Backward Compatibility

**Breaking Changes:** None
- Parameters stay the same
- User configuration unchanged
- Visual behavior changes (improvement, not breaking)

### Documentation Updates

**Files to Update:**

1. **ADVANCED_SESSION_BOX_MODE.md**
   - Update description: "Boxes now appear at session start, not end"
   - Update visual examples
   - Add note about full-height time zone bands

2. **SESSION_BOX_IMPLEMENTATION.md**
   - Update implementation guide
   - New drawing approach
   - Remove "draw at session end" instructions

3. **Master_Plan.md**
   - Mark Phase 2 as updated
   - Note: Session boxes now live/advance

### Rollout Plan

1. **Implement changes** (follow implementation details above)
2. **Test both modes** (Basic and Advanced)
3. **Verify internal tracking** (scoring still works)
4. **Update documentation**
5. **Commit and test in backtest**

---

## Testing Strategy

### Unit Testing

**Test 1: Box appears at session start**
```
Setup: M15 backtest starting at 07:59 UTC
Action: Advance to 08:00 UTC (London open)
Expected: Gold band appears immediately (Advanced Mode)
```

**Test 2: Box spans full chart height**
```
Setup: Any session active
Action: Check box coordinates
Expected: box.High ≈ Chart.TopY, box.Low ≈ Chart.BottomY
```

**Test 3: Multiple bands visible**
```
Setup: M15 backtest at 14:00 UTC (Overlap)
Action: Check chart objects
Expected: Green band visible (BEST TIME)
```

**Test 4: Internal tracking independent**
```
Setup: Active session
Action: Record actual session high/low
Expected: currentSession.High/Low tracks correctly, independent of box visuals
```

**Test 5: Mode switching**
```
Setup: Run backtest in Basic Mode
Action: Switch to Advanced Mode, re-run
Expected: Different boxes (sessions vs optimal periods)
```

### Visual Testing

**Test 1: Basic Mode - 48 hour backtest**
```
Parameters:
- Session Box Mode: Basic
- Show Session Boxes: TRUE
- Period: 2025-01-15 00:00 to 2025-01-17 00:00

Expected Visual:
- Yellow bands at 00:00 daily (Asian)
- Blue bands at 08:00 daily (London)
- Orange bands at 13:00 daily (NY)
- Purple bands at 13:00 daily (Overlap)
```

**Test 2: Advanced Mode - 48 hour backtest**
```
Parameters:
- Session Box Mode: Advanced
- Show Session Boxes: TRUE
- Period: 2025-01-15 00:00 to 2025-01-17 00:00

Expected Visual:
- Gold bands at 08:00 daily (GOOD TIME)
- Green bands at 13:00 daily (BEST TIME)
- Red bands at 04:00 and 20:00 daily (DANGER)
- No bands during neutral periods
```

**Test 3: Z-Index verification**
```
Setup: Active session with swing rectangle
Action: Visual inspection
Expected: Session band behind swing rectangle (not covering it)
```

### Performance Testing

**Test 1: Memory usage**
```
Action: Run 7-day backtest with session boxes enabled
Expected: No significant memory increase (boxes are lightweight)
```

**Test 2: Rendering performance**
```
Action: Zoom in/out on chart with multiple session boxes
Expected: Smooth rendering, no lag
```

---

## Success Criteria

### Functional Success

- ✅ Session boxes appear at period start (not end)
- ✅ Boxes span full chart height
- ✅ Both Basic and Advanced modes work correctly
- ✅ Internal session tracking still works (scoring unaffected)
- ✅ Old "draw at end" code completely removed
- ✅ No duplicate boxes
- ✅ Boxes appear behind swing rectangles (ZIndex = -1)

### Visual Success

- ✅ Bands are clearly visible but not obtrusive (30-40% opacity)
- ✅ Colors match specification (Gold/Green/Red for Advanced, Yellow/Blue/Orange/Purple for Basic)
- ✅ Multiple bands can coexist without visual conflicts
- ✅ Historical bands remain visible after period ends

### User Experience Success

- ✅ Trader can glance at chart and immediately know if they should trade
- ✅ "BEST TIME" (green) is instantly recognizable
- ✅ "DANGER ZONE" (red) provides clear warning
- ✅ Mode switching is intuitive and works as expected

---

## Implementation Checklist

- [ ] Add `activeSessionBoxes` tracking dictionary
- [ ] Modify `DrawSessionBox()` to use full chart height
- [ ] Add `GetSessionStartTime()` helper method
- [ ] Add `GetSessionEndTime()` helper method
- [ ] Add `GetOptimalPeriodStart()` helper method
- [ ] Add `GetOptimalPeriodEnd()` helper method
- [ ] Modify `UpdateSessionTracking()` to draw at period start
- [ ] Remove line 1797 (draw at session end)
- [ ] Delete `DrawAdvancedSessionBoxes()` method
- [ ] Remove duplicate box checking logic
- [ ] Test Basic Mode (48 hour backtest)
- [ ] Test Advanced Mode (48 hour backtest)
- [ ] Verify internal tracking still works
- [ ] Verify Z-Index (bands behind rectangles)
- [ ] Update ADVANCED_SESSION_BOX_MODE.md
- [ ] Update SESSION_BOX_IMPLEMENTATION.md
- [ ] Commit changes

---

## Appendix

### Key Terminology

- **Session Box:** Vertical time zone band indicating trading session/period
- **Full-Height:** Box spans entire visible chart range (Chart.TopY to Chart.BottomY)
- **Live/Advance Drawing:** Drawing boxes at period start (not at end)
- **Active Box Tracking:** Dictionary tracking currently visible boxes
- **Internal Tracking:** Session high/low tracking for scoring (separate from visuals)

### Related Files

- `Jcamp_1M_scalping.cs` - Main bot file
- `ADVANCED_SESSION_BOX_MODE.md` - Advanced mode documentation
- `SESSION_BOX_IMPLEMENTATION.md` - Implementation guide
- `Master_Plan.md` - Overall project plan

### References

- Line 1797: Current DrawSessionBox() call (to be removed)
- Lines 1142-1174: DrawAdvancedSessionBoxes() method (to be deleted)
- Lines 1818-1828: Internal high/low tracking (keep unchanged)
- Lines 308-315: Color definitions (keep unchanged)

---

**End of Design Specification**

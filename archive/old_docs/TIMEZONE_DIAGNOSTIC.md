# Timezone Diagnostic Guide

## Quick Check: What Time Zone Are My Logs Using?

### Step 1: Add Timezone Logging (2 minutes)

Open `Jcamp_1M_scalping.cs` and find the `OnStart()` method.

Add these lines after the initialization messages (around line 350):

```csharp
protected override void OnStart()
{
    // ... existing initialization code ...

    // ========== TIMEZONE DIAGNOSTIC ==========
    DateTime serverTime = Server.Time;
    DateTime utcTime = DateTime.UtcNow;
    TimeSpan offset = serverTime - utcTime;

    Print("========================================");
    Print("*** TIMEZONE DIAGNOSTIC ***");
    Print("========================================");
    Print("Server Time: {0}", serverTime.ToString("yyyy-MM-dd HH:mm:ss"));
    Print("UTC Time:    {0}", utcTime.ToString("yyyy-MM-dd HH:mm:ss"));
    Print("Offset:      {0} hours", offset.TotalHours);
    Print("Time Zone:   {0}", serverTime.Hour == utcTime.Hour ? "UTC" : "NOT UTC");
    Print("========================================");

    // ... rest of initialization ...
}
```

### Step 2: Run Quick Test

1. Build the code (Ctrl+B)
2. Run any backtest (even 1 day is fine)
3. Check the FIRST lines of console output

### Step 3: Read the Results

You should see something like:

```
========================================
*** TIMEZONE DIAGNOSTIC ***
========================================
Server Time: 2024-01-15 08:00:00
UTC Time:    2024-01-15 08:00:00
Offset:      0 hours
Time Zone:   UTC
========================================
```

**Interpretation:**

| Offset | Time Zone | Session Times Correct? |
|--------|-----------|------------------------|
| 0 hours | UTC | ✅ YES - Sessions will work correctly |
| +2 hours | EET (GMT+2) | ❌ NO - Sessions 2 hours early |
| -5 hours | EST (GMT-5) | ❌ NO - Sessions 5 hours late |
| Other | Custom | ❌ NO - Need adjustment |

### Step 4: Fix If Needed

**If offset = 0 hours (UTC):**
- ✅ No action needed
- Sessions will trigger at correct times

**If offset ≠ 0 hours:**
- ❌ Session times need adjustment
- See "Session Timezone Fix" section below

---

## Session Timezone Fix (If Needed)

If your broker time is NOT UTC, you need to adjust session hours.

### Calculate Adjusted Hours

Example: If broker time is **GMT+2 (EET)**:
- Asian session: 00:00-09:00 UTC → 02:00-11:00 EET
- London session: 08:00-17:00 UTC → 10:00-19:00 EET
- New York session: 13:00-22:00 UTC → 15:00-00:00 EET

### Formula:
```
Adjusted Hour = (UTC Hour + Offset) % 24
```

### Code Change:

Find the `GetSessionState()` method and adjust the hours:

```csharp
private SessionState GetSessionState(DateTime time)
{
    int hourServer = time.Hour;

    // ADJUST THESE if broker is NOT UTC
    int offsetHours = 0; // ← Change this to your offset (e.g., +2 for EET)
    int hourUTC = (hourServer - offsetHours + 24) % 24;

    return new SessionState
    {
        IsAsian = hourUTC >= 0 && hourUTC < 9,     // 00:00-09:00 UTC
        IsLondon = hourUTC >= 8 && hourUTC < 17,   // 08:00-17:00 UTC
        IsNewYork = hourUTC >= 13 && hourUTC < 22  // 13:00-22:00 UTC
    };
}
```

---

## Quick Reference: Common Broker Timezones

| Broker Time | Offset from UTC | Example Adjustment |
|-------------|-----------------|-------------------|
| UTC (GMT+0) | 0 hours | No change needed |
| EET (GMT+2) | +2 hours | offsetHours = 2 |
| EEST (GMT+3) | +3 hours | offsetHours = 3 |
| EST (GMT-5) | -5 hours | offsetHours = -5 |
| PST (GMT-8) | -8 hours | offsetHours = -8 |

---

## Enhanced Session Logging

For more detailed timezone tracking, add this to `UpdateSessionTracking()`:

```csharp
private void UpdateSessionTracking()
{
    var currentState = GetSessionState(m15Bars.OpenTimes.LastValue);
    var primarySession = GetPrimarySession(m15Bars.OpenTimes.LastValue);

    // Enhanced logging with timestamp
    if (primarySession != lastSession && primarySession != TradingSession.None)
    {
        DateTime sessionStart = m15Bars.OpenTimes.LastValue;

        Print("[Session] NEW {0} session started", primarySession);
        Print("          Server Time: {0}", sessionStart.ToString("yyyy-MM-dd HH:mm:ss"));
        Print("          Hour (Server): {0}", sessionStart.Hour);
        Print("          Expected UTC Hour: {0} (check diagnostic above)",
              primarySession == TradingSession.Asian ? "00-09" :
              primarySession == TradingSession.London ? "08-17" :
              primarySession == TradingSession.NewYork ? "13-22" : "Unknown");

        // ... rest of session tracking logic ...
    }
}
```

This will help you verify sessions are starting at the correct times.

---

## Verification Checklist

After adding timezone diagnostic:

- [ ] Server Time and UTC Time displayed
- [ ] Offset calculated
- [ ] If offset = 0: Sessions should work correctly
- [ ] If offset ≠ 0: Adjusted session hours in code
- [ ] Session start messages show correct hour
- [ ] Session boundaries align with expected UTC times

---

## Example: Verifying Session Start Times

Run a backtest covering 24 hours and check console:

**Expected (if UTC is correct):**
```
[Session] NEW Asian session started
          Server Time: 2024-01-15 00:00:00  ← Midnight UTC
          Hour (Server): 0
          Expected UTC Hour: 00-09

[Session] NEW London session started
          Server Time: 2024-01-15 08:00:00  ← 8am UTC
          Hour (Server): 8
          Expected UTC Hour: 08-17

[Session] NEW NewYork session started
          Server Time: 2024-01-15 13:00:00  ← 1pm UTC
          Hour (Server): 13
          Expected UTC Hour: 13-22
```

If the hours don't match, your broker is NOT using UTC and needs adjustment.

---

## Common Issues

### Issue: Sessions Starting at Wrong Times
**Cause:** Broker timezone ≠ UTC
**Fix:** Add offset adjustment to `GetSessionState()`

### Issue: Offset Changes (DST)
**Cause:** Daylight Saving Time shifts offset
**Fix:** Most forex brokers ignore DST and stay on standard time
**Check:** Run diagnostic in summer vs winter to verify

### Issue: Sessions Overlap Incorrectly
**Cause:** Timezone adjustment not applied consistently
**Fix:** Ensure offset is used in ALL session time checks

---

**Next Step:** Add the timezone diagnostic code and run a quick test to see your broker's timezone!

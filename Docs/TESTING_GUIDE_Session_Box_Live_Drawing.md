# Testing Guide: Live Session Box Drawing

**Feature**: Session boxes now draw immediately when periods start (live), not after they end.
**Implementation**: Tasks 1-13 complete + critical duplicate check fix
**Status**: Ready for testing

---

## Pre-Test Checklist

Before starting tests, verify:
- [ ] Code compiles successfully in cTrader
- [ ] Latest code is loaded (commit 292b95e or later)
- [ ] No compilation errors
- [ ] Bot parameters accessible

---

## Test 1: Basic Mode Visual Test (48-hour backtest)

**Objective**: Verify session boxes appear at period start in Basic Mode

### Configuration
```
Session Management:
├─ Show Session Boxes: TRUE
└─ Session Box Mode: Basic

Backtest Settings:
├─ Symbol: EURUSD
├─ Timeframe: M1
├─ Start: 2025-01-15 00:00
├─ End: 2025-01-17 00:00 (48 hours)
└─ Visual Mode: ON
```

### Expected Console Output
```
✓ "Session Boxes: ON | Mode: Basic"
✓ "[SessionBox] Drew Asian | 00:00 - 09:00"
✓ "[SessionBox] Drew London | 08:00 - 17:00"
✓ "[SessionBox] Drew NewYork | 13:00 - 22:00"
✓ "[SessionBox] Drew Overlap | 13:00 - 17:00"
```

### Expected Visual Results
- [ ] Yellow bands appear at 00:00 (Asian session)
- [ ] Blue bands appear at 08:00 (London session)
- [ ] Orange bands appear at 13:00 (NY session)
- [ ] Purple bands appear at 13:00 (Overlap session)
- [ ] Bands span full chart height (vertical time zones)
- [ ] Bands are semi-transparent (~30% opacity)
- [ ] Bands are behind swing rectangles (ZIndex = -1)
- [ ] Multiple bands visible simultaneously during overlaps
- [ ] No duplicate boxes (bot restart test)

### Success Criteria
All checkboxes above must be ✓ to pass.

### On Success
Create test evidence:
```bash
echo "Basic Mode Test - PASS
Date: $(date)
Bands drawn at period start: YES
Full-height coverage: YES
Colors correct: YES
ZIndex correct (behind rectangles): YES" > test_results_basic_mode.txt

git add test_results_basic_mode.txt
git commit -m "test: verify Basic Mode live session boxes (48h backtest)

Confirmed:
- Session boxes appear at period start (not end)
- Full-height bands visible
- Correct colors (Yellow/Blue/Orange/Purple)
- Proper ZIndex (behind swing rectangles)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### On Failure
Document issues found:
- Screenshot the chart showing the problem
- Copy console output showing errors
- Note which specific check failed
- Report to developer before proceeding

---

## Test 2: Advanced Mode Visual Test (48-hour backtest)

**Objective**: Verify optimal period boxes appear correctly in Advanced Mode

### Configuration
```
Session Management:
├─ Show Session Boxes: TRUE
└─ Session Box Mode: Advanced

Backtest Settings:
├─ Symbol: EURUSD
├─ Timeframe: M1
├─ Start: 2025-01-15 00:00
├─ End: 2025-01-17 00:00 (48 hours)
└─ Visual Mode: ON
```

### Expected Console Output
```
✓ "Session Boxes: ON | Mode: Advanced"
✓ "🟢 BEST TIME (Green):   13:00-17:00 UTC"
✓ "🟡 GOOD TIME (Gold):    08:00-12:00 UTC"
✓ "🔴 DANGER ZONE (Red):   04:00-08:00 UTC & 20:00-00:00 UTC"
✓ "[SessionBox] Drew BestOverlap | 13:00 - 17:00"
✓ "[SessionBox] Drew GoodLondonOpen | 08:00 - 12:00"
✓ "[SessionBox] Drew DangerDeadZone | 04:00 - 08:00"
✓ "[SessionBox] Drew DangerLateNY | 20:00 - 00:00"
```

### Expected Visual Results
- [ ] Green bands appear at 13:00 (BEST TIME - Overlap)
- [ ] Gold bands appear at 08:00 (GOOD TIME - London Open)
- [ ] Red bands appear at 04:00 (DANGER - Dead Zone)
- [ ] Red bands appear at 20:00 (DANGER - Late NY)
- [ ] NO bands during neutral periods:
  - [ ] 00:00-04:00 (no bands)
  - [ ] 12:00-13:00 (no bands)
  - [ ] 17:00-20:00 (no bands)
- [ ] Bands span full chart height
- [ ] Bands are behind swing rectangles
- [ ] DangerLateNY crosses midnight correctly (20:00→00:00)

### Success Criteria
All checkboxes above must be ✓ to pass.

### On Success
```bash
echo "Advanced Mode Test - PASS
Date: $(date)
Optimal period bands drawn at start: YES
Full-height coverage: YES
Colors correct (Green/Gold/Red): YES
Neutral periods have no bands: YES
ZIndex correct: YES" > test_results_advanced_mode.txt

git add test_results_advanced_mode.txt
git commit -m "test: verify Advanced Mode live session boxes (48h backtest)

Confirmed:
- Optimal period boxes appear at period start
- Full-height bands visible
- Correct colors (Green/Gold/Red)
- No bands during neutral periods
- Proper ZIndex

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Test 3: Verify Internal Session Tracking Still Works

**Objective**: Confirm visual changes didn't break session scoring

### Configuration
```
Session Management:
├─ Enable Session Filter: TRUE
├─ Session Weight: 0.20
└─ Show Session Boxes: TRUE (either mode)

Trade Management:
└─ Enable Trading: TRUE

Backtest: EURUSD M1, 48 hours
```

### Expected Console Output
```
✓ "[SessionAlign] BUY | 🟢 BEST TIME | Base: 1.00 | Volatility: ..."
✓ "[SessionAlign] SELL | 🟡 GOOD TIME | Base: 0.75 | Volatility: ..."
✓ "[Session] Asian session ended | High: X.XXXXX | Low: X.XXXXX | Duration: ..."
✓ Session high/low values are being tracked
✓ Scoring calculations work correctly
```

### Expected Behavior
- [ ] currentSession.High updates as new highs made
- [ ] currentSession.Low updates as new lows made
- [ ] Session tracking independent of visual boxes
- [ ] Scoring uses correct session data
- [ ] Visual tracking doesn't interfere with internal tracking

### Success Criteria
All session tracking functionality must work exactly as before.

### On Success
```bash
echo "Internal Session Tracking Test - PASS
Date: $(date)
Session high/low tracking: WORKING
Session scoring calculations: WORKING
Visual and internal tracking decoupled: YES" > test_results_internal_tracking.txt

git add test_results_internal_tracking.txt
git commit -m "test: verify internal session tracking unchanged

Confirmed internal session high/low tracking still works correctly and
is properly decoupled from visual session box drawing.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Test 4: Mode Switching

**Objective**: Verify both modes work independently

### Step 1: Basic Mode Test
```
Parameters:
├─ Session Box Mode: Basic
└─ Show Session Boxes: TRUE

Backtest: EURUSD M1, 24 hours
```

Verify: Yellow/Blue/Orange/Purple bands appear

### Step 2: Advanced Mode Test
Change ONLY: Session Box Mode → Advanced

Verify: Green/Gold/Red bands appear (different from Basic)

### Verification Checklist
- [ ] Basic Mode shows ALL sessions (informational)
- [ ] Advanced Mode shows ONLY optimal periods (actionable)
- [ ] Mode switching parameter works correctly
- [ ] No errors when switching modes
- [ ] Visual output changes appropriately
- [ ] Console output reflects correct mode

### On Success
```bash
echo "Mode Switching Test - PASS
Date: $(date)
Basic Mode: Shows all sessions correctly
Advanced Mode: Shows optimal periods only
Switching: Works without errors" > test_results_mode_switching.txt

git add test_results_mode_switching.txt
git commit -m "test: verify mode switching between Basic and Advanced

Confirmed both modes work independently and parameter switching works
correctly without errors.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Test 5: Price Buffer Coverage

**Objective**: Verify 10000 pip buffer covers all price action

### Configuration
```
Parameters:
├─ Show Session Boxes: TRUE
└─ Session Box Mode: Advanced

Backtest:
├─ Symbol: EURUSD
├─ Period with high volatility (e.g., major news event)
└─ Visual Mode: ON
```

### Visual Checks
- [ ] Session boxes extend beyond all visible price action
- [ ] Boxes don't cut off even during extreme moves
- [ ] 10000 pip buffer is sufficient for forex pairs
- [ ] Test on GBPUSD (similar to EURUSD) - if available
- [ ] Test on USDJPY (different pip sizing) - if available

### Success Criteria
Boxes must always extend beyond visible price range.

### On Success
```bash
echo "Price Buffer Coverage Test - PASS
Date: $(date)
EURUSD: Full coverage confirmed
10000 pip buffer: Sufficient
Extreme volatility: Handled correctly" > test_results_price_buffer.txt

git add test_results_price_buffer.txt
git commit -m "test: verify price buffer provides full-height coverage

Confirmed 10000 pip buffer extends beyond all price action on forex
pairs during normal and extreme volatility.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Test 6: Final Integration Test

**Objective**: Full realistic trading scenario

### Full Bot Configuration
```
Session Management:
├─ Enable Session Filter: TRUE
├─ Show Session Boxes: TRUE
├─ Session Box Mode: Advanced
└─ Session Weight: 0.20

Trade Management:
├─ Enable Trading: TRUE
├─ Risk Per Trade: 1.0%
└─ Minimum RR Ratio: 3.0

Backtest:
├─ Symbol: EURUSD
├─ Timeframe: M1
├─ Period: 1 week (2025-01-15 to 2025-01-22)
└─ Visual Mode: ON
```

### Verification During Backtest
- [ ] Green boxes (BEST TIME) appear during 13:00-17:00
- [ ] Gold boxes (GOOD TIME) appear during 08:00-12:00
- [ ] Red boxes (DANGER) appear during 04:00-08:00 & 20:00-00:00
- [ ] Boxes are visible WHILE trading is happening (not after)
- [ ] Session scoring still affects trade decisions
- [ ] No performance degradation
- [ ] Week-long backtest completes successfully

### Error Checks
- [ ] No compilation errors
- [ ] No runtime errors
- [ ] No drawing errors
- [ ] All sessions/periods drawn correctly
- [ ] No memory issues (long backtest)
- [ ] Chart remains responsive

### On Success
```bash
git add test_results*.txt
git commit -m "test: complete integration test with live trading scenario

Final verification:
- Live session boxes provide real-time guidance
- All features work together correctly
- No performance degradation
- Week-long backtest successful

Implementation complete.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Troubleshooting

### Issue: Boxes not appearing
**Possible causes**:
- Show Session Boxes parameter is FALSE
- Check console for errors
- Verify bot is running on M15 timeframe data (m15Bars)

**Solution**: Enable parameter, check logs

### Issue: Duplicate boxes
**Possible causes**:
- Bot restarted mid-session
- Transition tracking not working

**Solution**: Should be fixed by commit 292b95e (duplicate check), report if persists

### Issue: Boxes appear at wrong times
**Possible causes**:
- Day boundary logic error
- Timezone mismatch

**Solution**: Check console timestamps, verify UTC times

### Issue: Wrong colors
**Possible causes**:
- Mode parameter mismatch
- Color constant definitions

**Solution**: Verify Session Box Mode parameter, check console mode output

### Issue: Boxes too small/large
**Possible causes**:
- Price buffer calculation issue
- Symbol pip size mismatch

**Solution**: Check if using 5-digit broker, verify buffer calculation

---

## Test Summary Checklist

Mark tasks complete as you finish them:

- [ ] Task 14: Basic Mode Visual Test (48h backtest)
- [ ] Task 15: Advanced Mode Visual Test (48h backtest)
- [ ] Task 16: Verify Internal Session Tracking
- [ ] Task 17: Test Mode Switching
- [ ] Task 18: Test Price Buffer Coverage
- [ ] Task 19: Final Integration Test

**All tests passed?** ✓ Implementation complete!

**Any tests failed?** Document issues and report before proceeding.

---

## Expected Final State

After all tests pass:

**Git Status:**
- All test result files committed
- No uncommitted changes
- All commits have Co-Authored-By tags

**Feature Status:**
- ✅ Session boxes draw at period start (live)
- ✅ Full-height time zone bands
- ✅ Both Basic and Advanced modes working
- ✅ Internal session tracking unchanged
- ✅ No performance issues

**Next Steps:**
- Consider testing on demo account
- Monitor performance on live data
- Gather user feedback on visual guidance

---

## Contact

If you encounter issues not covered in troubleshooting:
1. Document the exact error message
2. Take screenshots of the chart
3. Copy relevant console output
4. Note the exact test configuration
5. Report to developer with evidence

---

**Good luck with testing!** 🚀

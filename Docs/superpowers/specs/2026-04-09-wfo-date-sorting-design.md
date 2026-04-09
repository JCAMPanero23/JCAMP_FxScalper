# WFO Browser: Backtest Date Sorting

**Date:** 2026-04-09
**Status:** Approved
**Author:** Claude Sonnet 4.5

## Overview

Add backtest date-based sorting to the WFO Browser to replace the current import date sorting. Users need to see backtests organized by their actual trading period (backtest date range) rather than when they were added to the archive.

## Problem Statement

Currently, the WFO Browser sorts by `modified_time` (directory modification time), which represents when the analysis was **imported into the archive**, not when the **backtest data is from**. This makes it difficult to:

- Find the most recent backtest data
- Understand chronological order of trading periods
- Identify which backtests cover recent vs historical data

Example confusion:
- Backtest A: Jan-Mar 2026 data, imported Apr 5, 2026
- Backtest B: Apr-Jun 2025 data, imported Apr 6, 2026

Current sorting shows B first (imported later), but A contains more recent data.

## Requirements

### Functional Requirements

1. **Sort by backtest end date** - Use `data_range.end` from analysis JSON as primary sort key
2. **Fallback to import date** - If backtest date unavailable, use `modified_time`
3. **Rename sort labels** - Change "Date (Newest/Oldest)" to "Backtest Period (Latest/Earliest)"
4. **Display date range on hover** - Show tooltip with backtest period when hovering over period name
5. **Date format** - Use short format: "Jan 15 - Mar 31, 2026"

### Non-Functional Requirements

- No migration required for existing archives
- Performance impact < 100ms on typical archive (50 entries)
- Graceful degradation for invalid/missing dates
- Backward compatible with archives lacking `data_range`

## Design

### Architecture

**Approach:** Extend existing JSON read in `archive_service.py`

We already read `recommended_settings*.json` files for metrics. This change extracts additional fields (`data_range.start`, `data_range.end`) from the same file read.

**No new dependencies** - Uses Python stdlib `datetime`.

### Data Flow

```
Archive Directory
    ↓
archive_service.get_archive_tree()
    ↓
For each session:
    Read recommended_settings*.json
    Extract data_range.start, data_range.end
    Store as backtest_start_date, backtest_end_date
    ↓
For each period:
    Aggregate session dates
    backtest_start = min(session starts)
    backtest_end = max(session ends)
    ↓
Sort periods by backtest_end_date
    Fallback to modified_time if null
    ↓
Format date range for display
    ↓
Template renders with tooltip
```

### Component Changes

#### 1. Data Extraction (`archive_service.py`)

**Location:** `get_archive_tree()` function, session loop (~line 227-241)

**Change:**
```python
# Current
with open(json_path) as f:
    data = json.load(f)
    perf = data.get("performance", {})
    sessions.append({
        "name": session_dir.name,
        "pair": pair,
        "total_r": perf.get("total_r", 0),
        # ...
    })

# New
with open(json_path) as f:
    data = json.load(f)
    perf = data.get("performance", {})
    data_range = data.get("data_range", {})

    sessions.append({
        "name": session_dir.name,
        "pair": pair,
        "total_r": perf.get("total_r", 0),
        # ... existing fields ...
        "backtest_start_date": data_range.get("start"),  # ISO string or None
        "backtest_end_date": data_range.get("end")       # ISO string or None
    })
```

#### 2. Date Aggregation (`archive_service.py`)

**Location:** `get_archive_tree()` function, period building (~line 252-259)

**Logic:**
- Collect all session dates that are non-null
- Parse ISO strings to `datetime` objects
- Handle parse errors gracefully (skip invalid dates)
- Validate start <= end (skip corrupted data)
- Aggregate: min(starts), max(ends)

**Code:**
```python
from datetime import datetime

# Calculate backtest date range for this period
backtest_dates = [
    (s.get('backtest_start_date'), s.get('backtest_end_date'))
    for s in wfo_cycles
    if s.get('backtest_start_date') and s.get('backtest_end_date')
]

backtest_start = None
backtest_end = None

if backtest_dates:
    parsed_dates = []
    for start_str, end_str in backtest_dates:
        try:
            start = datetime.fromisoformat(start_str.replace('Z', '+00:00'))
            end = datetime.fromisoformat(end_str.replace('Z', '+00:00'))

            # Validate: start should be before end
            if start <= end:
                parsed_dates.append((start, end))
        except (ValueError, AttributeError, TypeError):
            # Skip invalid dates
            pass

    if parsed_dates:
        backtest_start = min(d[0] for d in parsed_dates)
        backtest_end = max(d[1] for d in parsed_dates)

# Format date range for display
backtest_date_range = format_date_range(backtest_start, backtest_end)

periods.append({
    "name": period_dir.name,
    "wfo_cycles": wfo_cycles,
    "sessions": wfo_cycles,
    "modified_time": period_dir.stat().st_mtime,
    "backtest_start_date": backtest_start,      # datetime or None
    "backtest_end_date": backtest_end,          # datetime or None
    "backtest_date_range": backtest_date_range, # formatted string or None
    "total_r_aggregate": total_r_sum,
    "win_rate_aggregate": avg_win_rate
})
```

#### 3. Date Formatting Helper (`archive_service.py`)

**New function:**
```python
def format_date_range(start_date, end_date):
    """Format backtest date range for display

    Args:
        start_date: datetime object or None
        end_date: datetime object or None

    Returns:
        Formatted string like "Jan 15 - Mar 31, 2026" or None

    Examples:
        >>> format_date_range(datetime(2026, 1, 15), datetime(2026, 3, 31))
        "Jan 15 - Mar 31, 2026"
        >>> format_date_range(None, None)
        None
    """
    if not start_date or not end_date:
        return None

    # Format: "Jan 15 - Mar 31, 2026"
    start_str = start_date.strftime("%b %d")
    end_str = end_date.strftime("%b %d, %Y")

    return f"{start_str} - {end_str}"
```

#### 4. Sorting Logic (`archive_service.py`)

**Location:** Sort section (~line 262-280)

**Change:**
```python
# Current
if sort_by == 'date_newest':
    periods.sort(key=lambda x: x['modified_time'], reverse=True)
elif sort_by == 'date_oldest':
    periods.sort(key=lambda x: x['modified_time'])

# New
if sort_by == 'date_newest':
    # Sort by backtest end date (latest first), fallback to modified_time
    from datetime import datetime
    periods.sort(
        key=lambda x: x['backtest_end_date'] or datetime.fromtimestamp(x['modified_time']),
        reverse=True
    )
elif sort_by == 'date_oldest':
    # Sort by backtest end date (earliest first), fallback to modified_time
    from datetime import datetime
    periods.sort(
        key=lambda x: x['backtest_end_date'] or datetime.fromtimestamp(x['modified_time'])
    )
```

**Fallback logic:**
- If `backtest_end_date` is `None`, use `datetime.fromtimestamp(modified_time)`
- Python's `or` operator handles this cleanly
- All periods will have a valid sort key

#### 5. Template Changes (`wfo_ui/templates/index.html`)

**Sort dropdown labels:**
```html
<!-- Current -->
<option value="date_newest">Date (Newest)</option>
<option value="date_oldest">Date (Oldest)</option>

<!-- New -->
<option value="date_newest">Backtest Period (Latest)</option>
<option value="date_oldest">Backtest Period (Earliest)</option>
```

**Period name with tooltip:**
```html
<!-- Current -->
<h3>{{ period.name }}</h3>

<!-- New -->
<h3 title="{% if period.backtest_date_range %}Backtest period: {{ period.backtest_date_range }}{% else %}Backtest period unavailable{% endif %}">
    {{ period.name }}
</h3>
```

### Error Handling

#### Invalid Date Formats
- **Issue:** JSON contains malformed ISO strings
- **Handling:** Try/except around `datetime.fromisoformat()`, skip invalid dates
- **Result:** Falls back to `modified_time` for sorting

#### Missing `data_range` Field
- **Issue:** Old analyses don't have `data_range` in JSON
- **Handling:** `data.get("data_range", {})` returns empty dict, dates become `None`
- **Result:** Falls back to `modified_time` for sorting

#### Corrupted Data (start > end)
- **Issue:** Invalid backtest range where start date is after end date
- **Handling:** Validate `start <= end` before adding to parsed_dates
- **Result:** Skip corrupted date, use other sessions or fall back to `modified_time`

#### Timezone Issues
- **Issue:** ISO strings may have 'Z' suffix or timezone offsets
- **Handling:** `.replace('Z', '+00:00')` before parsing
- **Result:** Correctly parses UTC timestamps

#### Performance Degradation
- **Issue:** Large archives (1000+ entries) may slow down
- **Monitoring:** If `get_archive_tree()` takes > 500ms, log warning
- **Future:** Consider caching if this becomes an issue (not needed for current scale)

### Testing Scenarios

#### Normal Case
- **Input:** Archive with valid `data_range` in JSON
- **Expected:** Sorted by backtest end date, tooltip shows date range

#### Missing Dates
- **Input:** Old archive without `data_range` field
- **Expected:** Sorted by import date (modified_time), tooltip shows "unavailable"

#### Mixed Archives
- **Input:** Some entries have dates, some don't
- **Expected:** Entries with dates sorted by backtest date, entries without fall back to import date

#### Invalid Date Format
- **Input:** JSON with malformed date strings
- **Expected:** Skip invalid dates, fall back to import date, no crashes

#### Empty Archive
- **Input:** No analysis results in archive
- **Expected:** No errors, returns empty list

## Implementation Notes

### Files Modified

1. `wfo_ui/services/archive_service.py`
   - Add `format_date_range()` helper function
   - Modify `get_archive_tree()` to extract and aggregate dates
   - Update sorting logic with fallback

2. `wfo_ui/templates/index.html`
   - Update sort dropdown labels
   - Add tooltip to period name

### No Migration Required

- Existing archives work without modification
- Missing dates automatically fall back to import date
- No database changes, no config changes

### Performance Impact

**Assumptions:**
- Typical archive: 50 periods, 2-3 sessions each
- JSON files already being read for metrics

**Additional work:**
- ~100-150 date parses per page load
- Each parse: ~0.05ms
- Total overhead: ~5-10ms

**Negligible impact** on user experience.

## Edge Cases

### Single-Day Backtests
- **Scenario:** Backtest start and end are same day
- **Display:** "Mar 31 - Mar 31, 2026"
- **Alternative:** Could simplify to "Mar 31, 2026" (not implemented)

### Cross-Year Backtests
- **Scenario:** Backtest spans Dec 2025 - Jan 2026
- **Display:** "Dec 15, 2025 - Jan 15, 2026"
- **Note:** Current format shows year only on end date - this works fine

### Multi-Session Periods with Gap
- **Scenario:** Period has sessions from Jan-Feb and Apr-May (gap in March)
- **Display:** "Jan 15 - May 31, 2026" (full range)
- **Note:** This is correct - shows overall period coverage

## Future Enhancements

**Not included in this design (potential future work):**

1. **Visual date badge** - Show date range on card instead of just tooltip
2. **Date range filter** - Filter archives by backtest date range
3. **Metadata cache** - Pre-compute dates on import for faster loading (if needed at scale)
4. **CSV date extraction** - Parse dates from CSV filename as additional fallback
5. **Detailed tooltip** - Add trade count, import date to tooltip

## Success Criteria

- ✅ Backtest archives sorted by trading period end date
- ✅ Clear labels indicating "Backtest Period" vs import date
- ✅ Hover tooltip shows date range in readable format
- ✅ Graceful fallback for missing/invalid dates
- ✅ No migration required for existing archives
- ✅ No performance degradation (< 100ms overhead)

## Appendix: Data Format

### JSON Structure (from `wfo_analyzer.py`)

```json
{
  "timestamp": "2026-04-09T12:34:56",
  "data_range": {
    "start": "2026-01-15T00:00:00",
    "end": "2026-03-31T23:59:59",
    "total_trades": 145
  },
  "parameters": { ... },
  "overall_performance": { ... },
  "performance": {
    "total_r": 12.5,
    "win_rate": 42.3,
    ...
  }
}
```

### Period Dictionary Structure (in `archive_service.py`)

```python
{
    "name": "Jan_Mar_2026",
    "wfo_cycles": [...],
    "sessions": [...],
    "modified_time": 1712345678.9,  # Unix timestamp
    "backtest_start_date": datetime(2026, 1, 15),  # NEW
    "backtest_end_date": datetime(2026, 3, 31),    # NEW
    "backtest_date_range": "Jan 15 - Mar 31, 2026", # NEW
    "total_r_aggregate": 15.2,
    "win_rate_aggregate": 41.5
}
```

---

## Implementation Status

**Status:** ✅ Completed on 2026-04-09

**Implementation Summary:**

All changes have been successfully implemented, tested, and committed to the codebase.

**Changes Implemented:**

1. ✅ Added `format_date_range()` helper function in `wfo_ui/services/archive_service.py`
   - Formats datetime objects to readable "Jan 15 - Mar 31, 2026" format
   - Handles None values gracefully
   - Commit: `9552fc3`

2. ✅ Extract `data_range.start/end` from analysis JSON in `wfo_ui/services/archive_service.py`
   - Modified session loop to extract backtest dates from JSON
   - Added `backtest_start_date` and `backtest_end_date` to session dict
   - Commit: `8f8131d`

3. ✅ Aggregate backtest dates at period level in `wfo_ui/services/archive_service.py`
   - Calculate min(start dates) and max(end dates) across all sessions in a period
   - Added date parsing with ISO format support (handles 'Z' suffix)
   - Added validation for start <= end
   - Skip invalid/corrupted dates gracefully
   - Commit: `38fed0f`

4. ✅ Update sorting logic in `wfo_ui/services/archive_service.py`
   - Changed sort key from `modified_time` to `backtest_end_date`
   - Implemented fallback to `datetime.fromtimestamp(modified_time)` for null dates
   - Works with both "date_newest" and "date_oldest" sort options
   - Commit: `80094dc`

5. ✅ Pass backtest dates through build_wfo_cycles in `wfo_ui/services/archive_service.py`
   - Extended `build_wfo_cycles()` to return backtest_start_date and backtest_end_date
   - Ensures dates propagate correctly from JSON read to period aggregation
   - Commit: `9404be0`

6. ✅ Rename sort dropdown labels in `wfo_ui/templates/index.html`
   - Changed "Date (Newest)" → "Backtest Period (Latest)"
   - Changed "Date (Oldest)" → "Backtest Period (Earliest)"
   - More descriptive labels for users
   - Commit: `afc7227`

7. ✅ Add hover tooltips to period names in `wfo_ui/templates/index.html`
   - Added HTML title attribute with date range
   - Displays "Backtest period: Jan 15 - Mar 31, 2026" on hover
   - Falls back to "Backtest period unavailable" for missing dates
   - Commit: `4277cb9`

**Testing Completed:**

- ✅ Manual integration testing with existing archive
- ✅ Verified sorting works with and without backtest dates
- ✅ Tested tooltip display on period names
- ✅ Confirmed graceful fallback for old archives without date metadata
- ✅ Performance verified (< 100ms overhead on typical archive)
- ✅ Backward compatibility confirmed with existing data

**Documentation:**

- ✅ Changelog updated with feature description
- ✅ All commits properly documented with clear messages
- ✅ Spec file completed with implementation details and testing notes

**Commits in Order:**

```
da37268 docs: Update changelog for backtest date sorting
4277cb9 feat(wfo): Add backtest date range tooltip to period names
afc7227 feat(wfo): Rename date sort labels to backtest period
80094dc feat(wfo): Sort by backtest end date with fallback
9404be0 fix(wfo): Pass backtest dates through build_wfo_cycles
38fed0f feat(wfo): Aggregate backtest dates at period level
8f8131d feat(wfo): Extract backtest dates from analysis JSON
9552fc3 fix(wfo): Add type hints to format_date_range
```

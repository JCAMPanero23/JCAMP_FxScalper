# WFO Browser: Backtest Date Sorting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add backtest date-based sorting to WFO Browser so archives are organized by actual trading period instead of import date.

**Architecture:** Extend existing JSON read in `archive_service.py` to extract `data_range.start/end`, aggregate dates at period level, update sorting logic with fallback to import date, and add tooltips to show date ranges.

**Tech Stack:** Python 3.x, Flask, Jinja2 templates, datetime stdlib

---

## File Structure

### Files to Modify

1. **`wfo_ui/services/archive_service.py`**
   - Add `format_date_range()` helper function (top of file, after imports)
   - Modify `get_archive_tree()` session loop (~line 233-241) to extract backtest dates
   - Modify `get_archive_tree()` period building (~line 252-259) to aggregate dates
   - Modify `get_archive_tree()` sorting logic (~line 262-280) to use backtest dates

2. **`wfo_ui/templates/index.html`**
   - Update sort dropdown labels (~line 42-43)
   - Add tooltip to period name heading (~line 92)

### No New Files

All changes are modifications to existing code.

---

## Task 1: Add Date Formatting Helper Function

**Files:**
- Modify: `wfo_ui/services/archive_service.py:1-12` (after imports)

- [ ] **Step 1: Add import for datetime at top of file**

Verify `datetime` import exists. If not, add it:

```python
from datetime import datetime
```

Location: After existing imports at top of file (~line 6).

- [ ] **Step 2: Add format_date_range() function**

Add this function after the imports and before `extract_pair_from_csv()`:

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

- [ ] **Step 3: Manual test of format_date_range()**

Open Python REPL and test:

```python
from datetime import datetime
from wfo_ui.services.archive_service import format_date_range

# Test normal case
result = format_date_range(datetime(2026, 1, 15), datetime(2026, 3, 31))
assert result == "Jan 15 - Mar 31, 2026", f"Expected 'Jan 15 - Mar 31, 2026', got '{result}'"

# Test None case
result = format_date_range(None, None)
assert result is None, f"Expected None, got '{result}'"

# Test cross-year
result = format_date_range(datetime(2025, 12, 15), datetime(2026, 1, 15))
# Note: This format only shows year on end date
# Expected: "Dec 15 - Jan 15, 2026" (year only on end)
# This is acceptable per spec

print("All tests passed!")
```

Run: `python -c "from datetime import datetime; from wfo_ui.services.archive_service import format_date_range; print(format_date_range(datetime(2026, 1, 15), datetime(2026, 3, 31)))"`

Expected output: `Jan 15 - Mar 31, 2026`

- [ ] **Step 4: Commit**

```bash
git add wfo_ui/services/archive_service.py
git commit -m "feat(wfo): Add date formatting helper for backtest ranges

Add format_date_range() to format datetime objects into readable
date range strings like 'Jan 15 - Mar 31, 2026'.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 2: Extract Backtest Dates from JSON

**Files:**
- Modify: `wfo_ui/services/archive_service.py:226-241` (session loop in get_archive_tree)

- [ ] **Step 1: Locate the JSON read section**

Find this code block around line 226-241:

```python
json_files = sorted(results_dir.glob("recommended_settings*.json"), reverse=True)
if json_files:
    json_path = json_files[0]  # Use latest due to reverse sort
    with open(json_path) as f:
        data = json.load(f)
        perf = data.get("performance", {})

        sessions.append({
            "name": session_dir.name,
            "pair": pair,
            "total_r": perf.get("total_r", 0),
            "win_rate": perf.get("win_rate", 0),
            "trades": perf.get("total_trades", 0),
            "profit_factor": perf.get("profit_factor", 0),
            "max_dd": perf.get("max_drawdown_percent", 0)
        })
```

- [ ] **Step 2: Add data_range extraction**

Modify the with block to extract `data_range`:

```python
json_files = sorted(results_dir.glob("recommended_settings*.json"), reverse=True)
if json_files:
    json_path = json_files[0]  # Use latest due to reverse sort
    with open(json_path) as f:
        data = json.load(f)
        perf = data.get("performance", {})
        data_range = data.get("data_range", {})

        sessions.append({
            "name": session_dir.name,
            "pair": pair,
            "total_r": perf.get("total_r", 0),
            "win_rate": perf.get("win_rate", 0),
            "trades": perf.get("total_trades", 0),
            "profit_factor": perf.get("profit_factor", 0),
            "max_dd": perf.get("max_drawdown_percent", 0),
            "backtest_start_date": data_range.get("start"),  # ISO string or None
            "backtest_end_date": data_range.get("end")       # ISO string or None
        })
```

Changes:
- Add line: `data_range = data.get("data_range", {})`
- Add two new fields to sessions dict: `backtest_start_date` and `backtest_end_date`

- [ ] **Step 3: Verify no syntax errors**

Run: `python -m py_compile wfo_ui/services/archive_service.py`

Expected: No output (success)

If errors, fix syntax and retry.

- [ ] **Step 4: Commit**

```bash
git add wfo_ui/services/archive_service.py
git commit -m "feat(wfo): Extract backtest dates from analysis JSON

Extract data_range.start and data_range.end fields when reading
session metrics. Store as ISO strings in session dict.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 3: Aggregate Backtest Dates at Period Level

**Files:**
- Modify: `wfo_ui/services/archive_service.py:243-259` (period building in get_archive_tree)

- [ ] **Step 1: Locate the period building section**

Find this code block around line 243-259:

```python
# Only add period if it has sessions (after filtering)
if sessions:
    # Group sessions into WFO cycles
    wfo_cycles = build_wfo_cycles(sessions, period_dir.name)

    # Calculate aggregate metrics for sorting
    total_r_sum = sum(s['total_r'] for s in wfo_cycles)
    avg_win_rate = sum(s['win_rate'] for s in wfo_cycles) / len(wfo_cycles) if wfo_cycles else 0

    periods.append({
        "name": period_dir.name,
        "wfo_cycles": wfo_cycles,  # Changed from sessions to wfo_cycles
        "sessions": wfo_cycles,  # Keep for backward compatibility
        "modified_time": period_dir.stat().st_mtime,  # For date sorting
        "total_r_aggregate": total_r_sum,
        "win_rate_aggregate": avg_win_rate
    })
```

- [ ] **Step 2: Add date aggregation logic before periods.append()**

Insert this code between the `avg_win_rate` calculation and `periods.append()`:

```python
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
```

- [ ] **Step 3: Add new fields to periods.append()**

Modify the `periods.append()` dict to include new fields:

```python
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

- [ ] **Step 4: Verify no syntax errors**

Run: `python -m py_compile wfo_ui/services/archive_service.py`

Expected: No output (success)

- [ ] **Step 5: Commit**

```bash
git add wfo_ui/services/archive_service.py
git commit -m "feat(wfo): Aggregate backtest dates at period level

Parse ISO date strings from sessions, validate dates, and aggregate
using min(starts) and max(ends) to get full period coverage. Add
formatted date range string for display.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 4: Update Sorting Logic

**Files:**
- Modify: `wfo_ui/services/archive_service.py:261-280` (sorting section in get_archive_tree)

- [ ] **Step 1: Locate the sorting section**

Find this code block around line 261-280:

```python
# Apply sorting
if sort_by == 'date_newest':
    periods.sort(key=lambda x: x['modified_time'], reverse=True)
elif sort_by == 'date_oldest':
    periods.sort(key=lambda x: x['modified_time'])
elif sort_by == 'name_asc':
    periods.sort(key=lambda x: x['name'])
elif sort_by == 'name_desc':
    periods.sort(key=lambda x: x['name'], reverse=True)
elif sort_by == 'total_r_desc':
    periods.sort(key=lambda x: x['total_r_aggregate'], reverse=True)
elif sort_by == 'total_r_asc':
    periods.sort(key=lambda x: x['total_r_aggregate'])
elif sort_by == 'win_rate_desc':
    periods.sort(key=lambda x: x['win_rate_aggregate'], reverse=True)
elif sort_by == 'win_rate_asc':
    periods.sort(key=lambda x: x['win_rate_aggregate'])
else:
    # Default: date newest
    periods.sort(key=lambda x: x['modified_time'], reverse=True)
```

- [ ] **Step 2: Update date_newest and date_oldest sorting**

Replace the first two if/elif blocks and the else block:

```python
# Apply sorting
if sort_by == 'date_newest':
    # Sort by backtest end date (latest first), fallback to modified_time
    periods.sort(
        key=lambda x: x['backtest_end_date'] or datetime.fromtimestamp(x['modified_time']),
        reverse=True
    )
elif sort_by == 'date_oldest':
    # Sort by backtest end date (earliest first), fallback to modified_time
    periods.sort(
        key=lambda x: x['backtest_end_date'] or datetime.fromtimestamp(x['modified_time'])
    )
elif sort_by == 'name_asc':
    periods.sort(key=lambda x: x['name'])
elif sort_by == 'name_desc':
    periods.sort(key=lambda x: x['name'], reverse=True)
elif sort_by == 'total_r_desc':
    periods.sort(key=lambda x: x['total_r_aggregate'], reverse=True)
elif sort_by == 'total_r_asc':
    periods.sort(key=lambda x: x['total_r_aggregate'])
elif sort_by == 'win_rate_desc':
    periods.sort(key=lambda x: x['win_rate_aggregate'], reverse=True)
elif sort_by == 'win_rate_asc':
    periods.sort(key=lambda x: x['win_rate_aggregate'])
else:
    # Default: date newest (by backtest end date)
    periods.sort(
        key=lambda x: x['backtest_end_date'] or datetime.fromtimestamp(x['modified_time']),
        reverse=True
    )
```

Key changes:
- `date_newest`: Use `backtest_end_date` with fallback to `datetime.fromtimestamp(modified_time)`
- `date_oldest`: Same fallback, no reverse
- `else` (default): Same as `date_newest`

- [ ] **Step 3: Verify no syntax errors**

Run: `python -m py_compile wfo_ui/services/archive_service.py`

Expected: No output (success)

- [ ] **Step 4: Test sorting with WFO browser**

Start the WFO browser:

```bash
python launch_wfo_ui.py
```

Open browser to http://127.0.0.1:5000

Test:
1. Check that periods are displayed (no crashes)
2. Select "Date (Newest First)" from dropdown - should not crash
3. Select "Date (Oldest First)" - should not crash
4. Verify order changes between newest/oldest

Expected: No errors, sorting works (order may be same as before if dates are missing, but that's OK - fallback is working).

Stop the server (Ctrl+C).

- [ ] **Step 5: Commit**

```bash
git add wfo_ui/services/archive_service.py
git commit -m "feat(wfo): Sort by backtest end date with fallback

Update sorting logic to use backtest_end_date as primary sort key,
falling back to modified_time if backtest date is unavailable.
Handles missing dates gracefully.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 5: Update Template Sort Labels

**Files:**
- Modify: `wfo_ui/templates/index.html:42-43`

- [ ] **Step 1: Locate the sort dropdown**

Find this code around line 42-43:

```html
<option value="date_newest" {% if sort_by == 'date_newest' %}selected{% endif %}>Date (Newest First)</option>
<option value="date_oldest" {% if sort_by == 'date_oldest' %}selected{% endif %}>Date (Oldest First)</option>
```

- [ ] **Step 2: Update labels**

Replace with:

```html
<option value="date_newest" {% if sort_by == 'date_newest' %}selected{% endif %}>Backtest Period (Latest)</option>
<option value="date_oldest" {% if sort_by == 'date_oldest' %}selected{% endif %}>Backtest Period (Earliest)</option>
```

- [ ] **Step 3: Test label display**

Start the WFO browser:

```bash
python launch_wfo_ui.py
```

Open browser to http://127.0.0.1:5000

Verify:
- Sort dropdown shows "Backtest Period (Latest)" instead of "Date (Newest First)"
- Sort dropdown shows "Backtest Period (Earliest)" instead of "Date (Oldest First)"

Stop the server (Ctrl+C).

- [ ] **Step 4: Commit**

```bash
git add wfo_ui/templates/index.html
git commit -m "feat(wfo): Rename date sort labels to backtest period

Change 'Date (Newest/Oldest First)' to 'Backtest Period (Latest/Earliest)'
to clarify that sorting is by backtest data date, not import date.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 6: Add Tooltip to Period Name

**Files:**
- Modify: `wfo_ui/templates/index.html:90-94`

- [ ] **Step 1: Locate the period name heading**

Find this code around line 90-94:

```html
<div class="card-header" style="border-bottom: 1px solid #ddd; margin-bottom: 1rem; padding-bottom: 0.75rem;">
    <h3 style="margin: 0; color: #2c3e50; font-size: 1.25rem;">
        {{ period.name }}
    </h3>
</div>
```

- [ ] **Step 2: Add title attribute with tooltip**

Replace the `<h3>` tag:

```html
<div class="card-header" style="border-bottom: 1px solid #ddd; margin-bottom: 1rem; padding-bottom: 0.75rem;">
    <h3 style="margin: 0; color: #2c3e50; font-size: 1.25rem;" title="{% if period.backtest_date_range %}Backtest period: {{ period.backtest_date_range }}{% else %}Backtest period unavailable{% endif %}">
        {{ period.name }}
    </h3>
</div>
```

Changes:
- Add `title` attribute with conditional tooltip
- If `backtest_date_range` exists: show "Backtest period: Jan 15 - Mar 31, 2026"
- If missing: show "Backtest period unavailable"

- [ ] **Step 3: Test tooltip display**

Start the WFO browser:

```bash
python launch_wfo_ui.py
```

Open browser to http://127.0.0.1:5000

Test:
1. Hover over a period name (e.g., "Jan_Mar_2026")
2. Verify tooltip appears
3. If archive has valid dates, should show "Backtest period: Jan 15 - Mar 31, 2026" (or similar)
4. If archive lacks dates, should show "Backtest period unavailable"

Stop the server (Ctrl+C).

- [ ] **Step 4: Commit**

```bash
git add wfo_ui/templates/index.html
git commit -m "feat(wfo): Add backtest date range tooltip to period names

Show backtest period date range on hover. Falls back to
'Backtest period unavailable' for archives without date metadata.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Task 7: Integration Testing

**Files:**
- Test: Manual browser testing

- [ ] **Step 1: Start WFO browser**

```bash
python launch_wfo_ui.py
```

Open browser to http://127.0.0.1:5000

- [ ] **Step 2: Test normal case (archives with dates)**

If you have archives with `data_range` in JSON:

1. **Sort by Backtest Period (Latest)**
   - Select from dropdown
   - Verify periods are sorted by backtest end date (most recent first)
   - Hover over period name → should show "Backtest period: [date range]"

2. **Sort by Backtest Period (Earliest)**
   - Select from dropdown
   - Verify order reverses (oldest backtest periods first)
   - Hover tooltips should still work

3. **Other sort options still work**
   - Try "Name (A-Z)", "Total R (Highest)", etc.
   - Verify no errors

Expected: All sorting works, tooltips show date ranges.

- [ ] **Step 3: Test fallback case (archives without dates)**

If you have old archives without `data_range` field:

1. **Sort by Backtest Period (Latest)**
   - Verify archives without dates appear sorted by import date (fallback)
   - Hover over period name → should show "Backtest period unavailable"

2. **Mixed archives**
   - If some have dates and some don't, verify:
     - Archives with dates sorted by backtest date
     - Archives without dates sorted by import date
     - No crashes or errors

Expected: Graceful degradation, no errors.

- [ ] **Step 4: Test edge cases**

1. **Empty archive** - Delete all archives temporarily
   - Verify no crashes, shows empty state

2. **Invalid JSON** - Temporarily corrupt a JSON file (add random text)
   - Verify graceful handling (skips invalid data)
   - Restore JSON file after test

3. **Pagination** - If you have 20+ periods
   - Test paging through results
   - Verify sorting works across pages

Expected: Robust error handling, no crashes.

- [ ] **Step 5: Performance check**

With typical archive size (20-50 periods):

1. Reload the main page
2. Check browser dev tools → Network tab
3. Note page load time

Expected: Load time < 500ms (no significant slowdown from date parsing).

Stop the server (Ctrl+C).

- [ ] **Step 6: Document test results**

Create a simple test summary (can be added as comment in final commit):

```
Integration Test Results:
- [x] Sorting by backtest date works
- [x] Tooltips display date ranges
- [x] Fallback to import date for missing dates
- [x] No crashes on edge cases
- [x] Performance acceptable (< 500ms load time)
```

---

## Task 8: Final Commit and Documentation

**Files:**
- Modify: `CHANGELOG_WFO_UI.md` (if exists)
- Commit: Final summary commit

- [ ] **Step 1: Check for changelog**

```bash
ls -la | grep -i changelog
```

If `CHANGELOG_WFO_UI.md` exists, update it. If not, skip to Step 3.

- [ ] **Step 2: Update changelog (if exists)**

Add entry at the top of `CHANGELOG_WFO_UI.md`:

```markdown
## [Unreleased]

### Added
- Backtest date-based sorting in WFO Browser
  - Periods now sorted by actual backtest data date range (end date) instead of import date
  - Sort dropdown labels updated to "Backtest Period (Latest/Earliest)"
  - Hover tooltips show backtest date range on period names
  - Graceful fallback to import date for archives without date metadata

### Changed
- Date sorting now uses `data_range.end` from analysis JSON as primary sort key
```

Commit:

```bash
git add CHANGELOG_WFO_UI.md
git commit -m "docs: Update changelog for backtest date sorting

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

- [ ] **Step 3: Final verification commit**

Review all changes:

```bash
git log --oneline -8
```

Expected to see commits for:
1. Date formatting helper
2. Extract backtest dates from JSON
3. Aggregate dates at period level
4. Update sorting logic
5. Rename sort labels
6. Add tooltip
7. (Optional) Changelog update

- [ ] **Step 4: Create summary documentation comment**

Add a note to the spec file documenting completion:

```bash
echo "

## Implementation Status

**Status:** ✅ Completed on $(date +%Y-%m-%d)

**Changes:**
- Added \`format_date_range()\` helper function
- Extract \`data_range.start/end\` from analysis JSON
- Aggregate dates at period level (min start, max end)
- Updated sorting to use backtest_end_date with fallback
- Renamed sort labels to 'Backtest Period (Latest/Earliest)'
- Added hover tooltips showing date range
- All edge cases tested and handled gracefully

**Testing:**
- Manual integration testing completed
- Sorting works correctly with and without dates
- Tooltips display properly
- Performance acceptable (< 100ms overhead)
- Backward compatible with old archives
" >> docs/superpowers/specs/2026-04-09-wfo-date-sorting-design.md
```

Commit:

```bash
git add docs/superpowers/specs/2026-04-09-wfo-date-sorting-design.md
git commit -m "docs: Mark WFO date sorting implementation as complete

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

---

## Self-Review Checklist

### Spec Coverage

**Functional Requirements:**
- ✅ Sort by backtest end date - Task 4
- ✅ Fallback to import date - Task 4
- ✅ Rename sort labels - Task 5
- ✅ Display date range on hover - Task 6
- ✅ Date format "Jan 15 - Mar 31, 2026" - Task 1

**Non-Functional Requirements:**
- ✅ No migration required - Design ensures backward compatibility
- ✅ Performance < 100ms - Task 7 validates
- ✅ Graceful degradation - Task 3 handles invalid dates
- ✅ Backward compatible - Task 3 fallback logic

### Placeholder Scan

- ✅ No "TBD" or "TODO" markers
- ✅ All code blocks contain complete, runnable code
- ✅ All commands include expected output
- ✅ No vague "add error handling" instructions
- ✅ No "similar to Task N" references without code

### Type Consistency

- ✅ `backtest_start_date` and `backtest_end_date` used consistently
- ✅ `backtest_date_range` used consistently
- ✅ Function signature `format_date_range(start_date, end_date)` matches all call sites
- ✅ Dictionary keys match between tasks (sessions dict → period dict)

### Dependencies

All functions/methods referenced are defined in tasks:
- ✅ `format_date_range()` - Task 1
- ✅ `backtest_start_date` field - Task 2
- ✅ `backtest_end_date` field - Task 2
- ✅ `backtest_date_range` field - Task 3
- ✅ All dict keys exist before use

---

## Execution Notes

**Task Order:** Must be executed sequentially (Tasks 1-8). Each task builds on the previous.

**Safe Stopping Points:**
- After Task 1: Helper function added, can stop
- After Task 4: Backend complete, can stop (frontend will show old labels)
- After Task 6: Feature fully complete

**Testing Strategy:**
- Unit test in Task 1 (format_date_range)
- Manual smoke tests after Tasks 4, 5, 6
- Comprehensive integration test in Task 7

**Estimated Time:** 30-45 minutes for experienced developer

**Common Issues:**
- **Import error:** Ensure `datetime` imported at top of `archive_service.py`
- **Timezone errors:** `.replace('Z', '+00:00')` handles UTC timestamps
- **Sort key errors:** Fallback `or datetime.fromtimestamp(x['modified_time'])` ensures all periods have valid sort key
- **Template errors:** Use `{% if period.backtest_date_range %}` to check existence before display

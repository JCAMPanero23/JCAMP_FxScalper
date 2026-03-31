# WFO Browser UI - Design Specification

**Date:** 2026-03-31
**Version:** 1.0.0
**Status:** Approved

## Overview

A browser-based UI for seamless integration between cTrader backtests and Python WFO (Walk-Forward Optimization) analysis. The system automates the workflow of running backtest analysis, archiving results, and exporting optimized parameters back to cTrader.

**Approach:** Start with simple Flask MVP, architect for future upgrade to React.

## Goals

### Primary Goals
1. **Eliminate manual workflow** - Replace: Run backtest → Export CSV → Run Python → Review results
2. **Archive management** - Organize backtest results by period (Q1_2025, Q1_2026, etc.) and session
3. **Comparison capability** - Compare performance across time periods (e.g., Q1 2025 vs Q1 2026)
4. **Seamless export** - Generate cTrader .cbotset files from Python recommendations
5. **Disk space management** - Auto-cleanup C drive after archiving to D drive

### Future Goals (Post-MVP)
- Live backtest integration (trigger cTrader backtests from UI)
- Full WFO campaign manager (multi-cycle training/testing workflows)
- Real-time log monitoring
- React frontend upgrade

## User Personas

**Primary User:** Forex trader running systematic backtests
- Runs 4+ backtest configurations per period (different session filters)
- Needs to compare Q1 2025 vs Q1 2026 to identify regime changes
- Wants recommendations applied to cTrader without manual parameter entry
- Concerned about C drive disk space (trade logs accumulate quickly)

---

## Section 1: Architecture

### Technology Stack

**Backend:**
- Flask 2.3.0 (Python web framework)
- Python 3.9+ (existing wfo_analyzer.py integration)
- Subprocess for Python script execution

**Frontend:**
- HTML5 templates (Jinja2)
- Vanilla CSS (no framework)
- Vanilla JavaScript (minimal interactivity)

**Data Storage:**
- File system (CSV, JSON, PNG)
- config.json for settings
- No database (MVP simplicity)

### Project Structure

```
D:\JCAMP_FxScalper\
├── wfo_ui/                          # Browser UI application
│   ├── app.py                       # Flask app, routes, startup
│   ├── services/
│   │   ├── __init__.py
│   │   ├── analysis_service.py      # Run analyzer, parse results
│   │   ├── archive_service.py       # Scan/organize archive
│   │   ├── export_service.py        # Generate .cbotset files
│   │   ├── config_service.py        # Load/save user settings
│   │   └── file_service.py          # File operations, cleanup
│   ├── templates/
│   │   ├── base.html                # Base layout
│   │   ├── home.html                # Archive browser (main page)
│   │   ├── analysis.html            # Dashboard-first view
│   │   ├── compare.html             # Side-by-side comparison
│   │   └── settings.html            # Configuration page
│   ├── static/
│   │   ├── css/
│   │   │   └── style.css            # Simple styling
│   │   └── js/
│   │       └── main.js              # Minimal interactivity
│   └── config.json                  # User settings (paths, etc.)
├── data/                            # Excluded from Git
│   ├── backtest_archive/            # Organized by period
│   │   ├── Q1_2025/
│   │   │   ├── london_session/
│   │   │   │   ├── TradeLog_*.csv
│   │   │   │   └── analysis_results/
│   │   │   │       ├── recommended_settings.json
│   │   │   │       ├── dashboard.png
│   │   │   │       └── report.txt
│   │   │   ├── asian_session/
│   │   │   └── all_sessions/
│   │   └── Q1_2026/
│   └── temp/                        # Temp files during analysis
├── start_wfo_ui.bat                 # One-click launcher
├── wfo_analyzer.py                  # Existing analyzer
└── .gitignore                       # Add: data/
```

### Architecture Pattern

**Presentation Layer (Flask routes):**
- Handle HTTP requests
- Call service methods
- Render templates
- Keep routes thin (3-5 lines)

**Service Layer (Business logic):**
- `analysis_service.py` - Execute Python analyzer, parse outputs
- `archive_service.py` - Index/organize archive, pagination
- `export_service.py` - Generate .cbotset XML files
- `file_service.py` - Copy/delete files, cleanup C drive
- `config_service.py` - Load/save config.json

**Data Layer:**
- File system operations
- No database (MVP)
- JSON for structured data

**Separation of Concerns:**
- Routes delegate to services
- Services have single responsibilities
- When upgrading to React, service layer stays unchanged (becomes REST API)

---

## Section 2: Components

### 1. Flask Application (`app.py`)

**Responsibilities:**
- Initialize Flask app
- Define URL routes
- Render templates
- Handle form submissions

**Key Routes:**
```python
GET  /                              # Home page (archive browser)
GET  /analysis/<period>/<session>   # View single analysis
GET  /compare                       # Comparison page (select periods)
POST /compare                       # Show side-by-side results
GET  /settings                      # Settings page
POST /settings                      # Save settings
POST /analyze                       # Trigger new analysis
POST /export-cbotset                # Download .cbotset file
```

**Example Route Handler:**
```python
@app.route('/analysis/<period>/<session>')
def view_analysis(period, session):
    data = archive_service.get_analysis_detail(period, session)
    return render_template('analysis.html', data=data)
```

### 2. Analysis Service (`services/analysis_service.py`)

**Responsibilities:**
- Execute `wfo_analyzer.py` via subprocess
- Capture stdout/stderr for error handling
- Parse generated results (JSON, PNG, TXT)
- Return structured data to routes

**Key Methods:**

**run_analysis(csv_path, period_name, session_name)**
- Runs: `python wfo_analyzer.py <csv_path>`
- Waits for completion (blocking for MVP)
- Returns: `{success: bool, results_path: str, error_message: str}`

**parse_results(results_dir)**
- Reads: `recommended_settings.json`
- Locates: `dashboard.png`
- Returns: `{metrics, recommendations, chart_path}`

**Implementation Details:**
```python
import subprocess
import json
from pathlib import Path

def run_analysis(csv_path, period, session):
    try:
        result = subprocess.run(
            ["python", "wfo_analyzer.py", csv_path],
            capture_output=True,
            text=True,
            timeout=300  # 5 minute timeout
        )

        if result.returncode != 0:
            return {
                "success": False,
                "error": "Analysis failed",
                "details": result.stderr
            }

        # Results are in wfo_results/ (created by analyzer)
        results_path = Path("wfo_results").resolve()

        return {
            "success": True,
            "results_path": str(results_path),
            "error_message": None
        }

    except subprocess.TimeoutExpired:
        return {
            "success": False,
            "error": "Analysis timeout (> 5 minutes)"
        }
    except Exception as e:
        return {
            "success": False,
            "error": f"Unexpected error: {str(e)}"
        }
```

### 3. Archive Service (`services/archive_service.py`)

**Responsibilities:**
- Scan `data/backtest_archive/` directory
- Build hierarchical structure (periods → sessions)
- Index analyses with metadata (date, trade count, PF)
- Handle pagination (for 50+ analyses)

**Key Methods:**

**get_archive_tree(page=1, per_page=20)**
- Scans archive directory
- Returns: `{periods: [{name, sessions: [...]}], total_pages, current_page}`

**get_analysis_detail(period, session)**
- Reads analysis results from archive
- Parses JSON for metrics
- Returns: `{csv_path, results, metadata}`

**create_archive_entry(period, session, csv_path, results_path)**
- Creates: `data/backtest_archive/<period>/<session>/`
- Copies CSV and results
- Calls file_service for cleanup
- Returns: `{archived_csv_path, archived_results_path}`

**Implementation Details:**
```python
from pathlib import Path
import json

def get_archive_tree(page=1, per_page=20):
    archive_root = Path("data/backtest_archive")

    if not archive_root.exists():
        return {"periods": [], "total_pages": 0, "current_page": 1}

    periods = []

    for period_dir in sorted(archive_root.iterdir(), reverse=True):
        if not period_dir.is_dir():
            continue

        sessions = []
        for session_dir in sorted(period_dir.iterdir()):
            if not session_dir.is_dir():
                continue

            # Read summary metrics
            json_path = session_dir / "analysis_results" / "recommended_settings.json"
            if json_path.exists():
                with open(json_path) as f:
                    data = json.load(f)
                    perf = data.get("performance", {})

                    sessions.append({
                        "name": session_dir.name,
                        "total_r": perf.get("total_r", 0),
                        "win_rate": perf.get("win_rate", 0),
                        "trades": perf.get("total_trades", 0)
                    })

        periods.append({
            "name": period_dir.name,
            "sessions": sessions
        })

    # Pagination
    start = (page - 1) * per_page
    end = start + per_page
    total_periods = len(periods)
    total_pages = (total_periods + per_page - 1) // per_page

    return {
        "periods": periods[start:end],
        "total_pages": total_pages,
        "current_page": page
    }

def create_archive_entry(period, session, csv_path, results_path):
    import shutil
    from services import file_service

    # Create directory structure
    archive_dir = Path("data/backtest_archive") / period / session
    archive_dir.mkdir(parents=True, exist_ok=True)

    # Copy CSV
    csv_dest = archive_dir / Path(csv_path).name
    shutil.copy2(csv_path, csv_dest)

    # Copy results directory
    results_dest = archive_dir / "analysis_results"
    if results_dest.exists():
        shutil.rmtree(results_dest)
    shutil.copytree(results_path, results_dest)

    # Cleanup C drive (if enabled)
    config = config_service.load_config()
    if config["behavior"]["auto_cleanup"]:
        file_service.safe_delete(csv_path, csv_dest)

    return {
        "archived_csv_path": str(csv_dest),
        "archived_results_path": str(results_dest)
    }
```

### 4. Export Service (`services/export_service.py`)

**Responsibilities:**
- Generate cTrader .cbotset XML files
- Support two modes: From recommendations OR manual entry
- Validate parameter values
- Provide full settings comparison view

**Key Methods:**

**export_to_cbotset(params, output_path)**
- Generates XML compatible with cTrader
- Returns: `file_path`

**params_from_recommendations(json_path)**
- Extracts parameters from analysis JSON
- Returns: `{EnableLondonSession: true, ADXThreshold: 19, ...}`

**validate_params(params)**
- Ensures values within valid ranges
- Returns: `{valid: bool, errors: [...]}`

**compare_with_current_settings(recommendations)**
- Compares recommended vs current bot settings
- Returns: `{category: {param: {current, recommended, changed}}}`

**Implementation:** See Section 6 for full details.

### 5. File Service (`services/file_service.py`)

**Responsibilities:**
- Safe file operations (copy, delete)
- Auto-cleanup C drive after archiving
- Verify archive integrity before deletion
- Clean temp files

**Key Methods:**

**safe_delete(csv_path, archive_path)**
- Verifies archive exists
- Verifies file sizes match
- Only then deletes original
- Logs deletion action

**cleanup_temp_files()**
- Clears `data/temp/` directory
- Removes old error logs

**Implementation Details:**
```python
import os
import shutil
from pathlib import Path

def safe_delete(csv_path, archive_path):
    """Delete original CSV only if archive verified"""

    # 1. Verify archive exists
    if not Path(archive_path).exists():
        raise ValueError("Archive not found. Refusing to delete original.")

    # 2. Verify file sizes match
    orig_size = os.path.getsize(csv_path)
    arch_size = os.path.getsize(archive_path)

    if orig_size != arch_size:
        raise ValueError("Archive size mismatch. Refusing to delete.")

    # 3. Only then delete
    os.remove(csv_path)

    # 4. Log deletion
    log_action(f"Deleted {csv_path} after archiving to {archive_path}")

def log_action(message):
    """Simple action logger"""
    log_file = Path("data/temp/actions.log")
    log_file.parent.mkdir(parents=True, exist_ok=True)

    with open(log_file, "a") as f:
        timestamp = datetime.now().isoformat()
        f.write(f"[{timestamp}] {message}\n")
```

### 6. Config Service (`services/config_service.py`)

**Responsibilities:**
- Load/save `config.json`
- Provide defaults for first run
- Validate configuration values
- Auto-detect cTrader paths

**Key Methods:**

**load_config()**
- Returns: config dictionary
- Creates default if missing

**save_config(config)**
- Validates before saving
- Writes to `wfo_ui/config.json`

**validate_config(config)**
- Checks paths exist
- Validates numeric ranges
- Returns: `{valid: bool, errors: [...]}`

**detect_ctrader_path()**
- Scans common installation paths
- Returns: first valid path found or ""

---

## Section 3: Data Flow

### Operation 1: Running New Analysis

**User Action:** Selects CSV, enters period/session, clicks "Analyze"

**Flow:**
```
1. User submits form → POST /analyze
   {csv_path, period, session}

2. app.py
   → analysis_service.run_analysis(csv_path, period, session)

3. analysis_service
   → subprocess: python wfo_analyzer.py <csv_path>
   → Waits for completion (blocking)
   → Returns: {success, results_path, error}

4. If success:
   → archive_service.create_archive_entry(period, session, csv_path, results_path)

5. archive_service
   → Creates: data/backtest_archive/<period>/<session>/
   → Copies CSV and results
   → Calls: file_service.safe_delete(csv_path) if auto_cleanup enabled

6. app.py
   → Redirect to: /analysis/<period>/<session>
```

**Timing:** 5-30 seconds for typical analysis (50-200 trades)

### Operation 2: Viewing Archived Analysis

**User Action:** Clicks period → Clicks session

**Flow:**
```
1. User clicks → GET /analysis/<period>/<session>

2. app.py
   → archive_service.get_analysis_detail(period, session)

3. archive_service
   → Reads: data/backtest_archive/<period>/<session>/analysis_results/
   → Parses: recommended_settings.json
   → Returns: {metrics, recommendations, chart_path, csv_path}

4. app.py
   → Renders: analysis.html with data
   → Chart displayed prominently
   → Metrics in sidebar
   → Recommendations below
```

### Operation 3: Comparing Two Periods

**User Action:** Selects Q1_2025 and Q1_2026, clicks "Compare"

**Flow:**
```
1. User submits → POST /compare
   {period1, session1, period2, session2}

2. app.py
   → Calls get_analysis_detail() twice
   → Computes deltas (Δ win rate, Δ total R, etc.)

3. app.py
   → Renders: compare.html
   → Side-by-side metrics table
   → Delta column with color coding
   → Recommendations comparison
```

### Operation 4: Exporting .cbotset

**User Action:** Views analysis → Clicks "Export to .cbotset"

**Flow:**
```
1. User clicks → POST /export-cbotset
   {period, session, mode: "recommendations"}

2. app.py
   → export_service.params_from_recommendations(json_path)

3. export_service
   → Loads recommended_settings.json
   → Maps to cBot parameter names
   → Calls: compare_with_current_settings()
   → Returns: {params, comparison}

4. app.py
   → Renders modal with comparison view
   → Shows full settings with highlights
   → User confirms

5. User confirms → export_service.export_to_cbotset(params)
   → Generates XML
   → Returns file path

6. app.py
   → Sends file download
   → Filename: Jcamp_1M_scalping_<period>_<session>.cbotset
```

---

## Section 4: Error Handling

### Error Categories

#### 1. File System Errors

**Scenarios:**
- CSV file not found
- Permission denied
- Disk full
- Path doesn't exist

**Detection:**
```python
try:
    shutil.copy2(csv_path, archive_path)
except FileNotFoundError:
    return {"success": False, "error": "CSV file not found"}
except PermissionError:
    return {"success": False, "error": "Permission denied"}
except OSError as e:
    return {"success": False, "error": f"Disk error: {str(e)}"}
```

**UI Response:**
- Red alert banner at top of page
- Clear error message
- Keep form filled for retry
- Log to `data/temp/errors.log`

#### 2. Analysis Failures

**Scenarios:**
- Python script crashes
- Malformed CSV
- Missing required columns
- Timeout (> 5 minutes)

**Detection:**
```python
result = subprocess.run(
    ["python", "wfo_analyzer.py", csv_path],
    capture_output=True,
    text=True,
    timeout=300
)

if result.returncode != 0:
    return {
        "success": False,
        "error": "Analysis failed",
        "details": result.stderr
    }
```

**UI Response:**
- Error page with details
- Expandable section showing stderr output
- Suggest fixes: "Check CSV format. Expected columns: PositionID, EntryDate, ..."
- Button: "Download CSV for inspection"
- Button: "Try again"

#### 3. Configuration Errors

**Scenarios:**
- Invalid cTrader path
- Corrupted config.json
- Missing required fields

**Detection:**
```python
def validate_config(config):
    if not os.path.exists(config["paths"]["ctrader_logs"]):
        return {
            "valid": False,
            "error": "cTrader log path does not exist",
            "field": "ctrader_logs"
        }
    return {"valid": True}
```

**UI Response:**
- Highlight invalid field in red
- Inline error message
- Prevent save until fixed
- "Auto-detect" button to scan common paths

#### 4. Export Errors

**Scenarios:**
- Invalid parameter values
- XML generation fails
- Disk full

**Detection:**
```python
def validate_params(params):
    errors = []
    if params.get("ADXThreshold", 0) < 10 or params["ADXThreshold"] > 30:
        errors.append("ADX Threshold must be between 10 and 30")

    if errors:
        return {"valid": False, "errors": errors}
    return {"valid": True}
```

**UI Response:**
- Modal dialog with validation errors
- List all parameter errors
- Allow manual editing
- "Export Anyway" button (advanced users)

#### 5. Archive Corruption

**Scenarios:**
- Missing files in archive
- Incomplete analysis results

**Detection:**
```python
def verify_archive_integrity(period, session):
    required_files = [
        "analysis_results/recommended_settings.json",
        "analysis_results/dashboard.png"
    ]

    missing = []
    for file in required_files:
        path = archive_path / period / session / file
        if not path.exists():
            missing.append(file)

    if missing:
        return {"valid": False, "missing": missing}
    return {"valid": True}
```

**UI Response:**
- Warning icon in archive browser
- Tooltip: "Incomplete analysis. Missing: dashboard.png"
- Allow viewing partial data
- Button: "Re-run Analysis"

#### 6. Performance Issues

**Scenarios:**
- 100+ archived analyses
- Slow page load
- Large chart images

**Handling:**
- **Pagination:** Show 20 analyses per page (configurable)
- **Lazy loading:** Load chart images on scroll
- **Caching:** Store archive index in memory (rebuild on startup)
- **Progress indicators:** Show spinner during analysis

**UI Elements:**
```html
<!-- Pagination -->
<div class="pagination">
  <button>◀ Previous</button>
  <span>Page 1 of 5</span>
  <button>Next ▶</button>
</div>

<!-- Progress during analysis -->
<div class="progress-bar">
  <div class="spinner"></div>
  <p>Analyzing trades... This may take 30 seconds.</p>
</div>
```

#### 7. Cleanup Safety

**Scenario:** Accidentally delete file before archiving completes

**Protection:**
```python
def safe_delete(csv_path, archive_path):
    # 1. Verify archive exists
    if not os.path.exists(archive_path):
        raise ValueError("Archive not found. Refusing to delete.")

    # 2. Verify file sizes match
    if os.path.getsize(csv_path) != os.path.getsize(archive_path):
        raise ValueError("Archive size mismatch. Refusing to delete.")

    # 3. Only then delete
    os.remove(csv_path)

    # 4. Log deletion
    log_action(f"Deleted {csv_path} after archiving to {archive_path}")
```

**UI Response:**
- Settings: Checkbox "Auto-delete CSV after archiving" (default: ON)
- Success: "CSV archived successfully. Original file deleted from C drive."
- Failure: "Warning: Could not delete original file. Please remove manually."

---

## Section 5: UI Design

### Base Template (`base.html`)

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Header: "JCAMP WFO Analysis" | Settings | About    │
├─────────────────────────────────────────────────────┤
│                                                     │
│               {% block content %}                   │
│                                                     │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Features:**
- Responsive navigation bar
- Alert banner area (errors/success messages)
- Dark mode toggle (optional CSS)
- Footer: Version, last updated

### Home Page (`home.html`) - Archive Browser

**Primary Use Case:** Review past analyses, find specific results

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ [+ New Analysis]              🔍 Search [____]      │
├─────────────────────────────────────────────────────┤
│ Archive Browser                                     │
│                                                     │
│ ▼ Q1 2026 (3 analyses)                            │
│   → London Session (0.66R, 23% WR)    [View]       │
│   → Asian Session (-4.32R, 20% WR)    [View]       │
│   → All Sessions (-0.67R, 22% WR)     [View]       │
│                                                     │
│ ▼ Q1 2025 (4 analyses)                            │
│   → London Session (45.2R, 45% WR)    [View]       │
│   → Asian Session (15.3R, 38% WR)     [View]       │
│   → All Sessions (30.1R, 42% WR)      [View]       │
│   → NY Session (12.4R, 35% WR)        [View]       │
│                                                     │
│ [◀ Previous] Page 1 of 3 [Next ▶]                 │
└─────────────────────────────────────────────────────┘
```

**Features:**
- Collapsible period sections
- Color coding: Green (profit), Red (loss), Gray (break-even)
- Quick metrics in list
- "[+ New Analysis]" opens modal

**New Analysis Modal:**
```
┌─────────────────────────────────┐
│ New Analysis                   │
├─────────────────────────────────┤
│ CSV File: [Browse...]           │
│ File: TradeLog_EURUSD_*.csv     │
│                                 │
│ Period: [Q1_2025____]           │
│ Session: [london_session____]  │
│                                 │
│ [Cancel]        [Analyze]       │
└─────────────────────────────────┘
```

### Analysis View (`analysis.html`) - Dashboard First

**Primary Use Case:** Visual analysis, export settings

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ ← Back | Q1 2025 > London Session                   │
├─────────────────────────────────────────────────────┤
│                                                     │
│     ┌─────────────────────────────────────┐        │
│     │                                     │        │
│     │    [Dashboard Chart - Large]        │        │
│     │         (9-panel PNG)               │  ┌────┐│
│     │                                     │  │Win │││
│     │                                     │  │45% │││
│     └─────────────────────────────────────┘  ├────┤││
│                                              │Tot │││
│ ┌─────────────────────────────────────────┐ │45R │││
│ │ Recommendations                         │ ├────┤││
│ │                                         │ │PF  │││
│ │ ✓ Session: London Only (08:00-12:00)   │ │1.85│││
│ │ ✓ ADX Mode: FlipDirection               │ ├────┤││
│ │ ✓ ADX Threshold: 19                     │ │Trd │││
│ │ ✓ ADX Period: 18                        │ │125 │││
│ │                                         │ └────┘││
│ │ [Export .cbotset] [Download JSON]      │        │
│ └─────────────────────────────────────────┘        │
└─────────────────────────────────────────────────────┘
```

**Features:**
- Chart occupies top 60% of viewport
- Metrics sidebar: Fixed width, key stats
- Recommendations panel: Checklist style
- Export buttons prominent

### Compare View (`compare.html`) - Side-by-Side

**Primary Use Case:** Validate WFO recommendations across periods

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Compare Analyses                                    │
├─────────────────────────────────────────────────────┤
│ Period 1: [Q1_2025 ▼] Session: [london ▼]         │
│ Period 2: [Q1_2026 ▼] Session: [london ▼]         │
│                                         [Compare]   │
├─────────────────────────────────────────────────────┤
│ Metric         │ Q1 2025    │ Q1 2026    │ Δ       │
├────────────────┼────────────┼────────────┼─────────┤
│ Win Rate       │ 45.0%      │ 23.3%      │ -21.7%  │
│ Total R        │ +45.2R     │ +0.66R     │ -44.54R │
│ Profit Factor  │ 1.85       │ 1.05       │ -0.80   │
│ Total Trades   │ 125        │ 30         │ -95     │
│ Avg Trade Dur  │ 245m       │ 257m       │ +12m    │
├────────────────┴────────────┴────────────┴─────────┤
│ Recommendations                                     │
├─────────────────────────────────────────────────────┤
│ Period 1 Rec   │ Period 2 Rec      │ Changed?      │
├────────────────┼───────────────────┼───────────────┤
│ London Only    │ London Only       │ ✓ Same        │
│ FlipDirection  │ FlipDirection     │ ✓ Same        │
│ ADX: 18        │ ADX: 19           │ ⚠ Different   │
└─────────────────────────────────────────────────────┘
```

**Features:**
- Dropdown selectors
- Delta column: Color-coded
- Recommendations consistency check

### Settings Page (`settings.html`)

**Layout:**
```
┌─────────────────────────────────────────────────────┐
│ Settings                                            │
├─────────────────────────────────────────────────────┤
│ Paths                                               │
│                                                     │
│ cTrader Log Directory:                              │
│ [C:/Users/.../Trade_Logs/______] [Auto-Detect]     │
│                                                     │
│ Archive Directory:                                  │
│ [D:/JCAMP_FxScalper/data/backtest_archive/______]  │
│                                                     │
│ ─────────────────────────────────────────────────  │
│ Behavior                                            │
│                                                     │
│ ☑ Auto-delete CSV from C drive after archiving     │
│ ☑ Open browser automatically on startup             │
│ ☐ Dark mode                                         │
│                                                     │
│ Results per page: [20__] (10-100)                  │
│                                                     │
│ ─────────────────────────────────────────────────  │
│ Export Settings                                     │
│                                                     │
│ Default .cbotset filename pattern:                  │
│ [Jcamp_1M_scalping_{period}_{session}.cbotset___]  │
│                                                     │
│              [Reset to Defaults]  [Save Settings]   │
└─────────────────────────────────────────────────────┘
```

### Styling Approach

**Philosophy:** Clean, minimal, functional

**Color Palette:**
- Primary: `#007bff` (blue, links/buttons)
- Success: `#28a745` (green, profit/positive)
- Danger: `#dc3545` (red, loss/errors)
- Warning: `#ffc107` (yellow, changes/warnings)
- Neutral: `#6c757d` (gray, unchanged)

**Typography:**
- System fonts (no custom fonts)
- Headings: Bold, 1.5-2em
- Body: 14-16px, 1.5 line height

**Layout:**
- CSS Grid for page structure
- Flexbox for components
- Responsive breakpoints: 768px (tablet), 1024px (desktop)

**Key CSS Classes:**
```css
.alert-success { background: #d4edda; color: #155724; }
.alert-error { background: #f8d7da; color: #721c24; }
.metric-positive { color: #28a745; }
.metric-negative { color: #dc3545; }
.btn-primary { background: #007bff; color: white; }
.param-changed { background: #fff3cd; border-left: 4px solid #ffc107; }
```

---

## Section 6: .cbotset Export

### File Format

cTrader .cbotset files are XML-based parameter files.

**Example Output:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<BotSettings>
  <Parameters>
    <Parameter Name="EnableLondonSession">
      <Value>true</Value>
    </Parameter>
    <Parameter Name="EnableNYSession">
      <Value>false</Value>
    </Parameter>
    <Parameter Name="ADXMode">
      <Value>FlipDirection</Value>
    </Parameter>
    <Parameter Name="ADXPeriod">
      <Value>18</Value>
    </Parameter>
    <Parameter Name="ADXMinThreshold">
      <Value>19</Value>
    </Parameter>
    <Parameter Name="MTF_SMA_Period">
      <Value>275</Value>
    </Parameter>
    <Parameter Name="MinimumRR">
      <Value>5.0</Value>
    </Parameter>
  </Parameters>
</BotSettings>
```

### Export Modes

#### Mode 1: From Python Recommendations (Automatic)

**Workflow:**
1. User views analysis
2. Clicks "Export Recommended Settings"
3. Modal shows full settings comparison
4. User reviews changes (highlighted)
5. Clicks "Export .cbotset"
6. File downloads

**Mapping:**
```python
def params_from_recommendations(json_path):
    with open(json_path) as f:
        rec = json.load(f)

    return {
        "EnableLondonSession": rec["parameters"].get("EnableLondonSession", False),
        "EnableNYSession": rec["parameters"].get("EnableNYSession", False),
        "EnableAsianSession": rec["parameters"].get("EnableAsianSession", False),
        "ADXMode": rec["parameters"].get("ADXMode", "FlipDirection"),
        "ADXPeriod": rec["parameters"].get("ADXPeriod", 18),
        "ADXMinThreshold": rec["parameters"].get("ADXMinThreshold", 19),
        "MTF_SMA_Period": 275,  # From current bot
        "MinimumRR": 5.0,
        # ... other parameters
    }
```

#### Mode 2: Manual Parameter Entry

**Workflow:**
1. User clicks "Create Custom .cbotset"
2. Modal with form (all parameters)
3. User edits values
4. Validation on submit
5. Downloads file

### Full Settings View with Highlighting

**Before Export Modal:**
```
┌────────────────────────────────────────────────────────┐
│ Export Recommended Settings                            │
├────────────────────────────────────────────────────────┤
│ Summary: 2 parameters will change                      │
│                                                        │
│ [▼ Show All cBot Parameters] (click to expand)        │
└────────────────────────────────────────────────────────┘
```

**When Expanded:**
```
┌────────────────────────────────────────────────────────┐
│ [▲ Hide Full Settings]                                 │
├────────────────────────────────────────────────────────┤
│ Parameter                  Current    Recommended  Status│
├────────────────────────────────────────────────────────┤
│ SESSION FILTERS                                        │
│   EnableLondonSession      true       true         ✓   │
│   EnableNYSession          true  →    false        ⚠   │ ← Yellow highlight
│   EnableAsianSession       false      false        ✓   │
│                                                        │
│ ADX SETTINGS                                           │
│   ADXMode                  FlipDir    FlipDir      ✓   │
│   ADXPeriod                18         18           ✓   │
│   ADXMinThreshold          15    →    19           ⚠   │ ← Yellow highlight
│                                                        │
│ STRATEGY PARAMETERS                                    │
│   MTF_SMA_Period           275        275          ✓   │
│   Timeframe2               M4         M4           ✓   │
│   Timeframe3               M15        M15          ✓   │
│   MinimumRR                5.0        5.0          ✓   │
│                                                        │
│ RISK MANAGEMENT                                        │
│   DailyLossLimit           -3.0       -3.0         ✓   │
│   ConsecutiveLossLimit     9          9            ✓   │
│                                                        │
│ Legend: ✓ Unchanged | ⚠ Will Change | → Changed Value │
│                                                        │
│         [Cancel]  [Export .cbotset]                    │
└────────────────────────────────────────────────────────┘
```

**Visual Styling:**
```css
.param-unchanged {
  background: #f8f9fa;
  color: #6c757d;
}

.param-changed {
  background: #fff3cd;  /* Yellow highlight */
  border-left: 4px solid #ffc107;
  font-weight: 600;
}

.status-keep::before { content: "✓"; color: #28a745; }
.status-change::before { content: "⚠"; color: #ffc107; }

.value-changed {
  color: #dc3545;
  font-weight: bold;
}
.value-changed::before {
  content: "→ ";
  color: #ffc107;
}
```

### Parameter Types & Validation

**Type Definitions:**
```python
PARAM_TYPES = {
    "EnableLondonSession": {"type": "bool", "default": False},
    "EnableNYSession": {"type": "bool", "default": False},
    "EnableAsianSession": {"type": "bool", "default": False},
    "ADXMode": {"type": "enum", "values": ["BlockEntry", "FlipDirection"]},
    "ADXPeriod": {"type": "int", "min": 10, "max": 30},
    "ADXMinThreshold": {"type": "int", "min": 10, "max": 30},
    "MTF_SMA_Period": {"type": "int", "min": 100, "max": 500},
    "MinimumRR": {"type": "double", "min": 1.0, "max": 10.0},
    "DailyLossLimit": {"type": "double", "min": -10.0, "max": -1.0},
    "ConsecutiveLossLimit": {"type": "int", "min": 3, "max": 20},
}
```

### XML Generation

```python
import xml.etree.ElementTree as ET

def export_to_cbotset(params, output_path):
    # Validate
    validation = validate_params(params)
    if not validation["valid"]:
        raise ValueError(f"Invalid parameters: {validation['errors']}")

    # Build XML
    root = ET.Element("BotSettings")
    parameters = ET.SubElement(root, "Parameters")

    for param_name, param_value in params.items():
        param_elem = ET.SubElement(parameters, "Parameter")
        param_elem.set("Name", param_name)

        value_elem = ET.SubElement(param_elem, "Value")
        value_elem.text = str(param_value).lower() if isinstance(param_value, bool) else str(param_value)

    # Write with formatting
    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(output_path, encoding="utf-8", xml_declaration=True)

    return output_path
```

### Import Instructions

**After Export:**
```
┌────────────────────────────────────────────────┐
│ ✓ Settings exported successfully!             │
│                                                │
│ To import into cTrader:                        │
│ 1. Open cTrader                                │
│ 2. Go to Automate → Edit Bot                  │
│ 3. Click "Settings" → "Load"                   │
│ 4. Select the downloaded .cbotset file         │
│ 5. Click "OK" to apply                         │
│                                                │
│ [Download Again] [Close]                       │
└────────────────────────────────────────────────┘
```

---

## Section 7: Configuration

### First-Run Setup Wizard

**Step 1: Verify Paths**
```
┌──────────────────────────────────────────────────┐
│ Welcome to JCAMP WFO Analysis UI                 │
│                                                  │
│ cTrader Trade Logs:                              │
│ [C:/Users/Jcamp_Laptop/.../Trade_Logs/______]   │
│ Status: ✓ Found (12 CSV files)                  │
│                                                  │
│ Archive Location:                                │
│ [D:/JCAMP_FxScalper/data/backtest_archive/___]  │
│ Status: ⚠ Will be created                       │
│                                                  │
│               [Auto-Detect] [Continue]           │
└──────────────────────────────────────────────────┘
```

**Step 2: Set Preferences**
```
┌──────────────────────────────────────────────────┐
│ Preferences                                      │
│                                                  │
│ ☑ Auto-delete CSV from C drive after archiving  │
│ ☑ Open browser automatically on startup          │
│                                                  │
│ Results per page: [20__]                         │
│                                                  │
│             [Back] [Finish Setup]                │
└──────────────────────────────────────────────────┘
```

### config.json Structure

```json
{
  "version": "1.0.0",
  "paths": {
    "ctrader_logs": "C:/Users/Jcamp_Laptop/Documents/cAlgo/Data/cBots/Jcamp_1M_scalping/cAlgo/Trade_Logs/",
    "archive": "D:/JCAMP_FxScalper/data/backtest_archive/",
    "temp": "D:/JCAMP_FxScalper/data/temp/",
    "analyzer_script": "D:/JCAMP_FxScalper/wfo_analyzer.py"
  },
  "behavior": {
    "auto_cleanup": true,
    "auto_open_browser": true,
    "results_per_page": 20,
    "dark_mode": false
  },
  "export": {
    "default_filename_pattern": "Jcamp_1M_scalping_{period}_{session}.cbotset",
    "include_timestamp": false
  },
  "cbot_current_settings": {
    "EnableLondonSession": true,
    "EnableNYSession": true,
    "EnableAsianSession": false,
    "ADXMode": "FlipDirection",
    "ADXPeriod": 18,
    "ADXMinThreshold": 15,
    "MTF_SMA_Period": 275,
    "Timeframe2": "M4",
    "Timeframe3": "M15",
    "MinimumRR": 5.0,
    "DailyLossLimit": -3.0,
    "ConsecutiveLossLimit": 9,
    "MonthlyDDLimit": 10.0
  },
  "ui": {
    "theme": "light",
    "chart_max_width": 1200,
    "sidebar_width": 300
  }
}
```

### Config Service Methods

```python
def load_config():
    """Load config, create with defaults if missing"""
    config_path = Path("wfo_ui/config.json")

    if not config_path.exists():
        config = get_default_config()
        save_config(config)
        return config

    with open(config_path) as f:
        return json.load(f)

def detect_ctrader_path():
    """Auto-detect cTrader logs directory"""
    possible_paths = [
        Path.home() / "Documents" / "cAlgo" / "Data" / "cBots" / "Jcamp_1M_scalping" / "cAlgo" / "Trade_Logs",
        Path.home() / "Documents" / "cTrader" / "Trade_Logs",
    ]

    for path in possible_paths:
        if path.exists():
            return str(path)

    return ""

def validate_config(config):
    errors = []

    if not Path(config["paths"]["ctrader_logs"]).exists():
        errors.append("cTrader logs path does not exist")

    if not 10 <= config["behavior"]["results_per_page"] <= 100:
        errors.append("Results per page must be 10-100")

    if not Path(config["paths"]["analyzer_script"]).exists():
        errors.append("wfo_analyzer.py not found")

    return {"valid": len(errors) == 0, "errors": errors}
```

---

## Section 8: Testing (MVP)

### Testing Approach

**Philosophy:** Pragmatic testing for MVP - focus on critical paths

### Unit Tests

**Core Services:**
- `test_analysis_service.py` - Subprocess execution, error handling
- `test_export_service.py` - XML generation, validation
- `test_file_service.py` - Safe deletion, archiving

**Example:**
```python
def test_safe_delete():
    csv_path = "test_data/temp.csv"
    archive_path = "test_data/archive/temp.csv"
    shutil.copy2(csv_path, archive_path)

    file_service.safe_delete(csv_path, archive_path)

    assert not Path(csv_path).exists()  # Original deleted
    assert Path(archive_path).exists()   # Archive intact

def test_safe_delete_no_archive():
    csv_path = "test_data/temp.csv"

    with pytest.raises(ValueError, match="Archive not found"):
        file_service.safe_delete(csv_path, "nonexistent.csv")

    assert Path(csv_path).exists()  # Original preserved
```

### Manual Testing Checklist

**Critical Flows:**
- [ ] First run setup wizard
- [ ] Run new analysis (success case)
- [ ] Run new analysis (error case)
- [ ] View archived analysis
- [ ] Compare two periods
- [ ] Export .cbotset (recommended)
- [ ] Export .cbotset (manual)
- [ ] Settings save/load
- [ ] Auto-cleanup verification

### Error Testing

**Scenarios to Test:**
- [ ] Invalid CSV format
- [ ] Python analyzer crash
- [ ] Disk full during archive
- [ ] Permission denied
- [ ] Config.json corrupted

---

## Section 9: Deployment

### Prerequisites

**System:**
- Windows 10/11
- Python 3.9+
- 500MB free disk space (D drive)

**Python Packages:**
```bash
pip install flask
# pandas, numpy, matplotlib already installed for wfo_analyzer.py
```

### Installation

**1. Create Directory Structure:**
```bash
cd D:\JCAMP_FxScalper
mkdir wfo_ui wfo_ui\services wfo_ui\templates wfo_ui\static\css wfo_ui\static\js
mkdir data data\backtest_archive data\temp
```

**2. Add to .gitignore:**
```
data/
wfo_ui/config.json
*.pyc
__pycache__/
```

### Launch Script

**start_wfo_ui.bat:**
```batch
@echo off
echo ====================================
echo JCAMP WFO Analysis UI
echo ====================================
echo.

python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python not found
    pause
    exit /b 1
)

cd /d D:\JCAMP_FxScalper

python -c "import flask" >nul 2>&1
if errorlevel 1 (
    echo Installing Flask...
    pip install flask
)

echo Starting WFO UI...
echo Browser will open automatically.
echo Press Ctrl+C to stop.
echo.

start http://localhost:5000
python wfo_ui/app.py

pause
```

### First Launch

**User Experience:**
1. Double-click `start_wfo_ui.bat`
2. Terminal shows startup messages
3. Browser opens to localhost:5000
4. Setup wizard (first run only)
5. Home page loads

### Stopping the Server

- Focus terminal window
- Press `Ctrl+C`
- Server stops gracefully

### Future Upgrade Path

**When upgrading to React:**

**Backend becomes REST API:**
```python
@app.route("/api/archive")
def get_archive():
    return jsonify(archive_service.get_archive_tree())

@app.route("/api/analysis/<period>/<session>")
def get_analysis(period, session):
    return jsonify(archive_service.get_analysis_detail(period, session))
```

**Frontend replaced with React:**
```
wfo_ui/
├── backend/ (Flask REST API)
├── frontend/ (React app)
```

**Service layer unchanged** - This is the key benefit of modular architecture!

---

## Success Criteria

### MVP Launch (Week 1)
- [ ] User can run analysis from browser
- [ ] Results archive to `data/backtest_archive/`
- [ ] C drive CSV auto-deleted after archiving
- [ ] .cbotset export works (import to cTrader successfully)
- [ ] Compare two periods side-by-side
- [ ] No crashes or data loss

### Post-MVP (Month 1)
- [ ] 20+ analyses archived without performance issues
- [ ] Settings persist across sessions
- [ ] Error messages clear and actionable
- [ ] User prefers UI over manual workflow

### Long-term (Month 3+)
- [ ] Ready to upgrade to React frontend
- [ ] Service layer proven stable
- [ ] User workflow fully migrated to browser UI

---

## Non-Goals (Out of Scope for MVP)

- ❌ Trigger backtests from UI (cTrader automation)
- ❌ Real-time log monitoring
- ❌ Multi-user support / authentication
- ❌ Database storage
- ❌ Advanced charting (beyond PNG display)
- ❌ API for external tools
- ❌ Mobile app

These features may be added in future versions after MVP validation.

---

## Implementation Notes

### Development Order

**Phase 1: Foundation (Day 1-2)**
1. Create directory structure
2. Implement config_service.py
3. Implement file_service.py
4. Create base.html template
5. Basic Flask app with home route

**Phase 2: Core Features (Day 3-4)**
1. Implement analysis_service.py
2. Implement archive_service.py
3. Create home.html, analysis.html
4. Wire up "Run Analysis" workflow
5. Test end-to-end: CSV → Analysis → Archive

**Phase 3: Export & Comparison (Day 5)**
1. Implement export_service.py
2. Create compare.html
3. Create settings.html
4. Test .cbotset generation and import

**Phase 4: Polish (Day 6)**
1. Add error handling to all routes
2. Style CSS (clean, minimal)
3. Add progress indicators
4. Create start_wfo_ui.bat
5. Write user documentation

### Estimated Timeline

**Total: 4-6 days (30-40 hours) for MVP**

- Architecture setup: 4 hours
- Services implementation: 12 hours
- Templates & UI: 8 hours
- Testing: 6 hours
- Polish & documentation: 4 hours

---

## Appendices

### A. File Locations

```
D:\JCAMP_FxScalper\
├── wfo_ui/              (Browser UI code)
├── data/                (Archived results, NOT in Git)
├── wfo_analyzer.py      (Existing analyzer)
└── start_wfo_ui.bat     (Launcher)

C:\Users\Jcamp_Laptop\Documents\cAlgo\Data\cBots\Jcamp_1M_scalping\cAlgo\Trade_Logs\
└── TradeLog_*.csv       (cTrader exports here, auto-deleted after archiving)
```

### B. Dependencies

```
flask==2.3.0              (Web framework)
pandas==2.0.0             (Already installed)
numpy==1.24.0             (Already installed)
matplotlib==3.7.0         (Already installed)
seaborn==0.12.0           (Already installed)
```

### C. Glossary

- **Period:** Time range for backtest (e.g., Q1_2025, April_2026)
- **Session:** Trading session filter (london_session, asian_session, all_sessions)
- **Archive:** Organized storage of backtest results in `data/backtest_archive/`
- **.cbotset:** cTrader XML parameter file
- **WFO:** Walk-Forward Optimization (training → testing cycle)
- **Analysis:** Output of wfo_analyzer.py (metrics, recommendations, charts)

---

**End of Design Specification**

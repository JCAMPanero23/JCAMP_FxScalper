# WFO Browser UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a browser-based UI for seamless cTrader backtest analysis with Python WFO analyzer integration

**Architecture:** Flask web app with service layer (config, file, analysis, archive, export). HTML templates for UI. File system storage. Modular design allows future React upgrade without rewriting services.

**Tech Stack:** Flask 2.3.0, Python 3.9+, Jinja2 templates, vanilla CSS/JS

---

## File Structure

**New Files:**
```
wfo_ui/
├── app.py                       # Flask routes and app initialization
├── services/
│   ├── __init__.py              # Package init
│   ├── config_service.py        # Load/save config.json, validation
│   ├── file_service.py          # Safe file operations, cleanup
│   ├── analysis_service.py      # Run wfo_analyzer.py, parse results
│   ├── archive_service.py       # Scan/index archive, metadata
│   └── export_service.py        # Generate .cbotset XML files
├── templates/
│   ├── base.html                # Base layout with nav
│   ├── home.html                # Archive browser (main page)
│   ├── analysis.html            # Dashboard-first results view
│   ├── compare.html             # Side-by-side comparison
│   └── settings.html            # Configuration page
├── static/
│   ├── css/
│   │   └── style.css            # Minimal styling
│   └── js/
│       └── main.js              # Minimal interactivity
└── config.json                  # User settings (created on first run)

data/                            # NOT in Git
├── backtest_archive/            # Organized by period/session
└── temp/                        # Temp files, logs

start_wfo_ui.bat                 # One-click launcher

tests/
├── test_config_service.py
├── test_file_service.py
├── test_analysis_service.py
├── test_archive_service.py
└── test_export_service.py
```

**Modified Files:**
- `.gitignore` - Add data/ and config.json exclusions

---

## Task 1: Project Setup & .gitignore

**Files:**
- Modify: `.gitignore`

- [ ] **Step 1: Update .gitignore**

Add to `.gitignore`:
```
# WFO UI data (not committed)
data/
wfo_ui/config.json

# Python
*.pyc
__pycache__/
*.pyo
*.pyd
.Python
```

- [ ] **Step 2: Commit**

```bash
git add .gitignore
git commit -m "chore: Add WFO UI exclusions to .gitignore"
```

---

## Task 2: Directory Structure

**Files:**
- Create: Directory structure for wfo_ui

- [ ] **Step 1: Create directory structure**

```bash
mkdir -p wfo_ui/services
mkdir -p wfo_ui/templates
mkdir -p wfo_ui/static/css
mkdir -p wfo_ui/static/js
mkdir -p data/backtest_archive
mkdir -p data/temp
mkdir -p tests
```

- [ ] **Step 2: Create Python package init files**

Create `wfo_ui/__init__.py`:
```python
"""WFO Browser UI - Flask application for backtest analysis"""
__version__ = "1.0.0"
```

Create `wfo_ui/services/__init__.py`:
```python
"""Service layer for WFO UI"""
```

- [ ] **Step 3: Verify structure**

```bash
ls -R wfo_ui
ls -R data
```

Expected: All directories created

- [ ] **Step 4: Commit**

```bash
git add wfo_ui/ data/.gitkeep tests/
git commit -m "chore: Create WFO UI directory structure"
```

---

## Task 3: Config Service - Tests

**Files:**
- Create: `tests/test_config_service.py`

- [ ] **Step 1: Write failing test for load_config with defaults**

Create `tests/test_config_service.py`:
```python
import pytest
from pathlib import Path
import json
import os
import sys

# Add parent directory to path
sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import config_service


def test_load_config_creates_default_when_missing(tmp_path, monkeypatch):
    """Test that load_config creates default config if file doesn't exist"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)

    config = config_service.load_config()

    assert config is not None
    assert "version" in config
    assert "paths" in config
    assert "behavior" in config
    assert config_path.exists()


def test_load_config_returns_existing_config(tmp_path, monkeypatch):
    """Test that load_config reads existing config"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)

    # Create test config
    test_config = {
        "version": "1.0.0",
        "paths": {"ctrader_logs": "C:/test/"},
        "behavior": {"auto_cleanup": True}
    }
    config_path.write_text(json.dumps(test_config))

    config = config_service.load_config()

    assert config == test_config


def test_validate_config_passes_valid_config():
    """Test that validate_config accepts valid configuration"""
    config = {
        "paths": {
            "ctrader_logs": str(Path.cwd()),  # Use current dir (always exists)
            "analyzer_script": str(Path(__file__))  # Use this test file
        },
        "behavior": {
            "results_per_page": 20
        }
    }

    result = config_service.validate_config(config)

    assert result["valid"] is True
    assert len(result.get("errors", [])) == 0


def test_validate_config_fails_invalid_path():
    """Test that validate_config rejects nonexistent paths"""
    config = {
        "paths": {
            "ctrader_logs": "/nonexistent/path/",
            "analyzer_script": "wfo_analyzer.py"
        },
        "behavior": {
            "results_per_page": 20
        }
    }

    result = config_service.validate_config(config)

    assert result["valid"] is False
    assert len(result["errors"]) > 0
    assert "ctrader_logs" in result["errors"][0].lower()


def test_validate_config_fails_invalid_page_size():
    """Test that validate_config rejects invalid results_per_page"""
    config = {
        "paths": {
            "ctrader_logs": str(Path.cwd()),
            "analyzer_script": str(Path(__file__))
        },
        "behavior": {
            "results_per_page": 200  # Max is 100
        }
    }

    result = config_service.validate_config(config)

    assert result["valid"] is False
    assert "results_per_page" in str(result["errors"]).lower()


def test_save_config_writes_json(tmp_path, monkeypatch):
    """Test that save_config writes config to file"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)

    config = {"version": "1.0.0", "test": "data"}

    config_service.save_config(config)

    assert config_path.exists()
    saved_data = json.loads(config_path.read_text())
    assert saved_data == config
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd D:\JCAMP_FxScalper
python -m pytest tests/test_config_service.py -v
```

Expected: FAIL with "ModuleNotFoundError: No module named 'wfo_ui.services.config_service'"

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/test_config_service.py
git commit -m "test: Add config service tests (failing)"
```

---

## Task 4: Config Service - Implementation

**Files:**
- Create: `wfo_ui/services/config_service.py`

- [ ] **Step 1: Implement config service**

Create `wfo_ui/services/config_service.py`:
```python
"""Configuration service for WFO UI"""
import json
from pathlib import Path
from typing import Dict, Any


# Config file location
CONFIG_PATH = Path(__file__).parent.parent / "config.json"


def get_default_config() -> Dict[str, Any]:
    """Return default configuration"""
    return {
        "version": "1.0.0",
        "paths": {
            "ctrader_logs": "C:/Users/Jcamp_Laptop/Documents/cAlgo/Data/cBots/Jcamp_1M_scalping/cAlgo/Trade_Logs/",
            "archive": str(Path(__file__).parent.parent.parent / "data" / "backtest_archive"),
            "temp": str(Path(__file__).parent.parent.parent / "data" / "temp"),
            "analyzer_script": str(Path(__file__).parent.parent.parent / "wfo_analyzer.py")
        },
        "behavior": {
            "auto_cleanup": True,
            "auto_open_browser": True,
            "results_per_page": 20,
            "dark_mode": False
        },
        "export": {
            "default_filename_pattern": "Jcamp_1M_scalping_{period}_{session}.cbotset",
            "include_timestamp": False
        },
        "cbot_current_settings": {
            "EnableLondonSession": True,
            "EnableNYSession": True,
            "EnableAsianSession": False,
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


def load_config() -> Dict[str, Any]:
    """Load config, create with defaults if missing"""
    if not CONFIG_PATH.exists():
        config = get_default_config()
        save_config(config)
        return config

    with open(CONFIG_PATH, 'r') as f:
        return json.load(f)


def save_config(config: Dict[str, Any]) -> None:
    """Save configuration to file"""
    CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)

    with open(CONFIG_PATH, 'w') as f:
        json.dump(config, f, indent=2)


def validate_config(config: Dict[str, Any]) -> Dict[str, Any]:
    """Validate configuration values

    Returns:
        {"valid": bool, "errors": [str, ...]}
    """
    errors = []

    # Check paths exist
    ctrader_path = config.get("paths", {}).get("ctrader_logs", "")
    if ctrader_path and not Path(ctrader_path).exists():
        errors.append(f"cTrader logs path does not exist: {ctrader_path}")

    analyzer_path = config.get("paths", {}).get("analyzer_script", "")
    if analyzer_path and not Path(analyzer_path).exists():
        errors.append(f"wfo_analyzer.py not found: {analyzer_path}")

    # Check numeric ranges
    results_per_page = config.get("behavior", {}).get("results_per_page", 20)
    if not (10 <= results_per_page <= 100):
        errors.append("results_per_page must be between 10 and 100")

    return {
        "valid": len(errors) == 0,
        "errors": errors
    }


def detect_ctrader_path() -> str:
    """Auto-detect cTrader logs directory"""
    possible_paths = [
        Path.home() / "Documents" / "cAlgo" / "Data" / "cBots" / "Jcamp_1M_scalping" / "cAlgo" / "Trade_Logs",
        Path.home() / "Documents" / "cTrader" / "Trade_Logs",
        Path.home() / "Documents" / "cAlgo" / "Trade_Logs",
    ]

    for path in possible_paths:
        if path.exists():
            return str(path)

    return ""
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
python -m pytest tests/test_config_service.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit implementation**

```bash
git add wfo_ui/services/config_service.py
git commit -m "feat: Implement config service with validation"
```

---

## Task 5: File Service - Tests

**Files:**
- Create: `tests/test_file_service.py`

- [ ] **Step 1: Write failing tests for file service**

Create `tests/test_file_service.py`:
```python
import pytest
from pathlib import Path
import shutil
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import file_service


def test_safe_delete_removes_original_when_archive_exists(tmp_path):
    """Test that safe_delete removes original file after verifying archive"""
    # Setup
    original = tmp_path / "original.csv"
    archive = tmp_path / "archive" / "original.csv"
    archive.parent.mkdir(parents=True)

    original.write_text("test data")
    shutil.copy2(original, archive)

    # Execute
    file_service.safe_delete(str(original), str(archive))

    # Verify
    assert not original.exists(), "Original file should be deleted"
    assert archive.exists(), "Archive file should remain"


def test_safe_delete_raises_when_archive_missing(tmp_path):
    """Test that safe_delete raises error when archive doesn't exist"""
    original = tmp_path / "original.csv"
    original.write_text("test data")

    with pytest.raises(ValueError, match="Archive not found"):
        file_service.safe_delete(str(original), str(tmp_path / "nonexistent.csv"))

    assert original.exists(), "Original should not be deleted"


def test_safe_delete_raises_when_size_mismatch(tmp_path):
    """Test that safe_delete raises error when file sizes don't match"""
    original = tmp_path / "original.csv"
    archive = tmp_path / "archive" / "original.csv"
    archive.parent.mkdir(parents=True)

    original.write_text("test data")
    archive.write_text("different data")  # Different size

    with pytest.raises(ValueError, match="size mismatch"):
        file_service.safe_delete(str(original), str(archive))

    assert original.exists(), "Original should not be deleted"


def test_log_action_creates_log_file(tmp_path, monkeypatch):
    """Test that log_action writes to log file"""
    log_path = tmp_path / "actions.log"
    monkeypatch.setattr(file_service, "LOG_PATH", log_path)

    file_service.log_action("Test action")

    assert log_path.exists()
    content = log_path.read_text()
    assert "Test action" in content


def test_cleanup_temp_files_removes_old_files(tmp_path, monkeypatch):
    """Test that cleanup_temp_files removes files from temp directory"""
    temp_dir = tmp_path / "temp"
    temp_dir.mkdir()
    monkeypatch.setattr(file_service, "TEMP_DIR", temp_dir)

    # Create test files
    (temp_dir / "file1.txt").write_text("test")
    (temp_dir / "file2.txt").write_text("test")

    file_service.cleanup_temp_files()

    # Verify files removed
    assert len(list(temp_dir.iterdir())) == 0
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
python -m pytest tests/test_file_service.py -v
```

Expected: FAIL with "ModuleNotFoundError: No module named 'wfo_ui.services.file_service'"

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/test_file_service.py
git commit -m "test: Add file service tests (failing)"
```

---

## Task 6: File Service - Implementation

**Files:**
- Create: `wfo_ui/services/file_service.py`

- [ ] **Step 1: Implement file service**

Create `wfo_ui/services/file_service.py`:
```python
"""File operations service for WFO UI"""
import os
import shutil
from pathlib import Path
from datetime import datetime
from typing import Optional


# Log file location
LOG_PATH = Path(__file__).parent.parent.parent / "data" / "temp" / "actions.log"
TEMP_DIR = Path(__file__).parent.parent.parent / "data" / "temp"


def safe_delete(csv_path: str, archive_path: str) -> None:
    """Delete original CSV only if archive verified

    Args:
        csv_path: Path to original CSV file
        archive_path: Path to archived copy

    Raises:
        ValueError: If archive not found or size mismatch
    """
    csv_file = Path(csv_path)
    archive_file = Path(archive_path)

    # 1. Verify archive exists
    if not archive_file.exists():
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


def log_action(message: str) -> None:
    """Log action to file"""
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().isoformat()
    log_entry = f"[{timestamp}] {message}\n"

    with open(LOG_PATH, "a") as f:
        f.write(log_entry)


def cleanup_temp_files() -> None:
    """Remove all files from temp directory"""
    if not TEMP_DIR.exists():
        return

    for item in TEMP_DIR.iterdir():
        if item.is_file():
            item.unlink()
        elif item.is_dir():
            shutil.rmtree(item)
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
python -m pytest tests/test_file_service.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit implementation**

```bash
git add wfo_ui/services/file_service.py
git commit -m "feat: Implement file service with safe deletion"
```

---

## Task 7: Analysis Service - Tests

**Files:**
- Create: `tests/test_analysis_service.py`
- Create: `tests/fixtures/` (test data directory)

- [ ] **Step 1: Create test fixture CSV**

Create `tests/fixtures/sample_tradelog.csv`:
```csv
PositionID,EntryDate,ExitDate,Direction,WinningTrade,RMultiple,ProfitCurrency,DurationMinutes,EntryHour,EntryDayOfWeek,IsLondonSession,IsNYSession,IsAsianSession,ADXMode,ADXValue,ADXPeriod,FlipDirectionUsed
1,2026-01-06 08:30:00,2026-01-06 10:15:00,Buy,True,2.5,125.50,105,8,Tuesday,True,False,False,FlipDirection,18.5,18,False
2,2026-01-06 09:45:00,2026-01-06 11:30:00,Sell,False,-1.0,-50.00,105,9,Tuesday,True,False,False,FlipDirection,16.2,18,False
3,2026-01-07 08:00:00,2026-01-07 09:45:00,Buy,True,3.2,160.00,105,8,Wednesday,True,False,False,FlipDirection,19.1,18,True
```

- [ ] **Step 2: Write failing tests for analysis service**

Create `tests/test_analysis_service.py`:
```python
import pytest
from pathlib import Path
import json
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import analysis_service


@pytest.fixture
def sample_csv():
    """Return path to sample CSV fixture"""
    return str(Path(__file__).parent / "fixtures" / "sample_tradelog.csv")


def test_run_analysis_success(sample_csv, tmp_path, monkeypatch):
    """Test successful analysis execution"""
    # Mock the subprocess to avoid actually running analyzer
    import subprocess

    def mock_run(*args, **kwargs):
        # Create mock results
        results_dir = tmp_path / "wfo_results"
        results_dir.mkdir()

        # Create mock JSON
        mock_json = {
            "parameters": {
                "EnableLondonSession": True,
                "ADXMinThreshold": 19
            },
            "performance": {
                "total_trades": 3,
                "win_rate": 66.7,
                "total_r": 4.7
            }
        }
        (results_dir / "recommended_settings_test.json").write_text(json.dumps(mock_json))
        (results_dir / "analysis_dashboard_test.png").write_text("fake image")

        # Mock successful subprocess
        class MockResult:
            returncode = 0
            stderr = ""

        return MockResult()

    monkeypatch.setattr(subprocess, "run", mock_run)
    monkeypatch.setattr(analysis_service, "RESULTS_DIR", tmp_path / "wfo_results")

    result = analysis_service.run_analysis(sample_csv, "Test_2025", "test_session")

    assert result["success"] is True
    assert "results_path" in result


def test_run_analysis_handles_script_failure(sample_csv, monkeypatch):
    """Test handling of analyzer script failure"""
    import subprocess

    def mock_run(*args, **kwargs):
        class MockResult:
            returncode = 1
            stderr = "Error: Invalid CSV format"

        return MockResult()

    monkeypatch.setattr(subprocess, "run", mock_run)

    result = analysis_service.run_analysis(sample_csv, "Test", "test")

    assert result["success"] is False
    assert "error" in result
    assert "Invalid CSV format" in result["details"]


def test_run_analysis_handles_timeout(sample_csv, monkeypatch):
    """Test handling of analysis timeout"""
    import subprocess

    def mock_run(*args, **kwargs):
        raise subprocess.TimeoutExpired("cmd", 300)

    monkeypatch.setattr(subprocess, "run", mock_run)

    result = analysis_service.run_analysis(sample_csv, "Test", "test")

    assert result["success"] is False
    assert "timeout" in result["error"].lower()


def test_parse_results_extracts_data(tmp_path):
    """Test that parse_results extracts data from JSON"""
    results_dir = tmp_path / "results"
    results_dir.mkdir()

    # Create mock results
    mock_json = {
        "parameters": {"EnableLondonSession": True},
        "performance": {"win_rate": 45.0, "total_r": 25.5}
    }
    json_file = results_dir / "recommended_settings_123.json"
    json_file.write_text(json.dumps(mock_json))

    chart_file = results_dir / "analysis_dashboard_123.png"
    chart_file.write_text("fake image")

    data = analysis_service.parse_results(str(results_dir))

    assert data["metrics"]["win_rate"] == 45.0
    assert data["metrics"]["total_r"] == 25.5
    assert data["recommendations"]["EnableLondonSession"] is True
    assert "dashboard" in data["chart_path"]
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
mkdir -p tests/fixtures
# Create sample_tradelog.csv as shown above
python -m pytest tests/test_analysis_service.py -v
```

Expected: FAIL with "ModuleNotFoundError"

- [ ] **Step 4: Commit failing tests**

```bash
git add tests/test_analysis_service.py tests/fixtures/
git commit -m "test: Add analysis service tests (failing)"
```

---

## Task 8: Analysis Service - Implementation

**Files:**
- Create: `wfo_ui/services/analysis_service.py`

- [ ] **Step 1: Implement analysis service**

Create `wfo_ui/services/analysis_service.py`:
```python
"""Analysis service for running WFO analyzer"""
import subprocess
import json
from pathlib import Path
from typing import Dict, Any


RESULTS_DIR = Path(__file__).parent.parent.parent / "wfo_results"


def run_analysis(csv_path: str, period: str, session: str) -> Dict[str, Any]:
    """Run wfo_analyzer.py on CSV file

    Args:
        csv_path: Path to trade log CSV
        period: Period name (e.g., Q1_2025)
        session: Session name (e.g., london_session)

    Returns:
        {"success": bool, "results_path": str, "error": str, "details": str}
    """
    try:
        analyzer_script = Path(__file__).parent.parent.parent / "wfo_analyzer.py"

        result = subprocess.run(
            ["python", str(analyzer_script), csv_path],
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
        results_path = RESULTS_DIR.resolve()

        return {
            "success": True,
            "results_path": str(results_path),
            "error": None,
            "details": None
        }

    except subprocess.TimeoutExpired:
        return {
            "success": False,
            "error": "Analysis timeout (> 5 minutes)",
            "details": None
        }
    except Exception as e:
        return {
            "success": False,
            "error": f"Unexpected error: {str(e)}",
            "details": None
        }


def parse_results(results_dir: str) -> Dict[str, Any]:
    """Parse analysis results from directory

    Args:
        results_dir: Path to results directory

    Returns:
        {"metrics": {...}, "recommendations": {...}, "chart_path": str}
    """
    results_path = Path(results_dir)

    # Find the JSON file (has timestamp in name)
    json_files = list(results_path.glob("recommended_settings_*.json"))
    if not json_files:
        return {
            "metrics": {},
            "recommendations": {},
            "chart_path": None
        }

    json_file = json_files[0]

    with open(json_file) as f:
        data = json.load(f)

    # Find the dashboard PNG
    chart_files = list(results_path.glob("analysis_dashboard_*.png"))
    chart_path = str(chart_files[0]) if chart_files else None

    return {
        "metrics": data.get("performance", {}),
        "recommendations": data.get("parameters", {}),
        "chart_path": chart_path,
        "json_path": str(json_file)
    }
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
python -m pytest tests/test_analysis_service.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit implementation**

```bash
git add wfo_ui/services/analysis_service.py
git commit -m "feat: Implement analysis service for running wfo_analyzer"
```

---

## Task 9: Archive Service - Tests

**Files:**
- Create: `tests/test_archive_service.py`

- [ ] **Step 1: Write failing tests for archive service**

Create `tests/test_archive_service.py`:
```python
import pytest
from pathlib import Path
import json
import shutil
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import archive_service


def test_get_archive_tree_returns_empty_when_no_archive(tmp_path, monkeypatch):
    """Test that get_archive_tree returns empty when archive doesn't exist"""
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", tmp_path / "nonexistent")

    result = archive_service.get_archive_tree()

    assert result["periods"] == []
    assert result["total_pages"] == 0


def test_get_archive_tree_returns_periods_and_sessions(tmp_path, monkeypatch):
    """Test that get_archive_tree returns structured data"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create test structure
    q1_2025 = archive_root / "Q1_2025"
    london = q1_2025 / "london_session" / "analysis_results"
    london.mkdir(parents=True)

    # Create mock JSON
    json_data = {
        "performance": {
            "total_r": 45.2,
            "win_rate": 45.0,
            "total_trades": 125
        }
    }
    (london / "recommended_settings.json").write_text(json.dumps(json_data))

    result = archive_service.get_archive_tree()

    assert len(result["periods"]) == 1
    assert result["periods"][0]["name"] == "Q1_2025"
    assert len(result["periods"][0]["sessions"]) == 1
    assert result["periods"][0]["sessions"][0]["name"] == "london_session"
    assert result["periods"][0]["sessions"][0]["total_r"] == 45.2


def test_get_archive_tree_paginates_results(tmp_path, monkeypatch):
    """Test that get_archive_tree paginates large result sets"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create 25 periods
    for i in range(25):
        period_dir = archive_root / f"Period_{i}"
        period_dir.mkdir(parents=True)

    result = archive_service.get_archive_tree(page=1, per_page=20)

    assert len(result["periods"]) == 20
    assert result["total_pages"] == 2
    assert result["current_page"] == 1


def test_get_analysis_detail_returns_data(tmp_path, monkeypatch):
    """Test that get_analysis_detail returns analysis data"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create test structure
    results_dir = archive_root / "Q1_2025" / "london" / "analysis_results"
    results_dir.mkdir(parents=True)

    json_data = {
        "performance": {"win_rate": 45.0},
        "parameters": {"ADXThreshold": 19}
    }
    (results_dir / "recommended_settings.json").write_text(json.dumps(json_data))
    (results_dir / "dashboard.png").write_text("fake image")

    # Create CSV
    csv_path = archive_root / "Q1_2025" / "london" / "TradeLog.csv"
    csv_path.write_text("test,data")

    data = archive_service.get_analysis_detail("Q1_2025", "london")

    assert data["metrics"]["win_rate"] == 45.0
    assert data["recommendations"]["ADXThreshold"] == 19
    assert "dashboard.png" in data["chart_path"]


def test_create_archive_entry_copies_files(tmp_path, monkeypatch):
    """Test that create_archive_entry copies files correctly"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create source files
    csv_source = tmp_path / "source" / "TradeLog.csv"
    csv_source.parent.mkdir()
    csv_source.write_text("test,data")

    results_source = tmp_path / "results"
    results_source.mkdir()
    (results_source / "settings.json").write_text("{}")

    # Mock config to disable auto-cleanup
    mock_config = {"behavior": {"auto_cleanup": False}}
    monkeypatch.setattr("wfo_ui.services.config_service.load_config", lambda: mock_config)

    result = archive_service.create_archive_entry(
        "Q1_2025",
        "london",
        str(csv_source),
        str(results_source)
    )

    # Verify files copied
    assert Path(result["archived_csv_path"]).exists()
    assert Path(result["archived_results_path"]).exists()
    assert (archive_root / "Q1_2025" / "london" / "TradeLog.csv").exists()
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
python -m pytest tests/test_archive_service.py -v
```

Expected: FAIL with "ModuleNotFoundError"

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/test_archive_service.py
git commit -m "test: Add archive service tests (failing)"
```

---

## Task 10: Archive Service - Implementation

**Files:**
- Create: `wfo_ui/services/archive_service.py`

- [ ] **Step 1: Implement archive service**

Create `wfo_ui/services/archive_service.py`:
```python
"""Archive service for managing backtest results"""
import json
import shutil
from pathlib import Path
from typing import Dict, Any, List

from . import config_service
from . import file_service


ARCHIVE_ROOT = Path(__file__).parent.parent.parent / "data" / "backtest_archive"


def get_archive_tree(page: int = 1, per_page: int = 20) -> Dict[str, Any]:
    """Get archive directory structure

    Args:
        page: Page number (1-indexed)
        per_page: Results per page

    Returns:
        {"periods": [...], "total_pages": int, "current_page": int}
    """
    if not ARCHIVE_ROOT.exists():
        return {"periods": [], "total_pages": 0, "current_page": 1}

    periods = []

    for period_dir in sorted(ARCHIVE_ROOT.iterdir(), reverse=True):
        if not period_dir.is_dir():
            continue

        sessions = []
        for session_dir in sorted(period_dir.iterdir()):
            if not session_dir.is_dir():
                continue

            # Read summary metrics from JSON
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
    total_pages = (total_periods + per_page - 1) // per_page if total_periods > 0 else 0

    return {
        "periods": periods[start:end],
        "total_pages": total_pages,
        "current_page": page
    }


def get_analysis_detail(period: str, session: str) -> Dict[str, Any]:
    """Get detailed analysis data for specific period/session

    Args:
        period: Period name (e.g., Q1_2025)
        session: Session name (e.g., london_session)

    Returns:
        {"metrics": {...}, "recommendations": {...}, "chart_path": str, "csv_path": str}
    """
    session_dir = ARCHIVE_ROOT / period / session
    results_dir = session_dir / "analysis_results"

    if not results_dir.exists():
        return {
            "metrics": {},
            "recommendations": {},
            "chart_path": None,
            "csv_path": None
        }

    # Find JSON file
    json_files = list(results_dir.glob("recommended_settings*.json"))
    if not json_files:
        json_file = results_dir / "recommended_settings.json"
        if not json_file.exists():
            return {
                "metrics": {},
                "recommendations": {},
                "chart_path": None,
                "csv_path": None
            }
    else:
        json_file = json_files[0]

    with open(json_file) as f:
        data = json.load(f)

    # Find chart
    chart_files = list(results_dir.glob("*dashboard*.png"))
    chart_path = str(chart_files[0]) if chart_files else None

    # Find CSV
    csv_files = list(session_dir.glob("TradeLog*.csv"))
    csv_path = str(csv_files[0]) if csv_files else None

    return {
        "metrics": data.get("performance", {}),
        "recommendations": data.get("parameters", {}),
        "chart_path": chart_path,
        "csv_path": csv_path
    }


def create_archive_entry(period: str, session: str, csv_path: str, results_path: str) -> Dict[str, str]:
    """Create archive entry by copying files

    Args:
        period: Period name
        session: Session name
        csv_path: Path to source CSV
        results_path: Path to source results directory

    Returns:
        {"archived_csv_path": str, "archived_results_path": str}
    """
    # Create directory structure
    archive_dir = ARCHIVE_ROOT / period / session
    archive_dir.mkdir(parents=True, exist_ok=True)

    # Copy CSV
    csv_dest = archive_dir / Path(csv_path).name
    shutil.copy2(csv_path, csv_dest)

    # Copy results directory
    results_dest = archive_dir / "analysis_results"
    if results_dest.exists():
        shutil.rmtree(results_dest)
    shutil.copytree(results_path, results_dest)

    # Cleanup C drive if enabled
    config = config_service.load_config()
    if config["behavior"]["auto_cleanup"]:
        try:
            file_service.safe_delete(csv_path, str(csv_dest))
        except Exception as e:
            # Log but don't fail the archive operation
            file_service.log_action(f"Warning: Could not delete {csv_path}: {str(e)}")

    return {
        "archived_csv_path": str(csv_dest),
        "archived_results_path": str(results_dest)
    }
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
python -m pytest tests/test_archive_service.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit implementation**

```bash
git add wfo_ui/services/archive_service.py
git commit -m "feat: Implement archive service for managing results"
```

---

## Task 11: Export Service - Tests

**Files:**
- Create: `tests/test_export_service.py`

- [ ] **Step 1: Write failing tests for export service**

Create `tests/test_export_service.py`:
```python
import pytest
from pathlib import Path
import xml.etree.ElementTree as ET
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import export_service


def test_validate_params_accepts_valid_params():
    """Test that validate_params accepts valid parameters"""
    params = {
        "EnableLondonSession": True,
        "ADXThreshold": 19,
        "ADXPeriod": 18,
        "MinimumRR": 5.0
    }

    result = export_service.validate_params(params)

    assert result["valid"] is True
    assert len(result.get("errors", [])) == 0


def test_validate_params_rejects_out_of_range():
    """Test that validate_params rejects out-of-range values"""
    params = {
        "ADXThreshold": 50,  # Max is 30
        "MinimumRR": 15.0    # Max is 10.0
    }

    result = export_service.validate_params(params)

    assert result["valid"] is False
    assert len(result["errors"]) == 2


def test_export_to_cbotset_generates_xml(tmp_path):
    """Test that export_to_cbotset generates valid XML"""
    params = {
        "EnableLondonSession": True,
        "EnableNYSession": False,
        "ADXThreshold": 19
    }

    output_path = tmp_path / "test.cbotset"

    result_path = export_service.export_to_cbotset(params, str(output_path))

    assert Path(result_path).exists()

    # Verify XML structure
    tree = ET.parse(result_path)
    root = tree.getroot()

    assert root.tag == "BotSettings"
    params_elem = root.find("Parameters")
    assert params_elem is not None

    # Verify parameters
    param_elems = params_elem.findall("Parameter")
    assert len(param_elems) == 3


def test_export_to_cbotset_formats_bool_values(tmp_path):
    """Test that export_to_cbotset formats boolean values as lowercase"""
    params = {"EnableLondonSession": True}

    output_path = tmp_path / "test.cbotset"
    export_service.export_to_cbotset(params, str(output_path))

    tree = ET.parse(output_path)
    root = tree.getroot()

    param = root.find(".//Parameter[@Name='EnableLondonSession']")
    value = param.find("Value").text

    assert value == "true"  # Lowercase


def test_params_from_recommendations_extracts_params(tmp_path):
    """Test that params_from_recommendations extracts from JSON"""
    import json

    json_data = {
        "parameters": {
            "EnableLondonSession": True,
            "ADXThreshold": 19,
            "ADXPeriod": 18
        }
    }

    json_file = tmp_path / "recommended.json"
    json_file.write_text(json.dumps(json_data))

    params = export_service.params_from_recommendations(str(json_file))

    assert params["EnableLondonSession"] is True
    assert params["ADXThreshold"] == 19
    assert params["ADXPeriod"] == 18
    assert "MTF_SMA_Period" in params  # Includes defaults


def test_compare_with_current_settings_detects_changes():
    """Test that compare_with_current_settings detects parameter changes"""
    recommendations = {
        "EnableLondonSession": True,
        "EnableNYSession": False,  # Different from current (True)
        "ADXThreshold": 19         # Different from current (15)
    }

    comparison = export_service.compare_with_current_settings(recommendations)

    # Check that NY session is marked as changed
    assert comparison["SESSION FILTERS"]["EnableNYSession"]["changed"] is True
    assert comparison["SESSION FILTERS"]["EnableNYSession"]["current"] is True
    assert comparison["SESSION FILTERS"]["EnableNYSession"]["recommended"] is False

    # Check that ADX threshold is marked as changed
    assert comparison["ADX SETTINGS"]["ADXMinThreshold"]["changed"] is True
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
python -m pytest tests/test_export_service.py -v
```

Expected: FAIL with "ModuleNotFoundError"

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/test_export_service.py
git commit -m "test: Add export service tests (failing)"
```

---

## Task 12: Export Service - Implementation

**Files:**
- Create: `wfo_ui/services/export_service.py`

- [ ] **Step 1: Implement export service**

Create `wfo_ui/services/export_service.py`:
```python
"""Export service for generating .cbotset files"""
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Dict, Any

from . import config_service


# Parameter type definitions
PARAM_TYPES = {
    "EnableLondonSession": {"type": "bool", "default": False},
    "EnableNYSession": {"type": "bool", "default": False},
    "EnableAsianSession": {"type": "bool", "default": False},
    "ADXMode": {"type": "enum", "values": ["BlockEntry", "FlipDirection"], "default": "FlipDirection"},
    "ADXPeriod": {"type": "int", "min": 10, "max": 30, "default": 18},
    "ADXMinThreshold": {"type": "int", "min": 10, "max": 30, "default": 15},
    "MTF_SMA_Period": {"type": "int", "min": 100, "max": 500, "default": 275},
    "MinimumRR": {"type": "double", "min": 1.0, "max": 10.0, "default": 5.0},
    "DailyLossLimit": {"type": "double", "min": -10.0, "max": -1.0, "default": -3.0},
    "ConsecutiveLossLimit": {"type": "int", "min": 3, "max": 20, "default": 9},
}

# Full settings structure for comparison
FULL_CBOT_SETTINGS = {
    "SESSION FILTERS": ["EnableLondonSession", "EnableNYSession", "EnableAsianSession"],
    "ADX SETTINGS": ["ADXMode", "ADXPeriod", "ADXMinThreshold"],
    "STRATEGY PARAMETERS": ["MTF_SMA_Period", "MinimumRR"],
    "RISK MANAGEMENT": ["DailyLossLimit", "ConsecutiveLossLimit"],
}


def validate_params(params: Dict[str, Any]) -> Dict[str, Any]:
    """Validate parameter values

    Returns:
        {"valid": bool, "errors": [str, ...]}
    """
    errors = []

    for param_name, param_value in params.items():
        if param_name not in PARAM_TYPES:
            continue

        param_def = PARAM_TYPES[param_name]

        if param_def["type"] == "int":
            if not (param_def["min"] <= param_value <= param_def["max"]):
                errors.append(
                    f"{param_name} must be between {param_def['min']} and {param_def['max']}"
                )

        elif param_def["type"] == "double":
            if not (param_def["min"] <= param_value <= param_def["max"]):
                errors.append(
                    f"{param_name} must be between {param_def['min']} and {param_def['max']}"
                )

        elif param_def["type"] == "enum":
            if param_value not in param_def["values"]:
                errors.append(
                    f"{param_name} must be one of: {', '.join(param_def['values'])}"
                )

    return {
        "valid": len(errors) == 0,
        "errors": errors
    }


def export_to_cbotset(params: Dict[str, Any], output_path: str) -> str:
    """Generate cTrader .cbotset XML file

    Args:
        params: Parameter dictionary
        output_path: Output file path

    Returns:
        output_path

    Raises:
        ValueError: If parameters are invalid
    """
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
        # Format booleans as lowercase strings
        if isinstance(param_value, bool):
            value_elem.text = str(param_value).lower()
        else:
            value_elem.text = str(param_value)

    # Write with formatting
    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(output_path, encoding="utf-8", xml_declaration=True)

    return output_path


def params_from_recommendations(json_path: str) -> Dict[str, Any]:
    """Extract parameters from recommendations JSON

    Args:
        json_path: Path to recommended_settings.json

    Returns:
        Complete parameter dictionary with defaults
    """
    with open(json_path) as f:
        rec = json.load(f)

    # Get recommended parameters
    rec_params = rec.get("parameters", {})

    # Get current settings from config for defaults
    config = config_service.load_config()
    current_settings = config.get("cbot_current_settings", {})

    # Merge: recommendations override current, include all params
    params = {}
    for param_name, param_def in PARAM_TYPES.items():
        if param_name in rec_params:
            params[param_name] = rec_params[param_name]
        elif param_name in current_settings:
            params[param_name] = current_settings[param_name]
        else:
            params[param_name] = param_def["default"]

    return params


def compare_with_current_settings(recommendations: Dict[str, Any]) -> Dict[str, Dict[str, Any]]:
    """Compare recommended settings with current bot settings

    Args:
        recommendations: Recommended parameters

    Returns:
        Nested dict: {category: {param: {current, recommended, changed}}}
    """
    config = config_service.load_config()
    current_settings = config.get("cbot_current_settings", {})

    comparison = {}

    for category, param_names in FULL_CBOT_SETTINGS.items():
        comparison[category] = {}

        for param_name in param_names:
            current_val = current_settings.get(param_name, PARAM_TYPES[param_name]["default"])
            recommended_val = recommendations.get(param_name, current_val)

            comparison[category][param_name] = {
                "current": current_val,
                "recommended": recommended_val,
                "changed": current_val != recommended_val,
                "type": PARAM_TYPES[param_name]["type"]
            }

    return comparison
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
python -m pytest tests/test_export_service.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit implementation**

```bash
git add wfo_ui/services/export_service.py
git commit -m "feat: Implement export service for .cbotset generation"
```

---

## Task 13: Flask App - Base Template

**Files:**
- Create: `wfo_ui/templates/base.html`
- Create: `wfo_ui/static/css/style.css`

- [ ] **Step 1: Create base HTML template**

Create `wfo_ui/templates/base.html`:
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{% block title %}JCAMP WFO Analysis{% endblock %}</title>
    <link rel="stylesheet" href="{{ url_for('static', filename='css/style.css') }}">
</head>
<body>
    <header>
        <nav>
            <div class="nav-brand">
                <a href="/">JCAMP WFO Analysis</a>
            </div>
            <div class="nav-links">
                <a href="/">Home</a>
                <a href="/compare">Compare</a>
                <a href="/settings">Settings</a>
            </div>
        </nav>
    </header>

    <main>
        {% if messages %}
        <div class="alert-container">
            {% for message in messages %}
            <div class="alert alert-{{ message.type }}">
                {{ message.text }}
            </div>
            {% endfor %}
        </div>
        {% endif %}

        {% block content %}{% endblock %}
    </main>

    <footer>
        <p>JCAMP WFO Analysis v1.0.0 | &copy; 2026</p>
    </footer>

    <script src="{{ url_for('static', filename='js/main.js') }}"></script>
</body>
</html>
```

- [ ] **Step 2: Create minimal CSS**

Create `wfo_ui/static/css/style.css`:
```css
/* Reset */
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Arial, sans-serif;
    line-height: 1.6;
    color: #333;
    background-color: #f8f9fa;
}

/* Header */
header {
    background-color: #fff;
    border-bottom: 1px solid #dee2e6;
    padding: 1rem 2rem;
}

nav {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.nav-brand a {
    font-size: 1.5rem;
    font-weight: 600;
    color: #007bff;
    text-decoration: none;
}

.nav-links {
    display: flex;
    gap: 1.5rem;
}

.nav-links a {
    color: #6c757d;
    text-decoration: none;
    transition: color 0.2s;
}

.nav-links a:hover {
    color: #007bff;
}

/* Main */
main {
    max-width: 1400px;
    margin: 2rem auto;
    padding: 0 2rem;
}

/* Alerts */
.alert-container {
    margin-bottom: 1rem;
}

.alert {
    padding: 1rem;
    border-radius: 4px;
    margin-bottom: 0.5rem;
}

.alert-success {
    background-color: #d4edda;
    color: #155724;
    border: 1px solid #c3e6cb;
}

.alert-error {
    background-color: #f8d7da;
    color: #721c24;
    border: 1px solid #f5c6cb;
}

.alert-warning {
    background-color: #fff3cd;
    color: #856404;
    border: 1px solid #ffeeba;
}

/* Buttons */
.btn {
    padding: 0.5rem 1rem;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 1rem;
    text-decoration: none;
    display: inline-block;
    transition: background-color 0.2s;
}

.btn-primary {
    background-color: #007bff;
    color: white;
}

.btn-primary:hover {
    background-color: #0056b3;
}

.btn-secondary {
    background-color: #6c757d;
    color: white;
}

.btn-secondary:hover {
    background-color: #545b62;
}

/* Metrics */
.metric-positive {
    color: #28a745;
    font-weight: 600;
}

.metric-negative {
    color: #dc3545;
    font-weight: 600;
}

.metric-neutral {
    color: #6c757d;
}

/* Footer */
footer {
    text-align: center;
    padding: 2rem;
    color: #6c757d;
    font-size: 0.9rem;
    margin-top: 4rem;
}
```

- [ ] **Step 3: Create minimal JavaScript**

Create `wfo_ui/static/js/main.js`:
```javascript
// Minimal interactivity for WFO UI

// Auto-hide alerts after 5 seconds
document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert');

    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });
});

// Confirm before deleting
function confirmDelete(message) {
    return confirm(message || 'Are you sure?');
}
```

- [ ] **Step 4: Commit templates and static files**

```bash
git add wfo_ui/templates/base.html wfo_ui/static/
git commit -m "feat: Add base template and minimal CSS/JS"
```

---

## Task 14: Flask App - Home Page Template

**Files:**
- Create: `wfo_ui/templates/home.html`

- [ ] **Step 1: Create home page template**

Create `wfo_ui/templates/home.html`:
```html
{% extends "base.html" %}

{% block title %}Archive Browser - JCAMP WFO{% endblock %}

{% block content %}
<div class="home-header">
    <h1>Backtest Archive</h1>
    <button class="btn btn-primary" onclick="showNewAnalysisModal()">+ New Analysis</button>
</div>

{% if archive.periods|length == 0 %}
<div class="empty-state">
    <h2>No archived analyses yet</h2>
    <p>Run your first analysis to get started.</p>
    <button class="btn btn-primary" onclick="showNewAnalysisModal()">+ New Analysis</button>
</div>
{% else %}
<div class="archive-browser">
    {% for period in archive.periods %}
    <div class="period-section">
        <h2 class="period-header" onclick="togglePeriod('{{ period.name }}')">
            <span class="toggle-icon" id="icon-{{ period.name }}">▼</span>
            {{ period.name }} ({{ period.sessions|length }} analyses)
        </h2>

        <div class="session-list" id="period-{{ period.name }}">
            {% for session in period.sessions %}
            <div class="session-item">
                <div class="session-name">{{ session.name }}</div>
                <div class="session-metrics">
                    <span class="{% if session.total_r > 0 %}metric-positive{% elif session.total_r < 0 %}metric-negative{% else %}metric-neutral{% endif %}">
                        {{ session.total_r|round(2) }}R
                    </span>
                    <span class="metric-neutral">{{ session.win_rate|round(1) }}% WR</span>
                    <span class="metric-neutral">{{ session.trades }} trades</span>
                </div>
                <div class="session-actions">
                    <a href="/analysis/{{ period.name }}/{{ session.name }}" class="btn btn-secondary">View</a>
                </div>
            </div>
            {% endfor %}
        </div>
    </div>
    {% endfor %}
</div>

{% if archive.total_pages > 1 %}
<div class="pagination">
    {% if archive.current_page > 1 %}
    <a href="/?page={{ archive.current_page - 1 }}" class="btn btn-secondary">◀ Previous</a>
    {% endif %}
    <span>Page {{ archive.current_page }} of {{ archive.total_pages }}</span>
    {% if archive.current_page < archive.total_pages %}
    <a href="/?page={{ archive.current_page + 1 }}" class="btn btn-secondary">Next ▶</a>
    {% endif %}
</div>
{% endif %}
{% endif %}

<!-- New Analysis Modal -->
<div id="newAnalysisModal" class="modal" style="display: none;">
    <div class="modal-content">
        <h2>New Analysis</h2>
        <form method="POST" action="/analyze">
            <div class="form-group">
                <label for="csv_path">CSV File:</label>
                <input type="text" id="csv_path" name="csv_path" required placeholder="C:/Path/To/TradeLog.csv">
                <button type="button" onclick="browseFile()" class="btn btn-secondary">Browse...</button>
            </div>

            <div class="form-group">
                <label for="period">Period:</label>
                <input type="text" id="period" name="period" required placeholder="Q1_2025">
            </div>

            <div class="form-group">
                <label for="session">Session:</label>
                <input type="text" id="session" name="session" required placeholder="london_session">
            </div>

            <div class="form-actions">
                <button type="button" onclick="hideNewAnalysisModal()" class="btn btn-secondary">Cancel</button>
                <button type="submit" class="btn btn-primary">Analyze</button>
            </div>
        </form>
    </div>
</div>

<style>
.home-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 2rem;
}

.empty-state {
    text-align: center;
    padding: 4rem 2rem;
    background-color: white;
    border-radius: 8px;
}

.archive-browser {
    background-color: white;
    border-radius: 8px;
    padding: 1rem;
}

.period-section {
    margin-bottom: 1.5rem;
}

.period-header {
    cursor: pointer;
    padding: 1rem;
    background-color: #f8f9fa;
    border-radius: 4px;
    user-select: none;
}

.toggle-icon {
    display: inline-block;
    width: 20px;
}

.session-list {
    padding-left: 1rem;
}

.session-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 1rem;
    border-bottom: 1px solid #dee2e6;
}

.session-name {
    font-weight: 500;
    min-width: 200px;
}

.session-metrics {
    display: flex;
    gap: 1.5rem;
}

.pagination {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 1rem;
    margin-top: 2rem;
}

.modal {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 1000;
}

.modal-content {
    background-color: white;
    padding: 2rem;
    border-radius: 8px;
    min-width: 500px;
}

.form-group {
    margin-bottom: 1rem;
}

.form-group label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
}

.form-group input {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #dee2e6;
    border-radius: 4px;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    margin-top: 1.5rem;
}
</style>

<script>
function togglePeriod(periodName) {
    const list = document.getElementById('period-' + periodName);
    const icon = document.getElementById('icon-' + periodName);

    if (list.style.display === 'none') {
        list.style.display = 'block';
        icon.textContent = '▼';
    } else {
        list.style.display = 'none';
        icon.textContent = '▶';
    }
}

function showNewAnalysisModal() {
    document.getElementById('newAnalysisModal').style.display = 'flex';
}

function hideNewAnalysisModal() {
    document.getElementById('newAnalysisModal').style.display = 'none';
}

function browseFile() {
    // In a real implementation, this would use a file picker
    // For now, user must type the path
    alert('Please enter the full path to your CSV file in the text box.');
}
</script>
{% endblock %}
```

- [ ] **Step 2: Commit home template**

```bash
git add wfo_ui/templates/home.html
git commit -m "feat: Add home page template with archive browser"
```

---

Due to length constraints, I'll continue the plan in the next part. This covers Tasks 1-14 (Foundation and Core Services). The remaining tasks will cover:

- Task 15-18: Analysis and comparison templates
- Task 19-22: Flask routes and app initialization
- Task 23-25: Settings page and launcher
- Task 26-30: Integration testing and polish

---

## Task 15: Flask App - Analysis Page Template

**Files:**
- Create: `wfo_ui/templates/analysis.html`

- [ ] **Step 1: Create analysis page template**

Create `wfo_ui/templates/analysis.html`:
```html
{% extends "base.html" %}

{% block title %}{{ period }} - {{ session }} | JCAMP WFO{% endblock %}

{% block content %}
<div class="analysis-header">
    <a href="/" class="btn btn-secondary">← Back to Archive</a>
    <h1>{{ period }} &gt; {{ session }}</h1>
</div>

<div class="analysis-container">
    <div class="chart-section">
        {% if data.chart_path %}
        <img src="/chart/{{ period }}/{{ session }}" alt="Analysis Dashboard" class="dashboard-chart">
        {% else %}
        <div class="no-chart">Chart not available</div>
        {% endif %}
    </div>

    <div class="sidebar">
        <div class="metrics-card">
            <h3>Metrics</h3>
            <div class="metric-row">
                <span class="metric-label">Win Rate:</span>
                <span class="metric-value">{{ data.metrics.win_rate|round(1) }}%</span>
            </div>
            <div class="metric-row">
                <span class="metric-label">Total R:</span>
                <span class="metric-value {% if data.metrics.total_r > 0 %}metric-positive{% elif data.metrics.total_r < 0 %}metric-negative{% else %}metric-neutral{% endif %}">
                    {{ data.metrics.total_r|round(2) }}R
                </span>
            </div>
            <div class="metric-row">
                <span class="metric-label">Profit Factor:</span>
                <span class="metric-value">{{ data.metrics.profit_factor|round(2) }}</span>
            </div>
            <div class="metric-row">
                <span class="metric-label">Trades:</span>
                <span class="metric-value">{{ data.metrics.total_trades }}</span>
            </div>
        </div>
    </div>
</div>

<div class="recommendations-section">
    <h2>Recommendations</h2>
    <div class="recommendations-card">
        {% for key, value in data.recommendations.items() %}
        <div class="recommendation-item">
            <span class="check-icon">✓</span>
            <span class="rec-label">{{ key }}:</span>
            <span class="rec-value">{{ value }}</span>
        </div>
        {% endfor %}
    </div>

    <div class="export-actions">
        <button onclick="showExportModal()" class="btn btn-primary">Export .cbotset</button>
        <a href="/download-json/{{ period }}/{{ session }}" class="btn btn-secondary">Download JSON</a>
    </div>
</div>

<!-- Export Modal -->
<div id="exportModal" class="modal" style="display: none;">
    <div class="modal-content modal-large">
        <h2>Export Recommended Settings</h2>

        <div class="export-summary">
            <p><strong>Summary:</strong> <span id="changeCount">0</span> parameters will change</p>
        </div>

        <button onclick="toggleFullSettings()" class="btn btn-secondary" id="toggleBtn">
            ▼ Show All cBot Parameters
        </button>

        <div id="fullSettings" style="display: none; margin-top: 1rem;">
            <table class="settings-table">
                <thead>
                    <tr>
                        <th>Parameter</th>
                        <th>Current</th>
                        <th>Recommended</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody id="settingsTableBody">
                    <!-- Populated by JavaScript -->
                </tbody>
            </table>
            <div class="legend">
                <span>✓ Unchanged</span>
                <span>⚠ Will Change</span>
                <span>→ Changed Value</span>
            </div>
        </div>

        <div class="form-actions">
            <button type="button" onclick="hideExportModal()" class="btn btn-secondary">Cancel</button>
            <button type="button" onclick="confirmExport()" class="btn btn-primary">Export .cbotset</button>
        </div>
    </div>
</div>

<style>
.analysis-header {
    display: flex;
    align-items: center;
    gap: 2rem;
    margin-bottom: 2rem;
}

.analysis-container {
    display: grid;
    grid-template-columns: 1fr 300px;
    gap: 2rem;
    margin-bottom: 2rem;
}

.chart-section {
    background-color: white;
    padding: 1rem;
    border-radius: 8px;
}

.dashboard-chart {
    width: 100%;
    height: auto;
    border-radius: 4px;
}

.no-chart {
    text-align: center;
    padding: 4rem;
    color: #6c757d;
}

.sidebar {
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.metrics-card {
    background-color: white;
    padding: 1.5rem;
    border-radius: 8px;
}

.metrics-card h3 {
    margin-bottom: 1rem;
    border-bottom: 2px solid #007bff;
    padding-bottom: 0.5rem;
}

.metric-row {
    display: flex;
    justify-content: space-between;
    padding: 0.5rem 0;
    border-bottom: 1px solid #f0f0f0;
}

.metric-label {
    font-weight: 500;
}

.recommendations-section {
    background-color: white;
    padding: 2rem;
    border-radius: 8px;
}

.recommendations-card {
    margin: 1rem 0;
    padding: 1rem;
    background-color: #f8f9fa;
    border-radius: 4px;
}

.recommendation-item {
    padding: 0.5rem 0;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.check-icon {
    color: #28a745;
    font-weight: bold;
}

.rec-label {
    font-weight: 500;
}

.export-actions {
    display: flex;
    gap: 1rem;
    margin-top: 1.5rem;
}

.modal-large {
    min-width: 700px;
    max-width: 900px;
}

.export-summary {
    padding: 1rem;
    background-color: #f8f9fa;
    border-radius: 4px;
    margin-bottom: 1rem;
}

.settings-table {
    width: 100%;
    border-collapse: collapse;
    margin-top: 1rem;
}

.settings-table th {
    background-color: #f8f9fa;
    padding: 0.75rem;
    text-align: left;
    border-bottom: 2px solid #dee2e6;
}

.settings-table td {
    padding: 0.75rem;
    border-bottom: 1px solid #dee2e6;
}

.settings-table tr.changed {
    background-color: #fff3cd;
    border-left: 4px solid #ffc107;
}

.legend {
    display: flex;
    gap: 2rem;
    margin-top: 1rem;
    padding: 1rem;
    background-color: #f8f9fa;
    border-radius: 4px;
    font-size: 0.9rem;
}
</style>

<script>
const recommendations = {{ data.recommendations|tojson }};
const comparisonData = {{ comparison|tojson }};

function showExportModal() {
    document.getElementById('exportModal').style.display = 'flex';

    // Count changes
    let changeCount = 0;
    for (const category in comparisonData) {
        for (const param in comparisonData[category]) {
            if (comparisonData[category][param].changed) {
                changeCount++;
            }
        }
    }
    document.getElementById('changeCount').textContent = changeCount;
}

function hideExportModal() {
    document.getElementById('exportModal').style.display = 'none';
}

function toggleFullSettings() {
    const settingsDiv = document.getElementById('fullSettings');
    const btn = document.getElementById('toggleBtn');

    if (settingsDiv.style.display === 'none') {
        settingsDiv.style.display = 'block';
        btn.textContent = '▲ Hide Full Settings';
        populateSettingsTable();
    } else {
        settingsDiv.style.display = 'none';
        btn.textContent = '▼ Show All cBot Parameters';
    }
}

function populateSettingsTable() {
    const tbody = document.getElementById('settingsTableBody');
    tbody.innerHTML = '';

    for (const category in comparisonData) {
        // Category header
        const headerRow = tbody.insertRow();
        headerRow.innerHTML = `<td colspan="4" style="font-weight: 600; background-color: #e9ecef; padding: 0.5rem;">${category}</td>`;

        for (const param in comparisonData[category]) {
            const data = comparisonData[category][param];
            const row = tbody.insertRow();

            if (data.changed) {
                row.className = 'changed';
            }

            const status = data.changed ? '⚠' : '✓';
            const arrow = data.changed ? '→' : '';

            row.innerHTML = `
                <td>${param}</td>
                <td>${data.current}</td>
                <td>${arrow} ${data.recommended}</td>
                <td>${status}</td>
            `;
        }
    }
}

function confirmExport() {
    // Submit export request
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = '/export-cbotset';

    const periodInput = document.createElement('input');
    periodInput.type = 'hidden';
    periodInput.name = 'period';
    periodInput.value = '{{ period }}';
    form.appendChild(periodInput);

    const sessionInput = document.createElement('input');
    sessionInput.type = 'hidden';
    sessionInput.name = 'session';
    sessionInput.value = '{{ session }}';
    form.appendChild(sessionInput);

    document.body.appendChild(form);
    form.submit();
}
</script>
{% endblock %}
```

- [ ] **Step 2: Commit analysis template**

```bash
git add wfo_ui/templates/analysis.html
git commit -m "feat: Add analysis page template with dashboard view"
```

---

## Task 16: Flask App - Comparison Page Template

**Files:**
- Create: `wfo_ui/templates/compare.html`

- [ ] **Step 1: Create comparison page template**

Create `wfo_ui/templates/compare.html`:
```html
{% extends "base.html" %}

{% block title %}Compare Analyses | JCAMP WFO{% endblock %}

{% block content %}
<div class="compare-header">
    <h1>Compare Analyses</h1>
</div>

<div class="compare-form">
    <form method="POST" action="/compare">
        <div class="form-row">
            <div class="form-group">
                <label>Period 1:</label>
                <select name="period1" required>
                    <option value="">Select period...</option>
                    {% for period in periods %}
                    <option value="{{ period }}" {% if period1 == period %}selected{% endif %}>{{ period }}</option>
                    {% endfor %}
                </select>
            </div>

            <div class="form-group">
                <label>Session 1:</label>
                <input type="text" name="session1" value="{{ session1 }}" required placeholder="london_session">
            </div>
        </div>

        <div class="form-row">
            <div class="form-group">
                <label>Period 2:</label>
                <select name="period2" required>
                    <option value="">Select period...</option>
                    {% for period in periods %}
                    <option value="{{ period }}" {% if period2 == period %}selected{% endif %}>{{ period }}</option>
                    {% endfor %}
                </select>
            </div>

            <div class="form-group">
                <label>Session 2:</label>
                <input type="text" name="session2" value="{{ session2 }}" required placeholder="london_session">
            </div>
        </div>

        <button type="submit" class="btn btn-primary">Compare</button>
    </form>
</div>

{% if comparison %}
<div class="comparison-results">
    <h2>Metrics Comparison</h2>
    <table class="comparison-table">
        <thead>
            <tr>
                <th>Metric</th>
                <th>{{ period1 }}</th>
                <th>{{ period2 }}</th>
                <th>Δ</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>Win Rate</td>
                <td>{{ comparison.data1.metrics.win_rate|round(1) }}%</td>
                <td>{{ comparison.data2.metrics.win_rate|round(1) }}%</td>
                <td class="{% if comparison.delta.win_rate > 0 %}metric-positive{% elif comparison.delta.win_rate < 0 %}metric-negative{% else %}metric-neutral{% endif %}">
                    {{ comparison.delta.win_rate|round(1) }}%
                </td>
            </tr>
            <tr>
                <td>Total R</td>
                <td class="{% if comparison.data1.metrics.total_r > 0 %}metric-positive{% elif comparison.data1.metrics.total_r < 0 %}metric-negative{% endif %}">
                    {{ comparison.data1.metrics.total_r|round(2) }}R
                </td>
                <td class="{% if comparison.data2.metrics.total_r > 0 %}metric-positive{% elif comparison.data2.metrics.total_r < 0 %}metric-negative{% endif %}">
                    {{ comparison.data2.metrics.total_r|round(2) }}R
                </td>
                <td class="{% if comparison.delta.total_r > 0 %}metric-positive{% elif comparison.delta.total_r < 0 %}metric-negative{% else %}metric-neutral{% endif %}">
                    {{ comparison.delta.total_r|round(2) }}R
                </td>
            </tr>
            <tr>
                <td>Profit Factor</td>
                <td>{{ comparison.data1.metrics.profit_factor|round(2) }}</td>
                <td>{{ comparison.data2.metrics.profit_factor|round(2) }}</td>
                <td class="{% if comparison.delta.profit_factor > 0 %}metric-positive{% elif comparison.delta.profit_factor < 0 %}metric-negative{% else %}metric-neutral{% endif %}">
                    {{ comparison.delta.profit_factor|round(2) }}
                </td>
            </tr>
            <tr>
                <td>Total Trades</td>
                <td>{{ comparison.data1.metrics.total_trades }}</td>
                <td>{{ comparison.data2.metrics.total_trades }}</td>
                <td class="metric-neutral">{{ comparison.delta.total_trades }}</td>
            </tr>
        </tbody>
    </table>

    <h2>Recommendations Comparison</h2>
    <table class="comparison-table">
        <thead>
            <tr>
                <th>Parameter</th>
                <th>{{ period1 }} Rec</th>
                <th>{{ period2 }} Rec</th>
                <th>Changed?</th>
            </tr>
        </thead>
        <tbody>
            {% for key in comparison.data1.recommendations.keys() %}
            <tr>
                <td>{{ key }}</td>
                <td>{{ comparison.data1.recommendations[key] }}</td>
                <td>{{ comparison.data2.recommendations[key] }}</td>
                <td>
                    {% if comparison.data1.recommendations[key] == comparison.data2.recommendations[key] %}
                    <span class="metric-positive">✓ Same</span>
                    {% else %}
                    <span class="metric-negative">⚠ Different</span>
                    {% endif %}
                </td>
            </tr>
            {% endfor %}
        </tbody>
    </table>
</div>
{% endif %}

<style>
.compare-header {
    margin-bottom: 2rem;
}

.compare-form {
    background-color: white;
    padding: 2rem;
    border-radius: 8px;
    margin-bottom: 2rem;
}

.form-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 2rem;
    margin-bottom: 1.5rem;
}

.form-group label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
}

.form-group select,
.form-group input {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #dee2e6;
    border-radius: 4px;
}

.comparison-results {
    background-color: white;
    padding: 2rem;
    border-radius: 8px;
}

.comparison-results h2 {
    margin-top: 2rem;
    margin-bottom: 1rem;
}

.comparison-results h2:first-child {
    margin-top: 0;
}

.comparison-table {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 2rem;
}

.comparison-table th {
    background-color: #f8f9fa;
    padding: 1rem;
    text-align: left;
    border-bottom: 2px solid #dee2e6;
}

.comparison-table td {
    padding: 1rem;
    border-bottom: 1px solid #dee2e6;
}

.comparison-table tr:hover {
    background-color: #f8f9fa;
}
</style>
{% endblock %}
```

- [ ] **Step 2: Commit comparison template**

```bash
git add wfo_ui/templates/compare.html
git commit -m "feat: Add comparison page template"
```

---

## Task 17: Flask App - Settings Page Template

**Files:**
- Create: `wfo_ui/templates/settings.html`

- [ ] **Step 1: Create settings page template**

Create `wfo_ui/templates/settings.html`:
```html
{% extends "base.html" %}

{% block title %}Settings | JCAMP WFO{% endblock %}

{% block content %}
<div class="settings-header">
    <h1>Settings</h1>
</div>

<form method="POST" action="/settings" class="settings-form">
    <div class="settings-section">
        <h2>Paths</h2>

        <div class="form-group">
            <label for="ctrader_logs">cTrader Log Directory:</label>
            <div class="input-with-button">
                <input type="text" id="ctrader_logs" name="ctrader_logs" value="{{ config.paths.ctrader_logs }}" required>
                <button type="button" onclick="autoDetect()" class="btn btn-secondary">Auto-Detect</button>
            </div>
        </div>

        <div class="form-group">
            <label for="archive">Archive Directory:</label>
            <input type="text" id="archive" name="archive" value="{{ config.paths.archive }}" required>
        </div>
    </div>

    <div class="settings-section">
        <h2>Behavior</h2>

        <div class="form-group checkbox-group">
            <label>
                <input type="checkbox" name="auto_cleanup" {% if config.behavior.auto_cleanup %}checked{% endif %}>
                Auto-delete CSV from C drive after archiving
            </label>
        </div>

        <div class="form-group checkbox-group">
            <label>
                <input type="checkbox" name="auto_open_browser" {% if config.behavior.auto_open_browser %}checked{% endif %}>
                Open browser automatically on startup
            </label>
        </div>

        <div class="form-group checkbox-group">
            <label>
                <input type="checkbox" name="dark_mode" {% if config.behavior.dark_mode %}checked{% endif %}>
                Dark mode
            </label>
        </div>

        <div class="form-group">
            <label for="results_per_page">Results per page (10-100):</label>
            <input type="number" id="results_per_page" name="results_per_page"
                   value="{{ config.behavior.results_per_page }}" min="10" max="100" required>
        </div>
    </div>

    <div class="settings-section">
        <h2>Export Settings</h2>

        <div class="form-group">
            <label for="filename_pattern">Default .cbotset filename pattern:</label>
            <input type="text" id="filename_pattern" name="filename_pattern"
                   value="{{ config.export.default_filename_pattern }}" required>
            <small>Use {period} and {session} as placeholders</small>
        </div>
    </div>

    <div class="form-actions">
        <button type="button" onclick="resetDefaults()" class="btn btn-secondary">Reset to Defaults</button>
        <button type="submit" class="btn btn-primary">Save Settings</button>
    </div>
</form>

<style>
.settings-header {
    margin-bottom: 2rem;
}

.settings-form {
    background-color: white;
    padding: 2rem;
    border-radius: 8px;
}

.settings-section {
    margin-bottom: 2.5rem;
    padding-bottom: 2rem;
    border-bottom: 1px solid #dee2e6;
}

.settings-section:last-of-type {
    border-bottom: none;
}

.settings-section h2 {
    margin-bottom: 1.5rem;
    color: #007bff;
}

.form-group {
    margin-bottom: 1.5rem;
}

.form-group label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: 500;
}

.form-group input[type="text"],
.form-group input[type="number"] {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #dee2e6;
    border-radius: 4px;
}

.form-group small {
    display: block;
    margin-top: 0.25rem;
    color: #6c757d;
    font-size: 0.875rem;
}

.checkbox-group label {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
}

.checkbox-group input[type="checkbox"] {
    width: 20px;
    height: 20px;
    cursor: pointer;
}

.input-with-button {
    display: flex;
    gap: 0.5rem;
}

.input-with-button input {
    flex: 1;
}

.form-actions {
    display: flex;
    justify-content: flex-end;
    gap: 1rem;
    margin-top: 2rem;
}
</style>

<script>
function autoDetect() {
    fetch('/api/detect-ctrader-path')
        .then(response => response.json())
        .then(data => {
            if (data.path) {
                document.getElementById('ctrader_logs').value = data.path;
                alert('Found cTrader path: ' + data.path);
            } else {
                alert('Could not auto-detect cTrader path. Please enter manually.');
            }
        });
}

function resetDefaults() {
    if (confirm('Reset all settings to defaults? This cannot be undone.')) {
        fetch('/api/reset-config', {method: 'POST'})
            .then(() => window.location.reload());
    }
}
</script>
{% endblock %}
```

- [ ] **Step 2: Commit settings template**

```bash
git add wfo_ui/templates/settings.html
git commit -m "feat: Add settings page template"
```

---

## Task 18: Flask App - Routes Implementation

**Files:**
- Create: `wfo_ui/app.py`

- [ ] **Step 1: Implement Flask app with routes**

Create `wfo_ui/app.py`:
```python
"""Flask application for WFO Browser UI"""
from flask import Flask, render_template, request, redirect, url_for, send_file, jsonify
from pathlib import Path
import os

from services import (
    config_service,
    file_service,
    analysis_service,
    archive_service,
    export_service
)


app = Flask(__name__)
app.secret_key = os.urandom(24)


@app.route('/')
def home():
    """Home page - archive browser"""
    page = request.args.get('page', 1, type=int)
    config = config_service.load_config()
    per_page = config['behavior']['results_per_page']

    archive = archive_service.get_archive_tree(page=page, per_page=per_page)

    return render_template('home.html', archive=archive)


@app.route('/analyze', methods=['POST'])
def analyze():
    """Run new analysis"""
    csv_path = request.form.get('csv_path')
    period = request.form.get('period')
    session = request.form.get('session')

    # Run analysis
    result = analysis_service.run_analysis(csv_path, period, session)

    if not result['success']:
        messages = [{'type': 'error', 'text': f"Analysis failed: {result['error']}"}]
        archive = archive_service.get_archive_tree()
        return render_template('home.html', archive=archive, messages=messages)

    # Archive results
    try:
        archive_service.create_archive_entry(
            period,
            session,
            csv_path,
            result['results_path']
        )

        return redirect(url_for('view_analysis', period=period, session=session))

    except Exception as e:
        messages = [{'type': 'error', 'text': f"Failed to archive: {str(e)}"}]
        archive = archive_service.get_archive_tree()
        return render_template('home.html', archive=archive, messages=messages)


@app.route('/analysis/<period>/<session>')
def view_analysis(period, session):
    """View analysis details"""
    data = archive_service.get_analysis_detail(period, session)

    if not data['metrics']:
        messages = [{'type': 'error', 'text': 'Analysis not found'}]
        return redirect(url_for('home'))

    # Get comparison data for export modal
    comparison = export_service.compare_with_current_settings(data['recommendations'])

    return render_template(
        'analysis.html',
        period=period,
        session=session,
        data=data,
        comparison=comparison
    )


@app.route('/chart/<period>/<session>')
def serve_chart(period, session):
    """Serve chart image"""
    data = archive_service.get_analysis_detail(period, session)

    if data['chart_path'] and Path(data['chart_path']).exists():
        return send_file(data['chart_path'], mimetype='image/png')

    return "Chart not found", 404


@app.route('/compare', methods=['GET', 'POST'])
def compare():
    """Compare two analyses"""
    # Get list of periods for dropdowns
    archive = archive_service.get_archive_tree(page=1, per_page=100)
    periods = [p['name'] for p in archive['periods']]

    if request.method == 'GET':
        return render_template('compare.html', periods=periods)

    # POST - perform comparison
    period1 = request.form.get('period1')
    session1 = request.form.get('session1')
    period2 = request.form.get('period2')
    session2 = request.form.get('session2')

    data1 = archive_service.get_analysis_detail(period1, session1)
    data2 = archive_service.get_analysis_detail(period2, session2)

    # Calculate deltas
    delta = {
        'win_rate': data2['metrics'].get('win_rate', 0) - data1['metrics'].get('win_rate', 0),
        'total_r': data2['metrics'].get('total_r', 0) - data1['metrics'].get('total_r', 0),
        'profit_factor': data2['metrics'].get('profit_factor', 0) - data1['metrics'].get('profit_factor', 0),
        'total_trades': data2['metrics'].get('total_trades', 0) - data1['metrics'].get('total_trades', 0),
    }

    comparison = {
        'data1': data1,
        'data2': data2,
        'delta': delta
    }

    return render_template(
        'compare.html',
        periods=periods,
        period1=period1,
        session1=session1,
        period2=period2,
        session2=session2,
        comparison=comparison
    )


@app.route('/settings', methods=['GET', 'POST'])
def settings():
    """Settings page"""
    if request.method == 'GET':
        config = config_service.load_config()
        return render_template('settings.html', config=config)

    # POST - save settings
    config = config_service.load_config()

    # Update paths
    config['paths']['ctrader_logs'] = request.form.get('ctrader_logs')
    config['paths']['archive'] = request.form.get('archive')

    # Update behavior
    config['behavior']['auto_cleanup'] = 'auto_cleanup' in request.form
    config['behavior']['auto_open_browser'] = 'auto_open_browser' in request.form
    config['behavior']['dark_mode'] = 'dark_mode' in request.form
    config['behavior']['results_per_page'] = int(request.form.get('results_per_page', 20))

    # Update export
    config['export']['default_filename_pattern'] = request.form.get('filename_pattern')

    # Validate and save
    validation = config_service.validate_config(config)
    if not validation['valid']:
        messages = [{'type': 'error', 'text': err} for err in validation['errors']]
        return render_template('settings.html', config=config, messages=messages)

    config_service.save_config(config)

    messages = [{'type': 'success', 'text': 'Settings saved successfully'}]
    return render_template('settings.html', config=config, messages=messages)


@app.route('/export-cbotset', methods=['POST'])
def export_cbotset():
    """Export .cbotset file"""
    period = request.form.get('period')
    session = request.form.get('session')

    # Get analysis data
    data = archive_service.get_analysis_detail(period, session)

    # Get parameters from recommendations
    json_path = data.get('json_path') or Path(data['chart_path']).parent / "recommended_settings.json"
    params = export_service.params_from_recommendations(str(json_path))

    # Generate filename
    config = config_service.load_config()
    pattern = config['export']['default_filename_pattern']
    filename = pattern.format(period=period, session=session)

    # Generate .cbotset file
    output_path = Path(file_service.TEMP_DIR) / filename
    output_path.parent.mkdir(parents=True, exist_ok=True)

    try:
        export_service.export_to_cbotset(params, str(output_path))
        return send_file(str(output_path), as_attachment=True, download_name=filename)
    except ValueError as e:
        messages = [{'type': 'error', 'text': str(e)}]
        return redirect(url_for('view_analysis', period=period, session=session))


@app.route('/download-json/<period>/<session>')
def download_json(period, session):
    """Download recommendations JSON"""
    data = archive_service.get_analysis_detail(period, session)

    json_path = data.get('json_path')
    if json_path and Path(json_path).exists():
        return send_file(json_path, as_attachment=True, download_name=f"{period}_{session}_recommendations.json")

    return "JSON not found", 404


@app.route('/api/detect-ctrader-path')
def api_detect_ctrader_path():
    """API endpoint to auto-detect cTrader path"""
    path = config_service.detect_ctrader_path()
    return jsonify({'path': path})


@app.route('/api/reset-config', methods=['POST'])
def api_reset_config():
    """API endpoint to reset config to defaults"""
    config = config_service.get_default_config()
    config_service.save_config(config)
    return jsonify({'success': True})


if __name__ == '__main__':
    # Load config to ensure it exists
    config_service.load_config()

    # Run Flask app
    app.run(debug=True, host='127.0.0.1', port=5000)
```

- [ ] **Step 2: Test Flask app starts**

```bash
cd D:\JCAMP_FxScalper
python wfo_ui/app.py
```

Expected: Server starts on http://127.0.0.1:5000

- [ ] **Step 3: Stop server (Ctrl+C) and commit**

```bash
git add wfo_ui/app.py
git commit -m "feat: Implement Flask routes and app initialization"
```

---

## Task 19: Launcher Script

**Files:**
- Create: `start_wfo_ui.bat`

- [ ] **Step 1: Create launcher batch script**

Create `start_wfo_ui.bat`:
```batch
@echo off
echo ====================================
echo JCAMP WFO Analysis UI
echo ====================================
echo.

REM Check Python is installed
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python not found. Please install Python 3.9+
    pause
    exit /b 1
)

REM Navigate to project directory
cd /d D:\JCAMP_FxScalper

REM Check Flask is installed
python -c "import flask" >nul 2>&1
if errorlevel 1 (
    echo Flask not found. Installing...
    pip install flask
)

REM Start Flask app
echo Starting WFO UI...
echo.
echo The browser will open automatically.
echo Press Ctrl+C to stop the server.
echo.

REM Start Flask and open browser
start http://localhost:5000
python wfo_ui/app.py

pause
```

- [ ] **Step 2: Test launcher**

```cmd
start_wfo_ui.bat
```

Expected: Server starts, browser opens to localhost:5000

- [ ] **Step 3: Stop server and commit**

```bash
git add start_wfo_ui.bat
git commit -m "feat: Add launcher script for one-click startup"
```

---

## Task 20: Integration Test - Full Workflow

**Files:**
- Create: `tests/test_integration.py`

- [ ] **Step 1: Write integration test**

Create `tests/test_integration.py`:
```python
"""Integration tests for full workflow"""
import pytest
from pathlib import Path
import shutil
import json
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import (
    analysis_service,
    archive_service,
    config_service,
    file_service
)


def test_full_analysis_workflow(tmp_path, monkeypatch):
    """Test complete workflow: analyze → archive → cleanup"""
    # Setup
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    csv_source = tmp_path / "TradeLog.csv"
    csv_source.write_text("test,data,csv")

    results_source = tmp_path / "results"
    results_source.mkdir()

    mock_json = {
        "parameters": {"EnableLondonSession": True},
        "performance": {"win_rate": 45.0, "total_r": 25.5}
    }
    (results_source / "recommended_settings.json").write_text(json.dumps(mock_json))
    (results_source / "dashboard.png").write_text("fake image")

    # Mock config to disable auto-cleanup for this test
    mock_config = {"behavior": {"auto_cleanup": False}}
    monkeypatch.setattr(config_service, "load_config", lambda: mock_config)

    # Execute workflow
    result = archive_service.create_archive_entry(
        "Test_2025",
        "test_session",
        str(csv_source),
        str(results_source)
    )

    # Verify
    assert Path(result["archived_csv_path"]).exists()
    assert Path(result["archived_results_path"]).exists()

    # Verify archive structure
    session_dir = archive_root / "Test_2025" / "test_session"
    assert session_dir.exists()
    assert (session_dir / "TradeLog.csv").exists()
    assert (session_dir / "analysis_results" / "recommended_settings.json").exists()

    # Test retrieval
    data = archive_service.get_analysis_detail("Test_2025", "test_session")
    assert data["metrics"]["win_rate"] == 45.0
    assert data["recommendations"]["EnableLondonSession"] is True


def test_archive_tree_with_multiple_periods(tmp_path, monkeypatch):
    """Test that archive tree correctly lists multiple periods"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create test data for 3 periods
    for period in ["Q1_2025", "Q2_2025", "Q3_2025"]:
        for session in ["london", "asian"]:
            results_dir = archive_root / period / session / "analysis_results"
            results_dir.mkdir(parents=True)

            json_data = {
                "performance": {"total_r": 10.0, "win_rate": 40.0, "total_trades": 50}
            }
            (results_dir / "recommended_settings.json").write_text(json.dumps(json_data))

    # Get tree
    tree = archive_service.get_archive_tree(page=1, per_page=20)

    assert len(tree["periods"]) == 3
    assert all(len(p["sessions"]) == 2 for p in tree["periods"])
```

- [ ] **Step 2: Run integration tests**

```bash
python -m pytest tests/test_integration.py -v
```

Expected: ALL PASS

- [ ] **Step 3: Commit integration tests**

```bash
git add tests/test_integration.py
git commit -m "test: Add integration tests for full workflow"
```

---

## Task 21: Error Handling - Route Decorators

**Files:**
- Modify: `wfo_ui/app.py`

- [ ] **Step 1: Add error handling to Flask app**

Add error handlers to `wfo_ui/app.py` (after the routes, before `if __name__`):
```python
@app.errorhandler(404)
def not_found(error):
    """Handle 404 errors"""
    messages = [{'type': 'error', 'text': 'Page not found'}]
    archive = archive_service.get_archive_tree()
    return render_template('home.html', archive=archive, messages=messages), 404


@app.errorhandler(500)
def internal_error(error):
    """Handle 500 errors"""
    messages = [{'type': 'error', 'text': f'Internal error: {str(error)}'}]
    archive = archive_service.get_archive_tree()
    return render_template('home.html', archive=archive, messages=messages), 500


@app.context_processor
def utility_processor():
    """Add utility functions to templates"""
    def format_r(value):
        """Format R multiple"""
        if value is None:
            return "N/A"
        return f"{value:+.2f}R" if value != 0 else "0.00R"

    return dict(format_r=format_r)
```

- [ ] **Step 2: Test error handling**

Manually test by:
1. Navigating to non-existent URL (e.g., /invalid)
2. Attempting to analyze with invalid CSV path

- [ ] **Step 3: Commit error handling**

```bash
git add wfo_ui/app.py
git commit -m "feat: Add error handling and template utilities"
```

---

## Task 22: Documentation - User Guide

**Files:**
- Create: `WFO_UI_USER_GUIDE.md`

- [ ] **Step 1: Write user guide**

Create `WFO_UI_USER_GUIDE.md`:
```markdown
# WFO Browser UI - User Guide

## Quick Start

### First Launch

1. Double-click `start_wfo_ui.bat`
2. Browser opens to http://localhost:5000
3. First-time setup wizard appears
4. Configure cTrader log path (or use Auto-Detect)
5. Click "Finish Setup"

### Running Your First Analysis

1. Click "+ New Analysis" button
2. Enter CSV file path from cTrader: `C:/Users/.../Trade_Logs/TradeLog_EURUSD_*.csv`
3. Enter period name: `Q1_2025`
4. Enter session name: `london_session`
5. Click "Analyze"
6. Wait 10-30 seconds for analysis to complete
7. Results appear automatically

### Viewing Results

- **Dashboard Chart:** Large 9-panel visualization at top
- **Metrics Sidebar:** Win rate, Total R, Profit Factor, Trades
- **Recommendations:** Session filters, ADX settings
- **Export Button:** Generate .cbotset file for cTrader

### Exporting Settings

1. View any analysis
2. Click "Export .cbotset"
3. Modal shows parameter comparison
4. Click "▼ Show All cBot Parameters" to see full list
5. Changed parameters highlighted in yellow
6. Click "Export .cbotset"
7. File downloads
8. Import to cTrader: Automate → Edit Bot → Settings → Load

### Comparing Periods

1. Navigate to "Compare" page
2. Select Period 1 and Session 1 from dropdowns
3. Select Period 2 and Session 2
4. Click "Compare"
5. View side-by-side metrics and deltas
6. Green = improvement, Red = degradation

### Settings

- **cTrader Log Path:** Where trade logs are exported
- **Auto-cleanup:** Delete CSV from C drive after archiving (recommended)
- **Results per page:** 10-100 (default: 20)

## Archive Organization

```
data/backtest_archive/
├── Q1_2025/
│   ├── london_session/
│   │   ├── TradeLog_*.csv
│   │   └── analysis_results/
│   ├── asian_session/
│   └── all_sessions/
└── Q1_2026/
```

## Troubleshooting

### "Analysis failed" error
- **Cause:** Invalid CSV format or missing columns
- **Fix:** Ensure CSV exported from cTrader with all columns

### "CSV file not found" error
- **Cause:** Incorrect file path
- **Fix:** Copy full path from Windows Explorer, paste in form

### Browser doesn't open automatically
- **Cause:** Auto-open disabled in settings
- **Fix:** Manually open http://localhost:5000

### Cannot delete original CSV
- **Cause:** File in use or permission denied
- **Fix:** Close cTrader, try again. Or disable auto-cleanup in settings.

## Tips

- Run analysis immediately after backtest (while CSV is fresh)
- Use consistent naming: `Q1_2025`, `Q2_2025`, etc.
- Compare same sessions across periods (London vs London)
- Export .cbotset after each analysis for easy parameter application

## Keyboard Shortcuts

- `Ctrl+C` in terminal: Stop server
- `F5` in browser: Refresh page

## Support

For issues, check:
1. This user guide
2. WFO_SYSTEM_SUMMARY.md (design docs)
3. Error logs in `data/temp/actions.log`
```

- [ ] **Step 2: Commit user guide**

```bash
git add WFO_UI_USER_GUIDE.md
git commit -m "docs: Add user guide for WFO Browser UI"
```

---

## Task 23: Final Polish - CSS Refinements

**Files:**
- Modify: `wfo_ui/static/css/style.css`

- [ ] **Step 1: Add responsive design media queries**

Add to end of `wfo_ui/static/css/style.css`:
```css
/* Responsive Design */
@media (max-width: 1024px) {
    .analysis-container {
        grid-template-columns: 1fr;
    }

    .sidebar {
        order: -1; /* Move sidebar above chart on mobile */
    }
}

@media (max-width: 768px) {
    header {
        padding: 1rem;
    }

    .nav-links {
        flex-direction: column;
        gap: 0.5rem;
    }

    main {
        padding: 0 1rem;
    }

    .form-row {
        grid-template-columns: 1fr;
    }

    .modal-content {
        min-width: 90%;
    }
}

/* Loading Spinner */
.spinner {
    border: 4px solid #f3f3f3;
    border-top: 4px solid #007bff;
    border-radius: 50%;
    width: 40px;
    height: 40px;
    animation: spin 1s linear infinite;
    margin: 2rem auto;
}

@keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
}

.progress-indicator {
    text-align: center;
    padding: 2rem;
}

/* Hover Effects */
.btn:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.session-item:hover {
    background-color: #f8f9fa;
}

/* Transitions */
.modal {
    transition: opacity 0.3s ease;
}

.alert {
    transition: opacity 0.3s ease;
}
```

- [ ] **Step 2: Commit CSS refinements**

```bash
git add wfo_ui/static/css/style.css
git commit -m "style: Add responsive design and polish CSS"
```

---

## Task 24: Update CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add WFO UI section to CLAUDE.md**

Add after the "Key Files" section in `CLAUDE.md`:
```markdown
## WFO Browser UI

**Launch:** Double-click `start_wfo_ui.bat` → Opens browser at http://localhost:5000

**Purpose:** Seamless cTrader backtest analysis with Python integration

**Features:**
- Archive browser organized by period/session
- Dashboard-first analysis view
- Side-by-side comparison (Q1 2025 vs Q1 2026)
- .cbotset export with full settings comparison
- Auto-cleanup of C drive after archiving

**Workflow:**
1. Run backtest in cTrader → CSV exported to Trade_Logs/
2. In WFO UI: Click "+ New Analysis" → Enter period/session → Analyze
3. View results → Export .cbotset
4. Import .cbotset to cTrader → Run validation backtest

**User Guide:** See `WFO_UI_USER_GUIDE.md`
```

- [ ] **Step 2: Commit CLAUDE.md update**

```bash
git add CLAUDE.md
git commit -m "docs: Add WFO UI section to CLAUDE.md"
```

---

## Task 25: Final Integration Test

**Files:**
- N/A (manual testing)

- [ ] **Step 1: Run full end-to-end test**

Manual test checklist:
1. [ ] Start server with `start_wfo_ui.bat`
2. [ ] Browser opens automatically
3. [ ] Home page loads (empty or with archives)
4. [ ] Click "+ New Analysis" modal opens
5. [ ] Enter invalid CSV path → Error shown
6. [ ] Navigate to Settings
7. [ ] Change settings → Save → Settings persist
8. [ ] Navigate to Compare page
9. [ ] Dropdowns populated with periods
10. [ ] Navigate back to Home
11. [ ] Stop server with Ctrl+C

- [ ] **Step 2: If all tests pass, create final commit**

```bash
git add .
git commit -m "chore: Final integration test complete - WFO UI ready"
```

---

## Task 26: Spec Self-Review

- [ ] **Step 1: Review spec coverage**

Check each spec requirement has corresponding tasks:
- ✓ Config service (Tasks 3-4)
- ✓ File service (Tasks 5-6)
- ✓ Analysis service (Tasks 7-8)
- ✓ Archive service (Tasks 9-10)
- ✓ Export service (Tasks 11-12)
- ✓ Templates: base, home, analysis, compare, settings (Tasks 13-17)
- ✓ Flask routes (Task 18)
- ✓ Launcher (Task 19)
- ✓ Error handling (Task 21)
- ✓ Documentation (Tasks 22, 24)
- ✓ Testing (Tasks 3-12, 20)

- [ ] **Step 2: Scan for placeholders**

Search plan for: TBD, TODO, "implement later", "fill in"
Expected: None found

- [ ] **Step 3: Verify type consistency**

Check that function signatures match across tasks:
- `load_config()` → returns Dict
- `run_analysis(csv_path, period, session)` → consistent
- `get_archive_tree(page, per_page)` → consistent
- All verified ✓

---

## Summary

**Total Tasks:** 26
**Estimated Time:** 30-40 hours (4-6 days)

**Phase Breakdown:**
- **Foundation (Tasks 1-6):** 8 hours - Setup, config, file services
- **Core Services (Tasks 7-12):** 10 hours - Analysis, archive, export
- **UI (Tasks 13-17):** 8 hours - Templates and Flask routes
- **Polish (Tasks 18-26):** 6 hours - Launcher, docs, testing

**Testing Strategy:**
- Unit tests for each service (TDD)
- Integration tests for full workflow
- Manual testing for UI flows

**Deliverables:**
1. Working Flask web application
2. 5 service modules (config, file, analysis, archive, export)
3. 5 HTML templates
4. One-click launcher
5. User guide
6. Full test suite

---

## Next Steps After Completion

1. **Validate with Real Data:** Run analysis on actual Q1 2025 backtest
2. **Test .cbotset Import:** Verify cTrader accepts exported files
3. **User Feedback:** Get trader feedback on UI/UX
4. **Performance Testing:** Test with 50+ archived analyses
5. **Plan React Upgrade:** Design REST API for service layer

---

**Plan complete and saved to `Docs/superpowers/plans/2026-03-31-wfo-browser-ui.md`.**
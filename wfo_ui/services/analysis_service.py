"""Analysis service for running WFO analyzer"""
import subprocess
import sys
import json
import shutil
from pathlib import Path
from typing import Dict, Any
from .config_service import update_current_settings_from_backtest


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
        csv_file = Path(csv_path)

        # Clear old results before running new analysis
        old_results = csv_file.parent / 'analysis_results'
        if old_results.exists():
            shutil.rmtree(old_results)

        result = subprocess.run(
            [sys.executable, str(analyzer_script), csv_path],
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

        # Results are created next to the CSV file in analysis_results/
        results_path = csv_file.parent / 'analysis_results'

        if not results_path.exists():
            return {
                "success": False,
                "error": "Results directory not created",
                "details": f"Expected results at: {results_path}"
            }

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


def parse_results(results_dir: str, session: str = None) -> Dict[str, Any]:
    """Parse analysis results from directory

    Args:
        results_dir: Path to results directory
        session: Session name (e.g., 'EURUSD_all_sessions') for config sync

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

    try:
        with open(json_file) as f:
            data = json.load(f)
    except (json.JSONDecodeError, IOError) as e:
        return {
            "metrics": {},
            "recommendations": {},
            "chart_path": None,
            "error": str(e)
        }

    # Find the dashboard PNG
    chart_files = list(results_path.glob("analysis_dashboard_*.png"))
    chart_path = str(chart_files[0]) if chart_files else None

    # Auto-sync backtest settings to config.json (latest per pair)
    backtest_settings = data.get("backtest_settings", {})
    if backtest_settings and session:
        # Extract pair from session (e.g., 'EURUSD_all_sessions' -> 'EURUSD')
        pair = session.split('_')[0] if '_' in session else session
        synced = update_current_settings_from_backtest(backtest_settings, pair)
        if synced:
            print(f"[CONFIG] Auto-synced cbot_current_settings from {pair} backtest")

    return {
        "metrics": data.get("performance", {}),
        "recommendations": data.get("parameters", {}),
        "backtest_settings": backtest_settings,
        "chart_path": chart_path,
        "json_path": str(json_file)
    }

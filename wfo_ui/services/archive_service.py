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

            # Read summary metrics from JSON (use latest timestamped file)
            results_dir = session_dir / "analysis_results"
            if not results_dir.exists():
                continue

            json_files = sorted(results_dir.glob("recommended_settings*.json"), reverse=True)
            if json_files:
                json_path = json_files[0]  # Use latest due to reverse sort
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

    # Find JSON file (use latest by timestamp in filename)
    json_files = sorted(results_dir.glob("recommended_settings*.json"), reverse=True)
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
        json_file = json_files[0]  # Now gets latest due to reverse sort

    with open(json_file) as f:
        data = json.load(f)

    # Find chart (use latest by timestamp in filename)
    chart_files = sorted(results_dir.glob("*dashboard*.png"), reverse=True)
    chart_path = str(chart_files[0]) if chart_files else None

    # Find CSV
    csv_files = list(session_dir.glob("TradeLog*.csv"))
    csv_path = str(csv_files[0]) if csv_files else None

    return {
        "overall_metrics": data.get("overall_performance", {}),
        "session_breakdown": data.get("session_breakdown", []),
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

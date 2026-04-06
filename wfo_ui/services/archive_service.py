"""Archive service for managing backtest results"""
import json
import re
import shutil
from pathlib import Path
from typing import Dict, Any, List, Optional

from . import config_service
from . import file_service


ARCHIVE_ROOT = Path(__file__).parent.parent.parent / "data" / "backtest_archive"


def extract_pair_from_csv(csv_path: Path) -> str:
    """Extract trading pair from CSV filename

    Pattern: TradeLog_EURUSD_1092444_20260402_220400.csv
    Returns: EURUSD (or 'Unknown' if not found)
    """
    match = re.search(r'TradeLog_([A-Z]{6})_', csv_path.name)
    if match:
        return match.group(1)
    return "Unknown"


def get_all_pairs() -> List[str]:
    """Get list of all unique pairs in archive"""
    pairs = set()

    if not ARCHIVE_ROOT.exists():
        return []

    for period_dir in ARCHIVE_ROOT.iterdir():
        if not period_dir.is_dir():
            continue
        for session_dir in period_dir.iterdir():
            if not session_dir.is_dir():
                continue
            csv_files = list(session_dir.glob("TradeLog*.csv"))
            if csv_files:
                pair = extract_pair_from_csv(csv_files[0])
                pairs.add(pair)

    return sorted(pairs)


def get_archive_tree(page: int = 1, per_page: int = 20, pair_filter: Optional[str] = None, sort_by: str = 'date_newest', search_query: Optional[str] = None) -> Dict[str, Any]:
    """Get archive directory structure

    Args:
        page: Page number (1-indexed)
        per_page: Results per page
        pair_filter: Optional pair to filter by (e.g., 'EURUSD')
        sort_by: Sort order - 'date_newest', 'date_oldest', 'name_asc', 'name_desc', 'total_r_desc', 'total_r_asc', 'win_rate_desc', 'win_rate_asc'
        search_query: Optional search string to filter by period or session name

    Returns:
        {"periods": [...], "total_pages": int, "current_page": int, "pairs": [...], "current_pair": str, "sort_by": str, "search_query": str}
    """
    if not ARCHIVE_ROOT.exists():
        return {"periods": [], "total_pages": 0, "current_page": 1, "pairs": [], "current_pair": None, "sort_by": sort_by, "search_query": search_query}

    # Get all available pairs for tabs
    all_pairs = get_all_pairs()

    periods = []

    for period_dir in ARCHIVE_ROOT.iterdir():
        if not period_dir.is_dir():
            continue

        sessions = []
        for session_dir in sorted(period_dir.iterdir()):
            if not session_dir.is_dir():
                continue

            # Extract pair from CSV filename
            csv_files = list(session_dir.glob("TradeLog*.csv"))
            pair = extract_pair_from_csv(csv_files[0]) if csv_files else "Unknown"

            # Apply pair filter if specified
            if pair_filter and pair != pair_filter:
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
                        "pair": pair,
                        "total_r": perf.get("total_r", 0),
                        "win_rate": perf.get("win_rate", 0),
                        "trades": perf.get("total_trades", 0)
                    })

        # Only add period if it has sessions (after filtering)
        if sessions:
            # Calculate aggregate metrics for sorting
            total_r_sum = sum(s['total_r'] for s in sessions)
            avg_win_rate = sum(s['win_rate'] for s in sessions) / len(sessions) if sessions else 0

            periods.append({
                "name": period_dir.name,
                "sessions": sessions,
                "modified_time": period_dir.stat().st_mtime,  # For date sorting
                "total_r_aggregate": total_r_sum,
                "win_rate_aggregate": avg_win_rate
            })

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

    # Apply search filter (case-insensitive)
    if search_query:
        search_lower = search_query.lower()
        filtered_periods = []

        for period in periods:
            # Check if period name matches
            period_matches = search_lower in period['name'].lower()

            # Filter sessions that match search query
            matching_sessions = [
                s for s in period['sessions']
                if search_lower in s['name'].lower()
            ]

            # Include period if either period name matches OR it has matching sessions
            if period_matches:
                # Period name matches - include all sessions
                filtered_periods.append(period)
            elif matching_sessions:
                # Only session names match - include only matching sessions
                period_copy = period.copy()
                period_copy['sessions'] = matching_sessions
                filtered_periods.append(period_copy)

        periods = filtered_periods

    # Pagination
    start = (page - 1) * per_page
    end = start + per_page
    total_periods = len(periods)
    total_pages = (total_periods + per_page - 1) // per_page if total_periods > 0 else 0

    return {
        "periods": periods[start:end],
        "total_pages": total_pages,
        "current_page": page,
        "pairs": all_pairs,
        "current_pair": pair_filter,
        "sort_by": sort_by,
        "search_query": search_query
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
        "backtest_settings": data.get("backtest_settings", {}),
        "equity_trend": data.get("equity_trend", {}),
        "equity_degradation_detected": data.get("equity_degradation_detected", False),
        "optimization_recommendation": data.get("optimization_recommendation", {}),
        "chart_path": chart_path,
        "csv_path": csv_path
    }


def check_existing_analysis(period: str, session: str) -> Dict[str, Any]:
    """Check if analysis results already exist for a period/session

    Args:
        period: Period name (e.g., Jan_Mar_2026)
        session: Session name (e.g., EURUSD_all_sessions)

    Returns:
        {"exists": bool, "path": str or None, "metrics": dict or None}
    """
    session_dir = ARCHIVE_ROOT / period / session
    results_dir = session_dir / "analysis_results"

    if not results_dir.exists():
        return {"exists": False, "path": None, "metrics": None}

    # Try to get some metrics from existing analysis
    json_files = sorted(results_dir.glob("recommended_settings*.json"), reverse=True)
    metrics = None
    if json_files:
        try:
            with open(json_files[0]) as f:
                data = json.load(f)
                perf = data.get("performance", {})
                metrics = {
                    "total_r": perf.get("total_r", 0),
                    "win_rate": perf.get("win_rate", 0),
                    "trades": perf.get("total_trades", 0)
                }
        except Exception:
            pass

    return {"exists": True, "path": str(results_dir), "metrics": metrics}


def delete_analysis_results(period: str, session: str) -> Dict[str, Any]:
    """Delete existing analysis results for a period/session

    Args:
        period: Period name
        session: Session name

    Returns:
        {"success": bool, "error": str or None}
    """
    results_dir = ARCHIVE_ROOT / period / session / "analysis_results"

    if not results_dir.exists():
        return {"success": True, "error": None}

    try:
        shutil.rmtree(results_dir)
        return {"success": True, "error": None}
    except Exception as e:
        return {"success": False, "error": str(e)}


def rename_session(period: str, old_session: str, new_session: str) -> Dict[str, Any]:
    """Rename a session within a period

    Args:
        period: Period name (e.g., Jan_Mar_2026)
        old_session: Current session name
        new_session: New session name

    Returns:
        {"success": bool, "error": str or None}
    """
    old_path = ARCHIVE_ROOT / period / old_session
    new_path = ARCHIVE_ROOT / period / new_session

    # Validate old path exists
    if not old_path.exists():
        return {"success": False, "error": f"Session not found: {period}/{old_session}"}

    # Check new path doesn't already exist
    if new_path.exists():
        return {"success": False, "error": f"Session already exists: {period}/{new_session}"}

    # Validate new session name (alphanumeric, underscore, hyphen only)
    import re
    if not re.match(r'^[a-zA-Z0-9_-]+$', new_session):
        return {"success": False, "error": "Session name can only contain letters, numbers, underscores, and hyphens"}

    try:
        old_path.rename(new_path)
        return {"success": True, "error": None}
    except Exception as e:
        return {"success": False, "error": str(e)}


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

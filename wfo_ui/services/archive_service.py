"""Archive service for managing backtest results"""
import json
import re
import shutil
from datetime import datetime
from pathlib import Path
from typing import Dict, Any, List, Optional

from . import config_service
from . import file_service


ARCHIVE_ROOT = Path(__file__).parent.parent.parent / "data" / "backtest_archive"


def format_date_range(start_date: Optional[datetime], end_date: Optional[datetime]) -> Optional[str]:
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


def build_wfo_cycles(sessions: List[Dict[str, Any]], period_name: str) -> List[Dict[str, Any]]:
    """Group sessions into WFO cycles with status badges

    Args:
        sessions: List of session dicts with name, pair, metrics
        period_name: Period name for this group

    Returns:
        List of WFO cycle dicts with status, original, reopt, forward_test fields
    """
    # Load re-optimization tracking data
    config = config_service.load_config()
    reopt_history = config.get('reoptimization_tracking', {}).get('history', [])

    # Group sessions by base name (remove _reopt_timestamp suffix)
    wfo_groups = {}
    reopt_pattern = re.compile(r'(.+)_reopt_\d{8}_\d{6}$')

    for session in sessions:
        session_name = session['name']
        match = reopt_pattern.match(session_name)

        if match:
            # This is a re-optimized version
            base_name = match.group(1)
            if base_name not in wfo_groups:
                wfo_groups[base_name] = {'original': None, 'reopts': []}
            wfo_groups[base_name]['reopts'].append(session)
        else:
            # This is an original analysis
            if session_name not in wfo_groups:
                wfo_groups[session_name] = {'original': None, 'reopts': []}
            wfo_groups[session_name]['original'] = session

    # Build WFO cycle data
    wfo_cycles = []

    for base_name, group in wfo_groups.items():
        original = group['original']
        reopts = group['reopts']

        # Find latest re-optimization (if any)
        latest_reopt = reopts[-1] if reopts else None

        # Find forward test data from config and load its metrics
        forward_test = None
        if latest_reopt:
            reopt_key = f"{period_name}/{latest_reopt['name']}"
            for entry in reopt_history:
                if entry.get('reoptimized') == reopt_key and entry.get('forward_test'):
                    ft = entry['forward_test']
                    # Load forward test metrics from archive
                    ft_path = ARCHIVE_ROOT / ft['period'] / ft['session'] / 'analysis_results'
                    ft_metrics = {}
                    ft_equity_trend = {}
                    if ft_path.exists():
                        json_files = sorted(ft_path.glob("recommended_settings*.json"), reverse=True)
                        if json_files:
                            try:
                                with open(json_files[0]) as f:
                                    ft_data = json.load(f)
                                    ft_metrics = ft_data.get('overall_performance', ft_data.get('performance', {}))
                                    ft_equity_trend = ft_data.get('equity_trend', {})
                            except:
                                pass
                    forward_test = {
                        'period': ft['period'],
                        'session': ft['session'],
                        'result': ft.get('result', ''),
                        'metrics': ft_metrics,
                        'equity_trend': ft_equity_trend
                    }
                    break

        # Determine WFO status
        if forward_test:
            status = 'complete'
            status_label = '✓ Complete'
            status_color = '#27ae60'
        elif latest_reopt:
            status = 'reoptimized'
            status_label = 'Re-optimized'
            status_color = '#3498db'
        elif original:
            # Check if equity degradation detected (needs re-opt)
            # This would require loading the analysis detail, skip for now
            status = 'active'
            status_label = 'Active'
            status_color = '#95a5a6'
        else:
            status = 'incomplete'
            status_label = 'Incomplete'
            status_color = '#e74c3c'

        # Use original if exists, otherwise use first reopt
        display_session = original if original else latest_reopt

        if display_session:
            wfo_cycles.append({
                'base_name': base_name,
                'status': status,
                'status_label': status_label,
                'status_color': status_color,
                'original': original,
                'reopt': latest_reopt,
                'all_reopts': reopts,
                'forward_test': forward_test,
                # Display metrics (from latest version)
                'display_name': original['name'] if original else latest_reopt['name'],
                'pair': display_session['pair'],
                'total_r': latest_reopt['total_r'] if latest_reopt else (original['total_r'] if original else 0),
                'win_rate': latest_reopt['win_rate'] if latest_reopt else (original['win_rate'] if original else 0),
                'trades': latest_reopt['trades'] if latest_reopt else (original['trades'] if original else 0),
                'profit_factor': latest_reopt.get('profit_factor', 0) if latest_reopt else (original.get('profit_factor', 0) if original else 0)
            })

    return wfo_cycles


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

    # Build set of forward test keys to exclude from main listing
    # These are already shown as part of their parent WFO cycle
    config = config_service.load_config()
    reopt_history = config.get('reoptimization_tracking', {}).get('history', [])
    forward_test_keys = set()
    for entry in reopt_history:
        ft = entry.get('forward_test')
        if ft:
            # Key format: "period/session"
            forward_test_keys.add(f"{ft['period']}/{ft['session']}")

    periods = []

    for period_dir in ARCHIVE_ROOT.iterdir():
        if not period_dir.is_dir():
            continue

        sessions = []
        for session_dir in sorted(period_dir.iterdir()):
            if not session_dir.is_dir():
                continue

            # Skip sessions that are forward tests (already shown in parent WFO cycle)
            session_key = f"{period_dir.name}/{session_dir.name}"
            if session_key in forward_test_keys:
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
                    data_range = data.get("data_range", {})

                    sessions.append({
                        "name": session_dir.name,
                        "pair": pair,
                        "total_r": perf.get("total_r", 0),
                        "win_rate": perf.get("win_rate", 0),
                        "trades": perf.get("total_trades", 0),
                        "profit_factor": perf.get("profit_factor", 0),
                        "max_dd": perf.get("max_drawdown_percent", 0),
                        "backtest_start_date": data_range.get("start"),
                        "backtest_end_date": data_range.get("end")
                    })

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

    # Auto-sync backtest settings to config.json
    # This ensures "Current Value" reflects the backtest being viewed
    backtest_settings = data.get("backtest_settings", {})
    if backtest_settings:
        # Extract pair from session name (e.g., 'EURUSD_all_sessions' -> 'EURUSD')
        pair = session.split('_')[0] if '_' in session else session
        config_service.update_current_settings_from_backtest(backtest_settings, pair)

    return {
        "overall_metrics": data.get("overall_performance", {}),
        "session_breakdown": data.get("session_breakdown", []),
        "metrics": data.get("performance", {}),
        "recommendations": data.get("parameters", {}),
        "backtest_settings": backtest_settings,
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

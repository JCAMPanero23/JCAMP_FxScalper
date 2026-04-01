"""Configuration service for WFO UI"""
from pathlib import Path
from typing import Dict, Any
import json

CONFIG_PATH = Path(__file__).parent.parent / "config.json"

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

def get_default_config() -> Dict[str, Any]:
    """Return default configuration"""
    return {
        "version": "1.0.0",
        "paths": {
            "ctrader_logs": detect_ctrader_path() or "C:/Users/Jcamp_Laptop/Documents/cAlgo/Data/cBots/Jcamp_1M_scalping/cAlgo/Trade_Logs/",
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
    """Load config from disk, creating default if missing"""
    if not CONFIG_PATH.exists():
        config = get_default_config()
        save_config(config)
        return config

    try:
        with open(CONFIG_PATH, 'r') as f:
            return json.load(f)
    except (json.JSONDecodeError, IOError) as e:
        # Config file is corrupted, recreate with defaults
        config = get_default_config()
        save_config(config)
        return config

def save_config(config: Dict[str, Any]) -> None:
    """Save configuration to file

    Raises:
        IOError: If unable to write config file
    """
    CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)

    try:
        with open(CONFIG_PATH, 'w') as f:
            json.dump(config, f, indent=2)
    except IOError as e:
        raise IOError(f"Failed to save config to {CONFIG_PATH}: {e}")

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

"""Configuration service for WFO UI"""
from pathlib import Path
from typing import Dict, Any
import json

CONFIG_PATH = Path(__file__).parent.parent / "config.json"

def get_default_config() -> Dict[str, Any]:
    """Returns the default configuration"""
    return {
        "version": "1.0.0",
        "paths": {
            "backtest_archive": str(Path(__file__).parent.parent.parent / "data" / "backtest_archive"),
            "temp_dir": str(Path(__file__).parent.parent.parent / "data" / "temp"),
            "ctrader_log_dir": detect_ctrader_path()
        },
        "behavior": {
            "auto_delete_csv": True,
            "delete_after_days": 7,
            "auto_archive": True
        },
        "export": {
            "default_format": "cbotset",
            "include_comments": True
        },
        "cbot_current_settings": {},
        "ui": {
            "default_view": "archive",
            "items_per_page": 20
        }
    }

def load_config() -> Dict[str, Any]:
    """Load config from disk, creating default if missing"""
    if not CONFIG_PATH.exists():
        config = get_default_config()
        save_config(config)
        return config

    with open(CONFIG_PATH, 'r') as f:
        return json.load(f)

def save_config(config: Dict[str, Any]) -> None:
    """Save config to disk"""
    CONFIG_PATH.parent.mkdir(parents=True, exist_ok=True)
    with open(CONFIG_PATH, 'w') as f:
        json.dump(config, f, indent=2)

def validate_config(config: Dict[str, Any]) -> Dict[str, Any]:
    """Validate config and create missing directories"""
    # Create missing directories
    for path_key in ["backtest_archive", "temp_dir"]:
        if path_key in config.get("paths", {}):
            path = Path(config["paths"][path_key])
            path.mkdir(parents=True, exist_ok=True)

    # Validate delete_after_days
    delete_days = config.get("behavior", {}).get("delete_after_days", 7)
    if delete_days < 0:
        raise ValueError("delete_after_days must be >= 0")

    return config

def detect_ctrader_path() -> str:
    """Auto-detect cTrader log directory"""
    possible_paths = [
        Path.home() / "Documents" / "cAlgo" / "Data" / "cBots",
        Path.home() / "Documents" / "cTrader" / "Data" / "cBots"
    ]

    for path in possible_paths:
        if path.exists():
            return str(path)

    return ""

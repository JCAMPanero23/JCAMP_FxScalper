import pytest
from pathlib import Path
import json
import os
import sys

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
    """Test that load_config returns existing config without overwriting"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    custom_config = {"version": "1.0.0", "paths": {"backtest_archive": "custom_path"}}
    config_path.write_text(json.dumps(custom_config, indent=2))
    config = config_service.load_config()
    assert config["paths"]["backtest_archive"] == "custom_path"

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
    assert "ctrader" in result["errors"][0].lower()


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
    """Test that save_config writes valid JSON to disk"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    config = config_service.get_default_config()
    config_service.save_config(config)
    assert config_path.exists()
    loaded = json.loads(config_path.read_text())
    assert loaded["version"] == config["version"]

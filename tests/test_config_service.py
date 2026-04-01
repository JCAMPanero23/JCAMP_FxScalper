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

def test_validate_config_creates_missing_directories(tmp_path, monkeypatch):
    """Test that validate_config creates directories that don't exist"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    config = config_service.get_default_config()
    config["paths"]["backtest_archive"] = str(tmp_path / "archive")
    config["paths"]["temp_dir"] = str(tmp_path / "temp")
    validated = config_service.validate_config(config)
    assert Path(validated["paths"]["backtest_archive"]).exists()
    assert Path(validated["paths"]["temp_dir"]).exists()

def test_validate_config_accepts_valid_delete_after_days(tmp_path, monkeypatch):
    """Test that validate_config accepts valid delete_after_days values"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    config = config_service.get_default_config()
    config["behavior"]["delete_after_days"] = 30
    validated = config_service.validate_config(config)
    assert validated["behavior"]["delete_after_days"] == 30

def test_validate_config_rejects_invalid_delete_after_days(tmp_path, monkeypatch):
    """Test that validate_config rejects delete_after_days < 0"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    config = config_service.get_default_config()
    config["behavior"]["delete_after_days"] = -5
    with pytest.raises(ValueError, match="delete_after_days must be >= 0"):
        config_service.validate_config(config)

def test_save_config_writes_valid_json(tmp_path, monkeypatch):
    """Test that save_config writes valid JSON to disk"""
    config_path = tmp_path / "config.json"
    monkeypatch.setattr(config_service, "CONFIG_PATH", config_path)
    config = config_service.get_default_config()
    config_service.save_config(config)
    assert config_path.exists()
    loaded = json.loads(config_path.read_text())
    assert loaded["version"] == config["version"]

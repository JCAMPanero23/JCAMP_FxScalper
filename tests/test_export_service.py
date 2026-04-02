import pytest
from pathlib import Path
import json
import xml.etree.ElementTree as ET
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import export_service


def test_validate_params_accepts_valid_params():
    """Test that validate_params accepts parameters within valid ranges"""
    params = {
        "MTF_SMA_Period": 275,
        "ADXPeriod": 18,
        "ADXMinThreshold": 15,
        "MinimumRR": 5.0,
        "DailyLossLimit": -3.0,
        "ConsecutiveLossLimit": 9,
        "MonthlyDDLimit": 10.0,
        "EnableLondonSession": True,
        "EnableNYSession": True,
        "EnableAsianSession": False,
        "Timeframe2": "M4",
        "Timeframe3": "M15",
        "ADXMode": "FlipDirection"
    }

    result = export_service.validate_params(params)

    assert result["valid"] is True
    assert len(result.get("errors", [])) == 0


def test_validate_params_rejects_out_of_range():
    """Test that validate_params rejects parameters outside valid ranges"""
    params = {
        "MTF_SMA_Period": 500,  # Out of range (should be 200-350)
        "ADXPeriod": 18,
        "ADXMinThreshold": 50,  # Out of range (should be 10-30)
        "MinimumRR": 5.0,
        "DailyLossLimit": -3.0,
        "ConsecutiveLossLimit": 9,
        "MonthlyDDLimit": 10.0,
        "EnableLondonSession": True,
        "EnableNYSession": True,
        "EnableAsianSession": False,
        "Timeframe2": "M4",
        "Timeframe3": "M15",
        "ADXMode": "FlipDirection"
    }

    result = export_service.validate_params(params)

    assert result["valid"] is False
    assert len(result["errors"]) > 0


def test_export_to_cbotset_generates_xml(tmp_path):
    """Test that export_to_cbotset creates a valid XML file"""
    params = {
        "MTF_SMA_Period": 275,
        "ADXPeriod": 18,
        "ADXMinThreshold": 15,
        "MinimumRR": 5.0,
        "DailyLossLimit": -3.0,
        "ConsecutiveLossLimit": 9,
        "MonthlyDDLimit": 10.0,
        "EnableLondonSession": True,
        "EnableNYSession": True,
        "EnableAsianSession": False,
        "Timeframe2": "M4",
        "Timeframe3": "M15",
        "ADXMode": "FlipDirection"
    }

    output_file = tmp_path / "test_export.cbotset"
    result = export_service.export_to_cbotset(params, str(output_file))

    assert result["success"] is True
    assert output_file.exists()

    # Verify it's valid XML
    tree = ET.parse(str(output_file))
    root = tree.getroot()
    assert root is not None


def test_export_to_cbotset_formats_bool_values(tmp_path):
    """Test that boolean values are formatted as lowercase 'true'/'false' in XML"""
    params = {
        "MTF_SMA_Period": 275,
        "ADXPeriod": 18,
        "ADXMinThreshold": 15,
        "MinimumRR": 5.0,
        "DailyLossLimit": -3.0,
        "ConsecutiveLossLimit": 9,
        "MonthlyDDLimit": 10.0,
        "EnableLondonSession": True,
        "EnableNYSession": False,
        "EnableAsianSession": False,
        "Timeframe2": "M4",
        "Timeframe3": "M15",
        "ADXMode": "FlipDirection"
    }

    output_file = tmp_path / "test_bool_export.cbotset"
    export_service.export_to_cbotset(params, str(output_file))

    # Read file and check boolean formatting
    content = output_file.read_text()
    assert "true" in content or "false" in content  # Lowercase required
    assert "True" not in content  # Python format not allowed
    assert "False" not in content  # Python format not allowed


def test_params_from_recommendations_extracts_params():
    """Test that params_from_recommendations extracts parameters from JSON recommendations"""
    recommendations = {
        "parameters": {
            "MTF_SMA_Period": 280,
            "ADXPeriod": 19,
            "ADXMinThreshold": 16
        },
        "performance": {
            "win_rate": 66.7,
            "profit_factor": 1.8
        }
    }

    result = export_service.params_from_recommendations(recommendations)

    assert "MTF_SMA_Period" in result
    assert result["MTF_SMA_Period"] == 280
    assert result["ADXPeriod"] == 19
    assert result["ADXMinThreshold"] == 16


def test_compare_with_current_settings_detects_changes():
    """Test that compare_with_current_settings detects parameter changes"""
    current = {
        "MTF_SMA_Period": 275,
        "ADXPeriod": 18,
        "ADXMinThreshold": 15,
        "EnableLondonSession": True
    }

    recommended = {
        "MTF_SMA_Period": 280,  # Changed
        "ADXPeriod": 18,        # Unchanged
        "ADXMinThreshold": 16,  # Changed
        "EnableLondonSession": True  # Unchanged
    }

    result = export_service.compare_with_current_settings(current, recommended)

    assert result["has_changes"] is True
    assert len(result["changes"]) > 0

    # Check that changes include the modified parameters
    changed_params = [change["parameter"] for change in result["changes"]]
    assert "MTF_SMA_Period" in changed_params
    assert "ADXMinThreshold" in changed_params

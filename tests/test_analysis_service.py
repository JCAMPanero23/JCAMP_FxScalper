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

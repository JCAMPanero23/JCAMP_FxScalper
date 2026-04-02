import pytest
from pathlib import Path
import json
import shutil
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import archive_service


def test_get_archive_tree_returns_empty_when_no_archive(tmp_path, monkeypatch):
    """Test that get_archive_tree returns empty when archive doesn't exist"""
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", tmp_path / "nonexistent")

    result = archive_service.get_archive_tree()

    assert result["periods"] == []
    assert result["total_pages"] == 0


def test_get_archive_tree_returns_periods_and_sessions(tmp_path, monkeypatch):
    """Test that get_archive_tree returns structured data"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create test structure
    q1_2025 = archive_root / "Q1_2025"
    london = q1_2025 / "london_session" / "analysis_results"
    london.mkdir(parents=True)

    # Create mock JSON
    json_data = {
        "performance": {
            "total_r": 45.2,
            "win_rate": 45.0,
            "total_trades": 125
        }
    }
    (london / "recommended_settings.json").write_text(json.dumps(json_data))

    result = archive_service.get_archive_tree()

    assert len(result["periods"]) == 1
    assert result["periods"][0]["name"] == "Q1_2025"
    assert len(result["periods"][0]["sessions"]) == 1
    assert result["periods"][0]["sessions"][0]["name"] == "london_session"
    assert result["periods"][0]["sessions"][0]["total_r"] == 45.2


def test_get_archive_tree_paginates_results(tmp_path, monkeypatch):
    """Test that get_archive_tree paginates large result sets"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create 25 periods
    for i in range(25):
        period_dir = archive_root / f"Period_{i}"
        period_dir.mkdir(parents=True)

    result = archive_service.get_archive_tree(page=1, per_page=20)

    assert len(result["periods"]) == 20
    assert result["total_pages"] == 2
    assert result["current_page"] == 1


def test_get_analysis_detail_returns_data(tmp_path, monkeypatch):
    """Test that get_analysis_detail returns analysis data"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create test structure
    results_dir = archive_root / "Q1_2025" / "london" / "analysis_results"
    results_dir.mkdir(parents=True)

    json_data = {
        "performance": {"win_rate": 45.0},
        "parameters": {"ADXThreshold": 19}
    }
    (results_dir / "recommended_settings.json").write_text(json.dumps(json_data))
    (results_dir / "dashboard.png").write_text("fake image")

    # Create CSV
    csv_path = archive_root / "Q1_2025" / "london" / "TradeLog.csv"
    csv_path.write_text("test,data")

    data = archive_service.get_analysis_detail("Q1_2025", "london")

    assert data["metrics"]["win_rate"] == 45.0
    assert data["recommendations"]["ADXThreshold"] == 19
    assert "dashboard.png" in data["chart_path"]


def test_create_archive_entry_copies_files(tmp_path, monkeypatch):
    """Test that create_archive_entry copies files correctly"""
    archive_root = tmp_path / "archive"
    monkeypatch.setattr(archive_service, "ARCHIVE_ROOT", archive_root)

    # Create source files
    csv_source = tmp_path / "source" / "TradeLog.csv"
    csv_source.parent.mkdir()
    csv_source.write_text("test,data")

    results_source = tmp_path / "results"
    results_source.mkdir()
    (results_source / "settings.json").write_text("{}")

    # Mock config to disable auto-cleanup
    mock_config = {"behavior": {"auto_cleanup": False}}
    monkeypatch.setattr("wfo_ui.services.config_service.load_config", lambda: mock_config)

    result = archive_service.create_archive_entry(
        "Q1_2025",
        "london",
        str(csv_source),
        str(results_source)
    )

    # Verify files copied
    assert Path(result["archived_csv_path"]).exists()
    assert Path(result["archived_results_path"]).exists()
    assert (archive_root / "Q1_2025" / "london" / "TradeLog.csv").exists()

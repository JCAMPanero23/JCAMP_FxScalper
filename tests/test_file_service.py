import pytest
from pathlib import Path
import shutil
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.services import file_service


def test_safe_delete_removes_original_when_archive_exists(tmp_path):
    """Test that safe_delete removes original file after verifying archive"""
    # Setup
    original = tmp_path / "original.csv"
    archive = tmp_path / "archive" / "original.csv"
    archive.parent.mkdir(parents=True)

    original.write_text("test data")
    shutil.copy2(original, archive)

    # Execute
    file_service.safe_delete(str(original), str(archive))

    # Verify
    assert not original.exists(), "Original file should be deleted"
    assert archive.exists(), "Archive file should remain"


def test_safe_delete_raises_when_archive_missing(tmp_path):
    """Test that safe_delete raises error when archive doesn't exist"""
    original = tmp_path / "original.csv"
    original.write_text("test data")

    with pytest.raises(ValueError, match="Archive not found"):
        file_service.safe_delete(str(original), str(tmp_path / "nonexistent.csv"))

    assert original.exists(), "Original should not be deleted"


def test_safe_delete_raises_when_size_mismatch(tmp_path):
    """Test that safe_delete raises error when file sizes don't match"""
    original = tmp_path / "original.csv"
    archive = tmp_path / "archive" / "original.csv"
    archive.parent.mkdir(parents=True)

    original.write_text("test data")
    archive.write_text("different data")  # Different size

    with pytest.raises(ValueError, match="size mismatch"):
        file_service.safe_delete(str(original), str(archive))

    assert original.exists(), "Original should not be deleted"


def test_log_action_creates_log_file(tmp_path, monkeypatch):
    """Test that log_action writes to log file"""
    log_path = tmp_path / "actions.log"
    monkeypatch.setattr(file_service, "LOG_PATH", log_path)

    file_service.log_action("Test action")

    assert log_path.exists()
    content = log_path.read_text()
    assert "Test action" in content


def test_cleanup_temp_files_removes_old_files(tmp_path, monkeypatch):
    """Test that cleanup_temp_files removes files from temp directory"""
    temp_dir = tmp_path / "temp"
    temp_dir.mkdir()
    monkeypatch.setattr(file_service, "TEMP_DIR", temp_dir)

    # Create test files
    (temp_dir / "file1.txt").write_text("test")
    (temp_dir / "file2.txt").write_text("test")

    file_service.cleanup_temp_files()

    # Verify files removed
    assert len(list(temp_dir.iterdir())) == 0

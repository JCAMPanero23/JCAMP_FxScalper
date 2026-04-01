"""File operations service for WFO UI"""
import os
import shutil
from pathlib import Path
from datetime import datetime
from typing import Optional


# Log file location
LOG_PATH = Path(__file__).parent.parent.parent / "data" / "temp" / "actions.log"
TEMP_DIR = Path(__file__).parent.parent.parent / "data" / "temp"


def safe_delete(csv_path: str, archive_path: str) -> None:
    """Delete original CSV only if archive verified

    Args:
        csv_path: Path to original CSV file
        archive_path: Path to archived copy

    Raises:
        ValueError: If archive not found or size mismatch
    """
    csv_file = Path(csv_path)
    archive_file = Path(archive_path)

    # 1. Verify archive exists
    if not archive_file.exists():
        raise ValueError("Archive not found. Refusing to delete original.")

    # 2. Verify file sizes match
    orig_size = os.path.getsize(csv_path)
    arch_size = os.path.getsize(archive_path)

    if orig_size != arch_size:
        raise ValueError("Archive size mismatch. Refusing to delete.")

    # 3. Only then delete
    os.remove(csv_path)

    # 4. Log deletion
    log_action(f"Deleted {csv_path} after archiving to {archive_path}")


def log_action(message: str) -> None:
    """Log action to file"""
    LOG_PATH.parent.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.now().isoformat()
    log_entry = f"[{timestamp}] {message}\n"

    with open(LOG_PATH, "a") as f:
        f.write(log_entry)


def cleanup_temp_files() -> None:
    """Remove all files from temp directory"""
    if not TEMP_DIR.exists():
        return

    for item in TEMP_DIR.iterdir():
        if item.is_file():
            item.unlink()
        elif item.is_dir():
            shutil.rmtree(item)

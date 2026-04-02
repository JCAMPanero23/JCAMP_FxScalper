#!/usr/bin/env python
"""WFO Browser UI Launcher

Simple launcher script that starts the Flask development server and optionally
opens a web browser.
"""
import sys
import webbrowser
from pathlib import Path
import time

# Add project to path
sys.path.insert(0, str(Path(__file__).parent))

from wfo_ui.services import config_service

def main():
    """Launch the WFO Browser UI"""
    print("=" * 60)
    print("JCAMP WFO Browser UI")
    print("=" * 60)

    # Load config
    try:
        config = config_service.load_config()
        print(f"[OK] Configuration loaded")
    except Exception as e:
        print(f"[ERROR] Failed to load config: {e}")
        return 1

    # Check if browser should auto-open
    auto_open = config.get("behavior", {}).get("auto_open_browser", True)

    print(f"[INFO] Starting Flask server at http://127.0.0.1:5000")
    print(f"[INFO] Auto-open browser: {auto_open}")
    print()
    print("Press CTRL+C to stop the server")
    print("=" * 60)

    # Open browser if enabled
    if auto_open:
        time.sleep(1)  # Give server time to start
        webbrowser.open("http://127.0.0.1:5000")

    # Start Flask app
    from wfo_ui.app import app
    try:
        app.run(host='127.0.0.1', port=5000, debug=True)
    except KeyboardInterrupt:
        print("\n[INFO] Server stopped")
        return 0
    except Exception as e:
        print(f"\n[ERROR] Server error: {e}")
        return 1

if __name__ == "__main__":
    sys.exit(main())

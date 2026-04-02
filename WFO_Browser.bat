@echo off
title JCAMP WFO Browser UI
cd /d "D:\JCAMP_FxScalper"

echo ============================================================
echo   JCAMP WFO Browser UI
echo ============================================================
echo.
echo   URL: http://127.0.0.1:5000
echo   Press CTRL+C to stop
echo.
echo ============================================================

:: Open browser after 2 second delay (in background)
start /b cmd /c "timeout /t 2 /nobreak >nul && start http://127.0.0.1:5000"

:: Run Flask (this blocks and shows output)
python -c "from wfo_ui.app import app; app.run(host='127.0.0.1', port=5000, debug=False)"

pause

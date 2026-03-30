@echo off
REM WFO Analyzer Runner for Windows
REM Automatically finds the latest trade log and runs analysis

echo ================================================
echo    WFO Trade Log Analyzer
echo ================================================
echo.

REM Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.9+ from https://www.python.org/downloads/
    echo.
    pause
    exit /b 1
)

REM Check if dependencies are installed
python -c "import pandas" >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing Python dependencies...
    pip install -r requirements.txt
    if %errorlevel% neq 0 (
        echo ERROR: Failed to install dependencies
        pause
        exit /b 1
    )
)

REM Default log directory
set LOG_DIR=C:\Users\Jcamp_Laptop\Documents\cAlgo\Trade_Logs

REM Check if custom path provided as argument
if "%~1" neq "" (
    set TRADE_LOG=%~1
    goto :analyze
)

REM Find the latest trade log file
echo Searching for trade logs in: %LOG_DIR%
echo.

if not exist "%LOG_DIR%\" (
    echo ERROR: Trade log directory not found: %LOG_DIR%
    echo.
    echo Please check that:
    echo 1. You have run at least one backtest with the modified cBot
    echo 2. The log directory path is correct
    echo.
    pause
    exit /b 1
)

REM Find newest CSV file
for /f "delims=" %%a in ('dir /b /o-d "%LOG_DIR%\TradeLog_*.csv" 2^>nul') do (
    set LATEST_LOG=%%a
    goto :found
)

:not_found
echo ERROR: No trade log files found in %LOG_DIR%
echo.
echo Please run a backtest with the modified cBot first.
echo.
pause
exit /b 1

:found
set TRADE_LOG=%LOG_DIR%\%LATEST_LOG%
echo Found latest trade log: %LATEST_LOG%
echo.

:analyze
echo Analyzing: %TRADE_LOG%
echo.
echo ================================================
echo.

REM Run the analyzer
python wfo_analyzer.py "%TRADE_LOG%"

if %errorlevel% equ 0 (
    echo.
    echo ================================================
    echo    Analysis Complete!
    echo ================================================
    echo.
    echo Results saved in: wfo_results\
    echo.
    echo Files created:
    echo - recommended_settings_*.json  ^(Full data^)
    echo - recommended_settings_*.csv   ^(cAlgo ready^)
    echo - recommended_settings_*.txt   ^(Human readable^)
    echo - analysis_dashboard_*.png     ^(Visualizations^)
    echo.

    REM Open the results folder
    echo Opening results folder...
    start "" "wfo_results"

) else (
    echo.
    echo ERROR: Analysis failed with error code %errorlevel%
    echo Please check the error messages above.
    echo.
)

pause

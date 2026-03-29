@echo off
REM ========================================
REM JCAMP_FxScalper - Sync FROM Source TO MT5
REM ========================================
REM Use this after editing files in VS Code or other editors
REM to copy changes to MT5 for compilation/testing
REM ========================================

echo.
echo ========================================
echo JCAMP_FxScalper - Syncing TO MT5
echo ========================================
echo.

set MT5_BASE=C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5
set SOURCE_BASE=D:\JCAMP_FxScalper\MQL5

echo Copying Main EA...
copy /Y "%SOURCE_BASE%\Experts\JCAMP_FxScalper_v1.mq5" "%MT5_BASE%\Experts\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy main EA
) else (
    echo [OK] Main EA synced
)

echo.
echo Copying Include files...
copy /Y "%SOURCE_BASE%\Include\JC_Utils.mqh" "%MT5_BASE%\Include\" >nul
copy /Y "%SOURCE_BASE%\Include\JC_RiskManager.mqh" "%MT5_BASE%\Include\" >nul
copy /Y "%SOURCE_BASE%\Include\JC_MarketStructure.mqh" "%MT5_BASE%\Include\" >nul
copy /Y "%SOURCE_BASE%\Include\JC_EntryLogic.mqh" "%MT5_BASE%\Include\" >nul
copy /Y "%SOURCE_BASE%\Include\JC_TradeManager.mqh" "%MT5_BASE%\Include\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy some include files
) else (
    echo [OK] All 5 include files synced
)

echo.
echo Copying Preset file...
copy /Y "%SOURCE_BASE%\Presets\JCAMP_FxScalper_EURUSD.set" "%MT5_BASE%\Presets\" >nul
if errorlevel 1 (
    echo [ERROR] Failed to copy preset
) else (
    echo [OK] Preset file synced
)

echo.
echo ========================================
echo Sync complete! Ready to compile in MT5.
echo ========================================
echo.
pause

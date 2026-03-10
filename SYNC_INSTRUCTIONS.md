# File Sync Instructions for JCAMP_FxScalper

## Files Successfully Installed ✅

All EA files have been copied to your MT5 installation:

**Location:** `C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\`

- ✅ Main EA: `Experts\JCAMP_FxScalper_v1.mq5` (14 KB)
- ✅ Include files: `Include\JC_*.mqh` (5 files, 62 KB total)
- ✅ Preset: `Presets\JCAMP_FxScalper_EURUSD.set`

---

## Editing Workflow

Since Windows symlinks require administrator privileges, I've created sync scripts for easy file management:

### Option 1: Edit in MetaEditor (Recommended for MT5 Development)

1. Open MetaEditor (F4 in MT5)
2. Navigate to `Experts\JCAMP_FxScalper_v1.mq5` or any include file
3. Make your changes
4. Save the file
5. **After editing, run:** `sync_from_mt5.bat`
   - This copies changes FROM MT5 back TO the source folder (`D:\JCAMP_FxScalper\`)
6. Compile in MetaEditor (F7)

### Option 2: Edit in VS Code or Other Editors

1. Open files from `D:\JCAMP_FxScalper\MQL5\` in your preferred editor
2. Make your changes
3. Save the file
4. **After editing, run:** `sync_to_mt5.bat`
   - This copies changes FROM source TO MT5 for compilation
5. Open MetaEditor and compile (F7)

---

## Sync Script Reference

### `sync_from_mt5.bat`
- **Purpose:** Copy changes FROM MetaEditor TO source folder
- **Use when:** You edited files in MetaEditor and want to save to source
- **Direction:** MT5 → D:\JCAMP_FxScalper\

### `sync_to_mt5.bat`
- **Purpose:** Copy changes FROM source folder TO MT5
- **Use when:** You edited files in VS Code/other editor and want to compile in MT5
- **Direction:** D:\JCAMP_FxScalper\ → MT5

---

## Quick Start: Compile and Test Now

**You're ready to compile immediately!** Files are already in MT5:

1. Open MT5 Terminal
2. Press **F4** to open MetaEditor
3. In Navigator, expand `Experts` folder
4. Double-click `JCAMP_FxScalper_v1.mq5`
5. Press **F7** to compile
6. **Expected Result:** 0 errors, 0 warnings

**If compilation succeeds:**
- The EA will appear in MT5 Navigator under "Expert Advisors"
- You can drag it onto a chart to test
- Load the preset `JCAMP_FxScalper_EURUSD.set` for optimized settings

---

## Advanced: Create Symlinks (Optional)

If you want true symlinks (changes in one location automatically reflect in the other), you need to:

1. Open Command Prompt **as Administrator**
2. Run these commands:

```cmd
cd D:\JCAMP_FxScalper

REM Delete copied files first (to replace with symlinks)
del "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Experts\JCAMP_FxScalper_v1.mq5"
del "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_*.mqh"
del "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Presets\JCAMP_FxScalper_EURUSD.set"

REM Create symlinks
mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Experts\JCAMP_FxScalper_v1.mq5" "D:\JCAMP_FxScalper\MQL5\Experts\JCAMP_FxScalper_v1.mq5"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_Utils.mqh" "D:\JCAMP_FxScalper\MQL5\Include\JC_Utils.mqh"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_RiskManager.mqh" "D:\JCAMP_FxScalper\MQL5\Include\JC_RiskManager.mqh"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_MarketStructure.mqh" "D:\JCAMP_FxScalper\MQL5\Include\JC_MarketStructure.mqh"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_EntryLogic.mqh" "D:\JCAMP_FxScalper\MQL5\Include\JC_EntryLogic.mqh"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Include\JC_TradeManager.mqh" "D:\JCAMP_FxScalper\MQL5\Include\JC_TradeManager.mqh"

mklink "C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\Presets\JCAMP_FxScalper_EURUSD.set" "D:\JCAMP_FxScalper\MQL5\Presets\JCAMP_FxScalper_EURUSD.set"
```

**With symlinks:**
- Edit anywhere (MetaEditor or VS Code) - changes reflect instantly
- No need for sync scripts
- Source control always up to date

---

## File Locations Summary

### Source Files (Edit here with VS Code)
```
D:\JCAMP_FxScalper\MQL5\
├── Experts\JCAMP_FxScalper_v1.mq5
├── Include\
│   ├── JC_Utils.mqh
│   ├── JC_RiskManager.mqh
│   ├── JC_MarketStructure.mqh
│   ├── JC_EntryLogic.mqh
│   └── JC_TradeManager.mqh
└── Presets\JCAMP_FxScalper_EURUSD.set
```

### MT5 Installation (Edit here with MetaEditor)
```
C:\Users\Jcamp_Laptop\AppData\Roaming\MetaQuotes\
Terminal\D0E8209F77C8CF37AD8BF550E51FF075\MQL5\
├── Experts\JCAMP_FxScalper_v1.mq5
├── Include\JC_*.mqh (5 files)
└── Presets\JCAMP_FxScalper_EURUSD.set
```

---

## Troubleshooting

### Files not showing in MetaEditor Navigator?
1. Press **F5** to refresh Navigator
2. Or restart MetaEditor

### Compilation errors about missing includes?
1. Verify all 5 JC_*.mqh files are in `MT5\Include\`
2. Check file names match exactly (case-sensitive)
3. Run `sync_to_mt5.bat` to re-copy files

### Changes not reflecting after editing?
1. If you edited in **MetaEditor** → Run `sync_from_mt5.bat`
2. If you edited in **VS Code** → Run `sync_to_mt5.bat`
3. Always recompile after syncing (F7 in MetaEditor)

---

## Next Steps

1. ✅ **Compile the EA** (F7 in MetaEditor) - Should show 0 errors
2. ✅ **Load on EURUSD M5 chart** (drag from Navigator)
3. ✅ **Load preset** (JCAMP_FxScalper_EURUSD.set in inputs)
4. ✅ **Run Strategy Tester** (Ctrl+R, configure as per TESTING_GUIDE.md)

**Full testing instructions:** See `Docs\TESTING_GUIDE.md`

---

**Status: ✅ Files installed and ready for compilation!**

"""Export service for generating .cbotset JSON files for cTrader"""
import json
from pathlib import Path
from typing import Dict, Any


# Default cBot parameters (full structure matching cTrader format)
DEFAULT_CBOT_PARAMS = {
    "MTFHeader": "",
    "EnableMTFSMAEntry": True,
    "MTFSMAPeriod": 250,
    "Timeframe2": "m4",
    "Timeframe3": "m15",
    "RequireAllTFsAligned": True,
    "ATRPeriod": 16,
    "SLATRMultiplier": 2.0,
    "MinimumSLPips": 5.0,
    "ADXHeader": "",
    "EnableADXFilter": True,
    "ADXMode": 1,  # 0=Trend, 1=FlipDirection
    "ADXPeriod": 16,
    "ADXMinThreshold": 35.0,
    "ADXMaxThreshold": 40.0,
    "TradeHeader": "",
    "EnableTrading": True,
    "RiskPercent": 1.0,
    "SLBufferPips": 2.0,
    "MinimumRRRatio": 4.0,
    "MaxPositions": 1,
    "MagicNumber": 100001,
    "SessionHeader": "",
    "EnableSessionFilter": True,
    "EnableLondonSession": True,
    "EnableNYSession": True,
    "EnableAsianSession": True,
    "ShowSessionBoxes": True,
    "HourFilterHeader": "",
    "EnableHourFilter": False,
    "StartHour": 8,
    "EndHour": 12,
    "DayFilterHeader": "",
    "EnableDayFilter": False,
    "TradeMonday": True,
    "TradeTuesday": True,
    "TradeWednesday": True,
    "TradeThursday": True,
    "TradeFriday": True,
    "DirectionFilterHeader": "",
    "DirectionFilter": 0,
    "ChandelierHeader": "",
    "EnableChandelierSL": True,
    "ChandelierActivationRR": 0.75,
    "TrailIncrementPips": 5.0,
    "TPModeSelection": 1,
    "MinChandelierDistance": 5.0,
    "ExhaustionHeader": "",
    "EnableExhaustionExit": False,
    "MinChandelierMovesBeforeExit": 2,
    "ExhaustionSwingBars": 8,
    "ExhaustionRSIPeriod": 14,
    "RiskHeader": "",
    "EnableDailyLossLimit": True,
    "MaxDailyRLoss": -3.0,
    "MaxDailyLosingTrades": 5,
    "EnableConsecutiveLossLimit": True,
    "MaxConsecutiveLosses": 9,
    "EnableMonthlyDrawdownLimit": True,
    "MaxMonthlyDrawdownPercent": 10.0,
    "DiagnosticsHeader": "",
    "EnableDiagnostics": False,
    "DiagnosticIntervalBars": 60
}

# Mapping from WFO analyzer output names to cBot parameter names
PARAM_NAME_MAPPING = {
    "MTF_SMA_Period": "MTFSMAPeriod",
    "ADXPeriod": "ADXPeriod",
    "ADXMinThreshold": "ADXMinThreshold",
    "ADXMode": "ADXMode",
    "MinimumRR": "MinimumRRRatio",
    "DailyLossLimit": "MaxDailyRLoss",
    "ConsecutiveLossLimit": "MaxConsecutiveLosses",
    "MonthlyDDLimit": "MaxMonthlyDrawdownPercent",
    "EnableLondonSession": "EnableLondonSession",
    "EnableNYSession": "EnableNYSession",
    "EnableAsianSession": "EnableAsianSession",
    "Timeframe2": "Timeframe2",
    "Timeframe3": "Timeframe3",
}

# ADX Mode string to int mapping
ADX_MODE_MAP = {
    "Trend": 0,
    "FlipDirection": 1,
}


def _convert_value(param_name: str, value: Any) -> Any:
    """Convert parameter value to cBot format"""
    # ADXMode: string to int
    if param_name == "ADXMode" and isinstance(value, str):
        return ADX_MODE_MAP.get(value, 1)

    # Timeframes: uppercase to lowercase
    if param_name in ["Timeframe2", "Timeframe3"] and isinstance(value, str):
        return value.lower()

    # Ensure floats for threshold values
    if param_name in ["ADXMinThreshold", "MinimumRRRatio", "MaxDailyRLoss", "MaxMonthlyDrawdownPercent"]:
        return float(value)

    # Ensure ints for integer params
    if param_name in ["MTFSMAPeriod", "ADXPeriod", "MaxConsecutiveLosses"]:
        return int(value)

    return value


def export_to_cbotset(params: Dict[str, Any], output_path: str) -> Dict[str, Any]:
    """Generate .cbotset JSON file for cTrader

    Args:
        params: Dictionary of parameters from WFO analysis
        output_path: Path where .cbotset file should be written

    Returns:
        {"success": bool, "message": str, "file_path": str} or
        {"success": bool, "error": str}
    """
    try:
        # Start with default parameters
        cbot_params = DEFAULT_CBOT_PARAMS.copy()

        # Map and apply recommended parameters
        for wfo_name, value in params.items():
            cbot_name = PARAM_NAME_MAPPING.get(wfo_name, wfo_name)
            if cbot_name in cbot_params:
                cbot_params[cbot_name] = _convert_value(cbot_name, value)

        # Build the cbotset structure
        cbotset = {
            "Chart": {
                "Symbol": "EURUSD",
                "Period": "m1"
            },
            "Parameters": cbot_params
        }

        # Write to file with BOM for cTrader compatibility
        output_path_obj = Path(output_path)
        output_path_obj.parent.mkdir(parents=True, exist_ok=True)

        with open(output_path_obj, 'w', encoding='utf-8-sig') as f:
            json.dump(cbotset, f, indent=2)

        return {
            "success": True,
            "message": f"Successfully exported to {output_path}",
            "file_path": str(output_path_obj)
        }
    except Exception as e:
        return {
            "success": False,
            "error": f"Failed to write .cbotset file: {str(e)}"
        }


def validate_params(params: Dict[str, Any]) -> Dict[str, Any]:
    """Validate parameters (simplified validation)

    Args:
        params: Dictionary of parameters to validate

    Returns:
        {"valid": bool, "errors": [str, ...]}
    """
    errors = []

    # Basic validation - just check types
    for param_name, value in params.items():
        if param_name == "ADXMode" and isinstance(value, str):
            if value not in ADX_MODE_MAP:
                errors.append(f"Invalid ADXMode: {value}")

    return {
        "valid": len(errors) == 0,
        "errors": errors
    }


def compare_with_current_settings(
    current: Dict[str, Any],
    recommended: Dict[str, Any]
) -> Dict[str, Any]:
    """Compare current settings with recommended settings

    Args:
        current: Dictionary of current settings
        recommended: Dictionary of recommended settings

    Returns:
        {"has_changes": bool, "changes": [...]}
    """
    changes = []

    for param_name, recommended_value in recommended.items():
        current_value = current.get(param_name)

        if current_value != recommended_value:
            changes.append({
                "parameter": param_name,
                "current_value": current_value,
                "recommended_value": recommended_value
            })

    return {
        "has_changes": len(changes) > 0,
        "changes": changes
    }

"""Export service for generating .cbotset XML files for cTrader"""
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Dict, Any, List


# Parameter type definitions with validation rules
PARAM_TYPES = {
    "MTF_SMA_Period": {"type": "int", "min": 200, "max": 350, "default": 275},
    "ADXPeriod": {"type": "int", "min": 10, "max": 30, "default": 18},
    "ADXMinThreshold": {"type": "int", "min": 10, "max": 30, "default": 15},
    "MinimumRR": {"type": "float", "min": 1.0, "max": 10.0, "default": 5.0},
    "DailyLossLimit": {"type": "float", "min": -10.0, "max": 0.0, "default": -3.0},
    "ConsecutiveLossLimit": {"type": "int", "min": 5, "max": 50, "default": 9},
    "MonthlyDDLimit": {"type": "float", "min": 5.0, "max": 30.0, "default": 10.0},
    "EnableLondonSession": {"type": "bool", "default": True},
    "EnableNYSession": {"type": "bool", "default": True},
    "EnableAsianSession": {"type": "bool", "default": False},
    "ADXMode": {"type": "string", "allowed": ["FlipDirection", "Trend"], "default": "FlipDirection"},
    "Timeframe2": {"type": "string", "allowed": ["M2", "M3", "M4", "M5"], "default": "M4"},
    "Timeframe3": {"type": "string", "allowed": ["M10", "M15", "M20", "M30"], "default": "M15"},
}

# Full CBOT settings structure with categories
FULL_CBOT_SETTINGS = {
    "Session Config": [
        "EnableLondonSession",
        "EnableNYSession",
        "EnableAsianSession",
    ],
    "ADX Settings": [
        "ADXMode",
        "ADXPeriod",
        "ADXMinThreshold",
    ],
    "MTF Settings": [
        "MTF_SMA_Period",
        "Timeframe2",
        "Timeframe3",
    ],
    "Risk Management": [
        "MinimumRR",
        "DailyLossLimit",
        "ConsecutiveLossLimit",
        "MonthlyDDLimit",
    ],
}


def validate_params(params: Dict[str, Any]) -> Dict[str, Any]:
    """Validate parameters against type definitions and ranges

    Args:
        params: Dictionary of parameters to validate

    Returns:
        {"valid": bool, "errors": [str, ...]}
    """
    errors = []

    for param_name, param_value in params.items():
        if param_name not in PARAM_TYPES:
            errors.append(f"Unknown parameter: {param_name}")
            continue

        param_def = PARAM_TYPES[param_name]
        param_type = param_def["type"]

        # Type checking
        if param_type == "int":
            if not isinstance(param_value, int) or isinstance(param_value, bool):
                errors.append(f"{param_name} must be an integer")
                continue

            # Range checking
            if "min" in param_def and param_value < param_def["min"]:
                errors.append(f"{param_name} must be >= {param_def['min']}")
            if "max" in param_def and param_value > param_def["max"]:
                errors.append(f"{param_name} must be <= {param_def['max']}")

        elif param_type == "float":
            if not isinstance(param_value, (int, float)) or isinstance(param_value, bool):
                errors.append(f"{param_name} must be a number")
                continue

            # Range checking
            if "min" in param_def and param_value < param_def["min"]:
                errors.append(f"{param_name} must be >= {param_def['min']}")
            if "max" in param_def and param_value > param_def["max"]:
                errors.append(f"{param_name} must be <= {param_def['max']}")

        elif param_type == "bool":
            if not isinstance(param_value, bool):
                errors.append(f"{param_name} must be a boolean")

        elif param_type == "string":
            if not isinstance(param_value, str):
                errors.append(f"{param_name} must be a string")
            elif "allowed" in param_def and param_value not in param_def["allowed"]:
                errors.append(f"{param_name} must be one of: {', '.join(param_def['allowed'])}")

    return {
        "valid": len(errors) == 0,
        "errors": errors
    }


def _format_value(value: Any) -> str:
    """Format a value for XML output

    Booleans are formatted as lowercase 'true'/'false'
    """
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def export_to_cbotset(params: Dict[str, Any], output_path: str) -> Dict[str, Any]:
    """Generate .cbotset XML file with validation

    Args:
        params: Dictionary of parameters to export
        output_path: Path where .cbotset file should be written

    Returns:
        {"success": bool, "message": str, "file_path": str} or
        {"success": bool, "error": str}
    """
    # Validate parameters first
    validation_result = validate_params(params)
    if not validation_result["valid"]:
        return {
            "success": False,
            "error": "Parameter validation failed",
            "details": validation_result["errors"]
        }

    # Create root XML element
    root = ET.Element("cBotSettings")
    root.set("version", "1.0")

    # Add parameters grouped by category
    for category, param_names in FULL_CBOT_SETTINGS.items():
        category_elem = ET.SubElement(root, "Category")
        category_elem.set("name", category)

        for param_name in param_names:
            if param_name in params:
                param_elem = ET.SubElement(category_elem, "Parameter")
                param_elem.set("name", param_name)
                param_elem.set("value", _format_value(params[param_name]))

    # Write to file
    try:
        output_path_obj = Path(output_path)
        output_path_obj.parent.mkdir(parents=True, exist_ok=True)

        # Create tree and write with proper formatting
        tree = ET.ElementTree(root)
        tree.write(str(output_path_obj), encoding="utf-8", xml_declaration=True)

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


def params_from_recommendations(recommendations: Dict[str, Any]) -> Dict[str, Any]:
    """Extract parameters from recommendation JSON

    Args:
        recommendations: JSON object with 'parameters' and other analysis data

    Returns:
        Dictionary of extracted parameters
    """
    if "parameters" not in recommendations:
        return {}

    return recommendations["parameters"]


def compare_with_current_settings(
    current: Dict[str, Any],
    recommended: Dict[str, Any]
) -> Dict[str, Any]:
    """Compare current settings with recommended settings and detect changes

    Args:
        current: Dictionary of current settings
        recommended: Dictionary of recommended settings

    Returns:
        {
            "has_changes": bool,
            "changes": [
                {
                    "parameter": str,
                    "current_value": Any,
                    "recommended_value": Any
                },
                ...
            ]
        }
    """
    changes = []

    # Check all parameters in recommended settings
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

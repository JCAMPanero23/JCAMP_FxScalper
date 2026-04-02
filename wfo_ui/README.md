# JCAMP WFO Browser UI

Flask-based web interface for Walk-Forward Optimization analysis.

## Quick Start

```bash
# Launch the UI
python launch_wfo_ui.py
```

The browser will automatically open to http://127.0.0.1:5000

## Features

- Archive browser for viewing historical backtest results
- Detailed analysis view with metrics and recommendations
- Side-by-side period comparison
- Settings configuration interface
- .cbotset file export for cTrader

## Structure

- `wfo_ui/app.py` - Flask application and routes
- `wfo_ui/services/` - Service layer (config, file, analysis, archive, export)
- `wfo_ui/templates/` - Jinja2 HTML templates
- `wfo_ui/static/` - CSS and JavaScript files
- `tests/` - Test suite

## Configuration

Settings are stored in `wfo_ui/config.json`. Edit via the Settings page or manually.

## Development

```bash
# Run tests
pytest tests/

# Run with debug enabled
python -m wfo_ui.app
```

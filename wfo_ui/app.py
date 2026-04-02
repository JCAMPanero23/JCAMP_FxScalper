"""Flask application for WFO Browser UI"""
from flask import Flask, render_template, request, redirect, url_for, flash, send_file
from pathlib import Path
import secrets
import json
import io

from wfo_ui.services import (
    config_service,
    archive_service,
    export_service,
    analysis_service,
    file_service
)

# Initialize Flask app
app = Flask(__name__, template_folder='templates', static_folder='static')
app.config['SECRET_KEY'] = secrets.token_hex(16)
app.config['MAX_CONTENT_LENGTH'] = 50 * 1024 * 1024  # 50MB max file size

# Configure upload folder
UPLOAD_FOLDER = Path(__file__).parent.parent / "data" / "temp"
UPLOAD_FOLDER.mkdir(parents=True, exist_ok=True)
app.config['UPLOAD_FOLDER'] = str(UPLOAD_FOLDER)


@app.route('/')
def index():
    """Home page - Archive browser"""
    try:
        # Get pagination parameters
        page = request.args.get('page', 1, type=int)
        per_page = request.args.get('per_page', 20, type=int)

        # Validate per_page range
        if per_page < 10 or per_page > 100:
            per_page = 20

        # Get archive tree with pagination
        archive_data = archive_service.get_archive_tree(page=page, per_page=per_page)

        return render_template(
            'index.html',
            periods=archive_data.get('periods', []),
            total_pages=archive_data.get('total_pages', 0),
            current_page=archive_data.get('current_page', 1)
        )
    except Exception as e:
        flash(f'Error loading archive: {str(e)}', 'error')
        return render_template('index.html', periods=[], total_pages=0, current_page=1), 500


@app.route('/analysis/<period>/<session>')
def analysis(period, session):
    """Analysis detail page"""
    try:
        # Sanitize period and session parameters (basic security)
        if not (period.isalnum() or '_' in period):
            flash('Invalid period name', 'error')
            return redirect(url_for('index'))

        if not (session.isalnum() or '_' in session):
            flash('Invalid session name', 'error')
            return redirect(url_for('index'))

        # Get analysis details
        analysis_detail = archive_service.get_analysis_detail(period, session)

        # Check if data was found
        if not analysis_detail.get('metrics') and not analysis_detail.get('recommendations'):
            flash(f'No analysis found for {period}/{session}', 'warning')
            return redirect(url_for('index'))

        # Load current config for comparison
        config = config_service.load_config()
        current_settings = config.get('cbot_current_settings', {})

        # Compare current vs recommended
        comparison = export_service.compare_with_current_settings(
            current_settings,
            analysis_detail.get('recommendations', {})
        )

        # Convert file paths to URLs
        chart_url = None
        csv_url = None

        if analysis_detail.get('chart_path'):
            chart_filename = Path(analysis_detail['chart_path']).name
            chart_url = f"/archive/{period}/{session}/{chart_filename}"

        if analysis_detail.get('csv_path'):
            csv_filename = Path(analysis_detail['csv_path']).name
            csv_url = f"/archive/{period}/{session}/{csv_filename}"

        return render_template(
            'analysis.html',
            period=period,
            session=session,
            overall_metrics=analysis_detail.get('overall_metrics', {}),
            session_breakdown=analysis_detail.get('session_breakdown', []),
            metrics=analysis_detail.get('metrics', {}),
            recommendations=analysis_detail.get('recommendations', {}),
            chart_path=chart_url,
            csv_path=csv_url,
            current_settings=current_settings,
            comparison=comparison
        )
    except Exception as e:
        flash(f'Error loading analysis: {str(e)}', 'error')
        return redirect(url_for('index')), 500


@app.route('/archive/<period>/<session>/<filename>')
def serve_archive_file(period, session, filename):
    """Serve files from archive directory"""
    try:
        # Get config to find archive directory
        config = config_service.load_config()
        archive_dir = Path(config.get('paths', {}).get('archive', 'data/backtest_archive'))

        # Construct file path
        file_path = archive_dir / period / session / 'analysis_results' / filename

        # Security check - ensure path is within archive directory
        if not file_path.resolve().is_relative_to(archive_dir.resolve()):
            flash('Invalid file path', 'error')
            return redirect(url_for('index')), 403

        # Check if file exists
        if not file_path.exists():
            flash(f'File not found: {filename}', 'error')
            return redirect(url_for('index')), 404

        # Serve the file
        return send_file(file_path)
    except Exception as e:
        flash(f'Error serving file: {str(e)}', 'error')
        return redirect(url_for('index')), 500


@app.route('/compare', methods=['GET', 'POST'])
def compare():
    """Comparison page"""
    try:
        # Get archive tree for dropdown options
        archive_data = archive_service.get_archive_tree(per_page=1000)
        periods = archive_data.get('periods', [])

        # Initialize comparison data
        comparison_data = {
            'period1': None,
            'session1': None,
            'period2': None,
            'session2': None,
            'analysis1': None,
            'analysis2': None
        }

        # Handle query parameters or form submission
        period1 = request.args.get('period1') or request.form.get('period1')
        session1 = request.args.get('session1') or request.form.get('session1')
        period2 = request.args.get('period2') or request.form.get('period2')
        session2 = request.args.get('session2') or request.form.get('session2')

        # If both selections provided, fetch details
        if period1 and session1 and period2 and session2:
            try:
                analysis1 = archive_service.get_analysis_detail(period1, session1)
                analysis2 = archive_service.get_analysis_detail(period2, session2)

                comparison_data = {
                    'period1': period1,
                    'session1': session1,
                    'period2': period2,
                    'session2': session2,
                    'analysis1': analysis1,
                    'analysis2': analysis2
                }
            except Exception as e:
                flash(f'Error loading comparison data: {str(e)}', 'error')

        return render_template(
            'compare.html',
            periods=periods,
            selected_period1=period1,
            selected_session1=session1,
            selected_period2=period2,
            selected_session2=session2,
            comparison_data=comparison_data
        )
    except Exception as e:
        flash(f'Error loading comparison page: {str(e)}', 'error')
        archive_data = archive_service.get_archive_tree(per_page=1000)
        periods = archive_data.get('periods', [])
        return render_template('compare.html', periods=periods, comparison_data={}), 500


@app.route('/settings', methods=['GET', 'POST'])
def settings():
    """Settings page - GET to view, POST to save"""
    try:
        if request.method == 'POST':
            # Load current config
            config = config_service.load_config()

            # Update paths if provided
            if request.form.get('ctrader_logs_path'):
                config['paths']['ctrader_logs'] = request.form.get('ctrader_logs_path')

            if request.form.get('archive_path'):
                config['paths']['archive'] = request.form.get('archive_path')

            # Update behavior settings
            if request.form.get('auto_cleanup'):
                config['behavior']['auto_cleanup'] = request.form.get('auto_cleanup') == 'on'

            if request.form.get('auto_open_browser'):
                config['behavior']['auto_open_browser'] = request.form.get('auto_open_browser') == 'on'

            if request.form.get('results_per_page'):
                try:
                    config['behavior']['results_per_page'] = int(request.form.get('results_per_page'))
                except ValueError:
                    flash('results_per_page must be a number', 'error')
                    return redirect(url_for('settings'))

            if request.form.get('dark_mode'):
                config['behavior']['dark_mode'] = request.form.get('dark_mode') == 'on'

            # Update CBOT settings
            cbot_keys = [
                'EnableLondonSession', 'EnableNYSession', 'EnableAsianSession',
                'ADXMode', 'ADXPeriod', 'ADXMinThreshold',
                'MTF_SMA_Period', 'Timeframe2', 'Timeframe3',
                'MinimumRR', 'DailyLossLimit', 'ConsecutiveLossLimit', 'MonthlyDDLimit'
            ]

            for key in cbot_keys:
                if key in request.form:
                    value = request.form.get(key)

                    # Type conversion based on key
                    if key in ['EnableLondonSession', 'EnableNYSession', 'EnableAsianSession']:
                        config['cbot_current_settings'][key] = value == 'on'
                    elif key in ['ADXPeriod', 'MTF_SMA_Period', 'ConsecutiveLossLimit']:
                        try:
                            config['cbot_current_settings'][key] = int(value)
                        except ValueError:
                            flash(f'{key} must be an integer', 'error')
                            return redirect(url_for('settings'))
                    elif key in ['MinimumRR', 'DailyLossLimit', 'MonthlyDDLimit', 'ADXMinThreshold']:
                        try:
                            config['cbot_current_settings'][key] = float(value)
                        except ValueError:
                            flash(f'{key} must be a number', 'error')
                            return redirect(url_for('settings'))
                    else:
                        config['cbot_current_settings'][key] = value

            # Validate configuration
            validation = config_service.validate_config(config)
            if not validation['valid']:
                for error in validation['errors']:
                    flash(f'Validation error: {error}', 'error')
                return redirect(url_for('settings'))

            # Save configuration
            try:
                config_service.save_config(config)
                flash('Settings saved successfully', 'success')
            except IOError as e:
                flash(f'Error saving settings: {str(e)}', 'error')
                return redirect(url_for('settings'))

            return redirect(url_for('settings'))

        # GET request - load and display settings
        config = config_service.load_config()

        return render_template(
            'settings.html',
            config=config,
            paths=config.get('paths', {}),
            behavior=config.get('behavior', {}),
            export=config.get('export', {}),
            cbot_settings=config.get('cbot_current_settings', {}),
            ui=config.get('ui', {})
        )
    except Exception as e:
        flash(f'Error loading settings: {str(e)}', 'error')
        config = config_service.load_config()
        return render_template(
            'settings.html',
            config=config,
            paths=config.get('paths', {}),
            behavior=config.get('behavior', {}),
            export=config.get('export', {}),
            cbot_settings=config.get('cbot_current_settings', {}),
            ui=config.get('ui', {})
        ), 500


@app.route('/export/<period>/<session>', methods=['POST'])
def export(period, session):
    """Export .cbotset file"""
    try:
        # Sanitize parameters
        if not (period.isalnum() or '_' in period):
            flash('Invalid period name', 'error')
            return redirect(url_for('index'))

        if not (session.isalnum() or '_' in session):
            flash('Invalid session name', 'error')
            return redirect(url_for('index'))

        # Get analysis data
        analysis_detail = archive_service.get_analysis_detail(period, session)
        recommendations = analysis_detail.get('recommendations', {})

        if not recommendations:
            flash('No recommendations found to export', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Generate temporary file path
        temp_dir = Path(app.config['UPLOAD_FOLDER'])
        temp_dir.mkdir(parents=True, exist_ok=True)

        temp_file = temp_dir / f"export_{period}_{session}.cbotset"

        # Export to .cbotset file
        export_result = export_service.export_to_cbotset(
            recommendations,
            str(temp_file)
        )

        if not export_result['success']:
            error_msg = export_result.get('error', 'Unknown error')
            details = export_result.get('details', [])
            if details:
                error_msg = f"{error_msg}: {'; '.join(details)}"
            flash(f'Export failed: {error_msg}', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Send file to user
        try:
            # Create filename for download
            filename = f"Jcamp_1M_scalping_{period}_{session}.cbotset"

            return send_file(
                str(temp_file),
                mimetype='application/xml',
                as_attachment=True,
                download_name=filename
            )
        except Exception as e:
            flash(f'Error sending file: {str(e)}', 'error')
            return redirect(url_for('analysis', period=period, session=session))

    except Exception as e:
        flash(f'Export error: {str(e)}', 'error')
        return redirect(url_for('index')), 500


@app.route('/import')
def import_page():
    """Show import/new analysis page"""
    try:
        config = config_service.load_config()
        ctrader_path = config.get('paths', {}).get('ctrader_logs', '')

        # List available CSV files
        available_csvs = []
        if ctrader_path and Path(ctrader_path).exists():
            csv_path = Path(ctrader_path)
            for csv_file in sorted(csv_path.glob('TradeLog*.csv'), reverse=True):
                available_csvs.append({
                    'name': csv_file.name,
                    'path': str(csv_file),
                    'size': f"{csv_file.stat().st_size / 1024:.1f} KB",
                    'date': csv_file.stat().st_mtime
                })
                # Format date
                from datetime import datetime
                available_csvs[-1]['date'] = datetime.fromtimestamp(
                    available_csvs[-1]['date']
                ).strftime('%Y-%m-%d %H:%M')

        return render_template('import.html',
                             available_csvs=available_csvs,
                             ctrader_path=ctrader_path)
    except Exception as e:
        flash(f'Error loading import page: {str(e)}', 'error')
        return redirect(url_for('index'))


@app.route('/import/analyze', methods=['POST'])
def import_analyze():
    """Run WFO analysis and archive the results"""
    try:
        csv_path = request.form.get('csv_file')
        period = request.form.get('period', '').strip()
        session = request.form.get('session', '').strip()

        if not csv_path or not period or not session:
            flash('Please fill in all fields', 'error')
            return redirect(url_for('import_page'))

        if not Path(csv_path).exists():
            flash('CSV file not found', 'error')
            return redirect(url_for('import_page'))

        # Run WFO analysis
        flash('Running WFO analysis... this may take 1-2 minutes', 'info')
        analysis_result = analysis_service.run_analysis(csv_path, period, session)

        if not analysis_result.get('success'):
            flash(f"Analysis failed: {analysis_result.get('error')}", 'error')
            flash(f"Details: {analysis_result.get('details')}", 'error')
            return redirect(url_for('import_page'))

        # Archive the results
        results_path = analysis_result.get('results_path')
        archive_result = archive_service.create_archive_entry(
            period=period,
            session=session,
            csv_path=csv_path,
            results_path=results_path
        )

        flash(f'Successfully analyzed and archived: {period} / {session}', 'success')
        return redirect(url_for('analysis', period=period, session=session))

    except Exception as e:
        flash(f'Error during import: {str(e)}', 'error')
        return redirect(url_for('import_page'))


@app.route('/delete/<period>/<session>', methods=['POST'])
def delete_archive(period, session):
    """Delete an archive entry"""
    try:
        # Get config to find archive directory
        config = config_service.load_config()
        archive_dir = Path(config.get('paths', {}).get('archive', 'data/backtest_archive'))

        # Construct the directory path
        entry_path = archive_dir / period / session

        # Security check - ensure path is within archive directory
        if not entry_path.resolve().is_relative_to(archive_dir.resolve()):
            flash('Invalid archive path', 'error')
            return redirect(url_for('index')), 403

        # Check if directory exists
        if not entry_path.exists():
            flash(f'Archive entry not found: {period}/{session}', 'error')
            return redirect(url_for('index')), 404

        # Delete the directory and its contents
        import shutil
        shutil.rmtree(entry_path)

        flash(f'Successfully deleted: {period} / {session}', 'success')
        return redirect(url_for('index'))

    except Exception as e:
        flash(f'Error deleting archive: {str(e)}', 'error')
        return redirect(url_for('index')), 500


@app.errorhandler(404)
def not_found(error):
    """Handle 404 errors"""
    return render_template('404.html'), 404


@app.errorhandler(500)
def internal_error(error):
    """Handle 500 errors"""
    return render_template('500.html'), 500


@app.context_processor
def inject_config():
    """Inject config into all templates"""
    try:
        config = config_service.load_config()
        return {'app_config': config}
    except Exception:
        return {'app_config': config_service.get_default_config()}


if __name__ == '__main__':
    # Run development server
    app.run(
        host='127.0.0.1',
        port=5000,
        debug=True
    )

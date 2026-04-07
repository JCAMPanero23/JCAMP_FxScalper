"""Flask application for WFO Browser UI"""
from flask import Flask, render_template, request, redirect, url_for, flash, send_file
from pathlib import Path
from datetime import datetime
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
        # Get pagination, filter, sort, and search parameters
        page = request.args.get('page', 1, type=int)
        per_page = request.args.get('per_page', 20, type=int)
        pair_filter = request.args.get('pair', None, type=str)
        sort_by = request.args.get('sort', 'date_newest', type=str)
        search_query = request.args.get('search', None, type=str)

        # Validate per_page range
        if per_page < 10 or per_page > 100:
            per_page = 20

        # Validate sort_by
        valid_sorts = ['date_newest', 'date_oldest', 'name_asc', 'name_desc',
                       'total_r_desc', 'total_r_asc', 'win_rate_desc', 'win_rate_asc']
        if sort_by not in valid_sorts:
            sort_by = 'date_newest'

        # Clean search query
        if search_query:
            search_query = search_query.strip()
            if not search_query:
                search_query = None

        # Get archive tree with pagination, pair filter, sorting, and search
        archive_data = archive_service.get_archive_tree(
            page=page,
            per_page=per_page,
            pair_filter=pair_filter,
            sort_by=sort_by,
            search_query=search_query
        )

        return render_template(
            'index.html',
            periods=archive_data.get('periods', []),
            total_pages=archive_data.get('total_pages', 0),
            current_page=archive_data.get('current_page', 1),
            pairs=archive_data.get('pairs', []),
            current_pair=archive_data.get('current_pair'),
            sort_by=archive_data.get('sort_by', 'date_newest'),
            search_query=archive_data.get('search_query')
        )
    except Exception as e:
        flash(f'Error loading archive: {str(e)}', 'error')
        return render_template('index.html', periods=[], total_pages=0, current_page=1, pairs=[], current_pair=None, sort_by='date_newest', search_query=None), 500


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

        # Get backtest settings from the analysis (extracted from CSV)
        backtest_settings = analysis_detail.get('backtest_settings', {})

        # Get all available analyses for forward test selection
        archive_data = archive_service.get_archive_tree(per_page=1000)
        available_analyses = []
        for p in archive_data.get('periods', []):
            for wfo in p.get('sessions', []):  # sessions now contains wfo_cycles
                available_analyses.append({
                    'period': p['name'],
                    'session': wfo['display_name'],  # Use display_name from WFO cycle
                    'display': f"{p['name']} / {wfo['display_name']} ({wfo['total_r']:+.1f}R)",
                    'total_r': wfo.get('total_r', 0),
                    'win_rate': wfo.get('win_rate', 0),
                    'profit_factor': wfo.get('profit_factor', 0),
                    'max_dd': wfo.get('max_dd', 0),
                    'trades': wfo.get('trades', 0)  # WFO cycles use 'trades' not 'total_trades'
                })

        # Get available CSV files for "New Analysis" forward test option
        ctrader_path = config.get('paths', {}).get('ctrader_logs', '')
        available_csvs = []
        if ctrader_path and Path(ctrader_path).exists():
            csv_files = sorted(Path(ctrader_path).glob("TradeLog*.csv"), key=lambda p: p.stat().st_mtime, reverse=True)
            for csv_file in csv_files:
                available_csvs.append({
                    'path': str(csv_file),
                    'name': csv_file.name,
                    'size': f"{csv_file.stat().st_size / 1024:.1f} KB",
                    'date': datetime.fromtimestamp(csv_file.stat().st_mtime).strftime('%Y-%m-%d %H:%M')
                })

        # Check if this is a re-optimization or has been re-optimized
        reopt_link = config_service.find_reoptimization_link(period, session)
        comparison_data = None
        pending_reopt = config_service.get_pending_reoptimization()
        forward_test_data = None

        if reopt_link:
            # Load the linked analysis for comparison
            if 'is_reoptimization_of' in reopt_link:
                # This IS a re-optimization - load original for comparison
                orig_period, orig_session = reopt_link['is_reoptimization_of'].split('/')
                original_analysis = archive_service.get_analysis_detail(orig_period, orig_session)
                comparison_data = {
                    'type': 'reoptimization',
                    'original': {
                        'period': orig_period,
                        'session': orig_session,
                        'overall_metrics': original_analysis.get('overall_metrics', {}),
                        'equity_trend': original_analysis.get('equity_trend', {}),
                        'metrics': original_analysis.get('metrics', {})
                    },
                    'new': {
                        'period': period,
                        'session': session,
                        'overall_metrics': analysis_detail.get('overall_metrics', {}),
                        'equity_trend': analysis_detail.get('equity_trend', {}),
                        'metrics': analysis_detail.get('metrics', {})
                    }
                }

                # Check for forward test result
                config = config_service.load_config()
                history = config.get('reoptimization_tracking', {}).get('history', [])
                current_key = f"{period}/{session}"
                for entry in history:
                    if entry.get('reoptimized') == current_key and entry.get('forward_test'):
                        ft = entry['forward_test']
                        # Load forward test analysis
                        forward_analysis = archive_service.get_analysis_detail(ft['period'], ft['session'])
                        forward_test_data = {
                            'period': ft['period'],
                            'session': ft['session'],
                            'result': ft['result'],
                            'overall_metrics': forward_analysis.get('overall_metrics', {}),
                            'equity_trend': forward_analysis.get('equity_trend', {})
                        }
                        break

            elif 'has_reoptimization' in reopt_link:
                # This HAS been re-optimized - show link to new version
                new_period, new_session = reopt_link['has_reoptimization'].split('/')
                comparison_data = {
                    'type': 'has_reoptimization',
                    'new_period': new_period,
                    'new_session': new_session
                }

        return render_template(
            'analysis.html',
            period=period,
            session=session,
            overall_metrics=analysis_detail.get('overall_metrics', {}),
            session_breakdown=analysis_detail.get('session_breakdown', []),
            metrics=analysis_detail.get('metrics', {}),
            recommendations=analysis_detail.get('recommendations', {}),
            backtest_settings=backtest_settings,
            equity_trend=analysis_detail.get('equity_trend', {}),
            equity_degradation_detected=analysis_detail.get('equity_degradation_detected', False),
            optimization_recommendation=analysis_detail.get('optimization_recommendation', {}),
            chart_path=chart_url,
            csv_path=csv_url,
            current_settings=current_settings,
            comparison=comparison,
            reopt_comparison=comparison_data,
            pending_reopt=pending_reopt,
            forward_test=forward_test_data,
            available_analyses=available_analyses,
            available_csvs=available_csvs,
            ctrader_path=ctrader_path
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
        backtest_settings = analysis_detail.get('backtest_settings', {})

        if not recommendations:
            flash('No recommendations found to export', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Use backtest_settings from the CSV as base (settings actually used during backtest)
        # Fall back to config if backtest_settings not available (old analyses)
        if backtest_settings:
            base_settings = backtest_settings
        else:
            config = config_service.load_config()
            base_settings = config.get('cbot_current_settings', {})

        # Merge: backtest settings + analyzer recommendations (recommendations override base)
        merged_params = {**base_settings, **recommendations}

        # Generate temporary file path
        temp_dir = Path(app.config['UPLOAD_FOLDER'])
        temp_dir.mkdir(parents=True, exist_ok=True)

        temp_file = temp_dir / f"export_{period}_{session}.cbotset"

        # Export to .cbotset file
        export_result = export_service.export_to_cbotset(
            merged_params,
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


@app.route('/download-optset')
def download_optset():
    """Download the .optset file for re-optimization"""
    try:
        optimization_sets_dir = Path(__file__).parent.parent / "optimization_sets"

        # Find the latest dated .optset file (pattern: Jcamp_1M_scalping_YYYY-MM-DD.optset)
        optset_files = list(optimization_sets_dir.glob("Jcamp_1M_scalping_*.optset"))

        if not optset_files:
            # Fallback to old naming convention
            old_path = optimization_sets_dir / "Jcamp_1M_scalping, EURUSD m1.optset"
            if old_path.exists():
                optset_files = [old_path]

        if not optset_files:
            flash('No .optset file found in optimization_sets folder.', 'error')
            return redirect(url_for('index'))

        # Sort by name (date) and get the latest
        optset_files.sort(reverse=True)
        optset_path = optset_files[0]

        # Generate download filename with current date
        from datetime import datetime
        today = datetime.now().strftime('%Y-%m-%d')
        download_name = f'Jcamp_1M_scalping_{today}.optset'

        return send_file(
            str(optset_path),
            mimetype='application/json',
            as_attachment=True,
            download_name=download_name
        )

    except Exception as e:
        flash(f'Error downloading .optset file: {str(e)}', 'error')
        return redirect(url_for('index'))


@app.route('/download-optimization-guide')
def download_optimization_guide():
    """Download the optimization guide markdown file"""
    try:
        guide_path = Path(__file__).parent.parent / "optimization_sets" / "OPTIMIZATION_GUIDE.md"

        if not guide_path.exists():
            flash('Optimization guide not found', 'error')
            return redirect(url_for('index'))

        return send_file(
            str(guide_path),
            mimetype='text/markdown',
            as_attachment=True,
            download_name='OPTIMIZATION_GUIDE.md'
        )

    except Exception as e:
        flash(f'Error downloading guide: {str(e)}', 'error')
        return redirect(url_for('index'))


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


@app.route('/import/check-existing', methods=['POST'])
def check_existing():
    """Check if analysis results already exist for a period/session"""
    try:
        period = request.form.get('period', '').strip()
        session = request.form.get('session', '').strip()

        if not period or not session:
            return json.dumps({"exists": False})

        result = archive_service.check_existing_analysis(period, session)
        return json.dumps(result)

    except Exception as e:
        return json.dumps({"exists": False, "error": str(e)})


@app.route('/import/analyze', methods=['POST'])
def import_analyze():
    """Run WFO analysis and archive the results"""
    try:
        csv_path = request.form.get('csv_file')
        period = request.form.get('period', '').strip()
        session = request.form.get('session', '').strip()
        confirm_overwrite = request.form.get('confirm_overwrite', 'false') == 'true'
        is_reoptimization = request.form.get('is_reoptimization', 'false') == 'true'

        if not csv_path or not period or not session:
            flash('Please fill in all fields', 'error')
            return redirect(url_for('import_page'))

        if not Path(csv_path).exists():
            flash('CSV file not found', 'error')
            return redirect(url_for('import_page'))

        # Check for existing analysis results
        existing = archive_service.check_existing_analysis(period, session)

        # Store original period/session for re-optimization linking
        original_period = period
        original_session = session

        if existing['exists']:
            if is_reoptimization:
                # This is a re-optimization - create new session name
                # Append timestamp to make it unique
                from datetime import datetime
                timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
                session = f"{session}_reopt_{timestamp}"
                flash(f'Creating re-optimization analysis: {period}/{session}', 'info')
            elif confirm_overwrite:
                # Overwrite - delete existing results first
                delete_result = archive_service.delete_analysis_results(period, session)
                if not delete_result['success']:
                    flash(f"Failed to delete existing results: {delete_result['error']}", 'error')
                    return redirect(url_for('import_page'))
            else:
                # Neither reopt nor overwrite confirmed - shouldn't happen with JS, but handle it
                flash(f'Analysis already exists for {period}/{session}. Please confirm action.', 'warning')
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

        # Handle re-optimization linking
        if is_reoptimization:
            # Link the new analysis to the original
            config_service.link_reoptimization(
                original_period,
                original_session,
                period,
                session
            )
            flash(f'✓ Linked as re-optimization of {original_period}/{original_session}', 'success')
        else:
            # Check if this should be auto-linked as a re-optimization (from pending marker)
            pending = config_service.get_pending_reoptimization()
            if pending:
                # Extract pair from session name
                new_pair = session.split('_')[0] if '_' in session else session
                pending_pair = pending['session'].split('_')[0] if '_' in pending['session'] else pending['session']

                # Auto-link if same pair
                if new_pair == pending_pair:
                    config_service.link_reoptimization(
                        pending['period'],
                        pending['session'],
                        period,
                        session
                    )
                    flash(f'✓ Auto-linked as re-optimization of {pending["period"]}/{pending["session"]}', 'success')

        action = 'overwritten and re-analyzed' if confirm_overwrite else 'analyzed and archived'
        flash(f'Successfully {action}: {period} / {session}', 'success')
        return redirect(url_for('analysis', period=period, session=session))

    except Exception as e:
        flash(f'Error during import: {str(e)}', 'error')
        return redirect(url_for('import_page'))


@app.route('/rename/<period>/<session>', methods=['POST'])
def rename_session(period, session):
    """Rename a session"""
    try:
        new_session = request.form.get('new_session', '').strip()

        if not new_session:
            flash('New session name is required', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Perform rename
        result = archive_service.rename_session(period, session, new_session)

        if result['success']:
            flash(f'Successfully renamed to: {period} / {new_session}', 'success')
            return redirect(url_for('analysis', period=period, session=new_session))
        else:
            flash(f"Rename failed: {result['error']}", 'error')
            return redirect(url_for('analysis', period=period, session=session))

    except Exception as e:
        flash(f'Error renaming session: {str(e)}', 'error')
        return redirect(url_for('analysis', period=period, session=session))


@app.route('/mark-for-reoptimization/<period>/<session>', methods=['POST'])
def mark_for_reoptimization(period, session):
    """Mark this analysis for re-optimization"""
    try:
        config_service.mark_for_reoptimization(period, session, reason="equity_degradation")
        flash(f'Marked {period}/{session} for re-optimization. Next imported analysis for this pair will be linked automatically.', 'success')
        return redirect(url_for('analysis', period=period, session=session))
    except Exception as e:
        flash(f'Error marking for re-optimization: {str(e)}', 'error')
        return redirect(url_for('analysis', period=period, session=session))


@app.route('/link-reoptimization/<period>/<session>', methods=['POST'])
def link_reoptimization_manual(period, session):
    """Manually link this analysis as re-optimization of another"""
    try:
        original_period = request.form.get('original_period')
        original_session = request.form.get('original_session')

        if not original_period or not original_session:
            flash('Please select an original analysis to link to', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        config_service.link_reoptimization(original_period, original_session, period, session)
        flash(f'Linked as re-optimization of {original_period}/{original_session}', 'success')
        return redirect(url_for('analysis', period=period, session=session))
    except Exception as e:
        flash(f'Error linking re-optimization: {str(e)}', 'error')
        return redirect(url_for('analysis', period=period, session=session))


@app.route('/add-forward-test/<period>/<session>', methods=['POST'])
def add_forward_test(period, session):
    """Add forward test validation result to a re-optimization"""
    try:
        forward_period = request.form.get('forward_period')
        forward_session = request.form.get('forward_session')
        result_summary = request.form.get('result_summary', '').strip()

        if not forward_period or not forward_session or not result_summary:
            flash('Please fill in all forward test fields', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        success = config_service.add_forward_test_result(
            period, session,
            forward_period, forward_session,
            result_summary
        )

        if success:
            flash(f'✓ Added forward test result: {forward_period}/{forward_session} → {result_summary}', 'success')
        else:
            flash('Could not find re-optimization entry to update', 'error')

        return redirect(url_for('analysis', period=period, session=session))
    except Exception as e:
        flash(f'Error adding forward test: {str(e)}', 'error')
        return redirect(url_for('analysis', period=period, session=session))


@app.route('/import/analyze-as-forward-test/<period>/<session>', methods=['POST'])
def analyze_as_forward_test(period, session):
    """Import and analyze CSV as forward test, then link to re-optimization"""
    import subprocess
    import re

    try:
        csv_file = request.form.get('csv_file')
        forward_test_period = request.form.get('forward_test_period', '').strip()

        if not csv_file or not forward_test_period:
            flash('Please select a CSV file and enter a period name', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Extract base session name (remove _reopt_timestamp suffix if present)
        # e.g., EURUSD_all_sessions_reopt_20260407_213716 → EURUSD_all_sessions
        import re as regex
        reopt_match = regex.match(r'(.+)_reopt_\d{8}_\d{6}$', session)
        if reopt_match:
            forward_session = reopt_match.group(1)  # Use base session name
        else:
            forward_session = session  # Already base name

        # Get config
        config = config_service.load_config()
        analyzer_script = config.get('paths', {}).get('analyzer_script', 'wfo_analyzer.py')
        archive_dir = config.get('paths', {}).get('archive', 'data/backtest_archive')

        # Run WFO analyzer
        flash(f'Running WFO analysis on {Path(csv_file).name}...', 'info')

        cmd = [
            'python',
            analyzer_script,
            csv_file,
            '--period', forward_test_period,
            '--session', forward_session,
            '--archive-dir', archive_dir
        ]

        result = subprocess.run(cmd, capture_output=True, text=True, timeout=300)

        if result.returncode != 0:
            flash(f'Analysis failed: {result.stderr}', 'error')
            return redirect(url_for('analysis', period=period, session=session))

        # Extract metrics from analyzer output for summary
        total_r = 0
        win_rate = 0

        for line in result.stdout.split('\n'):
            if 'Total R:' in line:
                match = re.search(r'([-+]?\d+\.\d+)R', line)
                if match:
                    total_r = float(match.group(1))
            elif 'Win Rate:' in line:
                match = re.search(r'(\d+\.\d+)%', line)
                if match:
                    win_rate = float(match.group(1))

        # Generate result summary
        if total_r > 0:
            result_summary = f'+{total_r:.1f}R profit, {win_rate:.1f}% WR'
        elif total_r < 0:
            result_summary = f'{total_r:.1f}R loss, {win_rate:.1f}% WR'
        else:
            result_summary = f'Break even, {win_rate:.1f}% WR'

        # Link as forward test
        success = config_service.add_forward_test_result(
            period, session,
            forward_test_period, forward_session,
            result_summary
        )

        if success:
            flash(f'✓ Forward test analyzed and linked: {forward_test_period}/{forward_session}', 'success')
        else:
            flash(f'⚠️ Analysis completed but could not link as forward test', 'warning')

        # Clean up CSV if auto-cleanup enabled
        if config.get('behavior', {}).get('auto_cleanup', True):
            try:
                Path(csv_file).unlink()
                flash(f'Cleaned up CSV file: {Path(csv_file).name}', 'info')
            except Exception as e:
                flash(f'Could not clean up CSV: {str(e)}', 'warning')

        return redirect(url_for('analysis', period=period, session=session))

    except subprocess.TimeoutExpired:
        flash('Analysis timed out (exceeded 5 minutes)', 'error')
        return redirect(url_for('analysis', period=period, session=session))
    except Exception as e:
        flash(f'Error during forward test analysis: {str(e)}', 'error')
        return redirect(url_for('analysis', period=period, session=session))


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

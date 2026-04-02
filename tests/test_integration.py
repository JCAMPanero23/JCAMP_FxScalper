"""Integration tests for WFO Browser UI"""
import pytest
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).parent.parent))

from wfo_ui.app import app
from wfo_ui.services import config_service, archive_service


@pytest.fixture
def client():
    """Create Flask test client"""
    app.config['TESTING'] = True
    with app.test_client() as client:
        yield client


def test_home_page_loads(client):
    """Test that home page loads successfully"""
    response = client.get('/')
    assert response.status_code == 200
    assert b'JCAMP WFO Analysis' in response.data


def test_settings_page_loads(client):
    """Test that settings page loads successfully"""
    response = client.get('/settings')
    assert response.status_code == 200
    assert b'Path Settings' in response.data


def test_compare_page_loads(client):
    """Test that compare page loads successfully"""
    response = client.get('/compare')
    assert response.status_code == 200
    assert b'Side-by-side comparison' in response.data or b'Compare Analysis' in response.data


def test_config_service_integration():
    """Test that config service works end-to-end"""
    config = config_service.load_config()

    assert 'version' in config
    assert 'paths' in config
    assert 'behavior' in config

    # Validate config
    validation = config_service.validate_config(config)
    assert validation['valid'] is True or len(validation['errors']) == 0


def test_archive_service_integration():
    """Test that archive service works"""
    result = archive_service.get_archive_tree()

    assert 'periods' in result
    assert 'total_pages' in result
    assert isinstance(result['periods'], list)


def test_home_page_structure(client):
    """Test that home page has expected content structure"""
    response = client.get('/')
    assert response.status_code == 200
    # Check for basic page structure
    assert b'<!DOCTYPE' in response.data or b'<html' in response.data


def test_settings_structure(client):
    """Test that settings page has expected form structure"""
    response = client.get('/settings')
    assert response.status_code == 200
    assert b'form' in response.data.lower()

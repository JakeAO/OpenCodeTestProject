-- Migration 001: Create initial tables for OCTP analytics, config, and experiments
-- Created: 2026-02-09
-- Description: Sets up the core database schema for Nakama server modules

-- ============================================================================
-- ANALYTICS TABLES
-- ============================================================================

-- Store all analytics events with JSONB properties for flexibility
CREATE TABLE IF NOT EXISTS analytics_events (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    session_id VARCHAR(255),
    event_name VARCHAR(255) NOT NULL,
    event_properties JSONB DEFAULT '{}'::jsonb,
    experiment_id VARCHAR(255),
    cohort VARCHAR(255),
    client_timestamp BIGINT,
    server_timestamp BIGINT NOT NULL DEFAULT EXTRACT(EPOCH FROM NOW())::BIGINT * 1000,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================================================
-- REMOTE CONFIG TABLES
-- ============================================================================

-- Store config variants for different experiment cohorts
CREATE TABLE IF NOT EXISTS config_variants (
    id SERIAL PRIMARY KEY,
    experiment_id VARCHAR(255) NOT NULL,
    cohort VARCHAR(255) NOT NULL,
    config_data JSONB NOT NULL DEFAULT '{}'::jsonb,
    version INTEGER NOT NULL DEFAULT 1,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(experiment_id, cohort)
);

-- ============================================================================
-- A/B TESTING / EXPERIMENT TABLES
-- ============================================================================

-- Store experiment metadata and cohort definitions
CREATE TABLE IF NOT EXISTS experiment_metadata (
    id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    cohorts JSONB NOT NULL, -- {"control": 0.5, "variant_b": 0.5}
    is_active BOOLEAN NOT NULL DEFAULT true,
    start_date TIMESTAMP WITH TIME ZONE,
    end_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Store user-to-experiment-cohort assignments
CREATE TABLE IF NOT EXISTS user_experiment_assignments (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    experiment_id VARCHAR(255) NOT NULL,
    cohort VARCHAR(255) NOT NULL,
    assigned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UNIQUE(user_id, experiment_id),
    FOREIGN KEY (experiment_id) REFERENCES experiment_metadata(id) ON DELETE CASCADE
);

-- ============================================================================
-- COMMENTS AND DOCUMENTATION
-- ============================================================================

COMMENT ON TABLE analytics_events IS 'Stores all analytics events from Unity client with flexible JSONB properties';
COMMENT ON TABLE config_variants IS 'Stores remote config variants per experiment/cohort combination';
COMMENT ON TABLE experiment_metadata IS 'Defines active A/B tests with cohort distributions';
COMMENT ON TABLE user_experiment_assignments IS 'Tracks which cohort each user is assigned to for each experiment';

COMMENT ON COLUMN analytics_events.event_properties IS 'Arbitrary JSON data for each event (e.g., {"level": 5, "score": 1000})';
COMMENT ON COLUMN analytics_events.client_timestamp IS 'Timestamp from client in milliseconds since epoch';
COMMENT ON COLUMN analytics_events.server_timestamp IS 'Server-side timestamp in milliseconds since epoch';
COMMENT ON COLUMN config_variants.config_data IS 'JSON configuration data specific to this cohort';
COMMENT ON COLUMN experiment_metadata.cohorts IS 'Cohort names and weights as JSON object';

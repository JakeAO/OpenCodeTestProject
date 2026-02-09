-- Migration 002: Create indexes for query performance
-- Created: 2026-02-09
-- Description: Adds indexes to optimize common query patterns in analytics and experiments

-- ============================================================================
-- ANALYTICS INDEXES
-- ============================================================================

-- Index for querying events by user (most common query)
CREATE INDEX IF NOT EXISTS idx_analytics_events_user_id 
    ON analytics_events(user_id);

-- Index for querying events by event name (filtering specific events)
CREATE INDEX IF NOT EXISTS idx_analytics_events_event_name 
    ON analytics_events(event_name);

-- Index for time-based queries (analytics dashboards)
CREATE INDEX IF NOT EXISTS idx_analytics_events_server_timestamp 
    ON analytics_events(server_timestamp DESC);

-- Compound index for user + time range queries (user history)
CREATE INDEX IF NOT EXISTS idx_analytics_events_user_time 
    ON analytics_events(user_id, server_timestamp DESC);

-- Index for experiment-based queries (A/B test analysis)
CREATE INDEX IF NOT EXISTS idx_analytics_events_experiment 
    ON analytics_events(experiment_id, cohort) 
    WHERE experiment_id IS NOT NULL;

-- ============================================================================
-- CONFIG INDEXES
-- ============================================================================

-- Index for fetching config by experiment and cohort (hot path)
CREATE INDEX IF NOT EXISTS idx_config_variants_experiment_cohort 
    ON config_variants(experiment_id, cohort) 
    WHERE is_active = true;

-- Index for listing active configs
CREATE INDEX IF NOT EXISTS idx_config_variants_active 
    ON config_variants(is_active, experiment_id);

-- ============================================================================
-- EXPERIMENT INDEXES
-- ============================================================================

-- Index for listing active experiments
CREATE INDEX IF NOT EXISTS idx_experiment_metadata_active 
    ON experiment_metadata(is_active, id);

-- Index for user assignment lookups (checking existing assignments)
CREATE INDEX IF NOT EXISTS idx_user_experiment_assignments_user 
    ON user_experiment_assignments(user_id);

-- Index for experiment analysis (finding all users in an experiment)
CREATE INDEX IF NOT EXISTS idx_user_experiment_assignments_experiment 
    ON user_experiment_assignments(experiment_id, cohort);

-- ============================================================================
-- PERFORMANCE NOTES
-- ============================================================================

-- Expected query patterns:
-- 1. Analytics: Fetch recent events for a user (idx_analytics_events_user_time)
-- 2. Config: Fetch config for experiment+cohort (idx_config_variants_experiment_cohort)
-- 3. Experiments: Check if user already assigned (idx_user_experiment_assignments_user)
-- 4. Dashboard: Time-series analytics queries (idx_analytics_events_server_timestamp)

-- Performance targets (from TDDs):
-- - AnalyticsCollectEvents: < 500ms for 100 events
-- - FetchRemoteConfig: < 100ms (cache), < 500ms (cold start)
-- - GetExperimentAssignment: < 200ms

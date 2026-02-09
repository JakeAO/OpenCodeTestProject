-- Migration 003: Seed initial experiment and config data
-- Created: 2026-02-09
-- Description: Creates sample experiments and default configs for testing and MVP

-- ============================================================================
-- DEFAULT CONFIG (No Experiment)
-- ============================================================================

-- Insert a default config variant for users not in any experiment
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'default',
    'default',
    '{
        "game_version": "1.0.0",
        "maintenance_mode": false,
        "feature_flags": {
            "new_ui": false,
            "advanced_tutorial": false,
            "daily_challenges": true
        },
        "balance": {
            "starting_gold": 100,
            "max_party_size": 4,
            "ability_cooldown_multiplier": 1.0,
            "enemy_health_multiplier": 1.0
        },
        "rates": {
            "xp_multiplier": 1.0,
            "gold_multiplier": 1.0,
            "drop_rate_multiplier": 1.0
        }
    }'::jsonb,
    1,
    true
)
ON CONFLICT (experiment_id, cohort) DO UPDATE
SET config_data = EXCLUDED.config_data,
    updated_at = NOW();

-- ============================================================================
-- EXAMPLE EXPERIMENT: Tutorial Onboarding
-- ============================================================================

-- Create experiment metadata
INSERT INTO experiment_metadata (id, name, description, cohorts, is_active, start_date)
VALUES (
    'tutorial_onboarding_v1',
    'Tutorial Onboarding Experiment',
    'Testing different tutorial approaches to improve new player retention',
    '{
        "control": 0.5,
        "verbose_tutorial": 0.5
    }'::jsonb,
    true,
    NOW()
)
ON CONFLICT (id) DO UPDATE
SET cohorts = EXCLUDED.cohorts,
    updated_at = NOW();

-- Control group config (minimal tutorial)
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'tutorial_onboarding_v1',
    'control',
    '{
        "game_version": "1.0.0",
        "maintenance_mode": false,
        "feature_flags": {
            "new_ui": false,
            "advanced_tutorial": false,
            "daily_challenges": true
        },
        "balance": {
            "starting_gold": 100,
            "max_party_size": 4,
            "ability_cooldown_multiplier": 1.0,
            "enemy_health_multiplier": 1.0
        },
        "rates": {
            "xp_multiplier": 1.0,
            "gold_multiplier": 1.0,
            "drop_rate_multiplier": 1.0
        },
        "tutorial": {
            "skip_available": true,
            "step_by_step": false,
            "tooltips_enabled": true
        }
    }'::jsonb,
    1,
    true
)
ON CONFLICT (experiment_id, cohort) DO UPDATE
SET config_data = EXCLUDED.config_data,
    updated_at = NOW();

-- Verbose tutorial group config
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'tutorial_onboarding_v1',
    'verbose_tutorial',
    '{
        "game_version": "1.0.0",
        "maintenance_mode": false,
        "feature_flags": {
            "new_ui": false,
            "advanced_tutorial": true,
            "daily_challenges": true
        },
        "balance": {
            "starting_gold": 100,
            "max_party_size": 4,
            "ability_cooldown_multiplier": 1.0,
            "enemy_health_multiplier": 1.0
        },
        "rates": {
            "xp_multiplier": 1.0,
            "gold_multiplier": 1.0,
            "drop_rate_multiplier": 1.0
        },
        "tutorial": {
            "skip_available": false,
            "step_by_step": true,
            "tooltips_enabled": true,
            "force_completion": true
        }
    }'::jsonb,
    1,
    true
)
ON CONFLICT (experiment_id, cohort) DO UPDATE
SET config_data = EXCLUDED.config_data,
    updated_at = NOW();

-- ============================================================================
-- EXAMPLE EXPERIMENT: Starting Gold
-- ============================================================================

-- Create experiment metadata
INSERT INTO experiment_metadata (id, name, description, cohorts, is_active, start_date)
VALUES (
    'starting_gold_v1',
    'Starting Gold Amount Experiment',
    'Testing different starting gold amounts for new player engagement',
    '{
        "gold_100": 0.33,
        "gold_150": 0.33,
        "gold_200": 0.34
    }'::jsonb,
    false,
    NOW()
)
ON CONFLICT (id) DO UPDATE
SET cohorts = EXCLUDED.cohorts,
    updated_at = NOW();

-- Gold 100 config
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'starting_gold_v1',
    'gold_100',
    '{"game_version": "1.0.0", "balance": {"starting_gold": 100}}'::jsonb,
    1,
    false
)
ON CONFLICT (experiment_id, cohort) DO NOTHING;

-- Gold 150 config
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'starting_gold_v1',
    'gold_150',
    '{"game_version": "1.0.0", "balance": {"starting_gold": 150}}'::jsonb,
    1,
    false
)
ON CONFLICT (experiment_id, cohort) DO NOTHING;

-- Gold 200 config
INSERT INTO config_variants (experiment_id, cohort, config_data, version, is_active)
VALUES (
    'starting_gold_v1',
    'gold_200',
    '{"game_version": "1.0.0", "balance": {"starting_gold": 200}}'::jsonb,
    1,
    false
)
ON CONFLICT (experiment_id, cohort) DO NOTHING;

-- ============================================================================
-- VERIFICATION QUERIES (for testing)
-- ============================================================================

-- Uncomment to verify seeded data:
-- SELECT * FROM experiment_metadata ORDER BY created_at;
-- SELECT * FROM config_variants ORDER BY experiment_id, cohort;

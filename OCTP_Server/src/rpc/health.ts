/**
 * Health Check RPC Module
 * 
 * Provides server health status and database connectivity checks.
 * Used for monitoring and alerting.
 */

import { HealthCheckResponse } from '../lib/types';
import { getCurrentTimestamp } from '../lib/utils';

// Track server start time for uptime calculation
const serverStartTime = Date.now();

/**
 * RPC: HealthCheck
 * 
 * Performs basic health checks including database connectivity.
 * Returns status and diagnostic information.
 * 
 * @param ctx - Nakama context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - Empty payload
 * @returns JSON string with health status
 */
export const healthCheck: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  const response: HealthCheckResponse = {
    status: 'healthy',
    timestamp: getCurrentTimestamp(),
    database_connected: false,
    uptime_seconds: Math.floor((Date.now() - serverStartTime) / 1000)
  };

  try {
    // Test database connectivity with simple query
    const sql = 'SELECT 1 as test';
    const result = nk.sqlQuery(sql);

    if (result && result.length > 0 && result[0].test === 1) {
      response.database_connected = true;
      response.status = 'healthy';
      logger.debug('Health check passed: database connected');
    } else {
      response.database_connected = false;
      response.status = 'degraded';
      response.error = 'Database query returned unexpected result';
      logger.warn('Health check degraded: unexpected database result');
    }

  } catch (error) {
    response.database_connected = false;
    response.status = 'unhealthy';
    response.error = 'Database connection failed: ' + error;
    logger.error('Health check failed: %s', error);
  }

  return JSON.stringify(response);
};

/**
 * RPC: DetailedHealthCheck
 * 
 * Performs comprehensive health checks including table existence
 * and row counts for key tables.
 * 
 * @param ctx - Nakama context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - Empty payload
 * @returns JSON string with detailed health information
 */
export const detailedHealthCheck: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  const checks: { [key: string]: any } = {
    timestamp: getCurrentTimestamp(),
    uptime_seconds: Math.floor((Date.now() - serverStartTime) / 1000),
    status: 'healthy',
    checks: {}
  };

  try {
    // Check 1: Basic database connectivity
    try {
      const sql = 'SELECT 1 as test';
      nk.sqlQuery(sql);
      checks.checks['database_connection'] = { status: 'ok' };
    } catch (e) {
      checks.checks['database_connection'] = { status: 'failed', error: String(e) };
      checks.status = 'unhealthy';
    }

    // Check 2: Analytics events table
    try {
      const sql = 'SELECT COUNT(*) as count FROM analytics_events';
      const result = nk.sqlQuery(sql);
      checks.checks['analytics_events_table'] = { 
        status: 'ok', 
        row_count: result[0].count 
      };
    } catch (e) {
      checks.checks['analytics_events_table'] = { 
        status: 'failed', 
        error: 'Table may not exist or is inaccessible' 
      };
      checks.status = 'degraded';
    }

    // Check 3: Config variants table
    try {
      const sql = 'SELECT COUNT(*) as count FROM config_variants';
      const result = nk.sqlQuery(sql);
      checks.checks['config_variants_table'] = { 
        status: 'ok', 
        row_count: result[0].count 
      };
    } catch (e) {
      checks.checks['config_variants_table'] = { 
        status: 'failed', 
        error: 'Table may not exist or is inaccessible' 
      };
      checks.status = 'degraded';
    }

    // Check 4: User experiment assignments table
    try {
      const sql = 'SELECT COUNT(*) as count FROM user_experiment_assignments';
      const result = nk.sqlQuery(sql);
      checks.checks['user_experiment_assignments_table'] = { 
        status: 'ok', 
        row_count: result[0].count 
      };
    } catch (e) {
      checks.checks['user_experiment_assignments_table'] = { 
        status: 'failed', 
        error: 'Table may not exist or is inaccessible' 
      };
      checks.status = 'degraded';
    }

    // Check 5: Experiment metadata table
    try {
      const sql = 'SELECT COUNT(*) as count FROM experiment_metadata WHERE is_active = TRUE';
      const result = nk.sqlQuery(sql);
      checks.checks['experiment_metadata_table'] = { 
        status: 'ok', 
        active_experiments: result[0].count 
      };
    } catch (e) {
      checks.checks['experiment_metadata_table'] = { 
        status: 'failed', 
        error: 'Table may not exist or is inaccessible' 
      };
      checks.status = 'degraded';
    }

    logger.info('Detailed health check completed with status: %s', checks.status);

  } catch (error) {
    checks.status = 'unhealthy';
    checks.error = 'Unexpected error during health check: ' + error;
    logger.error('Detailed health check error: %s', error);
  }

  return JSON.stringify(checks);
};

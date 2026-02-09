/**
 * Remote Config RPC Module
 * 
 * Provides runtime configuration overrides based on user's experiment cohort.
 * Configs are cached in-memory for performance.
 */

import {
  FetchConfigRequest,
  FetchConfigResponse,
  UserExperimentAssignment,
  ConfigVariant
} from '../lib/types';
import { toSqlLiteral, safeJsonParse, parseJsonbColumn } from '../lib/utils';

// In-memory cache for config (simple cache, resets on module reload)
const configCache: { [cacheKey: string]: { config: any; timestamp: number } } = {};
const CACHE_TTL_MS = 60000; // 60 seconds

/**
 * RPC: FetchRemoteConfig
 * 
 * Fetches remote configuration for the user based on their experiment assignment.
 * Returns cohort-specific config values with fallback to default config.
 * 
 * @param ctx - Nakama context with userId
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - JSON string with optional request params
 * @returns JSON string with config object
 */
export const fetchRemoteConfig: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  try {
    const userId = ctx.userId;

    // Check cache first
    const cacheKey = `config_${userId}`;
    const cached = configCache[cacheKey];
    if (cached && (Date.now() - cached.timestamp) < CACHE_TTL_MS) {
      logger.debug('Config cache hit for user %s', userId);
      return JSON.stringify(cached.config);
    }

    // Get user's experiment assignment
    const assignmentSql = `
      SELECT experiment_id, cohort
      FROM user_experiment_assignments
      WHERE user_id = ${toSqlLiteral(userId)}
      LIMIT 1
    `;

    const assignmentResult = nk.sqlQuery(assignmentSql);
    
    let experimentId: string | null = null;
    let cohort: string | null = null;

    if (assignmentResult.length > 0) {
      experimentId = assignmentResult[0].experiment_id;
      cohort = assignmentResult[0].cohort;
      logger.debug('User %s assigned to experiment %s, cohort %s', userId, experimentId, cohort);
    } else {
      logger.debug('User %s has no experiment assignment', userId);
    }

    // Fetch config for user's cohort (or default config if no assignment)
    let configSql: string;
    
    if (experimentId && cohort) {
      // Fetch config for specific experiment and cohort
      configSql = `
        SELECT config_data
        FROM config_variants
        WHERE experiment_id = ${toSqlLiteral(experimentId)}
          AND cohort = ${toSqlLiteral(cohort)}
          AND is_active = TRUE
        LIMIT 1
      `;
    } else {
      // Fetch default config (use 'default' experiment with 'default' cohort)
      configSql = `
        SELECT config_data
        FROM config_variants
        WHERE experiment_id = 'default'
          AND cohort = 'default'
          AND is_active = TRUE
        LIMIT 1
      `;
    }

    const configResult = nk.sqlQuery(configSql);

    // Build config object from JSONB data
    let config: { [key: string]: any } = {};
    if (configResult.length > 0) {
      const row = configResult[0];
      // config_data is a JSONB column (byte array in Nakama)
      config = parseJsonbColumn(row.config_data, {});
    }

    logger.info('Fetched %d config keys for user %s', Object.keys(config).length, userId);

    const response: FetchConfigResponse = {
      success: true,
      experiment_id: experimentId,
      cohort: cohort,
      config: config
    };

    // Cache the response
    configCache[cacheKey] = {
      config: response,
      timestamp: Date.now()
    };

    return JSON.stringify(response);

  } catch (error) {
    logger.error('Error in fetchRemoteConfig: %s', error);
    return JSON.stringify({
      success: false,
      experiment_id: null,
      cohort: null,
      config: {},
      error: 'Failed to fetch config'
    } as FetchConfigResponse);
  }
};

/**
 * RPC: UpdateRemoteConfig (Admin only - not for client use)
 * 
 * Updates a specific config value for an experiment and cohort.
 * This should be restricted to admin users or internal tools.
 * 
 * @param ctx - Nakama context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - JSON with experiment_id, cohort, config_key, config_value
 * @returns JSON string with success status
 */
export const updateRemoteConfig: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  try {
    const params = JSON.parse(payload);
    
    if (!params.experiment_id || !params.cohort || !params.config_data) {
      return JSON.stringify({
        success: false,
        error: 'Missing required parameters: experiment_id, cohort, config_data'
      });
    }

    const experimentId = params.experiment_id;
    const cohort = params.cohort;
    const configData = params.config_data;

    // Upsert config data (full JSONB object)
    const sql = `
      INSERT INTO config_variants (experiment_id, cohort, config_data, updated_at)
      VALUES (
        ${toSqlLiteral(experimentId)},
        ${toSqlLiteral(cohort)},
        ${toSqlLiteral(configData)}::jsonb,
        NOW()
      )
      ON CONFLICT (experiment_id, cohort)
      DO UPDATE SET
        config_data = ${toSqlLiteral(configData)}::jsonb,
        updated_at = NOW()
    `;

    nk.sqlExec(sql);

    logger.info(
      'Config updated: experiment=%s, cohort=%s by user %s',
      experimentId,
      cohort,
      ctx.userId
    );

    // Clear cache for all users (simple invalidation)
    for (const key in configCache) {
      delete configCache[key];
    }

    return JSON.stringify({
      success: true
    });

  } catch (error) {
    logger.error('Error in updateRemoteConfig: %s', error);
    return JSON.stringify({
      success: false,
      error: 'Failed to update config'
    });
  }
};

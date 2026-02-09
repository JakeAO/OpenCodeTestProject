/**
 * Experiments RPC Module
 * 
 * Handles user assignment to experiment cohorts with weighted distribution.
 * Ensures deterministic assignment based on user ID.
 */

import {
  GetExperimentAssignmentRequest,
  GetExperimentAssignmentResponse,
  ExperimentMetadata,
  UserExperimentAssignment
} from '../lib/types';
import { 
  validateExperimentId, 
  toSqlLiteral, 
  assignCohort,
  safeJsonParse,
  parseJsonbColumn
} from '../lib/utils';

/**
 * RPC: GetExperimentAssignment
 * 
 * Gets the user's cohort assignment for a specific experiment.
 * If user has no assignment yet, creates one based on weighted distribution.
 * 
 * @param ctx - Nakama context with userId
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - JSON string with experiment_id
 * @returns JSON string with assignment result
 */
export const getExperimentAssignment: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  try {
    const request: GetExperimentAssignmentRequest = JSON.parse(payload);
    const userId = request.user_id || ctx.userId;
    const experimentId = request.experiment_id;

    // Validate experiment ID
    const validation = validateExperimentId(experimentId);
    if (!validation.valid) {
      return JSON.stringify({
        success: false,
        user_id: userId,
        experiment_id: experimentId,
        cohort: '',
        is_new_assignment: false,
        error: validation.error
      } as GetExperimentAssignmentResponse);
    }

    // Check if user already has assignment
    const existingSql = `
      SELECT cohort
      FROM user_experiment_assignments
      WHERE user_id = ${toSqlLiteral(userId)}
        AND experiment_id = ${toSqlLiteral(experimentId)}
      LIMIT 1
    `;

    const existingResult = nk.sqlQuery(existingSql);

    if (existingResult.length > 0) {
      // User already assigned
      const cohort = existingResult[0].cohort;
      logger.debug('User %s already assigned to cohort %s for experiment %s', userId, cohort, experimentId);

      return JSON.stringify({
        success: true,
        user_id: userId,
        experiment_id: experimentId,
        cohort: cohort,
        is_new_assignment: false
      } as GetExperimentAssignmentResponse);
    }

    // Get experiment metadata
    const metaSql = `
      SELECT 
        name,
        is_active,
        cohorts
      FROM experiment_metadata
      WHERE id = ${toSqlLiteral(experimentId)}
      LIMIT 1
    `;

    const metaResult = nk.sqlQuery(metaSql);

    if (metaResult.length === 0) {
      logger.warn('Experiment not found: %s', experimentId);
      return JSON.stringify({
        success: false,
        user_id: userId,
        experiment_id: experimentId,
        cohort: '',
        is_new_assignment: false,
        error: 'Experiment not found'
      } as GetExperimentAssignmentResponse);
    }

    const meta = metaResult[0];

    if (!meta.is_active) {
      logger.warn('Experiment is not active: %s', experimentId);
      return JSON.stringify({
        success: false,
        user_id: userId,
        experiment_id: experimentId,
        cohort: '',
        is_new_assignment: false,
        error: 'Experiment is not active'
      } as GetExperimentAssignmentResponse);
    }

    // Parse cohort distribution (handle JSONB byte arrays from Nakama)
    const distribution: { [key: string]: number } = parseJsonbColumn(meta.cohorts, { control: 1.0 });

    // Assign cohort deterministically
    const cohort = assignCohort(userId, distribution);

    // Save assignment
    const insertSql = `
      INSERT INTO user_experiment_assignments (user_id, experiment_id, cohort, assigned_at)
      VALUES (
        ${toSqlLiteral(userId)},
        ${toSqlLiteral(experimentId)},
        ${toSqlLiteral(cohort)},
        NOW()
      )
    `;

    try {
      nk.sqlExec(insertSql);
      logger.info('User %s assigned to cohort %s for experiment %s', userId, cohort, experimentId);

      return JSON.stringify({
        success: true,
        user_id: userId,
        experiment_id: experimentId,
        cohort: cohort,
        is_new_assignment: true
      } as GetExperimentAssignmentResponse);

    } catch (sqlError) {
      logger.error('Failed to save experiment assignment: %s', sqlError);
      return JSON.stringify({
        success: false,
        user_id: userId,
        experiment_id: experimentId,
        cohort: '',
        is_new_assignment: false,
        error: 'Failed to save assignment'
      } as GetExperimentAssignmentResponse);
    }

  } catch (error) {
    logger.error('Error in getExperimentAssignment: %s', error);
    return JSON.stringify({
      success: false,
      user_id: '',
      experiment_id: '',
      cohort: '',
      is_new_assignment: false,
      error: 'Internal server error'
    } as GetExperimentAssignmentResponse);
  }
};

/**
 * RPC: ListActiveExperiments
 * 
 * Lists all currently active experiments.
 * Useful for admin tools or debug UI.
 * 
 * @param ctx - Nakama context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - Empty or JSON with pagination params
 * @returns JSON string with experiment list
 */
export const listActiveExperiments: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  try {
    const sql = `
      SELECT 
        id,
        name,
        description,
        cohorts,
        start_date,
        end_date
      FROM experiment_metadata
      WHERE is_active = TRUE
      ORDER BY start_date DESC
    `;

    const result = nk.sqlQuery(sql);

    logger.info('Retrieved %d active experiments', result.length);

    return JSON.stringify({
      success: true,
      experiments: result,
      count: result.length
    });

  } catch (error) {
    logger.error('Error in listActiveExperiments: %s', error);
    return JSON.stringify({
      success: false,
      experiments: [],
      error: 'Failed to list experiments'
    });
  }
};

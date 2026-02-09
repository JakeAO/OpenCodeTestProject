/**
 * Analytics RPC Module
 * 
 * Handles collection and storage of analytics events from Unity client.
 * Events are batched and inserted into PostgreSQL for later analysis.
 */

import {
  AnalyticsEventBatch,
  AnalyticsCollectResponse,
  AnalyticsEventRow
} from '../lib/types';
import {
  validateEventBatch,
  toSqlLiteral,
  generateBatchId,
  getCurrentTimestamp
} from '../lib/utils';

/**
 * RPC: AnalyticsCollectEvents
 * 
 * Collects a batch of analytics events from the client and inserts them into
 * the analytics_events table. Supports up to 100 events per batch.
 * 
 * @param ctx - Nakama context with userId
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - JSON string with event batch
 * @returns JSON string with insertion result
 */
export const analyticsCollectEvents: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  const startTime = Date.now();
  
  try {
    // Parse payload
    let batch: AnalyticsEventBatch;
    try {
      batch = JSON.parse(payload) as AnalyticsEventBatch;
    } catch (e) {
      logger.error('Failed to parse analytics payload: %s', e);
      return JSON.stringify({
        success: false,
        events_inserted: 0,
        batch_id: '',
        error: 'Invalid JSON payload'
      } as AnalyticsCollectResponse);
    }

    // Validate batch
    const validation = validateEventBatch(batch);
    if (!validation.valid) {
      logger.warn('Invalid analytics batch from user %s: %s', ctx.userId, validation.error);
      return JSON.stringify({
        success: false,
        events_inserted: 0,
        batch_id: '',
        error: validation.error
      } as AnalyticsCollectResponse);
    }

    // Generate batch ID and session ID
    const batchId = generateBatchId();
    const sessionId = batch.session_id || `session_${ctx.userId}_${Date.now()}`;
    const userId = ctx.userId;

    // Build SQL INSERT statement
    // Note: We use multi-row INSERT for better performance
    const values: string[] = [];
    
    for (const event of batch.events) {
      const row: AnalyticsEventRow = {
        user_id: userId,
        session_id: sessionId,
        event_name: event.event_name,
        event_properties: event.properties || {},
        experiment_id: event.experiment_id || null,
        cohort: event.cohort || null,
        client_timestamp: event.timestamp
      };

      // Build value tuple for SQL
      const valueTuple = `(
        ${toSqlLiteral(row.user_id)},
        ${toSqlLiteral(row.session_id)},
        ${toSqlLiteral(row.event_name)},
        ${toSqlLiteral(row.event_properties)},
        ${toSqlLiteral(row.experiment_id)},
        ${toSqlLiteral(row.cohort)},
        ${toSqlLiteral(row.client_timestamp)},
        NOW()
      )`;
      
      values.push(valueTuple);
    }

    // Execute batch INSERT
    const sql = `
      INSERT INTO analytics_events (
        user_id,
        session_id,
        event_name,
        event_properties,
        experiment_id,
        cohort,
        client_timestamp,
        server_timestamp
      ) VALUES ${values.join(', ')}
    `;

    try {
      const result = nk.sqlExec(sql);
      const eventsInserted = result.rowsAffected || 0;
      
      const elapsedMs = Date.now() - startTime;
      logger.info(
        'Analytics: Inserted %d events for user %s (batch_id: %s, elapsed: %dms)',
        eventsInserted,
        userId,
        batchId,
        elapsedMs
      );

      return JSON.stringify({
        success: true,
        events_inserted: eventsInserted,
        batch_id: batchId
      } as AnalyticsCollectResponse);

    } catch (sqlError) {
      logger.error('SQL error inserting analytics events: %s', sqlError);
      return JSON.stringify({
        success: false,
        events_inserted: 0,
        batch_id: batchId,
        error: 'Database insertion failed'
      } as AnalyticsCollectResponse);
    }

  } catch (error) {
    logger.error('Unexpected error in analyticsCollectEvents: %s', error);
    return JSON.stringify({
      success: false,
      events_inserted: 0,
      batch_id: '',
      error: 'Internal server error'
    } as AnalyticsCollectResponse);
  }
};

/**
 * RPC: AnalyticsGetUserEvents (Optional)
 * 
 * Retrieves recent analytics events for a specific user.
 * Useful for debugging or displaying user activity history.
 * 
 * @param ctx - Nakama context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param payload - JSON string with query parameters (limit, offset, event_name filter)
 * @returns JSON string with event list
 */
export const analyticsGetUserEvents: nkruntime.RpcFunction = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  payload: string
): string {
  try {
    const params = payload ? JSON.parse(payload) : {};
    const limit = Math.min(params.limit || 100, 500); // Max 500 events
    const offset = params.offset || 0;
    const eventName = params.event_name || null;

    const userId = ctx.userId;

    // Build SQL query
    let sql = `
      SELECT 
        event_name,
        event_properties,
        experiment_id,
        cohort,
        client_timestamp,
        server_timestamp
      FROM analytics_events
      WHERE user_id = ${toSqlLiteral(userId)}
    `;

    if (eventName) {
      sql += ` AND event_name = ${toSqlLiteral(eventName)}`;
    }

    sql += `
      ORDER BY server_timestamp DESC
      LIMIT ${limit}
      OFFSET ${offset}
    `;

    const result = nk.sqlQuery(sql);

    logger.info('Retrieved %d analytics events for user %s', result.length, userId);

    return JSON.stringify({
      success: true,
      events: result,
      count: result.length
    });

  } catch (error) {
    logger.error('Error in analyticsGetUserEvents: %s', error);
    return JSON.stringify({
      success: false,
      events: [],
      error: 'Failed to retrieve events'
    });
  }
};

/**
 * Type definitions for OCTP Nakama Server
 * 
 * These types define the contracts between Unity client and Nakama server
 * for analytics, remote config, and experiment management.
 */

// ============================================================================
// ANALYTICS TYPES
// ============================================================================

/**
 * Single analytics event from client
 */
export interface AnalyticsEvent {
  event_name: string;
  properties?: { [key: string]: any };
  experiment_id?: string;
  cohort?: string;
  timestamp: number; // Unix timestamp in milliseconds
}

/**
 * Batch of analytics events sent from client
 */
export interface AnalyticsEventBatch {
  events: AnalyticsEvent[];
  session_id?: string;
}

/**
 * Response from AnalyticsCollectEvents RPC
 */
export interface AnalyticsCollectResponse {
  success: boolean;
  events_inserted: number;
  batch_id: string;
  error?: string;
}

/**
 * Database row for analytics_events table
 */
export interface AnalyticsEventRow {
  id?: number;
  user_id: string;
  session_id: string;
  event_name: string;
  event_properties: any; // JSONB in PostgreSQL
  experiment_id: string | null;
  cohort: string | null;
  client_timestamp: number;
  server_timestamp?: Date;
  created_at?: Date;
}

// ============================================================================
// REMOTE CONFIG TYPES
// ============================================================================

/**
 * Request for remote config from client
 */
export interface FetchConfigRequest {
  user_id?: string; // Optional; can use ctx.userId if not provided
}

/**
 * Response with remote config for user's cohort
 */
export interface FetchConfigResponse {
  success: boolean;
  experiment_id: string | null;
  cohort: string | null;
  config: { [key: string]: any };
  error?: string;
}

/**
 * Config value stored in database
 */
export interface ConfigVariant {
  id?: number;
  experiment_id: string;
  cohort: string;
  config_key: string;
  config_value: any; // JSONB in PostgreSQL
  created_at?: Date;
  updated_at?: Date;
}

// ============================================================================
// EXPERIMENT TYPES
// ============================================================================

/**
 * User's experiment assignment
 */
export interface UserExperimentAssignment {
  user_id: string;
  experiment_id: string;
  cohort: string;
  assigned_at?: Date;
}

/**
 * Experiment metadata
 */
export interface ExperimentMetadata {
  experiment_id: string;
  name: string;
  description?: string;
  is_active: boolean;
  cohort_distribution: { [cohort: string]: number }; // e.g., {"control": 0.5, "variant_b": 0.5}
  start_date?: Date;
  end_date?: Date;
}

/**
 * Request to get user's experiment assignment
 */
export interface GetExperimentAssignmentRequest {
  user_id?: string;
  experiment_id: string;
}

/**
 * Response with user's experiment assignment
 */
export interface GetExperimentAssignmentResponse {
  success: boolean;
  user_id: string;
  experiment_id: string;
  cohort: string;
  is_new_assignment: boolean;
  error?: string;
}

// ============================================================================
// HEALTH CHECK TYPES
// ============================================================================

/**
 * Health check response
 */
export interface HealthCheckResponse {
  status: 'healthy' | 'degraded' | 'unhealthy';
  timestamp: number; // Unix timestamp in milliseconds
  database_connected: boolean;
  uptime_seconds?: number;
  error?: string;
}

// ============================================================================
// UTILITY TYPES
// ============================================================================

/**
 * Generic RPC response wrapper
 */
export interface RpcResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

/**
 * Database query result
 */
export interface QueryResult {
  rows: any[];
  rowCount: number;
}

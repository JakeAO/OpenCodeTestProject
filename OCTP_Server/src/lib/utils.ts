/**
 * Utility functions for OCTP Nakama Server
 * 
 * Provides validation, sanitization, and helper functions for RPC handlers.
 */

import { AnalyticsEvent } from './types';

// ============================================================================
// VALIDATION FUNCTIONS
// ============================================================================

/**
 * Validate analytics event structure
 */
export function validateAnalyticsEvent(event: any): { valid: boolean; error?: string } {
  if (!event) {
    return { valid: false, error: 'Event is null or undefined' };
  }

  if (typeof event.event_name !== 'string' || event.event_name.length === 0) {
    return { valid: false, error: 'event_name must be a non-empty string' };
  }

  if (event.event_name.length > 255) {
    return { valid: false, error: 'event_name exceeds 255 characters' };
  }

  if (typeof event.timestamp !== 'number' || event.timestamp <= 0) {
    return { valid: false, error: 'timestamp must be a positive number' };
  }

  // Optional fields validation
  if (event.properties !== undefined && typeof event.properties !== 'object') {
    return { valid: false, error: 'properties must be an object if provided' };
  }

  if (event.experiment_id !== undefined && typeof event.experiment_id !== 'string') {
    return { valid: false, error: 'experiment_id must be a string if provided' };
  }

  if (event.cohort !== undefined && typeof event.cohort !== 'string') {
    return { valid: false, error: 'cohort must be a string if provided' };
  }

  return { valid: true };
}

/**
 * Validate batch of analytics events
 */
export function validateEventBatch(batch: any): { valid: boolean; error?: string } {
  if (!batch || !Array.isArray(batch.events)) {
    return { valid: false, error: 'events must be an array' };
  }

  if (batch.events.length === 0) {
    return { valid: false, error: 'events array cannot be empty' };
  }

  if (batch.events.length > 100) {
    return { valid: false, error: 'events array exceeds maximum batch size of 100' };
  }

  // Validate each event
  for (let i = 0; i < batch.events.length; i++) {
    const validation = validateAnalyticsEvent(batch.events[i]);
    if (!validation.valid) {
      return { valid: false, error: `Event at index ${i}: ${validation.error}` };
    }
  }

  return { valid: true };
}

/**
 * Validate experiment ID format
 */
export function validateExperimentId(experimentId: any): { valid: boolean; error?: string } {
  if (typeof experimentId !== 'string' || experimentId.length === 0) {
    return { valid: false, error: 'experiment_id must be a non-empty string' };
  }

  if (experimentId.length > 255) {
    return { valid: false, error: 'experiment_id exceeds 255 characters' };
  }

  // Only allow alphanumeric, underscore, and hyphen
  if (!/^[a-zA-Z0-9_-]+$/.test(experimentId)) {
    return { valid: false, error: 'experiment_id contains invalid characters' };
  }

  return { valid: true };
}

// ============================================================================
// SQL SANITIZATION FUNCTIONS
// ============================================================================

/**
 * Escape single quotes in string for SQL safety
 * Note: Nakama's nk.sqlExec doesn't support parameterized queries,
 * so we must sanitize manually
 */
export function escapeSqlString(value: string): string {
  if (value === null || value === undefined) {
    return 'NULL';
  }
  // Replace single quotes with two single quotes (SQL escape)
  return value.replace(/'/g, "''");
}

/**
 * Safely convert value to SQL literal
 */
export function toSqlLiteral(value: any): string {
  if (value === null || value === undefined) {
    return 'NULL';
  }

  if (typeof value === 'string') {
    return `'${escapeSqlString(value)}'`;
  }

  if (typeof value === 'number') {
    return value.toString();
  }

  if (typeof value === 'boolean') {
    return value ? 'TRUE' : 'FALSE';
  }

  if (typeof value === 'object') {
    // Convert to JSON string for JSONB columns
    return `'${escapeSqlString(JSON.stringify(value))}'::jsonb`;
  }

  return 'NULL';
}

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Generate unique batch ID for analytics
 */
export function generateBatchId(): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  return `batch_${timestamp}_${random}`;
}

/**
 * Convert byte array to string (for JSONB columns from Nakama)
 * Nakama's Goja runtime returns JSONB as byte arrays
 */
export function bytesToString(bytes: any): string {
  if (typeof bytes === 'string') {
    return bytes;
  }
  if (Array.isArray(bytes)) {
    return String.fromCharCode(...bytes);
  }
  return String(bytes);
}

/**
 * Parse JSONB column value (handles byte arrays from Nakama)
 */
export function parseJsonbColumn(value: any, defaultValue: any = null): any {
  if (value === null || value === undefined) {
    return defaultValue;
  }
  
  // If it's already an object, return it
  if (typeof value === 'object' && !Array.isArray(value)) {
    return value;
  }
  
  // If it's a byte array or string, parse it
  const str = bytesToString(value);
  return safeJsonParse(str, defaultValue);
}

/**
 * Get current Unix timestamp in milliseconds
 */
export function getCurrentTimestamp(): number {
  return Date.now();
}

/**
 * Assign user to cohort based on weighted distribution
 * 
 * @param userId - User ID (used for deterministic assignment)
 * @param distribution - Cohort distribution e.g., {"control": 0.5, "variant_b": 0.5}
 * @returns Assigned cohort name
 */
export function assignCohort(userId: string, distribution: { [key: string]: number }): string {
  // Use hash of userId for deterministic assignment
  const hash = simpleHash(userId);
  const normalized = (hash % 10000) / 10000; // Normalize to 0-1

  let cumulative = 0;
  for (const cohort in distribution) {
    cumulative += distribution[cohort];
    if (normalized < cumulative) {
      return cohort;
    }
  }

  // Fallback to first cohort (should never happen if distribution sums to 1.0)
  return Object.keys(distribution)[0] || 'control';
}

/**
 * Simple hash function for deterministic cohort assignment
 */
function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    const char = str.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash; // Convert to 32-bit integer
  }
  return Math.abs(hash);
}

/**
 * Parse JSON safely with error handling
 */
export function safeJsonParse<T>(json: string, fallback: T): T {
  try {
    return JSON.parse(json) as T;
  } catch (e) {
    return fallback;
  }
}

/**
 * Check if value is a valid object (not null, not array)
 */
export function isValidObject(value: any): boolean {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Truncate string to maximum length
 */
export function truncateString(str: string, maxLength: number): string {
  if (str.length <= maxLength) {
    return str;
  }
  return str.substring(0, maxLength - 3) + '...';
}

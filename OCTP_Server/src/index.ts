/**
 * OCTP Nakama Server - Main Entry Point
 * 
 * This module registers all RPC functions and initializes the server runtime.
 * All functions must be defined at global scope for Nakama to recognize them.
 */

import { 
  analyticsCollectEvents,
  analyticsGetUserEvents 
} from './rpc/analytics';

import { 
  fetchRemoteConfig,
  updateRemoteConfig 
} from './rpc/config';

import { 
  getExperimentAssignment,
  listActiveExperiments 
} from './rpc/experiments';

import { 
  healthCheck,
  detailedHealthCheck 
} from './rpc/health';

/**
 * InitModule - Main initialization function
 * 
 * Called once when Nakama server starts. Registers all RPC endpoints
 * and hooks. Must be defined at global scope.
 * 
 * @param ctx - Server initialization context
 * @param logger - Logger instance
 * @param nk - Nakama API
 * @param initializer - RPC/hook registration interface
 */
let InitModule: nkruntime.InitModule = function(
  ctx: nkruntime.Context,
  logger: nkruntime.Logger,
  nk: nkruntime.Nakama,
  initializer: nkruntime.Initializer
) {
  logger.info('=================================================');
  logger.info('OCTP Nakama Server - Initializing...');
  logger.info('=================================================');

  // ============================================================================
  // ANALYTICS RPCs
  // ============================================================================
  
  try {
    initializer.registerRpc('AnalyticsCollectEvents', analyticsCollectEvents);
    logger.info('✓ Registered RPC: AnalyticsCollectEvents');
  } catch (e) {
    logger.error('✗ Failed to register AnalyticsCollectEvents: %s', e);
  }

  try {
    initializer.registerRpc('AnalyticsGetUserEvents', analyticsGetUserEvents);
    logger.info('✓ Registered RPC: AnalyticsGetUserEvents');
  } catch (e) {
    logger.error('✗ Failed to register AnalyticsGetUserEvents: %s', e);
  }

  // ============================================================================
  // REMOTE CONFIG RPCs
  // ============================================================================

  try {
    initializer.registerRpc('FetchRemoteConfig', fetchRemoteConfig);
    logger.info('✓ Registered RPC: FetchRemoteConfig');
  } catch (e) {
    logger.error('✗ Failed to register FetchRemoteConfig: %s', e);
  }

  try {
    initializer.registerRpc('UpdateRemoteConfig', updateRemoteConfig);
    logger.info('✓ Registered RPC: UpdateRemoteConfig (Admin Only)');
  } catch (e) {
    logger.error('✗ Failed to register UpdateRemoteConfig: %s', e);
  }

  // ============================================================================
  // EXPERIMENT RPCs
  // ============================================================================

  try {
    initializer.registerRpc('GetExperimentAssignment', getExperimentAssignment);
    logger.info('✓ Registered RPC: GetExperimentAssignment');
  } catch (e) {
    logger.error('✗ Failed to register GetExperimentAssignment: %s', e);
  }

  try {
    initializer.registerRpc('ListActiveExperiments', listActiveExperiments);
    logger.info('✓ Registered RPC: ListActiveExperiments');
  } catch (e) {
    logger.error('✗ Failed to register ListActiveExperiments: %s', e);
  }

  // ============================================================================
  // HEALTH CHECK RPCs
  // ============================================================================

  try {
    initializer.registerRpc('HealthCheck', healthCheck);
    logger.info('✓ Registered RPC: HealthCheck');
  } catch (e) {
    logger.error('✗ Failed to register HealthCheck: %s', e);
  }

  try {
    initializer.registerRpc('DetailedHealthCheck', detailedHealthCheck);
    logger.info('✓ Registered RPC: DetailedHealthCheck');
  } catch (e) {
    logger.error('✗ Failed to register DetailedHealthCheck: %s', e);
  }

  // ============================================================================
  // INITIALIZATION COMPLETE
  // ============================================================================

  logger.info('=================================================');
  logger.info('OCTP Nakama Server - Initialization Complete');
  logger.info('Total RPCs Registered: 8');
  logger.info('=================================================');
  logger.info('');
  logger.info('Available RPCs:');
  logger.info('  Analytics:');
  logger.info('    - AnalyticsCollectEvents');
  logger.info('    - AnalyticsGetUserEvents');
  logger.info('  Remote Config:');
  logger.info('    - FetchRemoteConfig');
  logger.info('    - UpdateRemoteConfig (Admin)');
  logger.info('  Experiments:');
  logger.info('    - GetExperimentAssignment');
  logger.info('    - ListActiveExperiments');
  logger.info('  Health:');
  logger.info('    - HealthCheck');
  logger.info('    - DetailedHealthCheck');
  logger.info('=================================================');
};

// Note: InitModule must be globally accessible for Nakama to invoke it
// This is automatically handled by TypeScript compilation to ES5

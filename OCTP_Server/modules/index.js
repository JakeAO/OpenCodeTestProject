
// ===== lib/types.js =====


// ===== lib/utils.js =====
function validateAnalyticsEvent(event) {
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
function validateEventBatch(batch) {
    if (!batch || !Array.isArray(batch.events)) {
        return { valid: false, error: 'events must be an array' };
    }
    if (batch.events.length === 0) {
        return { valid: false, error: 'events array cannot be empty' };
    }
    if (batch.events.length > 100) {
        return { valid: false, error: 'events array exceeds maximum batch size of 100' };
    }
    for (var i = 0; i < batch.events.length; i++) {
        var validation = validateAnalyticsEvent(batch.events[i]);
        if (!validation.valid) {
            return { valid: false, error: "Event at index ".concat(i, ": ").concat(validation.error) };
        }
    }
    return { valid: true };
}
function validateExperimentId(experimentId) {
    if (typeof experimentId !== 'string' || experimentId.length === 0) {
        return { valid: false, error: 'experiment_id must be a non-empty string' };
    }
    if (experimentId.length > 255) {
        return { valid: false, error: 'experiment_id exceeds 255 characters' };
    }
    if (!/^[a-zA-Z0-9_-]+$/.test(experimentId)) {
        return { valid: false, error: 'experiment_id contains invalid characters' };
    }
    return { valid: true };
}
function escapeSqlString(value) {
    if (value === null || value === undefined) {
        return 'NULL';
    }
    return value.replace(/'/g, "''");
}
function toSqlLiteral(value) {
    if (value === null || value === undefined) {
        return 'NULL';
    }
    if (typeof value === 'string') {
        return "'".concat(escapeSqlString(value), "'");
    }
    if (typeof value === 'number') {
        return value.toString();
    }
    if (typeof value === 'boolean') {
        return value ? 'TRUE' : 'FALSE';
    }
    if (typeof value === 'object') {
        return "'".concat(escapeSqlString(JSON.stringify(value)), "'::jsonb");
    }
    return 'NULL';
}
function generateBatchId() {
    var timestamp = Date.now();
    var random = Math.random().toString(36).substring(2, 8);
    return "batch_".concat(timestamp, "_").concat(random);
}
function bytesToString(bytes) {
    if (typeof bytes === 'string') {
        return bytes;
    }
    if (Array.isArray(bytes)) {
        return String.fromCharCode.apply(String, bytes);
    }
    return String(bytes);
}
function parseJsonbColumn(value, defaultValue) {
    if (defaultValue === void 0) { defaultValue = null; }
    if (value === null || value === undefined) {
        return defaultValue;
    }
    if (typeof value === 'object' && !Array.isArray(value)) {
        return value;
    }
    var str = bytesToString(value);
    return safeJsonParse(str, defaultValue);
}
function getCurrentTimestamp() {
    return Date.now();
}
function assignCohort(userId, distribution) {
    var hash = simpleHash(userId);
    var normalized = (hash % 10000) / 10000;
    var cumulative = 0;
    for (var cohort in distribution) {
        cumulative += distribution[cohort];
        if (normalized < cumulative) {
            return cohort;
        }
    }
    return Object.keys(distribution)[0] || 'control';
}
function simpleHash(str) {
    var hash = 0;
    for (var i = 0; i < str.length; i++) {
        var char = str.charCodeAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash;
    }
    return Math.abs(hash);
}
function safeJsonParse(json, fallback) {
    try {
        return JSON.parse(json);
    }
    catch (e) {
        return fallback;
    }
}
function isValidObject(value) {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}
function truncateString(str, maxLength) {
    if (str.length <= maxLength) {
        return str;
    }
    return str.substring(0, maxLength - 3) + '...';
}


// ===== rpc/analytics.js =====
var analyticsCollectEvents = function (ctx, logger, nk, payload) {
    var startTime = Date.now();
    try {
        var batch = void 0;
        try {
            batch = JSON.parse(payload);
        }
        catch (e) {
            logger.error('Failed to parse analytics payload: %s', e);
            return JSON.stringify({
                success: false,
                events_inserted: 0,
                batch_id: '',
                error: 'Invalid JSON payload'
            });
        }
        var validation = validateEventBatch(batch);
        if (!validation.valid) {
            logger.warn('Invalid analytics batch from user %s: %s', ctx.userId, validation.error);
            return JSON.stringify({
                success: false,
                events_inserted: 0,
                batch_id: '',
                error: validation.error
            });
        }
        var batchId = generateBatchId();
        var sessionId = batch.session_id || "session_".concat(ctx.userId, "_").concat(Date.now());
        var userId = ctx.userId;
        var values = [];
        for (var _i = 0, _a = batch.events; _i < _a.length; _i++) {
            var event = _a[_i];
            var row = {
                user_id: userId,
                session_id: sessionId,
                event_name: event.event_name,
                event_properties: event.properties || {},
                experiment_id: event.experiment_id || null,
                cohort: event.cohort || null,
                client_timestamp: event.timestamp
            };
            var valueTuple = "(\n        ".concat(toSqlLiteral(row.user_id), ",\n        ").concat(toSqlLiteral(row.session_id), ",\n        ").concat(toSqlLiteral(row.event_name), ",\n        ").concat(toSqlLiteral(row.event_properties), ",\n        ").concat(toSqlLiteral(row.experiment_id), ",\n        ").concat(toSqlLiteral(row.cohort), ",\n        ").concat(toSqlLiteral(row.client_timestamp), ",\n        NOW()\n      )");
            values.push(valueTuple);
        }
        var sql = "\n      INSERT INTO analytics_events (\n        user_id,\n        session_id,\n        event_name,\n        event_properties,\n        experiment_id,\n        cohort,\n        client_timestamp,\n        server_timestamp\n      ) VALUES ".concat(values.join(', '), "\n    ");
        try {
            var result = nk.sqlExec(sql);
            var eventsInserted = result.rowsAffected || 0;
            var elapsedMs = Date.now() - startTime;
            logger.info('Analytics: Inserted %d events for user %s (batch_id: %s, elapsed: %dms)', eventsInserted, userId, batchId, elapsedMs);
            return JSON.stringify({
                success: true,
                events_inserted: eventsInserted,
                batch_id: batchId
            });
        }
        catch (sqlError) {
            logger.error('SQL error inserting analytics events: %s', sqlError);
            return JSON.stringify({
                success: false,
                events_inserted: 0,
                batch_id: batchId,
                error: 'Database insertion failed'
            });
        }
    }
    catch (error) {
        logger.error('Unexpected error in analyticsCollectEvents: %s', error);
        return JSON.stringify({
            success: false,
            events_inserted: 0,
            batch_id: '',
            error: 'Internal server error'
        });
    }
};
var analyticsGetUserEvents = function (ctx, logger, nk, payload) {
    try {
        var params = payload ? JSON.parse(payload) : {};
        var limit = Math.min(params.limit || 100, 500);
        var offset = params.offset || 0;
        var eventName = params.event_name || null;
        var userId = ctx.userId;
        var sql = "\n      SELECT \n        event_name,\n        event_properties,\n        experiment_id,\n        cohort,\n        client_timestamp,\n        server_timestamp\n      FROM analytics_events\n      WHERE user_id = ".concat(toSqlLiteral(userId), "\n    ");
        if (eventName) {
            sql += " AND event_name = ".concat(toSqlLiteral(eventName));
        }
        sql += "\n      ORDER BY server_timestamp DESC\n      LIMIT ".concat(limit, "\n      OFFSET ").concat(offset, "\n    ");
        var result = nk.sqlQuery(sql);
        logger.info('Retrieved %d analytics events for user %s', result.length, userId);
        return JSON.stringify({
            success: true,
            events: result,
            count: result.length
        });
    }
    catch (error) {
        logger.error('Error in analyticsGetUserEvents: %s', error);
        return JSON.stringify({
            success: false,
            events: [],
            error: 'Failed to retrieve events'
        });
    }
};


// ===== rpc/config.js =====
var configCache = {};
var CACHE_TTL_MS = 60000;
var fetchRemoteConfig = function (ctx, logger, nk, payload) {
    try {
        var userId = ctx.userId;
        var cacheKey = "config_".concat(userId);
        var cached = configCache[cacheKey];
        if (cached && (Date.now() - cached.timestamp) < CACHE_TTL_MS) {
            logger.debug('Config cache hit for user %s', userId);
            return JSON.stringify(cached.config);
        }
        var assignmentSql = "\n      SELECT experiment_id, cohort\n      FROM user_experiment_assignments\n      WHERE user_id = ".concat(toSqlLiteral(userId), "\n      LIMIT 1\n    ");
        var assignmentResult = nk.sqlQuery(assignmentSql);
        var experimentId = null;
        var cohort = null;
        if (assignmentResult.length > 0) {
            experimentId = assignmentResult[0].experiment_id;
            cohort = assignmentResult[0].cohort;
            logger.debug('User %s assigned to experiment %s, cohort %s', userId, experimentId, cohort);
        }
        else {
            logger.debug('User %s has no experiment assignment', userId);
        }
        var configSql = void 0;
        if (experimentId && cohort) {
            configSql = "\n        SELECT config_data\n        FROM config_variants\n        WHERE experiment_id = ".concat(toSqlLiteral(experimentId), "\n          AND cohort = ").concat(toSqlLiteral(cohort), "\n          AND is_active = TRUE\n        LIMIT 1\n      ");
        }
        else {
            configSql = "\n        SELECT config_data\n        FROM config_variants\n        WHERE experiment_id = 'default'\n          AND cohort = 'default'\n          AND is_active = TRUE\n        LIMIT 1\n      ";
        }
        var configResult = nk.sqlQuery(configSql);
        var config = {};
        if (configResult.length > 0) {
            var row = configResult[0];
            config = parseJsonbColumn(row.config_data, {});
        }
        logger.info('Fetched %d config keys for user %s', Object.keys(config).length, userId);
        var response = {
            success: true,
            experiment_id: experimentId,
            cohort: cohort,
            config: config
        };
        configCache[cacheKey] = {
            config: response,
            timestamp: Date.now()
        };
        return JSON.stringify(response);
    }
    catch (error) {
        logger.error('Error in fetchRemoteConfig: %s', error);
        return JSON.stringify({
            success: false,
            experiment_id: null,
            cohort: null,
            config: {},
            error: 'Failed to fetch config'
        });
    }
};
var updateRemoteConfig = function (ctx, logger, nk, payload) {
    try {
        var params = JSON.parse(payload);
        if (!params.experiment_id || !params.cohort || !params.config_data) {
            return JSON.stringify({
                success: false,
                error: 'Missing required parameters: experiment_id, cohort, config_data'
            });
        }
        var experimentId = params.experiment_id;
        var cohort = params.cohort;
        var configData = params.config_data;
        var sql = "\n      INSERT INTO config_variants (experiment_id, cohort, config_data, updated_at)\n      VALUES (\n        ".concat(toSqlLiteral(experimentId), ",\n        ").concat(toSqlLiteral(cohort), ",\n        ").concat(toSqlLiteral(configData), "::jsonb,\n        NOW()\n      )\n      ON CONFLICT (experiment_id, cohort)\n      DO UPDATE SET\n        config_data = ").concat(toSqlLiteral(configData), "::jsonb,\n        updated_at = NOW()\n    ");
        nk.sqlExec(sql);
        logger.info('Config updated: experiment=%s, cohort=%s by user %s', experimentId, cohort, ctx.userId);
        for (var key in configCache) {
            delete configCache[key];
        }
        return JSON.stringify({
            success: true
        });
    }
    catch (error) {
        logger.error('Error in updateRemoteConfig: %s', error);
        return JSON.stringify({
            success: false,
            error: 'Failed to update config'
        });
    }
};


// ===== rpc/experiments.js =====
var getExperimentAssignment = function (ctx, logger, nk, payload) {
    try {
        var request = JSON.parse(payload);
        var userId = request.user_id || ctx.userId;
        var experimentId = request.experiment_id;
        var validation = validateExperimentId(experimentId);
        if (!validation.valid) {
            return JSON.stringify({
                success: false,
                user_id: userId,
                experiment_id: experimentId,
                cohort: '',
                is_new_assignment: false,
                error: validation.error
            });
        }
        var existingSql = "\n      SELECT cohort\n      FROM user_experiment_assignments\n      WHERE user_id = ".concat(toSqlLiteral(userId), "\n        AND experiment_id = ").concat(toSqlLiteral(experimentId), "\n      LIMIT 1\n    ");
        var existingResult = nk.sqlQuery(existingSql);
        if (existingResult.length > 0) {
            var cohort_1 = existingResult[0].cohort;
            logger.debug('User %s already assigned to cohort %s for experiment %s', userId, cohort_1, experimentId);
            return JSON.stringify({
                success: true,
                user_id: userId,
                experiment_id: experimentId,
                cohort: cohort_1,
                is_new_assignment: false
            });
        }
        var metaSql = "\n      SELECT \n        name,\n        is_active,\n        cohorts\n      FROM experiment_metadata\n      WHERE id = ".concat(toSqlLiteral(experimentId), "\n      LIMIT 1\n    ");
        var metaResult = nk.sqlQuery(metaSql);
        if (metaResult.length === 0) {
            logger.warn('Experiment not found: %s', experimentId);
            return JSON.stringify({
                success: false,
                user_id: userId,
                experiment_id: experimentId,
                cohort: '',
                is_new_assignment: false,
                error: 'Experiment not found'
            });
        }
        var meta = metaResult[0];
        if (!meta.is_active) {
            logger.warn('Experiment is not active: %s', experimentId);
            return JSON.stringify({
                success: false,
                user_id: userId,
                experiment_id: experimentId,
                cohort: '',
                is_new_assignment: false,
                error: 'Experiment is not active'
            });
        }
        var distribution = parseJsonbColumn(meta.cohorts, { control: 1.0 });
        var cohort = assignCohort(userId, distribution);
        var insertSql = "\n      INSERT INTO user_experiment_assignments (user_id, experiment_id, cohort, assigned_at)\n      VALUES (\n        ".concat(toSqlLiteral(userId), ",\n        ").concat(toSqlLiteral(experimentId), ",\n        ").concat(toSqlLiteral(cohort), ",\n        NOW()\n      )\n    ");
        try {
            nk.sqlExec(insertSql);
            logger.info('User %s assigned to cohort %s for experiment %s', userId, cohort, experimentId);
            return JSON.stringify({
                success: true,
                user_id: userId,
                experiment_id: experimentId,
                cohort: cohort,
                is_new_assignment: true
            });
        }
        catch (sqlError) {
            logger.error('Failed to save experiment assignment: %s', sqlError);
            return JSON.stringify({
                success: false,
                user_id: userId,
                experiment_id: experimentId,
                cohort: '',
                is_new_assignment: false,
                error: 'Failed to save assignment'
            });
        }
    }
    catch (error) {
        logger.error('Error in getExperimentAssignment: %s', error);
        return JSON.stringify({
            success: false,
            user_id: '',
            experiment_id: '',
            cohort: '',
            is_new_assignment: false,
            error: 'Internal server error'
        });
    }
};
var listActiveExperiments = function (ctx, logger, nk, payload) {
    try {
        var sql = "\n      SELECT \n        id,\n        name,\n        description,\n        cohorts,\n        start_date,\n        end_date\n      FROM experiment_metadata\n      WHERE is_active = TRUE\n      ORDER BY start_date DESC\n    ";
        var result = nk.sqlQuery(sql);
        logger.info('Retrieved %d active experiments', result.length);
        return JSON.stringify({
            success: true,
            experiments: result,
            count: result.length
        });
    }
    catch (error) {
        logger.error('Error in listActiveExperiments: %s', error);
        return JSON.stringify({
            success: false,
            experiments: [],
            error: 'Failed to list experiments'
        });
    }
};


// ===== rpc/health.js =====
var serverStartTime = Date.now();
var healthCheck = function (ctx, logger, nk, payload) {
    var response = {
        status: 'healthy',
        timestamp: getCurrentTimestamp(),
        database_connected: false,
        uptime_seconds: Math.floor((Date.now() - serverStartTime) / 1000)
    };
    try {
        var sql = 'SELECT 1 as test';
        var result = nk.sqlQuery(sql);
        if (result && result.length > 0 && result[0].test === 1) {
            response.database_connected = true;
            response.status = 'healthy';
            logger.debug('Health check passed: database connected');
        }
        else {
            response.database_connected = false;
            response.status = 'degraded';
            response.error = 'Database query returned unexpected result';
            logger.warn('Health check degraded: unexpected database result');
        }
    }
    catch (error) {
        response.database_connected = false;
        response.status = 'unhealthy';
        response.error = 'Database connection failed: ' + error;
        logger.error('Health check failed: %s', error);
    }
    return JSON.stringify(response);
};
var detailedHealthCheck = function (ctx, logger, nk, payload) {
    var checks = {
        timestamp: getCurrentTimestamp(),
        uptime_seconds: Math.floor((Date.now() - serverStartTime) / 1000),
        status: 'healthy',
        checks: {}
    };
    try {
        try {
            var sql = 'SELECT 1 as test';
            nk.sqlQuery(sql);
            checks.checks['database_connection'] = { status: 'ok' };
        }
        catch (e) {
            checks.checks['database_connection'] = { status: 'failed', error: String(e) };
            checks.status = 'unhealthy';
        }
        try {
            var sql = 'SELECT COUNT(*) as count FROM analytics_events';
            var result = nk.sqlQuery(sql);
            checks.checks['analytics_events_table'] = {
                status: 'ok',
                row_count: result[0].count
            };
        }
        catch (e) {
            checks.checks['analytics_events_table'] = {
                status: 'failed',
                error: 'Table may not exist or is inaccessible'
            };
            checks.status = 'degraded';
        }
        try {
            var sql = 'SELECT COUNT(*) as count FROM config_variants';
            var result = nk.sqlQuery(sql);
            checks.checks['config_variants_table'] = {
                status: 'ok',
                row_count: result[0].count
            };
        }
        catch (e) {
            checks.checks['config_variants_table'] = {
                status: 'failed',
                error: 'Table may not exist or is inaccessible'
            };
            checks.status = 'degraded';
        }
        try {
            var sql = 'SELECT COUNT(*) as count FROM user_experiment_assignments';
            var result = nk.sqlQuery(sql);
            checks.checks['user_experiment_assignments_table'] = {
                status: 'ok',
                row_count: result[0].count
            };
        }
        catch (e) {
            checks.checks['user_experiment_assignments_table'] = {
                status: 'failed',
                error: 'Table may not exist or is inaccessible'
            };
            checks.status = 'degraded';
        }
        try {
            var sql = 'SELECT COUNT(*) as count FROM experiment_metadata WHERE is_active = TRUE';
            var result = nk.sqlQuery(sql);
            checks.checks['experiment_metadata_table'] = {
                status: 'ok',
                active_experiments: result[0].count
            };
        }
        catch (e) {
            checks.checks['experiment_metadata_table'] = {
                status: 'failed',
                error: 'Table may not exist or is inaccessible'
            };
            checks.status = 'degraded';
        }
        logger.info('Detailed health check completed with status: %s', checks.status);
    }
    catch (error) {
        checks.status = 'unhealthy';
        checks.error = 'Unexpected error during health check: ' + error;
        logger.error('Detailed health check error: %s', error);
    }
    return JSON.stringify(checks);
};


// ===== index.js =====
var InitModule = function (ctx, logger, nk, initializer) {
    logger.info('=================================================');
    logger.info('OCTP Nakama Server - Initializing...');
    logger.info('=================================================');
    try {
        initializer.registerRpc('AnalyticsCollectEvents', analyticsCollectEvents);
        logger.info('✓ Registered RPC: AnalyticsCollectEvents');
    }
    catch (e) {
        logger.error('✗ Failed to register AnalyticsCollectEvents: %s', e);
    }
    try {
        initializer.registerRpc('AnalyticsGetUserEvents', analyticsGetUserEvents);
        logger.info('✓ Registered RPC: AnalyticsGetUserEvents');
    }
    catch (e) {
        logger.error('✗ Failed to register AnalyticsGetUserEvents: %s', e);
    }
    try {
        initializer.registerRpc('FetchRemoteConfig', fetchRemoteConfig);
        logger.info('✓ Registered RPC: FetchRemoteConfig');
    }
    catch (e) {
        logger.error('✗ Failed to register FetchRemoteConfig: %s', e);
    }
    try {
        initializer.registerRpc('UpdateRemoteConfig', updateRemoteConfig);
        logger.info('✓ Registered RPC: UpdateRemoteConfig (Admin Only)');
    }
    catch (e) {
        logger.error('✗ Failed to register UpdateRemoteConfig: %s', e);
    }
    try {
        initializer.registerRpc('GetExperimentAssignment', getExperimentAssignment);
        logger.info('✓ Registered RPC: GetExperimentAssignment');
    }
    catch (e) {
        logger.error('✗ Failed to register GetExperimentAssignment: %s', e);
    }
    try {
        initializer.registerRpc('ListActiveExperiments', listActiveExperiments);
        logger.info('✓ Registered RPC: ListActiveExperiments');
    }
    catch (e) {
        logger.error('✗ Failed to register ListActiveExperiments: %s', e);
    }
    try {
        initializer.registerRpc('HealthCheck', healthCheck);
        logger.info('✓ Registered RPC: HealthCheck');
    }
    catch (e) {
        logger.error('✗ Failed to register HealthCheck: %s', e);
    }
    try {
        initializer.registerRpc('DetailedHealthCheck', detailedHealthCheck);
        logger.info('✓ Registered RPC: DetailedHealthCheck');
    }
    catch (e) {
        logger.error('✗ Failed to register DetailedHealthCheck: %s', e);
    }
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


/**
 * Nakama TypeScript Runtime Type Definitions
 * Based on official Nakama documentation (2024-2026)
 */

declare namespace nkruntime {
  /**
   * Context passed to all RPC and hook functions
   */
  interface Context {
    env: { [key: string]: string };
    executionMode: string;
    headers: { [key: string]: string[] };
    queryParams: { [key: string]: string[] };
    userId: string;
    username: string;
    vars: { [key: string]: string };
    userSessionExp: number;
    sessionId: string;
    clientIp: string;
    clientPort: string;
    matchId: string;
    matchNode: string;
    matchLabel: string;
    matchTickRate: number;
  }

  /**
   * Logger interface for logging messages
   */
  interface Logger {
    debug(message: string, ...args: any[]): void;
    info(message: string, ...args: any[]): void;
    warn(message: string, ...args: any[]): void;
    error(message: string, ...args: any[]): void;
  }

  /**
   * Main Nakama server runtime interface
   */
  interface Nakama {
    // SQL operations
    sqlExec(query: string, parameters?: any[]): nkruntime.SqlExecResult;
    sqlQuery(query: string, parameters?: any[]): nkruntime.SqlQueryResult;

    // Account operations
    accountGetId(userId: string): nkruntime.Account;
    accountUpdateId(userId: string, username?: string, metadata?: { [key: string]: any }): void;
    
    // Storage operations
    storageRead(reads: nkruntime.StorageReadRequest[]): nkruntime.StorageObject[];
    storageWrite(writes: nkruntime.StorageWriteRequest[]): nkruntime.StorageWriteAck[];
    storageDelete(deletes: nkruntime.StorageDeleteRequest[]): void;
    
    // Notification operations
    notificationSend(
      userId: string,
      subject: string,
      content: { [key: string]: any },
      code: number,
      senderId?: string,
      persistent?: boolean
    ): void;

    // Wallet operations
    walletUpdate(
      userId: string,
      changeset: { [key: string]: number },
      metadata?: { [key: string]: any },
      updateLedger?: boolean
    ): nkruntime.WalletUpdateResult;

    // Leaderboard operations
    leaderboardRecordWrite(
      leaderboardId: string,
      ownerId: string,
      username?: string,
      score?: number,
      subscore?: number,
      metadata?: { [key: string]: any }
    ): nkruntime.LeaderboardRecord;

    // Match operations
    matchCreate(module: string, params?: { [key: string]: any }): string;
    matchGet(matchId: string): nkruntime.Match;
    
    // Stream operations
    streamUserList(mode: number, subject: string, subcontext: string, label: string): nkruntime.Presence[];
    streamUserJoin(userId: string, sessionId: string, stream: nkruntime.Stream): void;
    
    // Tournament operations
    tournamentCreate(id: string, sortOrder: number, operator: string, resetSchedule?: string, metadata?: { [key: string]: any }, title?: string, description?: string, category?: number, startTime?: number, endTime?: number, duration?: number, maxSize?: number, maxNumScore?: number, joinRequired?: boolean): void;
    tournamentRecordWrite(tournamentId: string, ownerId: string, username?: string, score?: number, subscore?: number, metadata?: { [key: string]: any }): nkruntime.LeaderboardRecord;
    
    // HTTP request
    httpRequest(
      url: string,
      method: string,
      headers: { [key: string]: string },
      body: string,
      timeout?: number
    ): nkruntime.HttpResponse;
    
    // JWT operations
    jwtGenerate(signingMethod: string, claims: { [key: string]: any }): string;
    
    // UUID operations
    uuidv4(): string;
    
    // Time operations
    time(): number;
    cronNext(expression: string, timestamp: number): number;
    
    // Base64 operations
    base64Encode(input: string): string;
    base64Decode(input: string): string;
    base64UrlEncode(input: string): string;
    base64UrlDecode(input: string): string;
    
    // Bcrypt operations
    bcryptHash(password: string): string;
    bcryptCompare(hash: string, password: string): boolean;
    
    // AES operations
    aes128Encrypt(input: string, key: string): string;
    aes128Decrypt(input: string, key: string): string;
    aes256Encrypt(input: string, key: string): string;
    aes256Decrypt(input: string, key: string): string;
    
    // MD5 operations
    md5Hash(input: string): string;
    
    // SHA256 operations
    sha256Hash(input: string): string;
    
    // HMAC operations
    hmacSha256Hash(input: string, key: string): string;
    
    // RSA operations
    rsaSha256Hash(input: string, key: string): string;
    
    // Metrics
    metricsCounterAdd(name: string, tags: { [key: string]: string }, delta: number): void;
    metricsGaugeSet(name: string, tags: { [key: string]: string }, value: number): void;
    metricsTimerRecord(name: string, tags: { [key: string]: string }, value: number): void;
  }

  /**
   * Initializer for registering RPCs, hooks, and match handlers
   */
  interface Initializer {
    registerRpc(id: string, fn: RpcFunction): void;
    registerRtBefore(id: string, fn: RtBeforeFunction): void;
    registerRtAfter(id: string, fn: RtAfterFunction): void;
    registerBeforeReq(id: string, fn: BeforeReqFunction): void;
    registerAfterReq(id: string, fn: AfterReqFunction): void;
    registerMatch(name: string, handlers: MatchHandler): void;
    registerMatchmakerMatched(fn: MatchmakerMatchedFunction): void;
    registerTournamentEnd(fn: TournamentEndFunction): void;
    registerTournamentReset(fn: TournamentResetFunction): void;
    registerLeaderboardReset(fn: LeaderboardResetFunction): void;
    registerShutdown(fn: ShutdownFunction): void;
  }

  /**
   * RPC function signature
   */
  type RpcFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    payload: string
  ) => string | void;

  /**
   * Realtime before hook function signature
   */
  type RtBeforeFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    envelope: any
  ) => any;

  /**
   * Realtime after hook function signature
   */
  type RtAfterFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    envelope: any
  ) => void;

  /**
   * API before hook function signature
   */
  type BeforeReqFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    request: any
  ) => any;

  /**
   * API after hook function signature
   */
  type AfterReqFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    request: any,
    response: any
  ) => void;

  /**
   * Match handler interface
   */
  interface MatchHandler {
    matchInit: (ctx: Context, logger: Logger, nk: Nakama, params: { [key: string]: any }) => { state: any, tickRate: number, label: string };
    matchJoinAttempt: (ctx: Context, logger: Logger, nk: Nakama, dispatcher: MatchDispatcher, tick: number, state: any, presence: Presence, metadata: { [key: string]: any }) => { state: any, accept: boolean, rejectMessage?: string } | null;
    matchJoin: (ctx: Context, logger: Logger, nk: Nakama, dispatcher: MatchDispatcher, tick: number, state: any, presences: Presence[]) => { state: any } | null;
    matchLeave: (ctx: Context, logger: Logger, nk: Nakama, dispatcher: MatchDispatcher, tick: number, state: any, presences: Presence[]) => { state: any } | null;
    matchLoop: (ctx: Context, logger: Logger, nk: Nakama, dispatcher: MatchDispatcher, tick: number, state: any, messages: MatchMessage[]) => { state: any } | null;
    matchTerminate: (ctx: Context, logger: Logger, nk: Nakama, dispatcher: MatchDispatcher, tick: number, state: any, graceSeconds: number) => { state: any } | null;
  }

  /**
   * Matchmaker matched function signature
   */
  type MatchmakerMatchedFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    matches: MatchmakerResult[]
  ) => string | null;

  /**
   * Tournament end function signature
   */
  type TournamentEndFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    tournament: Tournament,
    end: number,
    reset: number
  ) => void;

  /**
   * Tournament reset function signature
   */
  type TournamentResetFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    tournament: Tournament,
    end: number,
    reset: number
  ) => void;

  /**
   * Leaderboard reset function signature
   */
  type LeaderboardResetFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    leaderboard: Leaderboard,
    reset: number
  ) => void;

  /**
   * Shutdown function signature
   */
  type ShutdownFunction = (
    ctx: Context,
    logger: Logger,
    nk: Nakama
  ) => void;

  /**
   * InitModule function signature - must be at global scope
   */
  type InitModule = (
    ctx: Context,
    logger: Logger,
    nk: Nakama,
    initializer: Initializer
  ) => void;

  // Supporting types
  interface SqlExecResult {
    rowsAffected: number;
  }

  interface SqlQueryResult extends Array<any> {
    rows?: any[];
  }

  interface Account {
    user: User;
    wallet: string;
    email: string;
    devices: Device[];
    customId: string;
  }

  interface User {
    id: string;
    username: string;
    displayName: string;
    avatarUrl: string;
    langTag: string;
    location: string;
    timezone: string;
    metadata: string;
    createTime: number;
    updateTime: number;
  }

  interface Device {
    id: string;
  }

  interface StorageReadRequest {
    collection: string;
    key: string;
    userId?: string;
  }

  interface StorageObject {
    collection: string;
    key: string;
    userId: string;
    value: string;
    version: string;
    permissionRead: number;
    permissionWrite: number;
    createTime: number;
    updateTime: number;
  }

  interface StorageWriteRequest {
    collection: string;
    key: string;
    userId?: string;
    value: string;
    version?: string;
    permissionRead?: number;
    permissionWrite?: number;
  }

  interface StorageWriteAck {
    collection: string;
    key: string;
    userId: string;
    version: string;
  }

  interface StorageDeleteRequest {
    collection: string;
    key: string;
    userId?: string;
    version?: string;
  }

  interface WalletUpdateResult {
    updated: { [key: string]: number };
    previous: { [key: string]: number };
  }

  interface LeaderboardRecord {
    leaderboardId: string;
    ownerId: string;
    username: string;
    score: number;
    subscore: number;
    numScore: number;
    metadata: string;
    createTime: number;
    updateTime: number;
    expiryTime: number;
    rank: number;
    maxNumScore: number;
  }

  interface Match {
    matchId: string;
    authoritative: boolean;
    size: number;
  }

  interface Presence {
    userId: string;
    sessionId: string;
    username: string;
    node: string;
  }

  interface Stream {
    mode: number;
    subject: string;
    subcontext: string;
    label: string;
  }

  interface HttpResponse {
    code: number;
    headers: { [key: string]: string[] };
    body: string;
  }

  interface MatchDispatcher {
    broadcastMessage(opCode: number, data: string, presences?: Presence[], sender?: Presence): void;
    broadcastMessageDeferred(opCode: number, data: string, presences?: Presence[], sender?: Presence): void;
    matchKick(presences: Presence[]): void;
    matchLabelUpdate(label: string): void;
  }

  interface MatchMessage {
    sender: Presence;
    opCode: number;
    data: string;
    reliable: boolean;
    receiveTime: number;
  }

  interface MatchmakerResult {
    presence: Presence;
    properties: { [key: string]: any };
  }

  interface Tournament {
    id: string;
    title: string;
    description: string;
    category: number;
    sortOrder: number;
    size: number;
    maxSize: number;
    maxNumScore: number;
    duration: number;
    startActive: number;
    endActive: number;
    canEnter: boolean;
    nextReset: number;
    metadata: string;
    createTime: number;
    startTime: number;
    endTime: number;
  }

  interface Leaderboard {
    id: string;
    authoritative: boolean;
    sortOrder: number;
    operator: string;
    prevReset: number;
    nextReset: number;
    metadata: string;
  }
}

import type { Schema } from "@shared/contracts";

export type HealthStatus = Schema<"HealthDto">;
export type RuntimeStatus = Schema<"RuntimeStatusDto">;
export type RuntimeSetupResponse = Schema<"RuntimeSetupResponse">;
export type SyncStatus = Schema<"SyncStatusDto">;
export type DiagnosticsHealthStatus = Schema<"DiagnosticsHealthStatus">;
export type DiagnosticsDatabase = Schema<"DiagnosticsDatabaseDto">;
export type DiagnosticsVectorIndex = Schema<"DiagnosticsVectorIndexDto">;
export type DiagnosticsStorage = Schema<"DiagnosticsStorageDto">;
export type DiagnosticsRuntime = Schema<"DiagnosticsRuntimeDto">;
export type DiagnosticsIngestionError = Schema<"DiagnosticsIngestionErrorDto">;
export type DiagnosticsStatus = Schema<"DiagnosticsDto">;

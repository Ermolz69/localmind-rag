namespace KnowledgeApp.Contracts.Runtime;

/// <summary>
/// Describes the LocalApi readiness state.
/// </summary>
/// <param name="Status">Current readiness status.</param>
/// <param name="Service">Service identifier.</param>
/// <param name="SupervisorInstanceId">Optional desktop supervisor instance identifier.</param>
public sealed record HealthDto(
    string Status,
    string Service,
    string? SupervisorInstanceId);

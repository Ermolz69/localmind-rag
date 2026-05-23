using KnowledgeApp.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Observability;

public sealed class AppDiagnosticLogger(
    ILogger<AppDiagnosticLogger> logger,
    IOptions<ObservabilityOptions> options) : IAppDiagnosticLogger
{
    private readonly ObservabilityOptions options = options.Value;

    public Guid BeginOperation(
        string area,
        string operationName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Guid operationId = Guid.NewGuid();
        if (!IsEnabled())
        {
            return operationId;
        }

        using IDisposable? scope = logger.BeginScope(ToScope(operationId, area, operationName, properties));
        logger.LogInformation(
            "Diagnostic operation started: {Area}.{OperationName} ({OperationId})",
            area,
            operationName,
            operationId);
        return operationId;
    }

    public void LogStep(
        Guid operationId,
        string stepName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!IsEnabled())
        {
            return;
        }

        using IDisposable? scope = logger.BeginScope(ToScope(operationId, area: null, operationName: null, properties));
        logger.LogInformation("Diagnostic operation step: {StepName} ({OperationId})", stepName, operationId);
    }

    public void LogFailure(
        Guid operationId,
        Exception exception,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!IsEnabled())
        {
            return;
        }

        using IDisposable? scope = logger.BeginScope(ToScope(operationId, area: null, operationName: null, properties));
        logger.LogError(exception, "Diagnostic operation failed: {OperationId}", operationId);
    }

    private bool IsEnabled()
    {
        return options.Enabled && options.Mode == ObservabilityMode.Advanced;
    }

    private static Dictionary<string, object?> ToScope(
        Guid operationId,
        string? area,
        string? operationName,
        IReadOnlyDictionary<string, object?>? properties)
    {
        Dictionary<string, object?> scope = new(StringComparer.Ordinal)
        {
            ["EventKind"] = "Diagnostic",
            ["OperationId"] = operationId,
        };

        if (!string.IsNullOrWhiteSpace(area))
        {
            scope["Area"] = area;
        }

        if (!string.IsNullOrWhiteSpace(operationName))
        {
            scope["OperationName"] = operationName;
        }

        if (properties is not null)
        {
            foreach (KeyValuePair<string, object?> property in properties)
            {
                scope[property.Key] = property.Value;
            }
        }

        return scope;
    }
}

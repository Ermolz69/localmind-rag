using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Observability;

public sealed class NoopAppDiagnosticLogger : IAppDiagnosticLogger
{
    public Guid BeginOperation(
        string area,
        string operationName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        return Guid.NewGuid();
    }

    public void LogStep(
        Guid operationId,
        string stepName,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
    }

    public void LogFailure(
        Guid operationId,
        Exception exception,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
    }
}

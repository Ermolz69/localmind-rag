namespace KnowledgeApp.Application.Abstractions;

public interface IAppDiagnosticLogger
{
    Guid BeginOperation(
        string area,
        string operationName,
        IReadOnlyDictionary<string, object?>? properties = null);

    void LogStep(
        Guid operationId,
        string stepName,
        IReadOnlyDictionary<string, object?>? properties = null);

    void LogFailure(
        Guid operationId,
        Exception exception,
        IReadOnlyDictionary<string, object?>? properties = null);
}

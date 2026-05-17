namespace KnowledgeApp.Application.Abstractions;

public interface IIngestionJobProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default);
}

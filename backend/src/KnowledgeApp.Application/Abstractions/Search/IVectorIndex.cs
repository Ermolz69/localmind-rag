namespace KnowledgeApp.Application.Abstractions;

public interface IVectorIndex
{
    Task UpsertAsync(Guid chunkId, float[] vector, CancellationToken cancellationToken = default);
}

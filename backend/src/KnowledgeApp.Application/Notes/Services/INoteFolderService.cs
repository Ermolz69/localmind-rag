using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.Application.Notes;

public interface INoteFolderService
{
    Task<Result> EnsureUniqueNameAsync(Guid bucketId, Guid? parentFolderId, string name, Guid? excludeFolderId = null, CancellationToken cancellationToken = default);
    Task<Result> EnsureNoCycleAsync(Guid folderId, Guid? newParentFolderId, CancellationToken cancellationToken = default);
    Task<Result> EnsureFolderIsEmptyAsync(Guid folderId, CancellationToken cancellationToken = default);
    Task<Result> ValidateParentAsync(Guid bucketId, Guid? parentFolderId, CancellationToken cancellationToken = default);
}

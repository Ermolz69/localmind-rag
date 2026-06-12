using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class GetNoteFoldersHandler(IAppDbContext dbContext)
{
    public async Task<Result<IReadOnlyCollection<NoteFolderDto>>> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        var folders = await dbContext.NoteFolders
            .AsNoTracking()
            .Where(f => f.BucketId == bucketId && f.DeletedAt == null)
            .OrderBy(f => f.Name)
            .Select(f => new NoteFolderDto(
                f.Id,
                f.BucketId,
                f.ParentFolderId,
                f.Name,
                (int)f.SyncStatus,
                f.CreatedAt,
                f.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<NoteFolderDto>>.Success(folders);
    }
}

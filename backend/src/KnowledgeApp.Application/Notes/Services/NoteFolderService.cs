using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class NoteFolderService(IAppDbContext dbContext) : INoteFolderService
{
    public async Task<Result> EnsureUniqueNameAsync(Guid bucketId, Guid? parentFolderId, string name, Guid? excludeFolderId = null, CancellationToken cancellationToken = default)
    {
        string normalizedName = name.Trim().ToUpperInvariant();

        bool exists = await dbContext.NoteFolders
            .AnyAsync(f => f.BucketId == bucketId &&
                           f.ParentFolderId == parentFolderId &&
                           f.NormalizedName == normalizedName &&
                           f.DeletedAt == null &&
                           (excludeFolderId == null || f.Id != excludeFolderId),
                cancellationToken);

        if (exists)
        {
            return Result.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.FolderExists, "A folder with this name already exists in the same location."));
        }

        return Result.Success();
    }

    public async Task<Result> EnsureNoCycleAsync(Guid folderId, Guid? newParentFolderId, CancellationToken cancellationToken = default)
    {
        Guid? currentId = newParentFolderId;

        while (currentId.HasValue)
        {
            if (currentId.Value == folderId)
            {
                return Result.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.CircularDependency, "A folder cannot be moved inside itself or its children."));
            }

            var parent = await dbContext.NoteFolders
                .AsNoTracking()
                .Where(f => f.Id == currentId.Value && f.DeletedAt == null)
                .Select(f => new { f.ParentFolderId })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null)
                break;

            currentId = parent.ParentFolderId;
        }

        return Result.Success();
    }

    public async Task<Result> EnsureFolderIsEmptyAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        bool hasChildren = await dbContext.NoteFolders
            .AnyAsync(f => f.ParentFolderId == folderId && f.DeletedAt == null, cancellationToken);

        if (hasChildren)
        {
            return Result.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.NotEmpty, "Cannot delete a folder that contains subfolders."));
        }

        bool hasNotes = await dbContext.Notes
            .AnyAsync(n => n.FolderId == folderId && n.DeletedAt == null, cancellationToken);

        if (hasNotes)
        {
            return Result.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.NotEmpty, "Cannot delete a folder that contains notes."));
        }

        return Result.Success();
    }

    public async Task<Result> ValidateParentAsync(Guid bucketId, Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        if (!parentFolderId.HasValue)
            return Result.Success();

        var parent = await dbContext.NoteFolders
            .AsNoTracking()
            .Where(f => f.Id == parentFolderId.Value && f.DeletedAt == null)
            .Select(f => new { f.BucketId })
            .FirstOrDefaultAsync(cancellationToken);

        if (parent == null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.FolderNotFound, "The specified parent folder was not found."));
        }

        if (parent.BucketId != bucketId)
        {
            return Result.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.InvalidBucket, "The parent folder must be in the same bucket."));
        }

        return Result.Success();
    }
}

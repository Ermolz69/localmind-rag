using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class MoveNoteFolderHandler(
    IAppDbContext dbContext,
    IUnitOfWork unitOfWork,
    INoteFolderService noteFolderService)
{
    public async Task<Result<NoteFolderDto>> HandleAsync(
        Guid folderId,
        MoveNoteFolderRequest request,
        CancellationToken cancellationToken = default)
    {
        var folder = await dbContext.NoteFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.DeletedAt == null, cancellationToken);
        if (folder == null)
            return Result<NoteFolderDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.FolderNotFound, "Note folder was not found."));

        if (request.BucketId != folder.BucketId)
        {
            // Moving across buckets requires recursive update. We will fail for MVP.
            return Result<NoteFolderDto>.Failure(ApplicationErrors.Conflict(ErrorCodes.Buckets.InvalidBucket, "Moving folders across buckets is not supported yet."));
        }

        if (request.ParentFolderId != folder.ParentFolderId)
        {
            Result parentValidation = await noteFolderService.ValidateParentAsync(request.BucketId, request.ParentFolderId, cancellationToken);
            if (!parentValidation.IsSuccess)
                return Result<NoteFolderDto>.Failure(parentValidation);

            Result cycleValidation = await noteFolderService.EnsureNoCycleAsync(folder.Id, request.ParentFolderId, cancellationToken);
            if (!cycleValidation.IsSuccess)
                return Result<NoteFolderDto>.Failure(cycleValidation);
        }

        Result uniqueness = await noteFolderService.EnsureUniqueNameAsync(request.BucketId, request.ParentFolderId, folder.Name, folder.Id, cancellationToken);
        if (!uniqueness.IsSuccess)
            return Result<NoteFolderDto>.Failure(uniqueness);

        folder.ParentFolderId = request.ParentFolderId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new NoteFolderDto(
            folder.Id,
            folder.BucketId,
            folder.ParentFolderId,
            folder.Name,
            (int)folder.SyncStatus,
            folder.CreatedAt,
            folder.UpdatedAt);

        return Result<NoteFolderDto>.Success(dto);
    }
}

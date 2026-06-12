using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class UpdateNoteFolderHandler(
    IAppDbContext dbContext,
    IUnitOfWork unitOfWork,
    INoteFolderService noteFolderService)
{
    public async Task<Result<NoteFolderDto>> HandleAsync(Guid folderId, UpdateNoteFolderRequest request, CancellationToken cancellationToken = default)
    {
        var folder = await dbContext.NoteFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.DeletedAt == null, cancellationToken);
        if (folder == null)
            return Result<NoteFolderDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.FolderNotFound, "Note folder was not found."));

        if (request.ParentFolderId.HasValue && request.ParentFolderId.Value != folder.ParentFolderId)
        {
            Result parentValidation = await noteFolderService.ValidateParentAsync(folder.BucketId, request.ParentFolderId, cancellationToken);
            if (!parentValidation.IsSuccess)
                return Result<NoteFolderDto>.Failure(parentValidation);

            Result cycleValidation = await noteFolderService.EnsureNoCycleAsync(folder.Id, request.ParentFolderId, cancellationToken);
            if (!cycleValidation.IsSuccess)
                return Result<NoteFolderDto>.Failure(cycleValidation);
        }

        Result uniqueness = await noteFolderService.EnsureUniqueNameAsync(folder.BucketId, request.ParentFolderId, request.Name, folder.Id, cancellationToken);
        if (!uniqueness.IsSuccess)
            return Result<NoteFolderDto>.Failure(uniqueness);

        folder.ParentFolderId = request.ParentFolderId;
        folder.Name = request.Name.Trim();
        folder.NormalizedName = request.Name.Trim().ToUpperInvariant();

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

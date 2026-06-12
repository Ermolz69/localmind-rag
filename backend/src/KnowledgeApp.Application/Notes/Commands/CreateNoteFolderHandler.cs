using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class CreateNoteFolderHandler(
    IAppDbContext dbContext,
    IUnitOfWork unitOfWork,
    INoteFolderService noteFolderService,
    ILocalDeviceResolver localDeviceResolver)
{
    public async Task<Result<NoteFolderDto>> HandleAsync(Guid bucketId, CreateNoteFolderRequest request, CancellationToken cancellationToken = default)
    {
        Result parentValidation = await noteFolderService.ValidateParentAsync(bucketId, request.ParentFolderId, cancellationToken);
        if (!parentValidation.IsSuccess)
            return Result<NoteFolderDto>.Failure(parentValidation);

        Result uniqueness = await noteFolderService.EnsureUniqueNameAsync(bucketId, request.ParentFolderId, request.Name, null, cancellationToken);
        if (!uniqueness.IsSuccess)
            return Result<NoteFolderDto>.Failure(uniqueness);

        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);

        NoteFolder folder = new()
        {
            Id = Guid.NewGuid(),
            BucketId = bucketId,
            ParentFolderId = request.ParentFolderId,
            Name = request.Name.Trim(),
            NormalizedName = request.Name.Trim().ToUpperInvariant(),
            LocalDeviceId = localDeviceId
        };

        dbContext.NoteFolders.Add(folder);
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

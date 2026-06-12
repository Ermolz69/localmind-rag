using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class DeleteNoteFolderHandler(
    IAppDbContext dbContext,
    IUnitOfWork unitOfWork,
    INoteFolderService noteFolderService,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(Guid folderId, CancellationToken cancellationToken = default)
    {
        var folder = await dbContext.NoteFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.DeletedAt == null, cancellationToken);
        if (folder == null)
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.FolderNotFound, "Note folder was not found."));

        Result emptyValidation = await noteFolderService.EnsureFolderIsEmptyAsync(folderId, cancellationToken);
        if (!emptyValidation.IsSuccess)
            return emptyValidation;

        folder.DeletedAt = dateTimeProvider.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

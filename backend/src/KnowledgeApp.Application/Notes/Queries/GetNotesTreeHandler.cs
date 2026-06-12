using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class GetNotesTreeHandler(IAppDbContext dbContext)
{
    public async Task<Result<NotesTreeResponse>> HandleAsync(Guid bucketId, CancellationToken cancellationToken = default)
    {
        var bucket = await dbContext.Buckets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bucketId && b.DeletedAt == null, cancellationToken);

        if (bucket == null)
        {
            return Result<NotesTreeResponse>.Failure(ApplicationErrors.NotFound(ErrorCodes.Buckets.BucketNotFound, "Bucket not found."));
        }

        var folders = await dbContext.NoteFolders
            .AsNoTracking()
            .Where(f => f.BucketId == bucketId && f.DeletedAt == null)
            .Select(f => new NoteFolderDto(
                f.Id,
                f.BucketId,
                f.ParentFolderId,
                f.Name,
                (int)f.SyncStatus,
                f.CreatedAt,
                f.UpdatedAt))
            .ToListAsync(cancellationToken);

        var notes = await dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.BucketId == bucketId && n.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var noteDtos = notes.Select(NoteMapper.ToDto).ToList();

        return Result<NotesTreeResponse>.Success(new NotesTreeResponse(
            BucketMapper.ToDto(bucket),
            folders,
            noteDtos));
    }
}

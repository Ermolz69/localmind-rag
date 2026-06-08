using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class NoteRepository(AppDbContext dbContext) : INoteRepository
{
    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notes
            .Include(note => note.Tags)
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Note>> ListAsync(Guid? bucketId, CancellationToken cancellationToken = default)
    {
        IQueryable<Note> query = dbContext.Notes
            .Include(note => note.Tags)
            .Where(note => note.DeletedAt == null);

        if (bucketId.HasValue)
        {
            query = query.Where(note => note.BucketId == bucketId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        await dbContext.Notes.AddAsync(note, cancellationToken);
    }

    public Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        dbContext.Notes.Update(note);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Note note, CancellationToken cancellationToken = default)
    {
        dbContext.Notes.Remove(note);
        return Task.CompletedTask;
    }
}

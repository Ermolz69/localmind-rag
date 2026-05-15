using System.Globalization;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Pagination;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Notes;

public sealed class GetNotesHandler(IAppDbContext dbContext)
{
    private const string CursorKind = "notes";

    public async Task<CursorPage<NoteDto>> HandleAsync(
        GetNotesQuery query,
        CancellationToken cancellationToken = default)
    {
        int limit = CursorPagination.ValidateLimit(query.Limit);
        string? normalizedQuery = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim();
        string filterHash = CursorPagination.CreateFilterHash(new { query.BucketId, Query = normalizedQuery });
        CursorPayload? cursor = CursorPagination.Decode(query.Cursor, CursorKind, filterHash);

        IQueryable<Note> notesQuery = dbContext.Notes
            .AsNoTracking()
            .Where(note => note.DeletedAt == null)
            .AsQueryable();

        if (query.BucketId.HasValue)
        {
            notesQuery = notesQuery.Where(note => note.BucketId == query.BucketId.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            notesQuery = notesQuery.Where(note =>
                note.Title.Contains(normalizedQuery) ||
                note.Markdown.Contains(normalizedQuery));
        }

        Note[] notes = await notesQuery.ToArrayAsync(cancellationToken);
        Note[] sortedNotes = notes
            .OrderByDescending(note => note.UpdatedAt.HasValue)
            .ThenByDescending(note => note.UpdatedAt)
            .ThenByDescending(note => note.CreatedAt)
            .ThenByDescending(note => note.Id.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();
        CursorPage<Note> notePage = CursorPagination.CreatePage(
            sortedNotes,
            cursor,
            limit,
            CompareNoteToCursor,
            note => new CursorPayload(
                CursorKind,
                filterHash,
                note.UpdatedAt,
                note.CreatedAt,
                note.Id,
                note.UpdatedAt.HasValue));
        NoteDto[] noteDtos = notePage.Items.Select(NoteMapper.ToDto).ToArray();

        return new CursorPage<NoteDto>(noteDtos, notePage.NextCursor, notePage.Limit, notePage.HasMore);
    }

    private static int CompareNoteToCursor(Note note, CursorPayload cursor)
    {
        if (note.Id == cursor.Id)
        {
            return 2;
        }

        bool hasUpdatedAt = note.UpdatedAt.HasValue;
        if (!hasUpdatedAt && cursor.HasPrimaryDate)
        {
            return 1;
        }

        if (hasUpdatedAt == cursor.HasPrimaryDate)
        {
            DateTimeOffset? updatedAt = note.UpdatedAt;
            if (updatedAt < cursor.PrimaryDate)
            {
                return 1;
            }

            if (updatedAt == cursor.PrimaryDate)
            {
                if (note.CreatedAt < cursor.CreatedAt)
                {
                    return 1;
                }

                if (note.CreatedAt == cursor.CreatedAt &&
                    string.Compare(
                        note.Id.ToString("N", CultureInfo.InvariantCulture),
                        cursor.Id.ToString("N", CultureInfo.InvariantCulture),
                        StringComparison.Ordinal) < 0)
                {
                    return 1;
                }
            }
        }

        return 0;
    }
}

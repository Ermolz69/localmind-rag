using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Search;

public sealed class SqliteFullTextChunkSearchService(AppDbContext dbContext) :
    IFullTextChunkSearchService,
    IFullTextChunkIndex
{
    private const int MaxFtsTerms = 16;

    public async Task<IReadOnlyList<FullTextChunkSearchResult>> SearchAsync(
        string query,
        FullTextSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options.Limit <= 0)
        {
            return [];
        }

        string? matchQuery = BuildMatchQuery(query);

        if (matchQuery is null)
        {
            return [];
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        ConnectionState originalState = connection.State;

        if (originalState != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = BuildSearchSql(options.Tags, options.FileType is not null);

            AddParameter(command, "$match", matchQuery);
            AddParameter(command, "$indexedStatus", (int)DocumentStatus.Indexed);
            AddParameter(command, "$limit", options.Limit);

            AddParameter(command, "$bucketId", options.BucketId);
            AddParameter(command, "$documentId", options.DocumentId);
            AddParameter(
                command,
                "$dateFrom",
                options.DateFrom.HasValue
                    ? SearchDateIndexing.ToUnixTimeMilliseconds(options.DateFrom.Value)
                    : null);
            AddParameter(
                command,
                "$dateTo",
                options.DateTo.HasValue
                    ? SearchDateIndexing.ToUnixTimeMilliseconds(
                        SearchDateRange.ToInclusiveEndOfDay(options.DateTo.Value))
                    : null);
            AddParameter(
                command,
                "$fileType",
                options.FileType is { } fileType
                    ? (int)FileTypeParser.Parse(fileType)
                    : null);

            AddTagParameters(command, options.Tags);

            List<FullTextChunkSearchResult> results = [];

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(new FullTextChunkSearchResult(
                    DocumentId: Guid.Parse(reader.GetString(0)),
                    DocumentName: reader.GetString(1),
                    ChunkId: Guid.Parse(reader.GetString(2)),
                    PageNumber: reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    Snippet: reader.GetString(4),
                    Rank: results.Count + 1,
                    Bm25Score: reader.GetDouble(5)));
            }

            return results;
        }
        finally
        {
            if (originalState != ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task SyncDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM document_chunks_fts
            WHERE document_id = {0};
            """,
            new object[] { documentId },
            cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO document_chunks_fts(chunk_id, document_id, document_name, text)
            SELECT chunk.Id, document.Id, document.Name, chunk.Text
            FROM document_chunks AS chunk
            INNER JOIN documents AS document ON document.Id = chunk.DocumentId
            WHERE document.Id = {0}
              AND document.DeletedAt IS NULL
              AND document.Status = {1};
            """,
            new object[] { documentId, (int)DocumentStatus.Indexed },
            cancellationToken);
    }

    private static string BuildSearchSql(
        IReadOnlyDictionary<string, string>? tags,
        bool hasFileType)
    {
        StringBuilder sql = new(
            """
            SELECT
                document.Id,
                document.Name,
                chunk.Id,
                chunk.PageNumber,
                chunk.Text,
                bm25(document_chunks_fts) AS bm25_score
            FROM document_chunks_fts
            INNER JOIN document_chunks AS chunk ON chunk.Id = document_chunks_fts.chunk_id
            INNER JOIN documents AS document ON document.Id = chunk.DocumentId
            WHERE document_chunks_fts MATCH $match
              AND document.DeletedAt IS NULL
              AND document.Status = $indexedStatus
            """);

        sql.AppendLine();
        sql.Append(
            """
              AND ($bucketId IS NULL OR document.BucketId = $bucketId)
              AND ($documentId IS NULL OR document.Id = $documentId)
              AND ($dateFrom IS NULL OR document.CreatedAtUnixTimeMs >= $dateFrom)
              AND ($dateTo IS NULL OR document.CreatedAtUnixTimeMs <= $dateTo)
            """);

        if (hasFileType)
        {
            sql.AppendLine();
            sql.Append(
                """
                  AND EXISTS (
                      SELECT 1
                      FROM document_files AS document_file
                      WHERE document_file.DocumentId = document.Id
                        AND document_file.FileType = $fileType
                  )
                """);
        }

        if (tags is { Count: > 0 })
        {
            int index = 0;

            foreach (var _ in tags)
            {
                sql.AppendLine();
                sql.Append(
                    $$"""
                      AND (
                          EXISTS (
                              SELECT 1
                              FROM document_tags AS document_tag
                              WHERE document_tag.DocumentId = document.Id
                                AND document_tag.Key = $tagKey{{index}}
                                AND document_tag.Value = $tagValue{{index}}
                          )
                          OR EXISTS (
                              SELECT 1
                              FROM document_chunk_tags AS chunk_tag
                              WHERE chunk_tag.DocumentChunkId = chunk.Id
                                AND chunk_tag.Key = $tagKey{{index}}
                                AND chunk_tag.Value = $tagValue{{index}}
                          )
                      )
                    """);
                index++;
            }
        }

        sql.AppendLine();
        sql.Append(
            """
            ORDER BY bm25_score ASC, document.Name COLLATE NOCASE ASC, chunk."Index" ASC
            LIMIT $limit;
            """);

        return sql.ToString();
    }

    private static string? BuildMatchQuery(string query)
    {
        string[] terms = Regex
            .Matches(query, @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(term => term.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxFtsTerms)
            .ToArray();

        if (terms.Length == 0)
        {
            return null;
        }

        return string.Join(" OR ", terms.Select(QuoteFtsTerm));
    }

    private static string QuoteFtsTerm(string term)
    {
        return "\"" + term.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static void AddTagParameters(
        DbCommand command,
        IReadOnlyDictionary<string, string>? tags)
    {
        if (tags is not { Count: > 0 })
        {
            return;
        }

        int index = 0;

        foreach (var tag in tags)
        {
            AddParameter(command, $"$tagKey{index}", tag.Key);
            AddParameter(command, $"$tagValue{index}", tag.Value);
            index++;
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

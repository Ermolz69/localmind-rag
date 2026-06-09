namespace KnowledgeApp.Infrastructure.Services.Search;

public static class FullTextSearchSchema
{
    public const string TableName = "document_chunks_fts";

    public const string CreateSql =
        """
        CREATE VIRTUAL TABLE document_chunks_fts USING fts5(
            chunk_id UNINDEXED,
            document_id UNINDEXED,
            document_name,
            text,
            tokenize = 'unicode61'
        );
        """;

    public const string BackfillSql =
        """
        INSERT INTO document_chunks_fts(chunk_id, document_id, document_name, text)
        SELECT chunk.Id, document.Id, document.Name, chunk.Text
        FROM document_chunks AS chunk
        INNER JOIN documents AS document ON document.Id = chunk.DocumentId
        WHERE document.DeletedAt IS NULL
          AND document.Status = 4;
        """;

    public const string DropSql =
        """
        DROP TABLE IF EXISTS document_chunks_fts;
        """;
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkingProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentId_TextHash",
                table: "document_chunks");

            migrationBuilder.RenameColumn(
                name: "TextHash",
                table: "document_chunks",
                newName: "TokenizerId");

            migrationBuilder.RenameIndex(
                name: "IX_document_chunks_TextHash",
                table: "document_chunks",
                newName: "IX_document_chunks_TokenizerId");

            migrationBuilder.AddColumn<string>(
                name: "ChunkIdentityHash",
                table: "document_chunks",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChunkType",
                table: "document_chunks",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<string>(
                name: "ChunkingAlgorithmId",
                table: "document_chunks",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingTextHash",
                table: "document_chunks",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeadingPath",
                table: "document_chunks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionTitle",
                table: "document_chunks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceEndOffset",
                table: "document_chunks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceStartOffset",
                table: "document_chunks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenCount",
                table: "document_chunks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_ChunkType",
                table: "document_chunks",
                column: "ChunkType");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_ChunkIdentityHash",
                table: "document_chunks",
                columns: new[] { "DocumentId", "ChunkIdentityHash" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_ChunkVersion",
                table: "document_chunks",
                columns: new[] { "DocumentId", "ChunkVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_Index",
                table: "document_chunks",
                columns: new[] { "DocumentId", "Index" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_EmbeddingTextHash",
                table: "document_chunks",
                column: "EmbeddingTextHash");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_EmbeddingTextHash_ChunkVersion",
                table: "document_chunks",
                columns: new[] { "EmbeddingTextHash", "ChunkVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_document_chunks_ChunkType",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentId_ChunkIdentityHash",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentId_ChunkVersion",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentId_Index",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_EmbeddingTextHash",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_EmbeddingTextHash_ChunkVersion",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "ChunkIdentityHash",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "ChunkType",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "ChunkingAlgorithmId",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingTextHash",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "HeadingPath",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SectionTitle",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SourceEndOffset",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "SourceStartOffset",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "TokenCount",
                table: "document_chunks");

            migrationBuilder.RenameColumn(
                name: "TokenizerId",
                table: "document_chunks",
                newName: "TextHash");

            migrationBuilder.RenameIndex(
                name: "IX_document_chunks_TokenizerId",
                table: "document_chunks",
                newName: "IX_document_chunks_TextHash");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_TextHash",
                table: "document_chunks",
                columns: new[] { "DocumentId", "TextHash" });
        }
    }
}

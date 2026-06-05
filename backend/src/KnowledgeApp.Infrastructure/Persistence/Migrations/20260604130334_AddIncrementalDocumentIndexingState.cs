using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncrementalDocumentIndexingState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IndexVersion",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IndexedContentHash",
                table: "documents",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChunkVersion",
                table: "document_chunks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TextHash",
                table: "document_chunks",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_documents_IndexedContentHash",
                table: "documents",
                column: "IndexedContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_DocumentId_TextHash",
                table: "document_chunks",
                columns: new[] { "DocumentId", "TextHash" });

            migrationBuilder.CreateIndex(
                name: "IX_document_chunks_TextHash",
                table: "document_chunks",
                column: "TextHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documents_IndexedContentHash",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_DocumentId_TextHash",
                table: "document_chunks");

            migrationBuilder.DropIndex(
                name: "IX_document_chunks_TextHash",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "IndexVersion",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IndexedContentHash",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "ChunkVersion",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "TextHash",
                table: "document_chunks");
        }
    }
}

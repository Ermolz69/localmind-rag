using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    public partial class AddSearchCreatedAtIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreatedAtUnixTimeMs",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "CreatedAtUnixTimeMs",
                table: "notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE documents
                SET CreatedAtUnixTimeMs = CAST(((julianday(CreatedAt) - 2440587.5) * 86400000.0) AS INTEGER);
                """);

            migrationBuilder.Sql("""
                UPDATE notes
                SET CreatedAtUnixTimeMs = CAST(((julianday(CreatedAt) - 2440587.5) * 86400000.0) AS INTEGER);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_documents_CreatedAtUnixTimeMs",
                table: "documents",
                column: "CreatedAtUnixTimeMs");

            migrationBuilder.CreateIndex(
                name: "IX_notes_CreatedAtUnixTimeMs",
                table: "notes",
                column: "CreatedAtUnixTimeMs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_documents_CreatedAtUnixTimeMs",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_notes_CreatedAtUnixTimeMs",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "CreatedAtUnixTimeMs",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "CreatedAtUnixTimeMs",
                table: "notes");
        }
    }
}

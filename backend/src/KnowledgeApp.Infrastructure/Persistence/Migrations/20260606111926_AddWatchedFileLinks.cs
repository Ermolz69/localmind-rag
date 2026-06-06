using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWatchedFileLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "watched_file_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WatchedFolderPath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    NormalizedFilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    LastContentHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LocalVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_watched_file_links", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_watched_file_links_DocumentId",
                table: "watched_file_links",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_watched_file_links_NormalizedFilePath",
                table: "watched_file_links",
                column: "NormalizedFilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_watched_file_links_WatchedFolderPath",
                table: "watched_file_links",
                column: "WatchedFolderPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "watched_file_links");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedWatchedFolderPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedWatchedFolderPath",
                table: "watched_file_links",
                type: "TEXT",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_watched_file_links_NormalizedWatchedFolderPath",
                table: "watched_file_links",
                column: "NormalizedWatchedFolderPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_watched_file_links_NormalizedWatchedFolderPath",
                table: "watched_file_links");

            migrationBuilder.DropColumn(
                name: "NormalizedWatchedFolderPath",
                table: "watched_file_links");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalVersionToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "sync_state",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "sync_outbox",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "semantic_cache_entries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "note_links",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "local_devices",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "ingestion_jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "documents",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "document_files",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "document_embeddings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "document_chunks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "conversations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "chat_messages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "buckets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "app_settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LocalVersion",
                table: "ai_models",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "sync_state");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "sync_outbox");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "semantic_cache_entries");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "note_links");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "local_devices");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "document_files");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "document_embeddings");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "document_chunks");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "buckets");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "app_settings");

            migrationBuilder.DropColumn(
                name: "LocalVersion",
                table: "ai_models");
        }
    }
}

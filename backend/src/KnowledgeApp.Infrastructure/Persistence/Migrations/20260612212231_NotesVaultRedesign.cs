using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotesVaultRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE notes SET BucketId = (SELECT Id FROM buckets WHERE Name = 'Default' AND DeletedAt IS NULL LIMIT 1)
                WHERE BucketId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "BucketId",
                table: "notes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "note_folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BucketId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalDeviceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LocalVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_note_folders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notes_FolderId",
                table: "notes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_note_folders_BucketId",
                table: "note_folders",
                column: "BucketId");

            migrationBuilder.CreateIndex(
                name: "IX_note_folders_DeletedAt",
                table: "note_folders",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_note_folders_LocalDeviceId",
                table: "note_folders",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_note_folders_ParentFolderId",
                table: "note_folders",
                column: "ParentFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "note_folders");

            migrationBuilder.DropIndex(
                name: "IX_notes_FolderId",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "notes");

            migrationBuilder.AlterColumn<Guid>(
                name: "BucketId",
                table: "notes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }
    }
}

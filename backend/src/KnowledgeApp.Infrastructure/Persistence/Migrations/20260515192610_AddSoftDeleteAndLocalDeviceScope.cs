using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAndLocalDeviceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDeviceId",
                table: "notes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDeviceId",
                table: "documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "conversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDeviceId",
                table: "conversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "chat_messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDeviceId",
                table: "chat_messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "buckets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalDeviceId",
                table: "buckets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "local_devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceKey = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_local_devices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notes_DeletedAt",
                table: "notes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notes_LocalDeviceId",
                table: "notes",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_DeletedAt",
                table: "documents",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_documents_LocalDeviceId",
                table: "documents",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_DeletedAt",
                table: "conversations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_LocalDeviceId",
                table: "conversations",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_DeletedAt",
                table: "chat_messages",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_LocalDeviceId",
                table: "chat_messages",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_buckets_DeletedAt",
                table: "buckets",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_buckets_LocalDeviceId",
                table: "buckets",
                column: "LocalDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_local_devices_DeviceKey",
                table: "local_devices",
                column: "DeviceKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "local_devices");

            migrationBuilder.DropIndex(
                name: "IX_notes_DeletedAt",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_notes_LocalDeviceId",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_documents_DeletedAt",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_LocalDeviceId",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_conversations_DeletedAt",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_conversations_LocalDeviceId",
                table: "conversations");

            migrationBuilder.DropIndex(
                name: "IX_chat_messages_DeletedAt",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "IX_chat_messages_LocalDeviceId",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "IX_buckets_DeletedAt",
                table: "buckets");

            migrationBuilder.DropIndex(
                name: "IX_buckets_LocalDeviceId",
                table: "buckets");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "LocalDeviceId",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "LocalDeviceId",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "LocalDeviceId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "LocalDeviceId",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "buckets");

            migrationBuilder.DropColumn(
                name: "LocalDeviceId",
                table: "buckets");
        }
    }
}

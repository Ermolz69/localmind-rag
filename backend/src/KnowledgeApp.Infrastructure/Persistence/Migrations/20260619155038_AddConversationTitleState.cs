using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationTitleState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TitleEditedAt",
                table: "conversations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TitleGeneratedAt",
                table: "conversations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TitleEditedAt",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "TitleGeneratedAt",
                table: "conversations");
        }
    }
}

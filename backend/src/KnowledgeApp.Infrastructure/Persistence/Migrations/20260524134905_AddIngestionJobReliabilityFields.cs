using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionJobReliabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "ingestion_jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LastOperationId",
                table: "ingestion_jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_Status",
                table: "ingestion_jobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_Status",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "LastOperationId",
                table: "ingestion_jobs");
        }
    }
}

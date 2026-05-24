using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIngestionJobLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastError",
                table: "ingestion_jobs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "AttemptCount",
                table: "ingestion_jobs",
                newName: "RetryCount");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStep",
                table: "ingestion_jobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "ingestion_jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "ingestion_jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE ingestion_jobs
                SET Status = CASE Status
                    WHEN 0 THEN 0 -- Queued -> Pending
                    WHEN 1 THEN 1 -- Running -> Processing
                    WHEN 2 THEN 4 -- Completed -> Indexed
                    WHEN 3 THEN 5 -- Failed -> Failed
                    WHEN 4 THEN 6 -- Cancelled -> Cancelled
                    ELSE Status
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE ingestion_jobs
                SET ErrorCode = 'INGESTION_JOB_FAILED'
                WHERE Status = 5
                  AND ErrorMessage IS NOT NULL
                  AND TRIM(ErrorMessage) <> '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE ingestion_jobs
                SET ProgressPercent = CASE Status
                    WHEN 0 THEN 0
                    WHEN 1 THEN 10
                    WHEN 2 THEN 50
                    WHEN 3 THEN 75
                    WHEN 4 THEN 100
                    ELSE COALESCE(ProgressPercent, 0)
                END,
                CurrentStep = CASE Status
                    WHEN 0 THEN 'Pending'
                    WHEN 1 THEN 'Processing'
                    WHEN 2 THEN 'Chunking document'
                    WHEN 3 THEN 'Generating embeddings'
                    WHEN 4 THEN 'Indexed'
                    WHEN 5 THEN 'Failed'
                    WHEN 6 THEN 'Cancelled'
                    ELSE 'Pending'
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStep",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "ingestion_jobs");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "ingestion_jobs",
                newName: "AttemptCount");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "ingestion_jobs",
                newName: "LastError");

            migrationBuilder.Sql(
                """
                UPDATE ingestion_jobs
                SET Status = CASE Status
                    WHEN 0 THEN 0 -- Pending -> Queued
                    WHEN 1 THEN 1 -- Processing -> Running
                    WHEN 2 THEN 1 -- Chunking -> Running
                    WHEN 3 THEN 1 -- Embedding -> Running
                    WHEN 4 THEN 2 -- Indexed -> Completed
                    WHEN 5 THEN 3 -- Failed -> Failed
                    WHEN 6 THEN 4 -- Cancelled -> Cancelled
                    ELSE Status
                END;
                """);
        }
    }
}

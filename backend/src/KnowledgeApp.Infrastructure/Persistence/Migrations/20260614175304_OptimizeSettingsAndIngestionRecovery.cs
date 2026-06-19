using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeSettingsAndIngestionRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_Status",
                table: "ingestion_jobs");

            migrationBuilder.AddColumn<long>(
                name: "CreatedAtUnixTimeMs",
                table: "ingestion_jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("""
                UPDATE ingestion_jobs
                SET CreatedAtUnixTimeMs = CAST(((julianday(CreatedAt) - 2440587.5) * 86400000.0) AS INTEGER);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_Status_CreatedAtUnixTimeMs_Id",
                table: "ingestion_jobs",
                columns: new[] { "Status", "CreatedAtUnixTimeMs", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ingestion_jobs_Status_CreatedAtUnixTimeMs_Id",
                table: "ingestion_jobs");

            migrationBuilder.DropColumn(
                name: "CreatedAtUnixTimeMs",
                table: "ingestion_jobs");

            migrationBuilder.CreateIndex(
                name: "IX_ingestion_jobs_Status",
                table: "ingestion_jobs",
                column: "Status");
        }
    }
}

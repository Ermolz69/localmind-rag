using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDefaultBucket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO buckets (Id, Name, Description, SyncStatus, CreatedAt, LocalVersion)
                SELECT '00000000-0000-0000-0000-000000000001', 'Default', 'Default knowledge bucket', 0, datetime('now'), 1
                WHERE NOT EXISTS (SELECT 1 FROM buckets WHERE Name = 'Default' AND DeletedAt IS NULL);
            ");

            migrationBuilder.Sql(@"
                UPDATE documents SET BucketId = (SELECT Id FROM buckets WHERE Name = 'Default' AND DeletedAt IS NULL LIMIT 1)
                WHERE BucketId IS NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE notes SET BucketId = (SELECT Id FROM buckets WHERE Name = 'Default' AND DeletedAt IS NULL LIMIT 1)
                WHERE BucketId IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down is a no-op as we do not want to destroy associations
        }
    }
}

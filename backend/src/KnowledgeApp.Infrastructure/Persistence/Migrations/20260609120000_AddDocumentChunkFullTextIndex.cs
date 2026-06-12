using KnowledgeApp.Infrastructure.Services.Search;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260609120000_AddDocumentChunkFullTextIndex")]
    public partial class AddDocumentChunkFullTextIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(FullTextSearchSchema.CreateSql);
            migrationBuilder.Sql(FullTextSearchSchema.BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(FullTextSearchSchema.DropSql);
        }
    }
}

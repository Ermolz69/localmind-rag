using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace KnowledgeApp.IntegrationTests;

public sealed class PersistenceMigrationTests
{
    [Fact]
    public async Task BackfillDefaultBucket_Should_Assign_Legacy_Documents_And_Notes()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            "localmind-migrations",
            $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        try
        {
            DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using (AppDbContext setupContext = new(options))
            {
                IMigrator migrator = setupContext.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260608203353_AddChunkingProfiles");

                await setupContext.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO documents (Id, Name, Status, SyncStatus, CreatedAt, LocalVersion)
                    VALUES ({0}, 'Legacy document', 2, 0, {1}, 1);
                    """,
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);

                await setupContext.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO notes (Id, Title, Markdown, SyncStatus, CreatedAt, LocalVersion)
                    VALUES ({0}, 'Legacy note', 'Legacy note body', 0, {1}, 1);
                    """,
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow);
            }

            await using (AppDbContext migratedContext = new(options))
            {
                await migratedContext.Database.MigrateAsync();

                Guid defaultBucketId = await migratedContext.Buckets
                    .Where(bucket => bucket.Name == "Default" && bucket.DeletedAt == null)
                    .Select(bucket => bucket.Id)
                    .SingleAsync();

                Assert.All(
                    await migratedContext.Documents.ToListAsync(),
                    document => Assert.Equal(defaultBucketId, document.BucketId));
                Assert.All(
                    await migratedContext.Notes.ToListAsync(),
                    note => Assert.Equal(defaultBucketId, note.BucketId));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                try
                {
                    File.Delete(databasePath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}

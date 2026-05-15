using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests;

internal sealed class ApplicationTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    private ApplicationTestDatabase(SqliteConnection connection, AppDbContext context)
    {
        this.connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<ApplicationTestDatabase> CreateAsync()
    {
        SqliteConnection? connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        DbContextOptions<AppDbContext>? options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        AppDbContext? context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new ApplicationTestDatabase(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}

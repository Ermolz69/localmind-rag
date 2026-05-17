using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KnowledgeApp.Infrastructure.Runtime;

public sealed class LocalRuntimeInitializer(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope? scope = services.CreateScope();
        IAppPathProvider? paths = scope.ServiceProvider.GetRequiredService<IAppPathProvider>();
        Directory.CreateDirectory(paths.DataDirectory);
        Directory.CreateDirectory(paths.FilesDirectory);
        Directory.CreateDirectory(paths.IndexDirectory);
        Directory.CreateDirectory(paths.LogsDirectory);

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

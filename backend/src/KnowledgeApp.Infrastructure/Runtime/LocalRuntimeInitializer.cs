using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

        AiOptions? aiOptions = scope.ServiceProvider.GetRequiredService<IOptions<AiOptions>>().Value;
        Directory.CreateDirectory(Path.GetFullPath(aiOptions.ModelsPath, paths.AppRootDirectory));

        AppDbContext? db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        IAiRuntimeManager? aiRuntime = scope.ServiceProvider.GetRequiredService<IAiRuntimeManager>();
        await aiRuntime.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

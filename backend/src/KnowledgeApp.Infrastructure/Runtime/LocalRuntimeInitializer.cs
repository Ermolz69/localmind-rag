using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Runtime;

public sealed class LocalRuntimeInitializer(
    IServiceProvider services,
    ILogger<LocalRuntimeInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = services.CreateScope();

        IAppPathProvider paths =
            scope.ServiceProvider.GetRequiredService<IAppPathProvider>();

        Directory.CreateDirectory(paths.DataDirectory);
        Directory.CreateDirectory(paths.FilesDirectory);
        Directory.CreateDirectory(paths.PreviewDirectory);
        Directory.CreateDirectory(paths.IndexDirectory);
        Directory.CreateDirectory(paths.LogsDirectory);

        EmbeddingOptions embeddingOptions =
            scope.ServiceProvider.GetRequiredService<IOptions<EmbeddingOptions>>().Value;

        Directory.CreateDirectory(
            Path.GetFullPath(
                embeddingOptions.ModelsPath,
                paths.AppRootDirectory));

        AppDbContext db =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        IAiRuntimeProviderRegistry aiRuntimeProviders =
            scope.ServiceProvider.GetRequiredService<IAiRuntimeProviderRegistry>();

        IAiRuntimeProvider selectedProvider = aiRuntimeProviders.GetSelectedProvider();

        _ = Task.Run(
            async () => await StartAiRuntimeAsync(selectedProvider, CancellationToken.None),
            CancellationToken.None);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task StartAiRuntimeAsync(
        IAiRuntimeProvider selectedProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            await selectedProvider.StartAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // LocalApi must still boot so runtime endpoints can report provider errors as envelopes.
            logger.LogWarning(exception, "AI runtime startup failed during LocalApi initialization.");
        }
    }
}

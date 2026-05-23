using System.IO.Compression;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class LlamaCppRuntimeSetupService(
    IAppPathProvider paths,
    IOptions<AiOptions> options,
    EmbeddingModelStore embeddingModelStore,
    HttpClient httpClient,
    ILogger<LlamaCppRuntimeSetupService> logger) : IAiRuntimeSetupService
{
    private readonly AiOptions options = options.Value;

    public async Task SetupAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRuntimeAsync(cancellationToken);
        await embeddingModelStore.EnsureDownloadedAsync(cancellationToken: cancellationToken);
    }

    private async Task EnsureRuntimeAsync(CancellationToken cancellationToken)
    {
        string runtimePath = ResolvePath(options.RuntimePath);
        if (File.Exists(runtimePath))
        {
            logger.LogInformation("AI runtime executable is already installed at {RuntimePath}.", runtimePath);
            return;
        }

        string runtimeDirectory = Path.GetDirectoryName(runtimePath)
            ?? throw new InvalidOperationException("AI runtime path does not contain a directory.");
        Directory.CreateDirectory(runtimeDirectory);

        string tempDirectory = Path.Combine(runtimeDirectory, ".setup", Guid.NewGuid().ToString("N"));
        string archivePath = Path.Combine(tempDirectory, "llama.cpp.zip");
        string extractDirectory = Path.Combine(tempDirectory, "extract");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            logger.LogInformation("Downloading llama.cpp runtime from {RuntimeDownloadUrl}.", options.RuntimeDownloadUrl);
            using HttpResponseMessage response = await httpClient.GetAsync(
                options.RuntimeDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (Stream remote = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (FileStream local = new(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await remote.CopyToAsync(local, cancellationToken);
            }

            ZipFile.ExtractToDirectory(archivePath, extractDirectory, overwriteFiles: true);

            FileInfo? extractedServer = new DirectoryInfo(extractDirectory)
                .EnumerateFiles("llama-server.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (extractedServer is null)
            {
                throw new InvalidOperationException("Downloaded llama.cpp archive does not contain llama-server.exe.");
            }

            DirectoryInfo sourceDirectory = extractedServer.Directory
                ?? throw new InvalidOperationException("Downloaded llama.cpp runtime directory was not found.");

            foreach (FileInfo file in sourceDirectory.EnumerateFiles())
            {
                string destination = Path.Combine(runtimeDirectory, file.Name);
                file.CopyTo(destination, overwrite: true);
            }

            if (!File.Exists(runtimePath))
            {
                throw new InvalidOperationException($"Installed llama.cpp runtime was not found at {runtimePath}.");
            }

            logger.LogInformation("AI runtime executable installed at {RuntimePath}.", runtimePath);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private string ResolvePath(string path) => Path.GetFullPath(path, paths.AppRootDirectory);
}

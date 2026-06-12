using System.IO.Compression;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Extensions;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class LlamaCppRuntimeSetupService(
    IAppPathProvider paths,
    IOptions<RuntimeOptions> options,
    EmbeddingModelStore embeddingModelStore,
    ChatModelStore chatModelStore,
    HttpClient httpClient,
    ILogger<LlamaCppRuntimeSetupService> logger,
    IAppDiagnosticLogger? diagnostics = null) : IAiRuntimeSetupService
{
    private readonly RuntimeOptions options = options.Value;

    public async Task SetupAsync(IProgress<RuntimeSetupProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Runtime,
            DiagnosticNames.Operations.AiRuntimeSetup) ?? Guid.Empty;

        try
        {
            progress?.Report(new RuntimeSetupProgress("checking", "Ensuring runtime is installed..."));
            await EnsureRuntimeAsync(progress, cancellationToken);

            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.RuntimeReady);

            progress?.Report(new RuntimeSetupProgress("checking", "Ensuring embedding model is installed..."));
            await embeddingModelStore.EnsureDownloadedAsync(progress: progress, cancellationToken: cancellationToken);

            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.ModelReady);

            progress?.Report(new RuntimeSetupProgress("checking", "Ensuring chat model is installed..."));
            await chatModelStore.EnsureDownloadedAsync(progress: progress, cancellationToken: cancellationToken);

            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.ModelReady);
            progress?.Report(new RuntimeSetupProgress("verifying", "Verifying installation..."));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            diagnostics?.LogFailure(operationId, exception);

            throw new ExternalDependencyAppException(
                ErrorCodes.Runtime.ExternalDependencyUnavailable,
                ErrorMessages.Runtime.ExternalDependencyUnavailable,
                exception);
        }
    }

    private async Task EnsureRuntimeAsync(IProgress<RuntimeSetupProgress>? progress, CancellationToken cancellationToken)
    {
        string runtimePath = ResolvePath(options.RuntimePath);

        if (File.Exists(runtimePath))
        {
            logger.LogInformation(
                "AI runtime executable is already installed at {RuntimePath}.",
                runtimePath);

            return;
        }

        string runtimeDirectory = Path.GetDirectoryName(runtimePath)
            ?? throw new InvalidOperationException(
                "AI runtime path does not contain a directory.");

        Directory.CreateDirectory(runtimeDirectory);

        string tempDirectory = Path.Combine(
            runtimeDirectory,
            ".setup",
            Guid.NewGuid().ToString("N"));

        string archivePath = Path.Combine(tempDirectory, "llama.cpp.zip");
        string extractDirectory = Path.Combine(tempDirectory, "extract");

        Directory.CreateDirectory(tempDirectory);

        try
        {
            logger.LogInformation(
                "Downloading llama.cpp runtime from {RuntimeDownloadUrl}.",
                options.RuntimeDownloadUrl);

            using HttpResponseMessage response = await httpClient.GetAsync(
                options.RuntimeDownloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            await using (Stream remote =
                await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (FileStream local = new(
                archivePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await remote.CopyToWithProgressAsync(
                    local,
                    totalBytes,
                    onProgress: (downloaded, total, speed) =>
                    {
                        progress?.Report(new RuntimeSetupProgress(
                            Stage: "downloading-runtime",
                            Message: "Downloading llama.cpp runtime...",
                            DownloadedBytes: downloaded,
                            TotalBytes: total,
                            SpeedBytesPerSecond: speed));
                    },
                    cancellationToken);
            }

            progress?.Report(new RuntimeSetupProgress("extracting-runtime", "Extracting llama.cpp archive..."));

            ZipFile.ExtractToDirectory(
                archivePath,
                extractDirectory,
                overwriteFiles: true);

            FileInfo? extractedServer = new DirectoryInfo(extractDirectory)
                .EnumerateFiles("llama-server.exe", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (extractedServer is null)
            {
                throw new InvalidOperationException(
                    "Downloaded llama.cpp archive does not contain llama-server.exe.");
            }

            DirectoryInfo sourceDirectory = extractedServer.Directory
                ?? throw new InvalidOperationException(
                    "Downloaded llama.cpp runtime directory was not found.");

            foreach (FileInfo file in sourceDirectory.EnumerateFiles())
            {
                string destination = Path.Combine(runtimeDirectory, file.Name);

                file.CopyTo(destination, overwrite: true);
            }

            if (!File.Exists(runtimePath))
            {
                throw new InvalidOperationException(
                    $"Installed llama.cpp runtime was not found at {runtimePath}.");
            }

            logger.LogInformation(
                "AI runtime executable installed at {RuntimePath}.",
                runtimePath);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private string ResolvePath(string path)
    {
        return Path.GetFullPath(path, paths.AppRootDirectory);
    }
}

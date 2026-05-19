using System.Diagnostics;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class AiRuntimeManager(
    IAppPathProvider paths,
    IOptions<AiOptions> options,
    EmbeddingModelCatalog embeddingModelCatalog,
    EmbeddingModelStore embeddingModelStore,
    ILogger<AiRuntimeManager> logger) : IAiRuntimeManager, IAiModelRegistry, IDisposable
{
    private readonly AiOptions options = options.Value;
    private readonly object syncRoot = new();
    private Process? runtimeProcess;

    public async Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        bool runtimeAvailable = File.Exists(ResolvePath(options.RuntimePath));
        bool modelAvailable = await embeddingModelStore.IsValidAsync(cancellationToken: cancellationToken);
        bool runtimeRunning = await IsRuntimeHealthyAsync(cancellationToken);

        string runtimeStatus = (runtimeAvailable, modelAvailable, runtimeRunning) switch
        {
            (false, _, _) => "RuntimeMissing",
            (_, false, _) => "ModelMissing",
            (_, _, true) => "Running",
            _ => "Stopped",
        };

        return new RuntimeStatusDto(
            LocalApiReady: true,
            AiRuntimeStatus: runtimeStatus,
            ModelsAvailable: modelAvailable,
            OfflineMode: true);
    }

    public Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> models = embeddingModelCatalog.List()
            .Select(model => model.ModelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(models);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!options.AutoStartRuntime)
        {
            logger.LogInformation("AI runtime autostart is disabled.");
            return;
        }

        if (await IsRuntimeHealthyAsync(cancellationToken))
        {
            logger.LogInformation("AI runtime is already available at {BaseUrl}.", options.BaseUrl);
            return;
        }

        string runtimePath = ResolvePath(options.RuntimePath);
        if (!File.Exists(runtimePath))
        {
            logger.LogWarning("AI runtime executable was not found at {RuntimePath}. Run scripts/setup-ai.ps1.", runtimePath);
            return;
        }

        if (!await embeddingModelStore.IsValidAsync(cancellationToken: cancellationToken))
        {
            logger.LogWarning("Embedding model is missing or invalid. Run scripts/setup-ai.ps1.");
            return;
        }

        EmbeddingModelManifest manifest = embeddingModelCatalog.GetDefault();
        string modelPath = embeddingModelStore.GetModelPath(manifest);
        Uri baseUri = new(options.BaseUrl);
        string host = string.IsNullOrWhiteSpace(baseUri.Host) ? "127.0.0.1" : baseUri.Host;
        string port = baseUri.Port > 0 ? baseUri.Port.ToString() : "11435";

        ProcessStartInfo startInfo = new()
        {
            FileName = runtimePath,
            WorkingDirectory = paths.AppRootDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        startInfo.ArgumentList.Add("-m");
        startInfo.ArgumentList.Add(modelPath);
        startInfo.ArgumentList.Add("--embedding");
        startInfo.ArgumentList.Add("--host");
        startInfo.ArgumentList.Add(host);
        startInfo.ArgumentList.Add("--port");
        startInfo.ArgumentList.Add(port);

        Process process = new() { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, args) => LogRuntimeOutput(args.Data, isError: false);
        process.ErrorDataReceived += (_, args) => LogRuntimeOutput(args.Data, isError: true);
        process.Exited += (_, _) => logger.LogInformation("AI runtime process exited with code {ExitCode}.", process.ExitCode);

        lock (syncRoot)
        {
            if (runtimeProcess is { HasExited: false })
            {
                return;
            }

            runtimeProcess = process;
        }

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        logger.LogInformation("Started AI runtime process {ProcessId} at {BaseUrl}.", process.Id, options.BaseUrl);
        await WaitForRuntimeAsync(cancellationToken);
    }

    public void Dispose()
    {
        lock (syncRoot)
        {
            if (runtimeProcess is null)
            {
                return;
            }

            try
            {
                if (!runtimeProcess.HasExited)
                {
                    runtimeProcess.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                runtimeProcess.Dispose();
                runtimeProcess = null;
            }
        }
    }

    private async Task<bool> IsRuntimeHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = new() { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromSeconds(2) };
            using HttpResponseMessage response = await client.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return false;
        }
    }

    private async Task WaitForRuntimeAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));

        try
        {
            while (!timeout.IsCancellationRequested)
            {
                if (await IsRuntimeHealthyAsync(timeout.Token))
                {
                    logger.LogInformation("AI runtime is ready at {BaseUrl}.", options.BaseUrl);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        logger.LogWarning("AI runtime did not become ready within the startup timeout.");
    }

    private string ResolvePath(string path) => Path.GetFullPath(path, paths.AppRootDirectory);

    private void LogRuntimeOutput(string? line, bool isError)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (isError)
        {
            logger.LogDebug("llama-server: {Line}", line);
        }
        else
        {
            logger.LogTrace("llama-server: {Line}", line);
        }
    }
}

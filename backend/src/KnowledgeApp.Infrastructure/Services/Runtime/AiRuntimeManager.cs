using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class AiRuntimeManager(
    IAppPathProvider paths,
    IOptions<RuntimeOptions> runtimeOptions,
    IOptions<EmbeddingOptions> embeddingOptions,
    EmbeddingModelCatalog embeddingModelCatalog,
    EmbeddingModelStore embeddingModelStore,
    ILogger<AiRuntimeManager> logger,
    IAppDiagnosticLogger? diagnostics = null) : IAiRuntimeManager, IAiModelRegistry, IAiRuntimeProvider, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RuntimeOptions runtime = runtimeOptions.Value;
    private readonly EmbeddingOptions embedding = embeddingOptions.Value;
    private readonly object syncRoot = new();

    private Process? runtimeProcess;

    public string ProviderId => "llama-cpp";

    public string ProviderName => "llama.cpp";

    public string EmbeddingModelName =>
        string.IsNullOrWhiteSpace(embedding.EmbeddingModel)
            ? embeddingModelCatalog.GetDefault().ModelName
            : embedding.EmbeddingModel;

    public AiRuntimeProviderCapabilities Capabilities { get; } = new(
        SupportsEmbeddings: true,
        SupportsChat: true,
        SupportsModelListing: true,
        SupportsSetup: true,
        SupportsStart: true,
        SupportsStop: false);

    public async Task<RuntimeStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        string runtimePath = ResolvePath(runtime.RuntimePath);

        EmbeddingModelManifest manifest = embeddingModelCatalog.GetDefault();

        string modelPath = embeddingModelStore.GetModelPath(manifest);

        bool runtimeAvailable = File.Exists(runtimePath);

        bool modelAvailable =
            await embeddingModelStore.IsValidAsync(cancellationToken: cancellationToken);

        bool runtimeRunning = await IsRuntimeHealthyAsync(cancellationToken);

        string runtimeStatus = (runtimeAvailable, modelAvailable, runtimeRunning) switch
        {
            (false, _, _) => "RuntimeMissing",
            (_, false, _) => "ModelMissing",
            (_, _, true) => "Running",
            _ => "Stopped",
        };

        string? setupReason = GetSetupReason(runtimeAvailable, modelAvailable);

        string providerStatus = runtimeStatus switch
        {
            "RuntimeMissing" or "ModelMissing" => AiRuntimeProviderStatus.Missing,
            "Running" => AiRuntimeProviderStatus.Running,
            _ => AiRuntimeProviderStatus.Stopped,
        };

        return new RuntimeStatusDto(
            LocalApiReady: true,
            AiRuntimeStatus: runtimeStatus,
            ModelsAvailable: modelAvailable,
            OfflineMode: true,
            RuntimePath: runtimePath,
            ModelPath: modelPath,
            SetupRequired: setupReason is not null,
            SetupReason: setupReason,
            ProviderId: ProviderId,
            ProviderName: ProviderName,
            ProviderStatus: providerStatus,
            Capabilities: Capabilities,
            BaseUrl: runtime.BaseUrl,
            FailureReason: setupReason);
    }

    public Task<IReadOnlyCollection<string>> ListModelsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> models = embeddingModelCatalog.List()
            .Select(model => model.ModelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(models);
    }

    public async Task<string> GenerateChatCompletionAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken = default)
    {
        ChatCompletionRequest payload = new(
            Model: runtime.ChatModel,
            Messages:
            [
                new ChatCompletionMessage(
                    "system",
                    "Answer using only the provided local context. If no relevant local context is available, say that no relevant local sources were found."),
                new ChatCompletionMessage(
                    "user",
                    $"Context:\n{request.ContextText}\n\nQuestion:\n{request.Question}"),
            ],
            Temperature: runtime.Temperature);

        using HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            BuildUri("/v1/chat/completions"),
            payload,
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRuntimeUnavailableExceptionAsync(
                "chat completion",
                response,
                cancellationToken);
        }

        ChatCompletionResponse? body =
            await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                SerializerOptions,
                cancellationToken);

        string? content = body?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ExternalDependencyAppException(
                ErrorCodes.Runtime.AiRuntimeUnavailable,
                ErrorMessages.Runtime.AiRuntimeUnavailable);
        }

        return content;
    }

    public IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(
        ChatModelRequest request,
        CancellationToken cancellationToken = default)
    {
        return GenerateChatCompletionStreamInternalAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<string> GenerateChatCompletionStreamInternalAsync(
        ChatModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatCompletionRequest payload = new(
            Model: runtime.ChatModel,
            Messages:
            [
                new ChatCompletionMessage(
                    "system",
                    "Answer using only the provided local context. If no relevant local context is available, say that no relevant local sources were found."),
                new ChatCompletionMessage(
                    "user",
                    $"Context:\n{request.ContextText}\n\nQuestion:\n{request.Question}"),
            ],
            Temperature: runtime.Temperature,
            Stream: true);

        using HttpClient client = new()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            BuildUri("/v1/chat/completions"),
            payload,
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRuntimeUnavailableExceptionAsync(
                "chat completion stream",
                response,
                cancellationToken);
        }

        using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                string data = line["data: ".Length..].Trim();
                if (data == "[DONE]")
                {
                    break;
                }

                ChatCompletionStreamResponse? chunk = JsonSerializer.Deserialize<ChatCompletionStreamResponse>(data, SerializerOptions);
                string? content = chunk?.Choices.FirstOrDefault()?.Delta?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        EmbeddingRequest payload = new(EmbeddingModelName, text);

        using HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            BuildUri("/v1/embeddings"),
            payload,
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateRuntimeUnavailableExceptionAsync(
                "embedding generation",
                response,
                cancellationToken);
        }

        EmbeddingResponse? body =
            await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
                SerializerOptions,
                cancellationToken);

        float[]? result = body?.Data.FirstOrDefault()?.Embedding;

        if (result is null || result.Length == 0)
        {
            throw new ExternalDependencyAppException(
                ErrorCodes.Runtime.AiRuntimeUnavailable,
                ErrorMessages.Runtime.AiRuntimeUnavailable);
        }

        return result;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Runtime,
            DiagnosticNames.Operations.AiRuntimeStart,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.BaseUrl] = runtime.BaseUrl,
                [DiagnosticNames.Properties.RuntimePath] = runtime.RuntimePath,
            }) ?? Guid.Empty;

        if (!runtime.AutoStartRuntime)
        {
            logger.LogInformation("AI runtime autostart is disabled.");

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.AutostartDisabled);

            return;
        }

        if (await IsRuntimeHealthyAsync(cancellationToken))
        {
            logger.LogInformation(
                "AI runtime is already available at {BaseUrl}.",
                runtime.BaseUrl);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.RuntimeAlreadyRunning);

            return;
        }

        string runtimePath = ResolvePath(runtime.RuntimePath);

        if (!File.Exists(runtimePath))
        {
            logger.LogWarning(
                "AI runtime executable was not found at {RuntimePath}. Use the first-run AI setup action to install llama.cpp.",
                runtimePath);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.RuntimeMissing,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.RuntimePath] = runtimePath,
                });

            return;
        }

        if (!await embeddingModelStore.IsValidAsync(cancellationToken: cancellationToken))
        {
            EmbeddingModelManifest missingManifest =
                embeddingModelCatalog.GetDefault();

            string missingModelPath =
                embeddingModelStore.GetModelPath(missingManifest);

            logger.LogWarning(
                "Embedding model is missing or invalid at {ModelPath}. Use the first-run AI setup action to download {ModelName}.",
                missingModelPath,
                missingManifest.DisplayName);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.ModelMissing,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.ModelPath] = missingModelPath,
                });

            return;
        }

        EmbeddingModelManifest manifest = embeddingModelCatalog.GetDefault();

        string modelPath = embeddingModelStore.GetModelPath(manifest);

        Uri baseUri = new(runtime.BaseUrl);

        string host = string.IsNullOrWhiteSpace(baseUri.Host)
            ? "127.0.0.1"
            : baseUri.Host;

        string port = baseUri.Port > 0
            ? baseUri.Port.ToString()
            : "11435";

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

        Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, args) =>
            LogRuntimeOutput(args.Data, isError: false);

        process.ErrorDataReceived += (_, args) =>
            LogRuntimeOutput(args.Data, isError: true);

        process.Exited += (_, _) =>
            logger.LogInformation(
                "AI runtime process exited with code {ExitCode}.",
                process.ExitCode);

        lock (syncRoot)
        {
            if (runtimeProcess is { HasExited: false })
            {
                return;
            }

            runtimeProcess = process;
        }

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            logger.LogInformation(
                "Started AI runtime process {ProcessId} at {BaseUrl}.",
                process.Id,
                runtime.BaseUrl);

            await WaitForRuntimeAsync(cancellationToken);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.RuntimeStartFinished);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(runtimeProcess, process))
                {
                    runtimeProcess = null;
                }
            }

            diagnostics?.LogFailure(operationId, exception);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.RuntimeStartFailed);

            throw new ExternalDependencyAppException(
                ErrorCodes.Runtime.ExternalDependencyUnavailable,
                ErrorMessages.Runtime.ExternalDependencyUnavailable,
                exception);
        }
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

    private async Task<bool> IsRuntimeHealthyAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = new()
            {
                BaseAddress = new Uri(runtime.BaseUrl),
                Timeout = TimeSpan.FromSeconds(2),
            };

            using HttpResponseMessage response =
                await client.GetAsync("/health", cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or TaskCanceledException
            or InvalidOperationException)
        {
            return false;
        }
    }

    private async Task WaitForRuntimeAsync(CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(60));

        try
        {
            while (!timeout.IsCancellationRequested)
            {
                if (await IsRuntimeHealthyAsync(timeout.Token))
                {
                    logger.LogInformation(
                        "AI runtime is ready at {BaseUrl}.",
                        runtime.BaseUrl);

                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(1), timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        logger.LogWarning(
            "AI runtime did not become ready within the startup timeout.");
    }

    private static string? GetSetupReason(
        bool runtimeAvailable,
        bool modelAvailable)
    {
        if (!runtimeAvailable)
        {
            return "AI runtime executable is missing. Install the local AI runtime first.";
        }

        if (!modelAvailable)
        {
            return "Embedding model is missing or failed checksum validation. Download the local embedding model first.";
        }

        return null;
    }

    private string ResolvePath(string path)
    {
        return Path.GetFullPath(path, paths.AppRootDirectory);
    }

    private Uri BuildUri(string path)
    {
        Uri baseUri = new(
            runtime.BaseUrl.TrimEnd('/') + "/",
            UriKind.Absolute);

        return new Uri(baseUri, path.TrimStart('/'));
    }

    private static async Task<ExternalDependencyAppException>
        CreateRuntimeUnavailableExceptionAsync(
            string operation,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
    {
        string body =
            await response.Content.ReadAsStringAsync(cancellationToken);

        string detail = string.IsNullOrWhiteSpace(body)
            ? $"Provider returned HTTP {(int)response.StatusCode} during {operation}."
            : $"Provider returned HTTP {(int)response.StatusCode} during {operation}.";

        return new ExternalDependencyAppException(
            ErrorCodes.Runtime.AiRuntimeUnavailable,
            $"{ErrorMessages.Runtime.AiRuntimeUnavailable} {detail}");
    }

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

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingResponseData> Data);

    private sealed record EmbeddingResponseData(
        [property: JsonPropertyName("embedding")] float[] Embedding);

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatCompletionMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("stream")] bool Stream = false);

    private sealed record ChatCompletionMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice> Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatCompletionMessage Message);

    private sealed record ChatCompletionStreamResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionStreamChoice> Choices);

    private sealed record ChatCompletionStreamChoice(
        [property: JsonPropertyName("delta")] ChatCompletionStreamDelta Delta);

    private sealed record ChatCompletionStreamDelta(
        [property: JsonPropertyName("content")] string? Content);
}

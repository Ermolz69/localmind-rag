using System.Security.Cryptography;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class EmbeddingModelStore(
    IAppPathProvider paths,
    IOptions<EmbeddingOptions> options,
    EmbeddingModelCatalog catalog,
    HttpClient httpClient)
{
    private readonly EmbeddingOptions options = options.Value;
    private readonly SemaphoreSlim validationLock = new(1, 1);
    private readonly object cacheLock = new();
    private ModelValidationCache? validationCache;

    public string ModelsDirectory =>
        Path.GetFullPath(options.ModelsPath, paths.AppRootDirectory);

    public string GetModelPath(EmbeddingModelManifest? manifest = null)
    {
        EmbeddingModelManifest selected = manifest ?? catalog.GetDefault();

        return Path.Combine(ModelsDirectory, selected.FileName);
    }

    public bool Exists(EmbeddingModelManifest? manifest = null)
    {
        return File.Exists(GetModelPath(manifest));
    }

    public async Task<bool> IsValidAsync(
        EmbeddingModelManifest? manifest = null,
        CancellationToken cancellationToken = default)
    {
        EmbeddingModelManifest selected = manifest ?? catalog.GetDefault();

        string modelPath = GetModelPath(selected);

        FileInfo modelFile = new(modelPath);

        if (!modelFile.Exists)
        {
            return false;
        }

        if (TryGetCachedValidation(selected, modelFile, out bool cachedResult))
        {
            return cachedResult;
        }

        await validationLock.WaitAsync(cancellationToken);

        try
        {
            modelFile.Refresh();

            if (!modelFile.Exists)
            {
                return false;
            }

            if (TryGetCachedValidation(selected, modelFile, out cachedResult))
            {
                return cachedResult;
            }

            string actual = await ComputeSha256Async(modelPath, cancellationToken);

            bool isValid = string.Equals(
                actual,
                selected.Sha256,
                StringComparison.OrdinalIgnoreCase);

            SetCachedValidation(selected, modelFile, actual, isValid);

            return isValid;
        }
        finally
        {
            validationLock.Release();
        }
    }

    public async Task<string> EnsureDownloadedAsync(
        EmbeddingModelManifest? manifest = null,
        CancellationToken cancellationToken = default)
    {
        EmbeddingModelManifest selected = manifest ?? catalog.GetDefault();

        string modelPath = GetModelPath(selected);

        if (await IsValidAsync(selected, cancellationToken))
        {
            return modelPath;
        }

        Directory.CreateDirectory(ModelsDirectory);

        string tempPath = $"{modelPath}.download";

        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using HttpResponseMessage response = await httpClient.GetAsync(
            selected.SourceUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using (Stream remote =
            await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (FileStream local = new(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            await remote.CopyToAsync(local, cancellationToken);
        }

        string actualSha256 =
            await ComputeSha256Async(tempPath, cancellationToken);

        if (!string.Equals(
            actualSha256,
            selected.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempPath);

            throw new InvalidOperationException(
                $"Downloaded embedding model checksum mismatch. Expected {selected.Sha256}, got {actualSha256}.");
        }

        if (File.Exists(modelPath))
        {
            File.Delete(modelPath);
        }

        File.Move(tempPath, modelPath);

        SetCachedValidation(
            selected,
            new FileInfo(modelPath),
            actualSha256,
            isValid: true);

        return modelPath;
    }

    private bool TryGetCachedValidation(
        EmbeddingModelManifest manifest,
        FileInfo modelFile,
        out bool isValid)
    {
        lock (cacheLock)
        {
            if (validationCache is not null
                && string.Equals(validationCache.ModelId, manifest.Id, StringComparison.Ordinal)
                && string.Equals(validationCache.Path, modelFile.FullName, StringComparison.OrdinalIgnoreCase)
                && validationCache.Length == modelFile.Length
                && validationCache.LastWriteTimeUtc == modelFile.LastWriteTimeUtc
                && string.Equals(validationCache.ExpectedSha256, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                isValid = validationCache.IsValid;

                return true;
            }
        }

        isValid = false;

        return false;
    }

    private void SetCachedValidation(
        EmbeddingModelManifest manifest,
        FileInfo modelFile,
        string actualSha256,
        bool isValid)
    {
        lock (cacheLock)
        {
            validationCache = new ModelValidationCache(
                manifest.Id,
                modelFile.FullName,
                modelFile.Length,
                modelFile.LastWriteTimeUtc,
                manifest.Sha256,
                actualSha256,
                isValid);
        }
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);

        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record ModelValidationCache(
        string ModelId,
        string Path,
        long Length,
        DateTime LastWriteTimeUtc,
        string ExpectedSha256,
        string ActualSha256,
        bool IsValid);
}

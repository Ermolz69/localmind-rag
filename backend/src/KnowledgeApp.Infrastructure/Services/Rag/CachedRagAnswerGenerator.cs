using System.Text.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Rag;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class CachedRagAnswerGenerator(
    IRagAnswerGenerator inner,
    ISemanticCacheRepository cache,
    IEmbeddingGenerator embeddingGenerator,
    IOptions<RagOptions> options,
    IAppDiagnosticLogger? diagnostics = null) : IRagAnswerGenerator
{
    private const double CacheThreshold = 0.95;

    public async Task<RagAnswerDto> AnswerAsync(
        Guid conversationId,
        string question,
        RetrievalFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        bool hasFilters = filters is { BucketId: not null } or { DateFrom: not null } or { DateTo: not null } or { FileType: not null } or { Tags.Count: > 0 };
        if (!options.Value.EnableSemanticCache || hasFilters)
        {
            return await inner.AnswerAsync(conversationId, question, filters, cancellationToken);
        }

        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.RagAnswer,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.ConversationId] = conversationId }) ?? Guid.Empty;

        float[] queryEmbedding = await embeddingGenerator.GenerateAsync(question, cancellationToken);

        SemanticCacheEntry? cachedEntry = await cache.FindBestMatchAsync(queryEmbedding, CacheThreshold, cancellationToken);

        if (cachedEntry != null)
        {
            var sources = JsonSerializer.Deserialize<IReadOnlyList<RagSourceDto>>(cachedEntry.SourcesJson) ?? [];

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.CacheHit,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.SourcesCount] = sources.Count,
                    [DiagnosticNames.Properties.AnswerLength] = cachedEntry.Answer.Length,
                });

            return new RagAnswerDto(cachedEntry.Answer, sources);
        }

        diagnostics?.LogStep(operationId, DiagnosticNames.Steps.CacheMiss);

        var result = await inner.AnswerAsync(conversationId, question, filters, cancellationToken);

        if (result.Sources.Count == 0)
        {
            return result;
        }

        var entry = new SemanticCacheEntry
        {
            Question = question,
            QuestionEmbedding = EmbeddingVectorSerializer.ToBytes(queryEmbedding),
            EmbeddingDimension = queryEmbedding.Length,
            Answer = result.Answer,
            SourcesJson = JsonSerializer.Serialize(result.Sources)
        };

        await cache.AddAsync(entry, cancellationToken);

        return result;
    }

    public async IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(
        Guid conversationId,
        string question,
        RetrievalFilters? filters = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        bool hasFilters = filters is { BucketId: not null } or { DateFrom: not null } or { DateTo: not null } or { FileType: not null } or { Tags.Count: > 0 };
        if (!options.Value.EnableSemanticCache || hasFilters)
        {
            await foreach (var chunk in inner.AnswerStreamAsync(conversationId, question, filters, cancellationToken))
            {
                yield return chunk;
            }

            yield break;
        }

        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.RagAnswer,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.ConversationId] = conversationId }) ?? Guid.Empty;

        float[] queryEmbedding = await embeddingGenerator.GenerateAsync(question, cancellationToken);

        SemanticCacheEntry? cachedEntry = await cache.FindBestMatchAsync(queryEmbedding, CacheThreshold, cancellationToken);

        if (cachedEntry != null)
        {
            var sources = JsonSerializer.Deserialize<IReadOnlyList<RagSourceDto>>(cachedEntry.SourcesJson) ?? [];

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.CacheHit,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.SourcesCount] = sources.Count,
                    [DiagnosticNames.Properties.AnswerLength] = cachedEntry.Answer.Length,
                });

            yield return new RagAnswerChunkDto(cachedEntry.Answer, sources);
            yield break;
        }

        diagnostics?.LogStep(operationId, DiagnosticNames.Steps.CacheMiss);

        string accumulatedAnswer = string.Empty;
        IReadOnlyList<RagSourceDto>? finalSources = null;

        await foreach (var chunk in inner.AnswerStreamAsync(conversationId, question, filters, cancellationToken))
        {
            accumulatedAnswer += chunk.Text;
            if (chunk.Sources != null)
            {
                finalSources = chunk.Sources;
            }
            yield return chunk;
        }

        if (finalSources is not { Count: > 0 })
        {
            yield break;
        }

        var entry = new SemanticCacheEntry
        {
            Question = question,
            QuestionEmbedding = EmbeddingVectorSerializer.ToBytes(queryEmbedding),
            EmbeddingDimension = queryEmbedding.Length,
            Answer = accumulatedAnswer,
            SourcesJson = JsonSerializer.Serialize(finalSources ?? [])
        };

        await cache.AddAsync(entry, cancellationToken);
    }
}

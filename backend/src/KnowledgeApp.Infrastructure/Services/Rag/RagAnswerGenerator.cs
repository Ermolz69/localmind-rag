using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class RagAnswerGenerator(
    IRagContextBuilder contextBuilder,
    IChatModelClient chatClient,
    IAppDiagnosticLogger? diagnostics = null) : IRagAnswerGenerator
{
    public async Task<RagAnswerDto> AnswerAsync(
        Guid conversationId,
        string question,
        RetrievalFilters? filters = null,
        CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.RagAnswer,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.ConversationId] = conversationId }) ?? Guid.Empty;

        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(
                conversationId,
                question,
                Limit: 12,
                BucketId: filters?.BucketId,
                DateFrom: filters?.DateFrom,
                DateTo: filters?.DateTo,
                FileType: filters?.FileType,
                Tags: filters?.Tags),
            cancellationToken);

        string answer = await chatClient.GenerateAsync(
            new ChatModelRequest(question, context.ContextText, context.Sources),
            cancellationToken);

        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.AnswerGenerated,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.SourcesCount] = context.Sources.Count,
                [DiagnosticNames.Properties.AnswerLength] = answer.Length,
            });

        return new RagAnswerDto(answer, context.Sources);
    }

    public async IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(
        Guid conversationId,
        string question,
        RetrievalFilters? filters = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.RagAnswer,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.ConversationId] = conversationId }) ?? Guid.Empty;

        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(
                conversationId,
                question,
                Limit: 12,
                BucketId: filters?.BucketId,
                DateFrom: filters?.DateFrom,
                DateTo: filters?.DateTo,
                FileType: filters?.FileType,
                Tags: filters?.Tags),
            cancellationToken);

        bool isFirstChunk = true;
        int answerLength = 0;

        await foreach (string chunk in chatClient.GenerateStreamAsync(
            new ChatModelRequest(question, context.ContextText, context.Sources),
            cancellationToken))
        {
            answerLength += chunk.Length;
            if (isFirstChunk)
            {
                yield return new RagAnswerChunkDto(chunk, context.Sources);
                isFirstChunk = false;
            }
            else
            {
                yield return new RagAnswerChunkDto(chunk);
            }
        }

        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.AnswerGenerated,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.SourcesCount] = context.Sources.Count,
                [DiagnosticNames.Properties.AnswerLength] = answerLength,
            });
    }
}

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class RagAnswerGenerator(
    IRagContextBuilder contextBuilder,
    IChatModelClient chatClient,
    IAppDiagnosticLogger? diagnostics = null) : IRagAnswerGenerator
{
    public async Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            "rag",
            "answer",
            new Dictionary<string, object?> { ["ConversationId"] = conversationId }) ?? Guid.Empty;

        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(conversationId, question),
            cancellationToken);

        string answer = await chatClient.GenerateAsync(
            new ChatModelRequest(question, context.ContextText, context.Sources),
            cancellationToken);

        diagnostics?.LogStep(
            operationId,
            "answer-generated",
            new Dictionary<string, object?>
            {
                ["SourcesCount"] = context.Sources.Count,
                ["AnswerLength"] = answer.Length,
            });

        return new RagAnswerDto(answer, context.Sources);
    }
}

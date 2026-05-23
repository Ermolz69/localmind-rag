using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
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
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.RagAnswer,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.ConversationId] = conversationId }) ?? Guid.Empty;

        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(conversationId, question),
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
}

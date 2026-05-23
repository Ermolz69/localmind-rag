using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class RagAnswerGenerator(IRagContextBuilder contextBuilder, IChatModelClient chatClient) : IRagAnswerGenerator
{
    public async Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
    {
        RagContext context = await contextBuilder.BuildAsync(
            new RagContextRequest(conversationId, question),
            cancellationToken);

        string answer = await chatClient.GenerateAsync(
            new ChatModelRequest(question, context.ContextText, context.Sources),
            cancellationToken);

        return new RagAnswerDto(answer, context.Sources);
    }
}

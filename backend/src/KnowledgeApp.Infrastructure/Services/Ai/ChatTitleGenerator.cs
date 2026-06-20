using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ChatTitleGenerator(IChatModelClient chatClient) : IChatTitleGenerator
{
    private const string SystemPrompt = "You generate concise chat titles.";

    public Task<string> GenerateAsync(string firstUserMessage, CancellationToken cancellationToken = default)
    {
        string prompt = $"""
            Generate a short chat title from the user's first message.

            Rules:
            - 2-6 words
            - same language as the user message
            - no quotes
            - no period
            - max 60 characters
            - return only the title

            User message:
            {firstUserMessage.Trim()}
            """;

        return chatClient.GenerateAsync(
            new ChatModelRequest(
                firstUserMessage.Trim(),
                ContextText: string.Empty,
                Sources: [],
                SystemPrompt: SystemPrompt,
                UserPrompt: prompt,
                Temperature: 0.2),
            cancellationToken);
    }
}

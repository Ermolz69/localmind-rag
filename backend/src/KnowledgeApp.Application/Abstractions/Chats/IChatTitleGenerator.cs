namespace KnowledgeApp.Application.Abstractions;

public interface IChatTitleGenerator
{
    Task<string> GenerateAsync(string firstUserMessage, CancellationToken cancellationToken = default);
}

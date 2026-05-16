namespace KnowledgeApp.Application.Abstractions;

public interface IChatModelClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}

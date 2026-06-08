namespace KnowledgeApp.Application.Abstractions.Ingestion;

public interface ITokenizerService
{
    string TokenizerId { get; }

    bool IsAvailable { get; }

    void EnsureAvailable();

    int CountTokens(string text);

    IReadOnlyList<int> Encode(string text);

    string Decode(IReadOnlyList<int> tokens);

    IReadOnlyList<TokenSpan> GetTokenSpans(string text);
}

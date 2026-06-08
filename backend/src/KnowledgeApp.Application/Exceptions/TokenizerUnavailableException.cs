namespace KnowledgeApp.Application.Exceptions;

public sealed class TokenizerUnavailableException : InvalidOperationException
{
    public TokenizerUnavailableException(string tokenizerId, string? modelPath)
        : base($"Tokenizer '{tokenizerId}' is unavailable. Model path: '{modelPath ?? "not configured"}'.")
    {
        TokenizerId = tokenizerId;
        ModelPath = modelPath;
    }

    public string TokenizerId { get; }
    public string? ModelPath { get; }
}

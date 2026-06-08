namespace KnowledgeApp.Infrastructure.Options;

public sealed class ChunkingTokenizerOptions
{
    public TokenizerKind Kind { get; set; } = TokenizerKind.Tiktoken;
    
    public string TokenizerId { get; set; } = "default";
    
    public string? ModelPath { get; set; }
    
    public bool Required { get; set; } = true;
}

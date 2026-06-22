using System.Security.Cryptography;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.ML.Tokenizers;

namespace KnowledgeApp.Infrastructure.Services.Ingestion.Tokenizers;

public sealed class MicrosoftMlTokenizerService : ITokenizerService
{
    private readonly IOptionsMonitor<ChunkingOptions> _options;
    private Tokenizer? _tokenizer;
    private string? _tokenizerId;
    private bool _initialized;

    public MicrosoftMlTokenizerService(IOptionsMonitor<ChunkingOptions> options)
    {
        _options = options;
    }

    public string TokenizerId
    {
        get
        {
            EnsureAvailable();
            return _tokenizerId!;
        }
    }

    public bool IsAvailable
    {
        get
        {
            try
            {
                EnsureAvailable();
                return true;
            }
            catch (TokenizerUnavailableException)
            {
                return false;
            }
        }
    }

    public void EnsureAvailable()
    {
        if (_initialized)
        {
            return;
        }

        ChunkingTokenizerOptions config = _options.CurrentValue.Tokenizer;

        try
        {
            if (config.Kind == TokenizerKind.Tiktoken)
            {
                // Tiktoken currently uses built-in models
                _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
                _tokenizerId = config.TokenizerId;
            }
            else if (config.Kind == TokenizerKind.Llama)
            {
                if (string.IsNullOrWhiteSpace(config.ModelPath) || !File.Exists(config.ModelPath))
                {
                    if (config.Required)
                    {
                        throw new TokenizerUnavailableException(config.TokenizerId, config.ModelPath);
                    }
                    else
                    {
                        // Safe fallback just to avoid crash if not required
                        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
                        _tokenizerId = "fallback-tiktoken-gpt4";
                    }
                }
                else
                {
                    using Stream stream = File.OpenRead(config.ModelPath);
                    _tokenizer = LlamaTokenizer.Create(stream);
                    _tokenizerId = $"{config.TokenizerId}:sha256-{ComputeSha256(config.ModelPath)}";
                }
            }
            else
            {
                // Default fallback
                _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
                _tokenizerId = config.TokenizerId;
            }

            _initialized = true;
        }
        catch (Exception ex) when (ex is not TokenizerUnavailableException)
        {
            throw new TokenizerUnavailableException(config.TokenizerId, config.ModelPath);
        }
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        EnsureAvailable();
        return _tokenizer!.CountTokens(text);
    }

    public IReadOnlyList<int> Encode(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        EnsureAvailable();
        return _tokenizer!.EncodeToIds(text);
    }

    public string Decode(IReadOnlyList<int> tokens)
    {
        if (tokens.Count == 0) return string.Empty;
        EnsureAvailable();
        return _tokenizer!.Decode(tokens.ToArray()) ?? string.Empty;
    }

    public IReadOnlyList<TokenSpan> GetTokenSpans(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        EnsureAvailable();

        IReadOnlyList<EncodedToken> encodedTokens =
            _tokenizer!.EncodeToTokens(text, out _, considerNormalization: true, considerPreTokenization: true);

        List<TokenSpan> spans = new(encodedTokens.Count);

        foreach (EncodedToken token in encodedTokens)
        {
            int startIndex = token.Offset.Item1;
            int length = ResolveOffsetLength(text, token.Offset, token.Value);

            if (startIndex < 0 || startIndex >= text.Length || length <= 0)
            {
                continue;
            }

            spans.Add(new TokenSpan(startIndex, Math.Min(length, text.Length - startIndex), 1));
        }

        return spans;
    }

    private static int ResolveOffsetLength(string text, (int, int) offset, string tokenValue)
    {
        int startIndex = offset.Item1;
        int secondValue = offset.Item2;

        if (secondValue > 0
            && startIndex + secondValue <= text.Length
            && TokenValueMatches(text, startIndex, secondValue, tokenValue))
        {
            return secondValue;
        }

        if (secondValue >= startIndex && secondValue <= text.Length)
        {
            return secondValue - startIndex;
        }

        if (secondValue > 0 && startIndex + secondValue <= text.Length)
        {
            return secondValue;
        }

        return 0;
    }

    private static bool TokenValueMatches(string text, int startIndex, int length, string tokenValue)
    {
        return tokenValue.Length == length
            && text.AsSpan(startIndex, length).SequenceEqual(tokenValue);
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services.Ingestion.Tokenizers;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion.Chunking;

public class MicrosoftMlTokenizerServiceTests
{
    [Fact]
    public void GetTokenSpans_ReturnsOrderedOffsets_ForAsciiText()
    {
        var options = new ChunkingOptions
        {
            Tokenizer = new ChunkingTokenizerOptions
            {
                Kind = TokenizerKind.Tiktoken,
                TokenizerId = "tiktoken-gpt4",
                Required = true
            }
        };

        var service = new MicrosoftMlTokenizerService(new TestOptionsMonitor<ChunkingOptions>(options));

        IReadOnlyList<TokenSpan> spans = service.GetTokenSpans("alpha beta gamma");

        Assert.NotEmpty(spans);
        Assert.Equal(service.CountTokens("alpha beta gamma"), spans.Sum(span => span.TokenCount));
        Assert.All(spans, span => Assert.InRange(span.StartIndex, 0, "alpha beta gamma".Length - 1));
        Assert.All(spans, span => Assert.InRange(span.Length, 1, "alpha beta gamma".Length));
        Assert.True(spans.Zip(spans.Skip(1), (left, right) => left.StartIndex <= right.StartIndex).All(value => value));
    }

    [Fact]
    public void GetTokenSpans_PreservesOffsetsAcrossWhitespaceAndNewlines()
    {
        var options = new ChunkingOptions
        {
            Tokenizer = new ChunkingTokenizerOptions
            {
                Kind = TokenizerKind.Tiktoken,
                TokenizerId = "tiktoken-gpt4",
                Required = true
            }
        };

        const string text = "alpha\n\nbeta gamma";
        var service = new MicrosoftMlTokenizerService(new TestOptionsMonitor<ChunkingOptions>(options));

        IReadOnlyList<TokenSpan> spans = service.GetTokenSpans(text);

        Assert.NotEmpty(spans);
        Assert.Equal(service.CountTokens(text), spans.Sum(span => span.TokenCount));
        Assert.Contains(spans, span => span.StartIndex > text.IndexOf("beta", StringComparison.Ordinal) - 1);
        Assert.All(spans, span =>
        {
            Assert.True(span.StartIndex + span.Length <= text.Length);
            Assert.False(string.IsNullOrEmpty(text.Substring(span.StartIndex, span.Length)));
        });
    }

    [Fact]
    public void TokenizerUnavailableException_DoesNotCrashStartup_WithLazyMode()
    {
        // Arrange
        var options = new ChunkingOptions
        {
            Tokenizer = new ChunkingTokenizerOptions
            {
                Kind = TokenizerKind.Llama,
                TokenizerId = "llama-local",
                ModelPath = "non_existent_file.model",
                Required = true
            }
        };

        var monitor = new TestOptionsMonitor<ChunkingOptions>(options);

        // Act - Constructor should not throw
        var service = new MicrosoftMlTokenizerService(monitor);

        // Assert - Exception is thrown only on access
        Assert.Throws<TokenizerUnavailableException>(() => service.CountTokens("test"));
    }

    [Fact]
    public void NoSilentFallback_WhenTokenizerMissingAndRequired()
    {
        // Arrange
        var options = new ChunkingOptions
        {
            Tokenizer = new ChunkingTokenizerOptions
            {
                Kind = TokenizerKind.Llama,
                TokenizerId = "llama-local",
                ModelPath = "missing.model",
                Required = true
            }
        };

        var monitor = new TestOptionsMonitor<ChunkingOptions>(options);
        var service = new MicrosoftMlTokenizerService(monitor);

        // Act & Assert
        Assert.Throws<TokenizerUnavailableException>(() => service.EnsureAvailable());
        Assert.False(service.IsAvailable);
    }

    private class TestOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        public TestOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

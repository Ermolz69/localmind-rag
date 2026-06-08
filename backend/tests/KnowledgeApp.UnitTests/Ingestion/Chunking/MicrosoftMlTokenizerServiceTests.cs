using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services.Ingestion.Tokenizers;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion.Chunking;

public class MicrosoftMlTokenizerServiceTests
{
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

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Rag;

public sealed class RagPipelineTests
{
    [Fact]
    public async Task RagContextBuilder_Should_Call_Embedding_And_Vector_Search_And_Build_Context()
    {
        RagSourceDto source = new(
            Guid.NewGuid(),
            "Architecture.md",
            Guid.NewGuid(),
            PageNumber: 3,
            Score: 0.92,
            "Localmind stores documents locally.");
        FakeEmbeddingGenerator embeddings = new();
        FakeVectorSearchService search = new([source]);
        RagContextBuilder builder = new(search, embeddings);
        Guid conversationId = Guid.NewGuid();

        RagContext context = await builder.BuildAsync(new RagContextRequest(conversationId, "local documents", Limit: 4));

        Assert.True(embeddings.WasCalled);
        Assert.True(search.WasCalled);
        Assert.Equal(4, search.Options?.Limit);
        Assert.Single(context.Sources);
        Assert.Contains("Architecture.md", context.ContextText, StringComparison.Ordinal);
        Assert.Contains(source.ChunkId.ToString(), context.ContextText, StringComparison.Ordinal);
        Assert.Contains("Localmind stores documents locally.", context.ContextText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Return_Empty_Context_When_Search_Has_No_Sources()
    {
        FakeEmbeddingGenerator embeddings = new();
        FakeVectorSearchService search = new([]);
        RagContextBuilder builder = new(search, embeddings);

        RagContext context = await builder.BuildAsync(new RagContextRequest(Guid.NewGuid(), "missing topic"));

        Assert.True(embeddings.WasCalled);
        Assert.True(search.WasCalled);
        Assert.Equal(6, search.Options?.Limit);
        Assert.Empty(context.Sources);
        Assert.Equal(string.Empty, context.ContextText);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Preserve_Source_Order_And_Normalize_Context_Text()
    {
        RagSourceDto first = new(
            Guid.NewGuid(),
            "First.txt",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.98,
            "First   snippet\r\nwith\tspaces.");
        RagSourceDto second = new(
            Guid.NewGuid(),
            "Second.txt",
            Guid.NewGuid(),
            PageNumber: 2,
            Score: 0.82,
            "Second snippet.");
        RagContextBuilder builder = new(new FakeVectorSearchService([first, second]), new FakeEmbeddingGenerator());

        RagContext context = await builder.BuildAsync(new RagContextRequest(Guid.NewGuid(), "ordered sources"));

        Guid[] expectedChunkIds = [first.ChunkId, second.ChunkId];
        Assert.Equal(expectedChunkIds, context.Sources.Select(source => source.ChunkId).ToArray());
        Assert.Contains("First snippet with spaces.", context.ContextText, StringComparison.Ordinal);
        Assert.Contains("Page: n/a", context.ContextText, StringComparison.Ordinal);
        Assert.Contains("Page: 2", context.ContextText, StringComparison.Ordinal);
        Assert.Contains("Score: 0.9800", context.ContextText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Bound_Very_Long_Context()
    {
        string longSnippet = new('a', 10_000);
        RagSourceDto source = new(
            Guid.NewGuid(),
            "Long.txt",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.71,
            longSnippet);
        RagContextBuilder builder = new(new FakeVectorSearchService([source]), new FakeEmbeddingGenerator());

        RagContext context = await builder.BuildAsync(new RagContextRequest(Guid.NewGuid(), "long"));

        Assert.True(context.ContextText.Length < longSnippet.Length);
        Assert.Contains(new string('a', 700), context.ContextText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagAnswerGenerator_Should_Call_Context_Builder_And_Chat_Client_With_Sources()
    {
        RagSourceDto source = new(
            Guid.NewGuid(),
            "Notes.txt",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.87,
            "Notes can be linked to documents.");
        FakeRagContextBuilder contextBuilder = new(source);
        CapturingChatModelClient chatClient = new();
        RagAnswerGenerator generator = new(contextBuilder, chatClient);
        Guid conversationId = Guid.NewGuid();

        RagAnswerDto answer = await generator.AnswerAsync(conversationId, "How do notes work?");

        Assert.True(contextBuilder.WasCalled);
        Assert.NotNull(chatClient.Request);
        Assert.Equal("How do notes work?", chatClient.Request.Question);
        Assert.Contains("Notes can be linked", chatClient.Request.ContextText, StringComparison.Ordinal);
        Assert.Same(contextBuilder.Context.Sources, chatClient.Request.Sources);
        Assert.Equal("Generated from context", answer.Answer);
        Assert.Single(answer.Sources);
        Assert.Equal(source.ChunkId, answer.Sources[0].ChunkId);
    }

    [Fact]
    public async Task RagAnswerGenerator_Should_Return_Empty_Sources_When_Context_Is_Empty()
    {
        EmptyRagContextBuilder contextBuilder = new();
        CapturingChatModelClient chatClient = new();
        RagAnswerGenerator generator = new(contextBuilder, chatClient);

        RagAnswerDto answer = await generator.AnswerAsync(Guid.NewGuid(), "Unknown question");

        Assert.Empty(answer.Sources);
        Assert.NotNull(chatClient.Request);
        Assert.Equal(string.Empty, chatClient.Request.ContextText);
        Assert.Empty(chatClient.Request.Sources);
    }

    [Fact]
    public async Task StubChatModelClient_Should_Return_Source_Based_Answer()
    {
        RagSourceDto source = new(
            Guid.NewGuid(),
            "Guide.txt",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.91,
            "Local RAG answers use indexed chunks.");
        StubChatModelClient client = new();

        string answer = await client.GenerateAsync(new ChatModelRequest("What is RAG?", "Context", [source]));

        Assert.Contains("Local RAG answers use indexed chunks.", answer, StringComparison.Ordinal);
        Assert.Contains("Guide.txt", answer, StringComparison.Ordinal);
        Assert.Contains(source.ChunkId.ToString(), answer, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StubChatModelClient_Should_Return_No_Source_Message_When_Context_Is_Empty()
    {
        StubChatModelClient client = new();

        string answer = await client.GenerateAsync(new ChatModelRequest("What is RAG?", string.Empty, []));

        Assert.Equal("No relevant local sources were found for this question.", answer);
    }

    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public bool WasCalled { get; private set; }

        public string ModelName => "fake";

        public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new[] { 1.0f, 0.0f });
        }
    }

    private sealed class FakeVectorSearchService(IReadOnlyList<RagSourceDto> sources) : IVectorSearchService
    {
        public bool WasCalled { get; private set; }

        public VectorSearchOptions? Options { get; private set; }

        public Task<IReadOnlyList<RagSourceDto>> SearchAsync(
            float[] queryVector,
            VectorSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Options = options;
            return Task.FromResult(sources);
        }
    }

    private sealed class FakeRagContextBuilder(RagSourceDto source) : IRagContextBuilder
    {
        public RagContext Context { get; } = new([source], source.Snippet);

        public bool WasCalled { get; private set; }

        public Task<RagContext> BuildAsync(RagContextRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Context);
        }
    }

    private sealed class EmptyRagContextBuilder : IRagContextBuilder
    {
        public Task<RagContext> BuildAsync(RagContextRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RagContext([], string.Empty));
        }
    }

    private sealed class CapturingChatModelClient : IChatModelClient
    {
        public ChatModelRequest? Request { get; private set; }

        public Task<string> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult("Generated from context");
        }
    }
}

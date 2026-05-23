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
        FakeVectorSearchService search = new(source);
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

    private sealed class FakeVectorSearchService(RagSourceDto source) : IVectorSearchService
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
            return Task.FromResult<IReadOnlyList<RagSourceDto>>([source]);
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

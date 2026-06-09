using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Rag;

public sealed class RagPipelineTests
{
    [Fact]
    public async Task RagContextBuilder_Should_Call_Embedding_And_Hybrid_Search_And_Build_Context()
    {
        RagSourceDto source = new(
            Guid.NewGuid(),
            "Architecture.md",
            Guid.NewGuid(),
            PageNumber: 3,
            Score: 0.92,
            "Localmind stores documents locally.");

        FakeEmbeddingGenerator embeddings = new();
        FakeHybridRetrievalService search = FakeHybridRetrievalService.FromVectorSources([source]);

        RagContextBuilder builder = CreateContextBuilder(search, embeddings);

        Guid conversationId = Guid.NewGuid();

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(
                conversationId,
                "local documents",
                Limit: 4));

        Assert.True(embeddings.WasCalled);
        Assert.True(search.WasCalled);
        Assert.Equal(4, search.Options?.Limit);
        Assert.Single(context.Sources);
        Assert.Contains(
            "Architecture.md",
            context.ContextText,
            StringComparison.Ordinal);
        Assert.Contains(
            source.ChunkId.ToString(),
            context.ContextText,
            StringComparison.Ordinal);
        Assert.Contains(
            "Localmind stores documents locally.",
            context.ContextText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Return_Empty_Context_When_Search_Has_No_Sources()
    {
        FakeEmbeddingGenerator embeddings = new();
        FakeHybridRetrievalService search = new([]);

        RagContextBuilder builder = CreateContextBuilder(search, embeddings);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "missing topic"));

        Assert.True(embeddings.WasCalled);
        Assert.True(search.WasCalled);
        Assert.Equal(12, search.Options?.Limit);
        Assert.Empty(context.Sources);
        Assert.Equal(string.Empty, context.ContextText);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Exclude_Sources_Below_Minimum_Score()
    {
        RagSourceDto relevant = new(
            Guid.NewGuid(),
            "VpnGuide.txt",
            Guid.NewGuid(),
            PageNumber: 1,
            Score: 0.94,
            "Remote VPN access requires the NorthGate client.");

        RagSourceDto weakMatch = new(
            Guid.NewGuid(),
            "MealPolicy.txt",
            Guid.NewGuid(),
            PageNumber: 1,
            Score: 0.24,
            "Meal reimbursement is limited to 35 EUR per day.");

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([relevant, weakMatch]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.65);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "How do I access the VPN?"));

        RagSourceDto source = Assert.Single(context.Sources);

        Assert.Equal(relevant.ChunkId, source.ChunkId);
        Assert.Contains(
            "NorthGate",
            context.ContextText,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Meal reimbursement",
            context.ContextText,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            weakMatch.ChunkId.ToString(),
            context.ContextText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Return_Empty_Context_When_All_Sources_Are_Below_Minimum_Score()
    {
        RagSourceDto weakMatch = new(
            Guid.NewGuid(),
            "Unrelated.txt",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.12,
            "This document is unrelated to the question.");

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([weakMatch]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.65);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "Unknown topic"));

        Assert.Empty(context.Sources);
        Assert.Equal(string.Empty, context.ContextText);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Keep_Vector_Sources_Above_Minimum_Score()
    {
        RagSourceDto topMatch = new(
            Guid.NewGuid(),
            "DeepWork.md",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.59,
            "The Roosevelt method uses a hard deadline.");

        RagSourceDto closeMatch = new(
            Guid.NewGuid(),
            "DeepWork.md",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.52,
            "Focus rituals support intense work.");

        RagSourceDto weakMatch = new(
            Guid.NewGuid(),
            "DeepWork.md",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.24,
            "Weekly reporting belongs to a different method.");

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([topMatch, closeMatch, weakMatch]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.3);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "What is the Roosevelt method?"));

        Assert.Equal(
            [topMatch.ChunkId, closeMatch.ChunkId],
            context.Sources.Select(source => source.ChunkId).ToArray());

        Assert.DoesNotContain(
            "Weekly reporting",
            context.ContextText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Include_Strong_Keyword_Only_Source()
    {
        HybridSearchResult keywordOnly = new(
            Guid.NewGuid(),
            "Security.md",
            Guid.NewGuid(),
            PageNumber: null,
            "Mutating endpoints require X-LocalMind-Token.",
            Score: 0.016,
            VectorScore: null,
            VectorRank: null,
            FullTextScore: -1.2,
            FullTextRank: 3);

        RagContextBuilder builder = CreateContextBuilder(
            new FakeHybridRetrievalService([keywordOnly]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.65);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "X-LocalMind-Token"));

        RagSourceDto source = Assert.Single(context.Sources);
        Assert.Equal(keywordOnly.ChunkId, source.ChunkId);
        Assert.Contains(
            "X-LocalMind-Token",
            context.ContextText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Exclude_Weak_Keyword_Only_Source()
    {
        HybridSearchResult keywordOnly = new(
            Guid.NewGuid(),
            "Broad.md",
            Guid.NewGuid(),
            PageNumber: null,
            "A broad low-ranked keyword match.",
            Score: 0.014,
            VectorScore: null,
            VectorRank: null,
            FullTextScore: -0.1,
            FullTextRank: 12);

        RagContextBuilder builder = CreateContextBuilder(
            new FakeHybridRetrievalService([keywordOnly]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.65);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "broad"));

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

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([first, second]),
            new FakeEmbeddingGenerator(),
            maxSourceScoreDistance: 1);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "ordered sources"));

        Guid[] expectedChunkIds = [first.ChunkId, second.ChunkId];

        Assert.Equal(
            expectedChunkIds,
            context.Sources.Select(source => source.ChunkId).ToArray());

        Assert.Contains(
            "First snippet with spaces.",
            context.ContextText,
            StringComparison.Ordinal);

        Assert.Contains(
            "Page: n/a",
            context.ContextText,
            StringComparison.Ordinal);

        Assert.Contains(
            "Page: 2",
            context.ContextText,
            StringComparison.Ordinal);

        Assert.Contains(
            "Score: 0.9800",
            context.ContextText,
            StringComparison.Ordinal);
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

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([source]),
            new FakeEmbeddingGenerator());

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "long"));

        Assert.True(context.ContextText.Length < longSnippet.Length);

        Assert.Contains(
            new string('a', 700),
            context.ContextText,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task RagContextBuilder_Should_Focus_Long_Snippet_Around_Question_Terms()
    {
        string prefix = new('a', 1_500);
        string answerText =
            "Метод роботи Рузвельта: обрати пріоритетне завдання, встановити жорсткий дедлайн і працювати інтенсивно.";

        RagSourceDto source = new(
            Guid.NewGuid(),
            "DeepWork.md",
            Guid.NewGuid(),
            PageNumber: null,
            Score: 0.34,
            $"{prefix} {answerText}");

        RagContextBuilder builder = CreateContextBuilder(
            FakeHybridRetrievalService.FromVectorSources([source]),
            new FakeEmbeddingGenerator(),
            minimumSourceScore: 0.3);

        RagContext context = await builder.BuildAsync(
            new RagContextRequest(Guid.NewGuid(), "В чому полягає метод роботи Рузвельта?"));

        Assert.Single(context.Sources);
        Assert.Contains(
            answerText,
            context.ContextText,
            StringComparison.Ordinal);
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

        RagAnswerDto answer = await generator.AnswerAsync(
            conversationId,
            "How do notes work?");

        Assert.True(contextBuilder.WasCalled);
        Assert.NotNull(chatClient.Request);
        Assert.Equal("How do notes work?", chatClient.Request.Question);

        Assert.Contains(
            "Notes can be linked",
            chatClient.Request.ContextText,
            StringComparison.Ordinal);

        Assert.Same(
            contextBuilder.Context.Sources,
            chatClient.Request.Sources);

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

        RagAnswerDto answer = await generator.AnswerAsync(
            Guid.NewGuid(),
            "Unknown question");

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

        string answer = await client.GenerateAsync(
            new ChatModelRequest(
                "What is RAG?",
                "Context",
                [source]));

        Assert.Contains(
            "Local RAG answers use indexed chunks.",
            answer,
            StringComparison.Ordinal);

        Assert.Contains(
            "Guide.txt",
            answer,
            StringComparison.Ordinal);

        Assert.Contains(
            source.ChunkId.ToString(),
            answer,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task StubChatModelClient_Should_Return_No_Source_Message_When_Context_Is_Empty()
    {
        StubChatModelClient client = new();

        string answer = await client.GenerateAsync(
            new ChatModelRequest(
                "What is RAG?",
                string.Empty,
                []));

        Assert.Equal(
            "No relevant local sources were found for this question.",
            answer);
    }

    private static RagContextBuilder CreateContextBuilder(
        IHybridRetrievalService search,
        IEmbeddingGenerator embeddings,
        double minimumSourceScore = 0.65,
        double maxSourceScoreDistance = 0.1)
    {
        return new RagContextBuilder(
            search,
            embeddings,
            Options.Create(new RagOptions
            {
                MinimumSourceScore = minimumSourceScore,
                MaxSourceScoreDistance = maxSourceScoreDistance,
            }));
    }

    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
    {
        public bool WasCalled { get; private set; }

        public string ModelName => "fake";

        public Task<float[]> GenerateAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            return Task.FromResult(new[] { 1.0f, 0.0f });
        }
    }

    private sealed class FakeHybridRetrievalService(
        IReadOnlyList<HybridSearchResult> results) : IHybridRetrievalService
    {
        public bool WasCalled { get; private set; }

        public HybridSearchOptions? Options { get; private set; }

        public static FakeHybridRetrievalService FromVectorSources(IReadOnlyList<RagSourceDto> sources)
        {
            HybridSearchResult[] results = sources
                .Select((source, index) => new HybridSearchResult(
                    source.DocumentId,
                    source.DocumentName,
                    source.ChunkId,
                    source.PageNumber,
                    source.Snippet,
                    source.Score,
                    VectorScore: source.Score,
                    VectorRank: index + 1,
                    FullTextScore: null,
                    FullTextRank: null))
                .ToArray();

            return new FakeHybridRetrievalService(results);
        }

        public Task<IReadOnlyList<HybridSearchResult>> SearchAsync(
            string query,
            float[] queryVector,
            HybridSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Options = options;

            return Task.FromResult(results);
        }
    }

    private sealed class FakeRagContextBuilder(
        RagSourceDto source) : IRagContextBuilder
    {
        public RagContext Context { get; } =
            new([source], source.Snippet);

        public bool WasCalled { get; private set; }

        public Task<RagContext> BuildAsync(
            RagContextRequest request,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            return Task.FromResult(Context);
        }
    }

    private sealed class EmptyRagContextBuilder : IRagContextBuilder
    {
        public Task<RagContext> BuildAsync(
            RagContextRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new RagContext([], string.Empty));
        }
    }

    private sealed class CapturingChatModelClient : IChatModelClient
    {
        public ChatModelRequest? Request { get; private set; }

        public Task<string> GenerateAsync(
            ChatModelRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;

            return Task.FromResult("Generated from context");
        }

        public async IAsyncEnumerable<string> GenerateStreamAsync(
            ChatModelRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Request = request;
            yield return "Generated from context";
            await Task.Yield();
        }
    }
}

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.UnitTests.TestSupport.Fakes;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Chats;

public sealed class ChatHandlersTests
{
    private static readonly DateTimeOffset EarlierMessageTime = new(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset LaterMessageTime = new(2026, 5, 15, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ChatHandlers_Should_Create_List_And_Save_User_And_Assistant_Messages()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        ChatRequestValidator validator = new();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CreateChatHandler create = new(conversationRepository, unitOfWork, validator, new FakeLocalDeviceResolver());
        GetChatsHandler list = new(conversationRepository);
        SendChatMessageHandler send = new(
            conversationRepository,
            unitOfWork,
            new FakeRagAnswerGenerator(),
            validator,
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver(),
            new FakeOperationLogRepository());

        ConversationDto? conversation = (await create.HandleAsync(new CreateConversationRequest("Question"))).AssertSuccess();
        Contracts.Common.CursorPage<ConversationDto> conversations = (await list.HandleAsync(new GetChatsQuery())).AssertSuccess();
        RagAnswerDto? answer = (await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?"))).AssertSuccess();

        Domain.Entities.ChatMessage[]? messages = await database.Context.ChatMessages.ToArrayAsync();

        Assert.Contains(conversations.Items, item => item.Id == conversation.Id);
        Assert.Equal("Stub answer", answer.Answer);
        Assert.Single(answer.Sources);
        Assert.Equal("Matched chunk", answer.Sources[0].Snippet);
        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == "What is local RAG?");
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == "Stub answer");
    }

    [Fact]
    public async Task SendChatMessageHandler_Should_Pass_Filters_To_Rag_Generator()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        ChatRequestValidator validator = new();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CreateChatHandler create = new(conversationRepository, unitOfWork, validator, new FakeLocalDeviceResolver());
        FakeRagAnswerGenerator rag = new();
        SendChatMessageHandler send = new(
            conversationRepository,
            unitOfWork,
            rag,
            validator,
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver(),
            new FakeOperationLogRepository());
        RetrievalFilters filters = new(BucketId: Guid.NewGuid(), FileType: "pdf");
        ConversationDto? conversation = (await create.HandleAsync(new CreateConversationRequest("Question"))).AssertSuccess();

        await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?", filters));

        Assert.Same(filters, rag.LastFilters);
    }

    [Fact]
    public async Task SendChatMessageHandler_Should_Reject_Missing_Conversation()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        SendChatMessageHandler send = new(
            conversationRepository,
            unitOfWork,
            new FakeRagAnswerGenerator(),
            new ChatRequestValidator(),
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver(),
            new FakeOperationLogRepository());

        Result<RagAnswerDto> result = await send.HandleAsync(Guid.NewGuid(), new ChatMessageRequest("Hello"));

        Assert.Equal("CHAT_NOT_FOUND", result.AssertFailure(ErrorType.NotFound).Code);
    }

    [Fact]
    public async Task GenerateConversationTitleHandler_Should_Generate_Title_From_First_User_Message()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        Domain.Entities.Conversation conversation = new() { Title = "New chat 1" };
        database.Context.Conversations.Add(conversation);
        database.Context.ChatMessages.AddRange(
            new Domain.Entities.ChatMessage
            {
                ConversationId = conversation.Id,
                CreatedAt = LaterMessageTime,
                Role = ChatRole.User,
                Content = "Second question",
            },
            new Domain.Entities.ChatMessage
            {
                ConversationId = conversation.Id,
                CreatedAt = EarlierMessageTime,
                Role = ChatRole.User,
                Content = "How do I configure local RAG?",
            });
        await database.Context.SaveChangesAsync();
        FakeChatTitleGenerator titleGenerator = new("Local RAG Setup");
        GenerateConversationTitleHandler handler = new(
            conversationRepository,
            titleGenerator,
            unitOfWork,
            new FixedDateTimeProvider());

        ConversationDto result = (await handler.HandleAsync(conversation.Id)).AssertSuccess();

        Assert.Equal("Local RAG Setup", result.Title);
        Assert.Equal("How do I configure local RAG?", titleGenerator.LastMessage);
        Assert.Equal(new FixedDateTimeProvider().UtcNow, conversation.TitleGeneratedAt);
        Assert.Null(conversation.TitleEditedAt);
    }

    [Fact]
    public async Task GenerateConversationTitleHandler_Should_Not_Regenerate_Title_If_TitleGeneratedAt_Is_Set()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        Domain.Entities.Conversation conversation = new()
        {
            Title = "Existing title",
            TitleGeneratedAt = EarlierMessageTime,
        };
        database.Context.Conversations.Add(conversation);
        database.Context.ChatMessages.Add(new Domain.Entities.ChatMessage
        {
            ConversationId = conversation.Id,
            CreatedAt = EarlierMessageTime,
            Role = ChatRole.User,
            Content = "First question",
        });
        await database.Context.SaveChangesAsync();
        FakeChatTitleGenerator titleGenerator = new("Replacement title");
        GenerateConversationTitleHandler handler = new(
            conversationRepository,
            titleGenerator,
            unitOfWork,
            new FixedDateTimeProvider());

        ConversationDto result = (await handler.HandleAsync(conversation.Id)).AssertSuccess();

        Assert.Equal("Existing title", result.Title);
        Assert.Null(titleGenerator.LastMessage);
    }

    [Fact]
    public async Task GenerateConversationTitleHandler_Should_Not_Overwrite_Manual_Title_If_TitleEditedAt_Is_Set()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        Domain.Entities.Conversation conversation = new()
        {
            Title = "Manual title",
            TitleEditedAt = EarlierMessageTime,
        };
        database.Context.Conversations.Add(conversation);
        database.Context.ChatMessages.Add(new Domain.Entities.ChatMessage
        {
            ConversationId = conversation.Id,
            CreatedAt = EarlierMessageTime,
            Role = ChatRole.User,
            Content = "First question",
        });
        await database.Context.SaveChangesAsync();
        FakeChatTitleGenerator titleGenerator = new("Replacement title");
        GenerateConversationTitleHandler handler = new(
            conversationRepository,
            titleGenerator,
            unitOfWork,
            new FixedDateTimeProvider());

        ConversationDto result = (await handler.HandleAsync(conversation.Id)).AssertSuccess();

        Assert.Equal("Manual title", result.Title);
        Assert.Null(titleGenerator.LastMessage);
    }

    [Fact]
    public async Task GenerateConversationTitleHandler_Should_Return_Current_Conversation_When_No_User_Message()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        Domain.Entities.Conversation conversation = new() { Title = "New chat 1" };
        database.Context.Conversations.Add(conversation);
        await database.Context.SaveChangesAsync();
        FakeChatTitleGenerator titleGenerator = new("Generated title");
        GenerateConversationTitleHandler handler = new(
            conversationRepository,
            titleGenerator,
            unitOfWork,
            new FixedDateTimeProvider());

        ConversationDto result = (await handler.HandleAsync(conversation.Id)).AssertSuccess();

        Assert.Equal("New chat 1", result.Title);
        Assert.Null(titleGenerator.LastMessage);
        Assert.Null(conversation.TitleGeneratedAt);
    }

    [Fact]
    public async Task GenerateConversationTitleHandler_Should_Use_Fallback_When_Generated_Title_Is_Invalid()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        Domain.Entities.Conversation conversation = new() { Title = "New chat 1" };
        const string firstQuestion = "Explain local-first document indexing for desktop RAG applications";
        database.Context.Conversations.Add(conversation);
        database.Context.ChatMessages.Add(new Domain.Entities.ChatMessage
        {
            ConversationId = conversation.Id,
            CreatedAt = EarlierMessageTime,
            Role = ChatRole.User,
            Content = firstQuestion,
        });
        await database.Context.SaveChangesAsync();
        GenerateConversationTitleHandler handler = new(
            conversationRepository,
            new FakeChatTitleGenerator("This title is intentionally much longer than sixty characters so fallback is used"),
            unitOfWork,
            new FixedDateTimeProvider());

        ConversationDto result = (await handler.HandleAsync(conversation.Id)).AssertSuccess();

        Assert.Equal(firstQuestion[..60], result.Title);
        Assert.Equal(new FixedDateTimeProvider().UtcNow, conversation.TitleGeneratedAt);
    }

    [Fact]
    public async Task UpdateConversationHandler_Should_Set_TitleEditedAt()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        ChatRequestValidator validator = new();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CreateChatHandler create = new(conversationRepository, unitOfWork, validator, new FakeLocalDeviceResolver());
        ConversationDto conversation = (await create.HandleAsync(new CreateConversationRequest("Question"))).AssertSuccess();
        UpdateConversationHandler update = new(
            conversationRepository,
            unitOfWork,
            new FixedDateTimeProvider(),
            validator);

        (await update.HandleAsync(conversation.Id, new UpdateConversationRequest(" Renamed chat "))).AssertSuccess();

        Domain.Entities.Conversation updatedConversation = await database.Context.Conversations.FindAsync(conversation.Id)
            ?? throw new InvalidOperationException("Conversation was not found.");
        Assert.Equal("Renamed chat", updatedConversation.Title);
        Assert.Equal(new FixedDateTimeProvider().UtcNow, updatedConversation.TitleEditedAt);
        Assert.Equal(new FixedDateTimeProvider().UtcNow, updatedConversation.UpdatedAt);
    }

    private sealed class FakeRagAnswerGenerator : IRagAnswerGenerator
    {
        public RetrievalFilters? LastFilters { get; private set; }

        public Task<RagAnswerDto> AnswerAsync(
            Guid conversationId,
            string question,
            RetrievalFilters? filters = null,
            CancellationToken cancellationToken = default)
        {
            LastFilters = filters;
            RagSourceDto source = new(
                Guid.NewGuid(),
                "Document",
                Guid.NewGuid(),
                PageNumber: null,
                Score: 0.95,
                "Matched chunk");

            return Task.FromResult(new RagAnswerDto("Stub answer", [source]));
        }

        public async IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(
            Guid conversationId,
            string question,
            RetrievalFilters? filters = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new RagAnswerChunkDto("Stub answer");
            await Task.Yield();
        }
    }

    private sealed class FakeChatTitleGenerator(string title) : IChatTitleGenerator
    {
        public string? LastMessage { get; private set; }

        public Task<string> GenerateAsync(string firstUserMessage, CancellationToken cancellationToken = default)
        {
            LastMessage = firstUserMessage;
            return Task.FromResult(title);
        }
    }
}

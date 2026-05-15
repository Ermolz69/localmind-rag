using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests;

public sealed class ApplicationHandlersTests
{
    [Fact]
    public async Task BucketHandlers_Should_List_Create_Update_And_Delete()
    {
        await using var database = await TestDatabase.CreateAsync();
        var create = new CreateBucketHandler(database.Context);
        var list = new GetBucketsHandler(database.Context);
        var update = new UpdateBucketHandler(database.Context, new FixedDateTimeProvider());
        var delete = new DeleteBucketHandler(database.Context);

        var bucket = await create.HandleAsync(new Bucket { Name = "Work", Description = "Initial" });
        var buckets = await list.HandleAsync();

        Assert.Contains(buckets, item => item.Id == bucket.Id);
        Assert.True(await update.HandleAsync(bucket.Id, new Bucket { Name = "Personal", Description = "Updated" }));
        Assert.False(await update.HandleAsync(Guid.NewGuid(), new Bucket { Name = "Missing" }));
        Assert.True(await delete.HandleAsync(bucket.Id));
        Assert.False(await delete.HandleAsync(bucket.Id));
    }

    [Fact]
    public async Task NoteHandlers_Should_List_Create_Update_And_Delete()
    {
        await using var database = await TestDatabase.CreateAsync();
        var create = new CreateNoteHandler(database.Context);
        var list = new GetNotesHandler(database.Context);
        var update = new UpdateNoteHandler(database.Context);
        var delete = new DeleteNoteHandler(database.Context);

        var note = await create.HandleAsync(new Note { Title = "Draft", Markdown = "Body" });
        var notes = await list.HandleAsync();

        Assert.Contains(notes, item => item.Id == note.Id);
        Assert.True(await update.HandleAsync(note.Id, new Note { Title = "Done", Markdown = "Updated" }));
        Assert.False(await update.HandleAsync(Guid.NewGuid(), new Note { Title = "Missing" }));
        Assert.True(await delete.HandleAsync(note.Id));
        Assert.False(await delete.HandleAsync(note.Id));
    }

    [Fact]
    public async Task ChatHandlers_Should_Create_List_And_Save_User_And_Assistant_Messages()
    {
        await using var database = await TestDatabase.CreateAsync();
        var create = new CreateChatHandler(database.Context);
        var list = new GetChatsHandler(database.Context);
        var send = new SendChatMessageHandler(database.Context, new FakeRagAnswerGenerator());

        var conversation = await create.HandleAsync(new Conversation { Title = "Question" });
        var conversations = await list.HandleAsync();
        var answer = await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?"));

        var messages = await database.Context.ChatMessages.ToArrayAsync();

        Assert.Contains(conversations, item => item.Id == conversation.Id);
        Assert.Equal("Stub answer", answer.Answer);
        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == "What is local RAG?");
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == "Stub answer");
    }

    [Fact]
    public async Task DocumentHandlers_Should_Delete_And_Reindex_Documents()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = new Document { Name = "notes.txt", Status = DocumentStatus.Indexed };
        database.Context.Documents.Add(document);
        await database.Context.SaveChangesAsync();

        var reindex = new ReindexDocumentHandler(database.Context);
        var delete = new DeleteDocumentHandler(database.Context);

        Assert.True(await reindex.HandleAsync(document.Id));
        Assert.False(await reindex.HandleAsync(Guid.NewGuid()));
        Assert.True(await delete.HandleAsync(document.Id));
        Assert.False(await delete.HandleAsync(Guid.NewGuid()));

        var storedDocument = await database.Context.Documents.SingleAsync(item => item.Id == document.Id);
        var job = await database.Context.IngestionJobs.SingleAsync(item => item.DocumentId == document.Id);

        Assert.Equal(DocumentStatus.Deleted, storedDocument.Status);
        Assert.Equal(SyncStatus.DeletedLocal, storedDocument.SyncStatus);
        Assert.Equal(IngestionJobStatus.Queued, job.Status);
    }

    [Fact]
    public async Task ProcessIngestionJobHandler_Should_Return_NotFound_Or_Call_Processor()
    {
        await using var database = await TestDatabase.CreateAsync();
        var job = new IngestionJob { DocumentId = Guid.NewGuid() };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        var processor = new FakeIngestionJobProcessor();
        var handler = new ProcessIngestionJobHandler(database.Context, processor);

        Assert.False(await handler.HandleAsync(Guid.NewGuid()));
        Assert.True(await handler.HandleAsync(job.Id));
        Assert.Equal(job.Id, processor.LastProcessedJobId);
    }

    private sealed class FakeRagAnswerGenerator : IRagAnswerGenerator
    {
        public Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RagAnswerDto("Stub answer", []));
        }
    }

    private sealed class FakeIngestionJobProcessor : IIngestionJobProcessor
    {
        public Guid? LastProcessedJobId { get; private set; }

        public Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            LastProcessedJobId = jobId;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, AppDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public AppDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}

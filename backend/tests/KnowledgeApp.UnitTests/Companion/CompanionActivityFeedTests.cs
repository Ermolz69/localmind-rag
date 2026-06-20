using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Companion;

public sealed class CompanionActivityFeedTests
{
    [Fact]
    public void GetRecent_Should_Return_Events_Newest_First()
    {
        CompanionActivityFeed feed = new(new FixedDateTimeProvider());

        feed.Publish("document.added", "a.pdf added");
        feed.Publish("ingestion.indexed", "a.pdf indexed successfully");

        IReadOnlyList<CompanionActivityEventDto> events = feed.GetRecent(10);

        Assert.Equal(2, events.Count);
        Assert.Equal("a.pdf indexed successfully", events[0].Message);
        Assert.Equal("a.pdf added", events[1].Message);
    }

    [Fact]
    public void Publish_Should_Carry_Kind_And_Detail()
    {
        CompanionActivityFeed feed = new(new FixedDateTimeProvider());

        feed.Publish("ingestion.failed", "b.pdf failed", "Could not extract text");

        CompanionActivityEventDto item = Assert.Single(feed.GetRecent(10));
        Assert.Equal("ingestion.failed", item.Kind);
        Assert.Equal("Could not extract text", item.Detail);
    }

    [Fact]
    public void Publish_Should_Ignore_Blank_Messages()
    {
        CompanionActivityFeed feed = new(new FixedDateTimeProvider());

        feed.Publish("noise", "   ");

        Assert.Empty(feed.GetRecent(10));
    }

    [Fact]
    public void GetRecent_Should_Respect_Limit_And_Capacity()
    {
        CompanionActivityFeed feed = new(new FixedDateTimeProvider());

        for (int index = 0; index < 130; index++)
        {
            feed.Publish("ingestion.indexed", $"doc-{index} indexed");
        }

        Assert.Equal(5, feed.GetRecent(5).Count);
        // Capacity is 100, so the buffer never grows beyond it.
        Assert.Equal(100, feed.GetRecent(1000).Count);
    }
}

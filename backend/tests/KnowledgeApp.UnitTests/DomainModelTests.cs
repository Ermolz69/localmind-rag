using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.UnitTests;

public sealed class DomainModelTests
{
    [Fact]
    public void NewDocument_Should_Start_With_LocalOnlySyncStatus()
    {
        var document = new Document { Name = "notes.md" };

        Assert.Equal(SyncStatus.LocalOnly, document.SyncStatus);
        Assert.Equal(DocumentStatus.Uploaded, document.Status);
    }
}

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using A = DocumentFormat.OpenXml.Drawing;
using PresentationSlideId = DocumentFormat.OpenXml.Presentation.SlideId;
using SlideText = DocumentFormat.OpenXml.Drawing.Text;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class SyncService(AppDbContext dbContext) : ISyncService, ISyncQueue, ISyncClient
{
    public async Task<SyncStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.SyncOutbox.CountAsync(x => x.Status == SyncStatus.PendingUpload, cancellationToken);
        return new SyncStatusDto(false, false, pending, "Sync disabled");
    }

    public Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PullAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task EnqueueAsync(Guid entityId, SyncOperation operation, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

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

public sealed class AiRuntimeManager : IAiRuntimeManager, IAiModelRegistry
{
    public Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RuntimeStatusDto(true, "Missing", false, true));
    public Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<string>>([]);
    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

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

public sealed class StubEmbeddingGenerator : IEmbeddingGenerator
{
    private const string DefaultModelName = "BGE-M3";
    private readonly string modelName;

    public StubEmbeddingGenerator() : this(DefaultModelName)
    {
    }

    public StubEmbeddingGenerator(IOptions<AiOptions> options) : this(options.Value.EmbeddingModel)
    {
    }

    private StubEmbeddingGenerator(string modelName)
    {
        this.modelName = string.IsNullOrWhiteSpace(modelName) ? DefaultModelName : modelName;
    }

    public string ModelName => modelName;

    public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        byte[]? bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Task.FromResult(bytes.Select(x => (float)x / byte.MaxValue).ToArray());
    }
}

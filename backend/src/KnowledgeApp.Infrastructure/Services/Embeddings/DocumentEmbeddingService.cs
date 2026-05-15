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

public sealed class DocumentEmbeddingService(
    IEmbeddingGenerator embeddingGenerator,
    IDateTimeProvider dateTimeProvider) : IDocumentEmbeddingService
{
    public async Task<IReadOnlyList<DocumentEmbedding>> GenerateAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<DocumentEmbedding>(chunks.Count);
        foreach (var chunk in chunks)
        {
            var vector = await embeddingGenerator.GenerateAsync(chunk.Text, cancellationToken);
            embeddings.Add(new DocumentEmbedding
            {
                CreatedAt = dateTimeProvider.UtcNow,
                DocumentChunkId = chunk.Id,
                ModelName = embeddingGenerator.ModelName,
                Dimension = vector.Length,
                Embedding = EmbeddingVectorSerializer.ToBytes(vector),
            });
        }

        return embeddings;
    }
}

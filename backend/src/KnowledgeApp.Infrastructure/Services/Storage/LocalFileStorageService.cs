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

public sealed class LocalFileStorageService(IAppPathProvider paths) : IFileStorageService
{
    public async Task<StoredFileDto> SaveAsync(Stream content, Guid documentId, string fileName, CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("Document file name is required.", nameof(fileName));
        }

        var documentDirectory = Path.Combine(paths.FilesDirectory, documentId.ToString());
        Directory.CreateDirectory(documentDirectory);
        var localPath = Path.Combine(documentDirectory, safeFileName);
        long sizeBytes;
        await using (var output = File.Create(localPath))
        {
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
            sizeBytes = output.Length;
        }

        await using var input = File.OpenRead(localPath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken));
        return new StoredFileDto(safeFileName, localPath, sizeBytes, hash);
    }
}

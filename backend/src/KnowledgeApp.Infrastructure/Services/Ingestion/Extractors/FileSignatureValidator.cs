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

internal static class FileSignatureValidator
{
    public static void EnsurePdf(string filePath)
    {
        Span<byte> signature = stackalloc byte[5];
        using FileStream? input = File.OpenRead(filePath);
        if (input.Read(signature) != signature.Length || !signature.SequenceEqual("%PDF-"u8))
        {
            throw new InvalidOperationException("PDF file signature is invalid.");
        }
    }

    public static void EnsureZipPackage(string filePath, string format)
    {
        Span<byte> signature = stackalloc byte[4];
        using FileStream? input = File.OpenRead(filePath);
        if (input.Read(signature) != signature.Length
            || signature[0] != (byte)'P'
            || signature[1] != (byte)'K')
        {
            throw new InvalidOperationException($"{format} file signature is invalid.");
        }
    }
}

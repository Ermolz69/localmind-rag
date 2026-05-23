using System.Text;
using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class StubChatModelClient : IChatModelClient
{
    private const int AnswerSnippetLimit = 280;

    public Task<string> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Sources.Count == 0 || string.IsNullOrWhiteSpace(request.ContextText))
        {
            return Task.FromResult("No relevant local sources were found for this question.");
        }

        RagSourceDto topSource = request.Sources[0];
        string snippet = NormalizeSnippet(topSource.Snippet);
        if (snippet.Length > AnswerSnippetLimit)
        {
            snippet = snippet[..AnswerSnippetLimit];
        }

        StringBuilder answer = new();
        answer.Append("Based on local sources, ");
        answer.Append(snippet);
        answer.Append(" Source: ");
        answer.Append(topSource.DocumentName);
        answer.Append(" / chunk ");
        answer.Append(topSource.ChunkId);
        answer.Append('.');

        return Task.FromResult(answer.ToString());
    }

    private static string NormalizeSnippet(string snippet)
    {
        return Regex.Replace(snippet, "\\s+", " ").Trim();
    }
}

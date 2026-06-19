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
        if (!string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            return Task.FromResult(CreateTitleFromQuestion(request.Question));
        }

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

    public async IAsyncEnumerable<string> GenerateStreamAsync(ChatModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (request.Question.Contains("trigger-error", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated AI runtime error during streaming.");
        }

        if (request.Question.Contains("cancel", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 1; i <= 50; i++)
            {
                if (i > 3)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                yield return $"word{i} ";
                await Task.Delay(20, cancellationToken);
            }
            yield break;
        }

        string fullAnswer = await GenerateAsync(request, cancellationToken);
        string[] words = fullAnswer.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            yield return words[i] + (i < words.Length - 1 ? " " : "");
            await Task.Delay(20, cancellationToken); // simulate token generation
        }
    }

    private static string NormalizeSnippet(string snippet)
    {
        return Regex.Replace(snippet, "\\s+", " ").Trim();
    }

    private static string CreateTitleFromQuestion(string question)
    {
        string normalized = Regex.Replace(question, "\\s+", " ").Trim().TrimEnd('.');
        if (normalized.Length <= 60)
        {
            return normalized;
        }

        return normalized[..60].Trim();
    }
}

using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal sealed class ControlledFixtureEmbeddingGenerator : IEmbeddingGenerator
{
    public string ModelName => "controlled-fixture-embedding-v1";

    public Task<float[]> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (ContainsAny(
            text,
            "NorthGate",
            "hardware security key",
            "company VPN",
            "VPN access"))
        {
            return Task.FromResult<float[]>([1, 0, 0, 0]);
        }

        if (ContainsAny(
            text,
            "OrbitHR",
            "annual leave",
            "planned absence"))
        {
            return Task.FromResult<float[]>([0, 1, 0, 0]);
        }

        if (ContainsAny(
            text,
            "LedgerBox",
            "meal reimbursement",
            "receipts"))
        {
            return Task.FromResult<float[]>([0, 0, 1, 0]);
        }

        return Task.FromResult<float[]>([0, 0, 0, 1]);
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term =>
            text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

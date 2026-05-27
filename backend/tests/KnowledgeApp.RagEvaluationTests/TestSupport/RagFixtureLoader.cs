using System.Text.Json;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal static class RagFixtureLoader
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static string FixturesRoot =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static IReadOnlyList<RagEvaluationCase> LoadCases()
    {
        string path = Path.Combine(FixturesRoot, "questions.json");

        string json = File.ReadAllText(path);

        RagEvaluationCase[]? cases =
            JsonSerializer.Deserialize<RagEvaluationCase[]>(
                json,
                SerializerOptions);

        return cases
            ?? throw new InvalidOperationException(
                "RAG evaluation questions fixture could not be loaded.");
    }

    public static IReadOnlyDictionary<string, string> LoadDocuments()
    {
        string documentsPath = Path.Combine(FixturesRoot, "Documents");

        return Directory.EnumerateFiles(documentsPath, "*.txt")
            .ToDictionary(
                path => Path.GetFileName(path),
                File.ReadAllText,
                StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<RagEvaluationCase> PositiveCases()
    {
        return LoadCases()
            .Where(testCase => !testCase.ExpectsNoContext)
            .ToArray();
    }

    public static RagEvaluationCase NoContextCase()
    {
        return LoadCases()
            .Single(testCase => testCase.ExpectsNoContext);
    }
}

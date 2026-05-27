namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal sealed record RagEvaluationCase(
    string Id,
    string Question,
    string? ExpectedDocument,
    string[] RequiredContextTerms,
    string[] RequiredAnswerTerms,
    string[] ForbiddenTerms,
    bool ExpectsNoContext);

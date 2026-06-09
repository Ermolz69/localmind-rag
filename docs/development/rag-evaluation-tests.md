# RAG Evaluation Tests

`KnowledgeApp.RagEvaluationTests` contains business-level tests for the local RAG pipeline.

These tests are different from standard API integration tests. They do not only verify that endpoints return successful HTTP responses. They verify that controlled questions retrieve the expected local source documents, that the generated RAG context contains required evidence terms, and that chat answers do not use unsupported context.

## Purpose

The evaluation suite protects the following behavior:

- semantic search returns the expected document for a known question;
- retrieved RAG context contains required terms from the expected source;
- retrieved RAG context excludes terms from unrelated fixture documents;
- chat answers are grounded in retrieved context;
- unrelated questions return the no-context fallback even when fixture documents exist.

This is intended to catch obvious retrieval and grounding regressions.

## Controlled fixtures

The test project uses small local text documents stored under:

```text
backend/tests/KnowledgeApp.RagEvaluationTests/Fixtures/Documents/
```

Current fixture documents:

```text
vpn-access.txt
leave-policy.txt
expense-policy.txt
```

Expected questions and required terms are stored in:

```text
backend/tests/KnowledgeApp.RagEvaluationTests/Fixtures/questions.json
```

The tests use a deterministic fixture embedding generator instead of a real local model. This keeps the suite fast, stable and independent of llama.cpp or downloaded embedding models.

The controlled embedding mapping is topic-based:

```text
VPN question/document      -> [1, 0, 0, 0]
Leave question/document    -> [0, 1, 0, 0]
Expense question/document  -> [0, 0, 1, 0]
Unknown question           -> [0, 0, 0, 1]
```

This allows the tests to validate the RAG pipeline with predictable source ranking.

## RAG retrieval guardrails

The RAG context builder receives hybrid retrieval results from vector search plus SQLite FTS/BM25. `Rag:MinimumSourceScore` still gates vector-only candidates, while keyword candidates must be high-ranked and contain enough specific query-term overlap before entering chat context.

The evaluation test factory sets:

```text
Rag:MinimumSourceScore = 0.8
```

This means that unrelated fixture documents with low vector similarity are not passed into the chat context just because BM25 matched broad words.

The normal semantic search endpoint can still return ranked hybrid search results. The stricter guardrails apply to RAG context construction so chat answers are not generated from weak or unrelated sources.

## Test groups

The project contains four groups of tests:

```text
RetrievalQualityTests.cs
RetrievedContextTests.cs
ChatGroundingTests.cs
NoRelevantContextTests.cs
```

### Retrieval quality

Verifies that semantic search ranks the expected fixture document first.

Example:

```text
"What is required for an employee to connect through the company VPN?"
-> vpn-access.txt
```

### Retrieved context

Verifies that the RAG context contains required evidence terms from the expected document and excludes unrelated fixture terms.

Example:

```text
VPN context contains:
- NorthGate
- hardware security key

VPN context does not contain:
- OrbitHR
- LedgerBox
```

### Chat grounding

Verifies that chat answers use the retrieved source document and include required answer terms.

### No relevant context

Verifies that an unrelated question returns the fallback response even when the fixture corpus is already populated.

Expected fallback:

```text
No relevant local sources were found for this question.
```

## Run locally

Run only the RAG evaluation suite:

```powershell
dotnet test backend/tests/KnowledgeApp.RagEvaluationTests/KnowledgeApp.RagEvaluationTests.csproj
```

Run a focused no-context check:

```powershell
dotnet test backend/tests/KnowledgeApp.RagEvaluationTests/KnowledgeApp.RagEvaluationTests.csproj --filter "NoRelevantContext"
```

Run the full local check pipeline:

```powershell
pnpm check
```

## Adding a new evaluation case

To add a new RAG evaluation case:

1. Add a small fixture document under:

   ```text
   backend/tests/KnowledgeApp.RagEvaluationTests/Fixtures/Documents/
   ```

2. Add a case to:

   ```text
   backend/tests/KnowledgeApp.RagEvaluationTests/Fixtures/questions.json
   ```

3. Include:
   - `id`;
   - `question`;
   - `expectedDocument`;
   - `requiredContextTerms`;
   - `requiredAnswerTerms`;
   - `forbiddenTerms`;
   - `expectsNoContext`.

4. Update `ControlledFixtureEmbeddingGenerator` if the new case introduces a new topic.

Keep fixtures small and deterministic. These tests are not intended to benchmark a real embedding model; they are intended to protect RAG pipeline behavior.

# Streaming Chat API

The LocalMind RAG backend supports streaming chat responses using **Server-Sent Events (SSE)**. This allows the frontend to display generated tokens progressively as they are produced by the local AI runtime.

## Endpoint

`POST /api/v1/chats/{conversationId}/messages/stream`

### Request Body

Standard `ChatMessageRequest`:

```json
{
  "content": "What is the capital of France?"
}
```

### Response Format

The endpoint returns a `text/event-stream`. Each event contains a JSON-serialized `RagAnswerChunkDto`.

```typescript
type RagAnswerChunkDto = {
  text: string;
  sources?: RagSourceDto[];
};
```

- **First chunk:** Typically contains the first token and the list of `sources` (if any relevant sources were found).
- **Subsequent chunks:** Contain generated text tokens.
- **Last chunk:** Standard SSE stream termination (client-side `reader.read()` returns `done: true`).

## Frontend Implementation Example (TypeScript)

The following example uses the `fetch` API and `ReadableStream` to parse the SSE events.

```typescript
async function* streamChat(conversationId: string, question: string) {
  const response = await fetch(`/api/v1/chats/${conversationId}/messages/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ content: question }),
  });

  if (!response.ok) {
    throw new Error(`Chat stream failed: ${response.statusText}`);
  }

  const reader = response.body?.getReader();
  if (!reader) return;

  const decoder = new TextDecoder();
  let buffer = '';

  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      
      const lines = buffer.split('\n');
      buffer = lines.pop() || ''; // keep partial line in buffer

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const json = line.substring(6).trim();
          if (json === '[DONE]') break;
          
          try {
            const chunk: RagAnswerChunkDto = JSON.parse(json);
            yield chunk;
          } catch (e) {
            console.error('Failed to parse SSE chunk', e);
          }
        }
      }
    }
  } finally {
    reader.releaseLock();
  }
}
```

## Error Handling

If an error occurs before the stream starts, the endpoint returns a standard `ApiResponse<object?>` JSON envelope with a `400 Bad Request` or `404 Not Found` status code.

If an error occurs *during* streaming (mid-stream), the backend will yield a standard `ApiResponse` failure envelope as an SSE chunk and close the connection:

```json
data: {"success":false,"data":null,"error":{"code":"INTERNAL_SERVER_ERROR","message":"Error message details...","details":[]},"metadata":{"timestamp":"2026-05-29T10:27:00Z","requestId":"..."}}
```

## Cancellation

Streaming can be cancelled by the client by aborting the fetch request or closing the readable stream reader. The backend will catch the `OperationCanceledException` and save whatever has been generated so far to the local database, ensuring the conversation history remains consistent.

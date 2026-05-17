using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Chats;

public sealed class ChatRequestValidator
{
    public void Validate(CreateConversationRequest request)
    {
        ValidateTitle(request.Title);
    }

    public void Validate(UpdateConversationRequest request)
    {
        ValidateTitle(request.Title);
    }

    public void Validate(ChatMessageRequest request)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            errors["content"] = ["Chat message content is required."];
        }
        else if (request.Content.Length > 20_000)
        {
            errors["content"] = ["Chat message content must be 20000 characters or less."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("chats.validationFailed", "Chat request is invalid.", errors);
        }
    }

    private static void ValidateTitle(string title)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Chat title is required."];
        }
        else if (title.Length > 200)
        {
            errors["title"] = ["Chat title must be 200 characters or less."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("chats.validationFailed", "Chat request is invalid.", errors);
        }
    }
}

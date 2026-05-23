using KnowledgeApp.Application.Common.Errors;
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
            errors["content"] = [ErrorMessages.Chats.ContentRequired];
        }
        else if (request.Content.Length > 20_000)
        {
            errors["content"] = [ErrorMessages.Chats.ContentTooLong];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(ErrorCodes.Chats.ValidationFailed, ErrorMessages.Chats.RequestInvalid, errors);
        }
    }

    private static void ValidateTitle(string title)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = [ErrorMessages.Chats.TitleRequired];
        }
        else if (title.Length > 200)
        {
            errors["title"] = [ErrorMessages.Chats.TitleTooLong];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(ErrorCodes.Chats.ValidationFailed, ErrorMessages.Chats.RequestInvalid, errors);
        }
    }
}

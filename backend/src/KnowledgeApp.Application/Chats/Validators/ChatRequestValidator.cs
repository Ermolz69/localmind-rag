using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Chats;

public sealed class ChatRequestValidator
{
    public Result Validate(CreateConversationRequest request)
    {
        return ValidateTitle(request.Title);
    }

    public Result Validate(UpdateConversationRequest request)
    {
        return ValidateTitle(request.Title);
    }

    public Result Validate(ChatMessageRequest request)
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
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Chats.ValidationFailed,
                ErrorMessages.Chats.RequestInvalid,
                errors));
        }

        return Result.Success();
    }

    private static Result ValidateTitle(string title)
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
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Chats.ValidationFailed,
                ErrorMessages.Chats.RequestInvalid,
                errors));
        }

        return Result.Success();
    }
}

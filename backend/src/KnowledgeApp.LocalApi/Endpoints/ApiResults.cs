using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ApiResults
{
    public static Ok<ApiResponse<T>> Ok<T>(T data, HttpContext context)
    {
        return TypedResults.Ok(ApiResponse.Success(data, context.TraceIdentifier));
    }

    public static Created<ApiResponse<T>> Created<T>(string location, T data, HttpContext context)
    {
        return TypedResults.Created(location, ApiResponse.Success(data, context.TraceIdentifier));
    }

    public static Accepted<ApiResponse<T>> Accepted<T>(string? location, T data, HttpContext context)
    {
        return TypedResults.Accepted(location, ApiResponse.Success(data, context.TraceIdentifier));
    }

    public static Ok<ApiResponse<object?>> Empty(HttpContext context)
    {
        return TypedResults.Ok(ApiResponse.Success<object?>(null, context.TraceIdentifier));
    }

    public static IResult Failure(ApplicationError error, HttpContext context)
    {
        ApiResponse<object?> response = ApiResponse.Failure(
            error.Code,
            error.Message,
            context.TraceIdentifier,
            error.Details);

        return error.Type switch
        {
            ErrorType.Validation => TypedResults.BadRequest(response),
            ErrorType.Unauthorized => TypedResults.Json(response, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => TypedResults.Json(response, statusCode: StatusCodes.Status403Forbidden),
            ErrorType.NotFound => TypedResults.NotFound(response),
            ErrorType.Conflict => TypedResults.Conflict(response),
            ErrorType.UnsupportedMedia => TypedResults.Json(response, statusCode: StatusCodes.Status415UnsupportedMediaType),
            ErrorType.Unprocessable => TypedResults.UnprocessableEntity(response),
            ErrorType.ExternalDependency => TypedResults.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable),
            ErrorType.NotImplemented => TypedResults.Json(response, statusCode: StatusCodes.Status501NotImplemented),
            _ => TypedResults.Json(response, statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    public static IResult ToApiResult<T>(this Result<T> result, HttpContext context)
    {
        return result.IsSuccess
            ? Ok(result.Value!, context)
            : Failure(result.Error!, context);
    }

    public static IResult ToCreatedApiResult<T>(this Result<T> result, HttpContext context, Func<T, string> locationFactory)
    {
        return result.IsSuccess
            ? Created(locationFactory(result.Value!), result.Value!, context)
            : Failure(result.Error!, context);
    }

    public static IResult ToAcceptedApiResult<T>(this Result<T> result, HttpContext context, Func<T, string?> locationFactory)
    {
        return result.IsSuccess
            ? Accepted(locationFactory(result.Value!), result.Value!, context)
            : Failure(result.Error!, context);
    }

    public static IResult ToApiResult(this Result result, HttpContext context)
    {
        return result.IsSuccess ? Empty(context) : Failure(result.Error!, context);
    }
}

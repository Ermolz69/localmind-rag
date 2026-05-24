using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Bootstrap.ProblemDetails;

public sealed class AppExceptionHandler(
    IHostEnvironment environment,
    ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ApiExceptionEnvelope envelope = CreateEnvelope(httpContext, exception);
        if (envelope.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            logger.LogInformation(exception, "Handled API exception: {Code}", envelope.Response.Error?.Code);
        }

        httpContext.Response.StatusCode = envelope.StatusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(envelope.Response, cancellationToken);
        return true;
    }

    private ApiExceptionEnvelope CreateEnvelope(HttpContext httpContext, Exception exception)
    {
        return exception switch
        {
            ValidationAppException validationException => CreateValidationEnvelope(httpContext, validationException),
            NotFoundAppException appException => Create(StatusCodes.Status404NotFound, appException.Code, appException.Message, httpContext),
            ConflictAppException appException => Create(StatusCodes.Status409Conflict, appException.Code, appException.Message, httpContext),
            UnsupportedFileAppException appException => Create(StatusCodes.Status415UnsupportedMediaType, appException.Code, appException.Message, httpContext),
            ExternalDependencyAppException appException => Create(StatusCodes.Status503ServiceUnavailable, appException.Code, appException.Message, httpContext),
            ArgumentException argumentException => Create(StatusCodes.Status400BadRequest, ErrorCodes.RequestInvalid, argumentException.Message, httpContext),
            _ => Create(
                StatusCodes.Status500InternalServerError,
                ErrorCodes.Unexpected,
                environment.IsDevelopment() ? ErrorMessages.UnexpectedDevelopment : ErrorMessages.UnexpectedProduction,
                httpContext),
        };
    }

    private static ApiExceptionEnvelope Create(int statusCode, string code, string message, HttpContext httpContext)
    {
        return new ApiExceptionEnvelope(
            statusCode,
            ApiResponse.Failure(code, message, httpContext.TraceIdentifier));
    }

    private static ApiExceptionEnvelope CreateValidationEnvelope(HttpContext httpContext, ValidationAppException exception)
    {
        ApiErrorDetail[] details = exception.Errors
            .SelectMany(error => error.Value.Select(message => new ApiErrorDetail(error.Key, message)))
            .ToArray();

        return new ApiExceptionEnvelope(
            StatusCodes.Status400BadRequest,
            ApiResponse.Failure(exception.Code, exception.Message, httpContext.TraceIdentifier, details));
    }

    private sealed record ApiExceptionEnvelope(int StatusCode, ApiResponse<object?> Response);
}

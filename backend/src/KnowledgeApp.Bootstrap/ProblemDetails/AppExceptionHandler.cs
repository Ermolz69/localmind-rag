using KnowledgeApp.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace KnowledgeApp.Bootstrap.ProblemDetails;

public sealed class AppExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        MvcProblemDetails? problemDetails = CreateProblemDetails(httpContext, exception);
        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled API exception.");
        }
        else
        {
            logger.LogInformation(exception, "Handled API exception: {Code}", problemDetails.Extensions["code"]);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }

    private MvcProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        string? traceId = httpContext.TraceIdentifier;
        MvcProblemDetails? problem = exception switch
        {
            ValidationAppException validationException => CreateValidationProblem(validationException),
            NotFoundAppException appException => Create(StatusCodes.Status404NotFound, "Resource was not found.", appException.Message, appException.Code),
            ConflictAppException appException => Create(StatusCodes.Status409Conflict, "Request conflicts with current state.", appException.Message, appException.Code),
            UnsupportedFileAppException appException => Create(StatusCodes.Status415UnsupportedMediaType, "Unsupported file.", appException.Message, appException.Code),
            ExternalDependencyAppException appException => Create(StatusCodes.Status503ServiceUnavailable, "External dependency is unavailable.", appException.Message, appException.Code),
            ArgumentException argumentException => Create(StatusCodes.Status400BadRequest, "Invalid request.", argumentException.Message, "request.invalid"),
            _ => Create(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                environment.IsDevelopment() ? exception.Message : "The server encountered an unexpected error.",
                "errors.unexpected"),
        };

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = traceId;
        return problem;
    }

    private static MvcProblemDetails Create(int status, string title, string detail, string code)
    {
        MvcProblemDetails? problem = new MvcProblemDetails
        {
            Detail = detail,
            Status = status,
            Title = title,
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationAppException exception)
    {
        ValidationProblemDetails? problem = new ValidationProblemDetails(exception.Errors.ToDictionary(x => x.Key, x => x.Value))
        {
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
        };
        problem.Extensions["code"] = exception.Code;
        return problem;
    }
}

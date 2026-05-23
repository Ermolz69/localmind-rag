using KnowledgeApp.Application.Common.Errors;
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
            NotFoundAppException appException => Create(StatusCodes.Status404NotFound, ProblemDetailsTitles.NotFound, appException.Message, appException.Code),
            ConflictAppException appException => Create(StatusCodes.Status409Conflict, ProblemDetailsTitles.Conflict, appException.Message, appException.Code),
            UnsupportedFileAppException appException => Create(StatusCodes.Status415UnsupportedMediaType, ProblemDetailsTitles.UnsupportedFile, appException.Message, appException.Code),
            ExternalDependencyAppException appException => Create(StatusCodes.Status503ServiceUnavailable, ProblemDetailsTitles.ExternalDependencyUnavailable, appException.Message, appException.Code),
            ArgumentException argumentException => Create(StatusCodes.Status400BadRequest, ProblemDetailsTitles.InvalidRequest, argumentException.Message, ErrorCodes.RequestInvalid),
            _ => Create(
                StatusCodes.Status500InternalServerError,
                ProblemDetailsTitles.Unexpected,
                environment.IsDevelopment() ? exception.Message : ErrorMessages.UnexpectedProduction,
                ErrorCodes.Unexpected),
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
            Title = ProblemDetailsTitles.ValidationFailed,
        };
        problem.Extensions["code"] = exception.Code;
        return problem;
    }
}

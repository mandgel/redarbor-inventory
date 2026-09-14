using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Common.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);

        LogException(exception, problemDetails.Status);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static (int StatusCode, string Title, string Detail) MapException(
        Exception exception)
    {
        return exception switch
        {
            InsufficientStockException =>
                (StatusCodes.Status409Conflict,
                    "Conflict",
                    exception.Message),
            IdempotencyConflictException =>
                (StatusCodes.Status409Conflict,
                    "Conflict",
                    exception.Message),
            ProductNotFoundException =>
                (StatusCodes.Status404NotFound,
                    "Not Found",
                    exception.Message),
            ArgumentException =>
                (StatusCodes.Status400BadRequest,
                    "Bad Request",
                    exception.Message),
            _ =>
                (StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred.")
        };
    }

    private void LogException(Exception exception, int? statusCode)
    {
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing the request.");

            return;
        }

        _logger.LogInformation(
            "A known business error occurred: {Message}",
            exception.Message);
    }
}

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EHR.ServiceDefaults;

public sealed class EhrExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EhrExceptionHandler> _logger;

    public EhrExceptionHandler(IHostEnvironment environment, ILogger<EhrExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = GetStatusCode(exception);
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(exception, "Handled request exception. TraceId: {TraceId}", traceId);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = GetDetail(exception, statusCode),
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["exceptionType"] = exception.GetType().Name;

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exceptionMessage"] = exception.Message;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static int GetStatusCode(Exception exception)
    {
        var exceptionTypeName = exception.GetType().Name;

        if (exception is UnauthorizedAccessException)
        {
            return StatusCodes.Status403Forbidden;
        }

        if (exception is ArgumentException || (exception is InvalidOperationException && exceptionTypeName != "TenantNotFoundException"))
        {
            return StatusCodes.Status400BadRequest;
        }

        return exceptionTypeName switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "TenantNotFoundException" => StatusCodes.Status404NotFound,
            "DuplicateStaffUserEmailException" => StatusCodes.Status409Conflict,
            "DbUpdateConcurrencyException" => StatusCodes.Status409Conflict,
            "DbUpdateException" => StatusCodes.Status503ServiceUnavailable,
            "PostgresException" => StatusCodes.Status503ServiceUnavailable,
            "NpgsqlException" => StatusCodes.Status503ServiceUnavailable,
            "KafkaException" => StatusCodes.Status503ServiceUnavailable,
            "ProduceException`2" => StatusCodes.Status503ServiceUnavailable,
            "TenantAccessDeniedException" => StatusCodes.Status403Forbidden,
            "OperationCanceledException" => StatusCodes.Status499ClientClosedRequest,
            "TaskCanceledException" => StatusCodes.Status499ClientClosedRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad request",
            StatusCodes.Status404NotFound => "Not found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status499ClientClosedRequest => "Request canceled",
            StatusCodes.Status503ServiceUnavailable => "Service unavailable",
            _ => "Unexpected error"
        };

    private string GetDetail(Exception exception, int statusCode)
    {
        if (_environment.IsDevelopment() || statusCode < StatusCodes.Status500InternalServerError)
        {
            return exception.Message;
        }

        return "An unexpected error occurred while processing the request.";
    }
}

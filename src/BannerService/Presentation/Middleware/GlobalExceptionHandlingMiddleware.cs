namespace BannerService.Presentation.Middleware;

using Microsoft.Extensions.Logging;
using System.Text.Json;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                code = GetErrorCode(exception),
                message = GetErrorMessage(exception)
            }
        };

        context.Response.StatusCode = GetStatusCode(exception);

        return context.Response.WriteAsJsonAsync(response);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => "VALIDATION_ERROR",
            InvalidOperationException => "CONFLICT",
            UnauthorizedAccessException => "FORBIDDEN",
            _ => "INTERNAL_ERROR"
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException or InvalidOperationException => exception.Message,
            UnauthorizedAccessException => "Access denied",
            _ => "An unexpected error occurred"
        };
    }
}

using EventsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EventsApi.Presentation.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleException(httpContext, ex);
        }
    }

    private async Task HandleException(HttpContext httpContext, Exception ex)
    {
        _logger.LogError(ex, "Unhandled exception. Method={Method}, Path={Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        if (httpContext.Response.HasStarted) return;

        var statusCode = MapStatusCode(ex);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(new
        {
            Status = statusCode,
            Detail = ex.Message
        });
    }

    private static int MapStatusCode(Exception ex) => ex switch
    {
        // 403 — нет прав
        ForbiddenOperationException => StatusCodes.Status403Forbidden,

        // 400 — событие в прошлом / валидация
        EventAlreadyStartedException => StatusCodes.Status400BadRequest,
        ValidationException => StatusCodes.Status400BadRequest,
        ArgumentException => StatusCodes.Status400BadRequest,
        InvalidOperationException => StatusCodes.Status400BadRequest,

        // 409 — конфликты (лимит, нет мест, повторная отмена)
        BookingLimitExceededException => StatusCodes.Status409Conflict,
        NoAvailableSeatsException => StatusCodes.Status409Conflict,
        BookingAlreadyCancelledException => StatusCodes.Status409Conflict,

        // 404
        NotFoundException => StatusCodes.Status404NotFound,
        KeyNotFoundException => StatusCodes.Status404NotFound,

        // 500
        _ => StatusCodes.Status500InternalServerError
    };
}
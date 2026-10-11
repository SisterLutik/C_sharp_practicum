using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UserService.Domain.Exceptions;

namespace UserService.Presentation.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        _logger.LogError(
            ex,
            "Unhandled exception. Method={Method}, Path={Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        if (httpContext.Response.HasStarted)
            return;

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
        // ✅ 401 — неверные учётные данные
        InvalidCredentialsException => StatusCodes.Status401Unauthorized,

        // ✅ 404 — ресурс не найден
        NotFoundException => StatusCodes.Status404NotFound,

        // ✅ 400 — ошибка валидации
        ValidationException => StatusCodes.Status400BadRequest,
        ArgumentException => StatusCodes.Status400BadRequest,
        InvalidOperationException => StatusCodes.Status400BadRequest,

        // 500 — всё остальное
        _ => StatusCodes.Status500InternalServerError
    };
}
using System.Net;
using System.Text.Json;
using LoanManagementSystem.Application.Common.Exceptions;
using LoanManagementSystem.Domain.Common;

namespace LoanManagementSystem.Api.Middleware;

/// <summary>
/// Maps exception types to HTTP status codes so controllers don't need
/// try/catch blocks: a DomainException (business rule violation, e.g.
/// "loan already fully paid") becomes 400, a NotFoundException becomes
/// 404, and anything else is an unexpected 500 with details hidden from
/// the response body.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var (statusCode, message) = ex switch
            {
                AuthenticationFailedException => (HttpStatusCode.Unauthorized, ex.Message),
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                DomainException => (HttpStatusCode.BadRequest, ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred."),
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseAppExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}

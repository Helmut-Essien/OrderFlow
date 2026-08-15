using System.Net;
using System.Text.Json;
using FluentValidation;
using OrderFlow.Application.Common.Exceptions;

namespace OrderFlow.Api.Middleware;

/// <summary>
/// Maps <see cref="AppException"/> and FluentValidation failures to camelCase JSON. Unhandled exceptions become 500 without leaking internals.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>Catches the rest of the pipeline and writes a camelCase error body.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        var (status, message, code, errors) = exception switch
        {
            UnauthorizedAppException ex => (HttpStatusCode.Unauthorized, ex.Message, (string?)null, (object?)null),
            ConcurrencyAppException ex => (HttpStatusCode.Conflict, ex.Message, "concurrency", null),
            ConflictAppException ex => (HttpStatusCode.Conflict, ex.Message, null, null),
            NotFoundAppException ex => (HttpStatusCode.NotFound, ex.Message, null, null),
            ForbiddenAppException ex => (HttpStatusCode.Forbidden, ex.Message, null, null),
            ValidationException ex => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                null,
                ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", null, (object?)null)
        };

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogInformation(exception, "Request failed with {StatusCode}", (int)status);

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            message,
            code,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

using System.Net;
using System.Text.Json;
using FluentValidation;
using OrderFlow.Application.Common.Exceptions;

namespace OrderFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
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
        var (status, message, errors) = exception switch
        {
            UnauthorizedAppException ex => (HttpStatusCode.Unauthorized, ex.Message, (object?)null),
            ConflictAppException ex => (HttpStatusCode.Conflict, ex.Message, null),
            NotFoundAppException ex => (HttpStatusCode.NotFound, ex.Message, null),
            ForbiddenAppException ex => (HttpStatusCode.Forbidden, ex.Message, null),
            ValidationException ex => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null)
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
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

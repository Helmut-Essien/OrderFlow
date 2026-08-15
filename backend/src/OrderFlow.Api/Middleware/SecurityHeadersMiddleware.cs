namespace OrderFlow.Api.Middleware;

/// <summary>
/// Adds conservative browser headers on API responses. HSTS is applied separately via <c>UseHsts</c> in Production.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cache-Control"] = "no-store";
        return next(context);
    }
}

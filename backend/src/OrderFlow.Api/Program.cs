using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Hosting;
using OrderFlow.Api.Logging;
using OrderFlow.Api.Middleware;
using OrderFlow.Application;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options =>
    {
        // JSON auth/product payloads are small; keep a tight bound until file uploads exist.
        options.Limits.MaxRequestBodySize = 128 * 1024;
    });

    builder.Host.UseSerilog((context, logger) =>
    {
        logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Destructure.With<SecretRedactingPolicy>();

        if (context.HostingEnvironment.IsEnvironment("Testing"))
        {
            // Integration tests use Testcontainers; skip host file/console sinks and keep noise down.
            logger.MinimumLevel.Warning();
            return;
        }

        logger.WriteTo.Console();

        // File sink is local-dev convenience. Production hosts should collect stdout (12-factor).
        if (context.HostingEnvironment.IsDevelopment())
        {
            logger.WriteTo.File(
                "logs/orderflow-.log",
                rollingInterval: RollingInterval.Day);
        }
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddHealthChecks();
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Trust the terminating reverse proxy; otherwise X-Forwarded-For never reaches the rate limiter.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"message":"Too many requests. Try again shortly."}""",
                cancellationToken);
        };
        options.AddPolicy("auth", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                ip,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = CorsOrigins.Resolve(builder.Configuration);
            if (origins.Length == 0)
            {
                // Same-origin SPA behind a reverse proxy: do not allow any browser cross-origin calls.
                policy.SetIsOriginAllowed(_ => false);
                return;
            }

            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // WebApplicationFactory tests apply migrations against Testcontainers themselves.
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    if (app.Environment.IsProduction())
    {
        app.UseForwardedHeaders();
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    // CORS must wrap exception handling so 400/401/409 JSON still gets Allow-Origin (ng serve is cross-origin).
    app.UseCors("Frontend");
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapControllers();

    app.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point partial so API tests can host this assembly with <c>WebApplicationFactory</c>.</summary>
public partial class Program;

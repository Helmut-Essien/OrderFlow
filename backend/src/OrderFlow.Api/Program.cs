using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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

        if (context.HostingEnvironment.IsDevelopment() || context.HostingEnvironment.IsProduction())
        {
            logger.WriteTo.File(
                "logs/orderflow-.log",
                rollingInterval: RollingInterval.Day);
        }
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = 20;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:4200"];

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

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseCors("Frontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();

    app.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point partial so API tests can host this assembly with <c>WebApplicationFactory</c>.</summary>
public partial class Program;

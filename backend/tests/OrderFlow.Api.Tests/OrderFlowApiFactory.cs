using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace OrderFlow.Api.Tests;

public class OrderFlowApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orderflow_test")
        .WithUsername("orderflow")
        .WithPassword("orderflow_dev")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            services.RemoveAll<IPlatformLicenseClient>();
            services.AddSingleton<IPlatformLicenseClient, StubPlatformLicenseClient>();
        });
    }

    private sealed class StubPlatformLicenseClient : IPlatformLicenseClient
    {
        public Task<LicenseValidationResult> ValidateAsync(
            string licenseKey,
            CancellationToken cancellationToken = default)
        {
            if (licenseKey.StartsWith("ORDERFLOW-DEVK-", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new LicenseValidationResult(
                    true,
                    "Growth",
                    DateTime.UtcNow.AddYears(1),
                    null));
            }

            if (licenseKey.StartsWith("ORDERFLOW-STARTER-", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new LicenseValidationResult(
                    true,
                    "Starter",
                    DateTime.UtcNow.AddYears(1),
                    null));
            }

            return Task.FromResult(new LicenseValidationResult(false, null, null, "Invalid license key."));
        }
    }
}

[CollectionDefinition("OrderFlowApi")]
public sealed class OrderFlowApiCollection : ICollectionFixture<OrderFlowApiFactory>;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Tests;

public class OrderFlowApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5433;Database=orderflow_db;Username=orderflow;Password=orderflow_dev",
                ["Platform:BaseUrl"] = "http://platform.test",
                ["Platform:IntegrationKey"] = "test-key",
                ["Platform:ServiceCode"] = "ORDERFLOW"
            });
        });

        builder.ConfigureServices(services =>
        {
            RemoveEntityFramework(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("orderflow-tests"));

            services.AddSingleton<IPlatformLicenseClient, StubPlatformLicenseClient>();
        });
    }

    private static void RemoveEntityFramework(IServiceCollection services)
    {
        var toRemove = services.Where(descriptor =>
            IsEfType(descriptor.ServiceType) ||
            IsEfType(descriptor.ImplementationType) ||
            descriptor.ImplementationInstance is IDbContextOptionsConfiguration<AppDbContext>).ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }

    private static bool IsEfType(Type? type)
    {
        if (type is null)
            return false;

        if (type == typeof(AppDbContext) || type == typeof(DbContextOptions<AppDbContext>))
            return true;

        var name = type.FullName ?? type.Name;
        return name.Contains("EntityFrameworkCore", StringComparison.Ordinal)
            || name.Contains("Npgsql", StringComparison.Ordinal);
    }

    private sealed class StubPlatformLicenseClient : IPlatformLicenseClient
    {
        public Task<LicenseValidationResult> ValidateAsync(
            string licenseKey,
            CancellationToken cancellationToken = default)
        {
            if (licenseKey == "ORDERFLOW-DEVK-TEST")
            {
                return Task.FromResult(new LicenseValidationResult(
                    true,
                    "Growth",
                    DateTime.UtcNow.AddYears(1),
                    null));
            }

            return Task.FromResult(new LicenseValidationResult(false, null, null, "Invalid license key."));
        }
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Hosting;
using OrderFlow.Infrastructure.Identity;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Infrastructure.Persistence.Repositories;
using OrderFlow.Infrastructure.Platform;

namespace OrderFlow.Infrastructure;

/// <summary>Wires EF, JWT bearer, Data Protection, repositories, and the Platform HTTP client.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        StartupConfiguration.Validate(configuration, environment.EnvironmentName);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        AddDataProtectionKeys(services, configuration, environment);
        services.AddHttpContextAccessor();

        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ILicenseKeyProtector, LicenseKeyProtector>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PlatformOptions>(configuration.GetSection(PlatformOptions.SectionName));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerConfigurator>();

        services.AddAuthorization();

        var platform = configuration.GetSection(PlatformOptions.SectionName).Get<PlatformOptions>()
            ?? new PlatformOptions();

        services.AddHttpClient<IPlatformLicenseClient, PlatformLicenseClient>(client =>
        {
            client.BaseAddress = new Uri(platform.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }

    /// <summary>
    /// Persists Data Protection keys so encrypted license payloads survive process restarts.
    /// Testing keeps ephemeral keys so the suite does not write to disk.
    /// </summary>
    private static void AddDataProtectionKeys(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection();
        if (environment.IsEnvironment("Testing"))
            return;

        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
            keysPath = Path.Combine(environment.ContentRootPath, "dataprotection-keys");

        Directory.CreateDirectory(keysPath);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }
}

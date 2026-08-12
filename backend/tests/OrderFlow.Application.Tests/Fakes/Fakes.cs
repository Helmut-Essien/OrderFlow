using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Tests.Fakes;

internal sealed class FakeShopRepository : IShopRepository
{
    public List<Shop> Items { get; } = [];

    public Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<Shop?> GetByLicenseLookupHashAsync(string licenseLookupHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(s => s.LicenseLookupHash == licenseLookupHash));

    public void Add(Shop shop) => Items.Add(shop);
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(u => u.Email == email));

    public void Add(User user) => Items.Add(user);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakePlatformLicenseClient : IPlatformLicenseClient
{
    public LicenseValidationResult Result { get; set; } = new(true, "Growth", DateTime.UtcNow.AddYears(1), null);

    public string? LastLicenseKey { get; private set; }

    public Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        LastLicenseKey = licenseKey;
        return Task.FromResult(Result);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == $"hash:{password}";
}

internal sealed class FakeLicenseKeyProtector : ILicenseKeyProtector
{
    public string Protect(string licenseKey) => $"p:{licenseKey}";

    public string Unprotect(string protectedLicenseKey) => protectedLicenseKey[2..];
}

internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public JwtTokenResult Create(User user, Shop shop, PlanQuota plan)
        => new("test-token", DateTime.UtcNow.AddHours(8));
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; }

    public string? ShopId { get; set; }

    public string? Role { get; set; }

    public bool IsAuthenticated { get; set; }
}

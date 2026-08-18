using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Auth;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Orders;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Tests.Fakes;

internal sealed class FakeShopRepository : IShopRepository
{
    public List<Shop> Items { get; } = [];

    public Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<Shop?> GetByLicenseLookupHashAsync(string licenseLookupHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(s => s.LicenseLookupHash == licenseLookupHash));

    public void Add(Shop shop) => Items.Add(shop);

    public Task AcquirePlanCapLockAsync(string shopId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
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

    public bool IsInTransaction { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        IsInTransaction = true;
        try
        {
            await work(cancellationToken);
        }
        finally
        {
            IsInTransaction = false;
        }
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

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];

    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<Product?> GetBySkuAsync(string shopId, string sku, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(p => p.ShopId == shopId && p.Sku == sku));

    public Task<IReadOnlyList<Product>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => ids.Contains(p.Id)).ToList());

    public Task<ProductListResult> ListAsync(
        string shopId,
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> query = Items.Where(p => p.ShopId == shopId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLowerInvariant().Contains(term) || p.Sku.ToLowerInvariant().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category.Trim());

        var materialized = query.OrderBy(p => p.Name).ThenBy(p => p.Sku).ToList();
        var pageItems = materialized.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList();
        return Task.FromResult(new ProductListResult(pageItems, materialized.Count));
    }

    public Task<int> CountByShopAsync(string shopId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(p => p.ShopId == shopId && p.IsActive));

    public Task<IReadOnlyList<string>> ListCategoriesAsync(
        string shopId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(
            Items
                .Where(p => p.ShopId == shopId && !string.IsNullOrWhiteSpace(p.Category))
                .Select(p => p.Category!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList());

    public Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LowStockItemDto>>(
            Items.Where(p => p.ShopId == shopId && p.IsActive && p.IsLowStock)
                .OrderBy(p => p.Stock)
                .ThenBy(p => p.Name)
                .Take(50)
                .Select(p => new LowStockItemDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Stock = p.Stock,
                    LowStockThreshold = p.LowStockThreshold
                })
                .ToList());

    public void Add(Product product) => Items.Add(product);

    public Task<StockAdjustmentResult?> TryAdjustStockAsync(
        string productId,
        string shopId,
        long expectedVersion,
        int quantityDelta,
        CancellationToken cancellationToken = default)
    {
        var item = Items.FirstOrDefault(p => p.Id == productId && p.ShopId == shopId);
        if (item is null || item.Version != expectedVersion)
            return Task.FromResult<StockAdjustmentResult?>(null);

        var newStock = item.Stock + quantityDelta;
        if (newStock < 0 || newStock > ProductConstraints.MaxStock)
            return Task.FromResult<StockAdjustmentResult?>(null);

        item.ApplyStock(newStock, item.Version + 1);
        return Task.FromResult<StockAdjustmentResult?>(new StockAdjustmentResult(item.Stock, item.Version));
    }

    private static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        ShopId = product.ShopId,
        Name = product.Name,
        Sku = product.Sku,
        Category = product.Category,
        Price = product.Price,
        Stock = product.Stock,
        LowStockThreshold = product.LowStockThreshold,
        IsActive = product.IsActive,
        IsLowStock = product.IsLowStock,
        Version = product.Version,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}

internal sealed class FakeStockMovementRepository : IStockMovementRepository
{
    public List<StockMovement> Items { get; } = [];

    public void Add(StockMovement movement) => Items.Add(movement);
}

internal sealed class FakeOrderRepository : IOrderRepository
{
    public List<Order> Items { get; } = [];

    public Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

    public Task<Order?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<OrderListResult> ListAsync(
        string shopId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Order> query = Items.Where(o => o.ShopId == shopId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o =>
                o.CustomerName.ToLowerInvariant().Contains(term)
                || (o.CustomerPhone != null && o.CustomerPhone.ToLowerInvariant().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OrderFlow.Domain.Enums.OrderStatus>(status, ignoreCase: true, out var parsed)
            && Enum.GetNames<OrderFlow.Domain.Enums.OrderStatus>().Any(n => n.Equals(status, StringComparison.OrdinalIgnoreCase)))
        {
            query = query.Where(o => o.Status == parsed);
        }

        var materialized = query.OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).ToList();
        var pageItems = materialized.Skip((page - 1) * pageSize).Take(pageSize).Select(ToListDto).ToList();
        return Task.FromResult(new OrderListResult(pageItems, materialized.Count));
    }

    public Task<int> CountCreatedInRangeAsync(
        string shopId,
        DateTime monthStartUtc,
        DateTime monthEndUtc,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(o =>
            o.ShopId == shopId && o.CreatedAt >= monthStartUtc && o.CreatedAt < monthEndUtc));

    public Task<OrderDashboardStats> GetDashboardStatsAsync(
        string shopId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken cancellationToken = default)
    {
        var shopOrders = Items.Where(o => o.ShopId == shopId).ToList();
        var paidToday = shopOrders.Where(o =>
            o.PaidAt >= dayStartUtc
            && o.PaidAt < dayEndUtc
            && Order.CountsTowardTodaysSales(o.Status)).ToList();
        var pendingWhatsApp = shopOrders.Count(o =>
            o.Source == OrderFlow.Domain.Enums.OrderSource.WhatsApp
            && o.Status == OrderFlow.Domain.Enums.OrderStatus.Pending);
        var recent = shopOrders
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(10)
            .Select(o => new DashboardOrderDto
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToList();

        return Task.FromResult(new OrderDashboardStats(
            paidToday.Sum(o => o.TotalAmount),
            paidToday.Count,
            pendingWhatsApp,
            recent));
    }

    public void Add(Order order) => Items.Add(order);

    private static OrderListDto ToListDto(Order order) => new()
    {
        Id = order.Id,
        ShopId = order.ShopId,
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        Status = order.Status.ToString(),
        Source = order.Source.ToString(),
        NeedsClarification = order.NeedsClarification,
        TotalAmount = order.TotalAmount,
        LineCount = order.Lines.Count,
        Version = order.Version,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };
}

internal sealed class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; }

    public string? ShopId { get; set; }

    public string? Role { get; set; }

    public bool IsAuthenticated { get; set; }
}

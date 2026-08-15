using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Features.Products.AdjustStock;
using OrderFlow.Application.Features.Products.CreateProduct;
using OrderFlow.Application.Features.Products.UpdateProduct;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Tests;

public class ProductHandlerTests
{
    [Fact]
    public async Task Create_AddsProductAndOpeningStockMovement()
    {
        var (shops, products, movements, user) = SeedShop("Growth");
        var handler = new CreateProductCommandHandler(user, shops, products, movements, new FakeUnitOfWork());

        var result = await handler.Handle(
            new CreateProductCommand("Voltic Water 500ml", "volt-500", "Beverages", 3.50m, 48, 6),
            CancellationToken.None);

        Assert.Equal("VOLT-500", result.Sku);
        Assert.Equal(48, result.Stock);
        Assert.Equal(1, result.Version);
        Assert.False(result.IsLowStock);
        Assert.Single(products.Items);
        Assert.Single(movements.Items);
        Assert.Equal(48, movements.Items[0].QuantityDelta);
    }

    [Fact]
    public async Task Create_ThrowsForbidden_WhenPlanProductLimitReached()
    {
        var (shops, products, movements, user) = SeedShop("Starter");
        for (var i = 0; i < 50; i++)
        {
            products.Add(Product.Create(user.ShopId!, $"Item {i}", $"SKU-{i:000}", null, 1m, 1, 0));
        }

        var handler = new CreateProductCommandHandler(user, shops, products, movements, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            handler.Handle(new CreateProductCommand("Extra", "SKU-999", null, 1m, 0, 0), CancellationToken.None));

        Assert.Contains("50 products", ex.Message);
    }

    [Fact]
    public async Task Create_ThrowsConflict_WhenSkuExists()
    {
        var (shops, products, movements, user) = SeedShop("Growth");
        products.Add(Product.Create(user.ShopId!, "Existing", "VOLT-500", null, 1m, 1, 0));
        var handler = new CreateProductCommandHandler(user, shops, products, movements, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            handler.Handle(new CreateProductCommand("Other", "volt-500", null, 1m, 0, 0), CancellationToken.None));
    }

    [Fact]
    public async Task Update_ThrowsConcurrency_WhenVersionDoesNotMatch()
    {
        var (_, products, _, user) = SeedShop("Growth");
        var product = Product.Create(user.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        products.Add(product);

        var handler = new UpdateProductCommandHandler(user, products, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConcurrencyAppException>(() =>
            handler.Handle(
                new UpdateProductCommand(product.Id, "Voltic", "VOLT-500", null, 4m, 2, true, 99),
                CancellationToken.None));
    }

    [Fact]
    public async Task AdjustStock_IncrementsVersionAndRecordsMovement()
    {
        var (_, products, movements, user) = SeedShop("Growth");
        var product = Product.Create(user.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        products.Add(product);
        var handler = new AdjustStockCommandHandler(user, products, movements, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AdjustStockCommand(product.Id, -3, 1, "Sold a crate"),
            CancellationToken.None);

        Assert.Equal(7, result.Stock);
        Assert.Equal(2, result.Version);
        Assert.Single(movements.Items);
        Assert.Equal(-3, movements.Items[0].QuantityDelta);
        Assert.Equal(7, movements.Items[0].ResultingStock);
    }

    [Fact]
    public async Task AdjustStock_ThrowsConflict_WhenStockWouldGoNegative()
    {
        var (_, products, movements, user) = SeedShop("Growth");
        var product = Product.Create(user.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 1, 0);
        products.Add(product);
        var handler = new AdjustStockCommandHandler(user, products, movements, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ConflictAppException>(() =>
            handler.Handle(new AdjustStockCommand(product.Id, -5, 1, null), CancellationToken.None));

        Assert.Contains("below zero", ex.Message);
    }

    [Fact]
    public async Task AdjustStock_ThrowsConflict_WhenStockWouldExceedMaximum()
    {
        var (_, products, movements, user) = SeedShop("Growth");
        var product = Product.Create(user.ShopId!, "Voltic", "VOLT-500", null, 3.5m, ProductConstraints.MaxStock, 0);
        products.Add(product);
        var handler = new AdjustStockCommandHandler(user, products, movements, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ConflictAppException>(() =>
            handler.Handle(new AdjustStockCommand(product.Id, 1, 1, null), CancellationToken.None));

        Assert.Contains("exceed", ex.Message);
    }

    [Fact]
    public async Task Create_AllowsNewProduct_WhenInactiveProductFreesPlanSlot()
    {
        var (shops, products, movements, user) = SeedShop("Starter");
        for (var i = 0; i < 50; i++)
        {
            var item = Product.Create(user.ShopId!, $"Item {i}", $"SKU-{i:000}", null, 1m, 1, 0);
            products.Add(item);
        }

        products.Items[0].UpdateDetails(
            products.Items[0].Name,
            products.Items[0].Sku,
            products.Items[0].Category,
            products.Items[0].Price,
            products.Items[0].LowStockThreshold,
            isActive: false);

        var handler = new CreateProductCommandHandler(user, shops, products, movements, new FakeUnitOfWork());

        var result = await handler.Handle(
            new CreateProductCommand("Extra", "SKU-999", null, 1m, 0, 0),
            CancellationToken.None);

        Assert.Equal("SKU-999", result.Sku);
        Assert.Equal(51, products.Items.Count);
        Assert.Equal(50, products.Items.Count(p => p.IsActive));
    }

    private static (FakeShopRepository Shops, FakeProductRepository Products, FakeStockMovementRepository Movements, FakeCurrentUser User)
        SeedShop(string planName)
    {
        var shop = Shop.Create(
            "Makola Mart",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:key",
            planName,
            null,
            false);
        var shops = new FakeShopRepository();
        shops.Add(shop);

        var user = new FakeCurrentUser
        {
            IsAuthenticated = true,
            ShopId = shop.Id,
            UserId = "user_1",
            Role = "Owner"
        };

        return (shops, new FakeProductRepository(), new FakeStockMovementRepository(), user);
    }
}

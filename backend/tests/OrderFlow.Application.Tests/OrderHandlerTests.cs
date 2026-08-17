using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Features.Orders.ChangeOrderStatus;
using OrderFlow.Application.Features.Orders.CreateOrder;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Tests;

public class OrderHandlerTests
{
    [Fact]
    public async Task Create_Pending_DoesNotTouchStock()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        var handler = CreateHandler(seed);

        var result = await handler.Handle(
            new CreateOrderCommand("Ama Boateng", "0244000000", null, false, [new CreateOrderLineInput(product.Id, 3)]),
            CancellationToken.None);

        Assert.Equal("Pending", result.Status);
        Assert.Equal("Manual", result.Source);
        Assert.Equal(10.50m, result.TotalAmount);
        Assert.Equal(10, product.Stock);
        Assert.Empty(seed.Movements.Items);
        Assert.Single(seed.Orders.Items);
    }

    [Fact]
    public async Task Create_ConfirmImmediately_ReservesStock()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        var handler = CreateHandler(seed);

        var result = await handler.Handle(
            new CreateOrderCommand("Ama", null, null, true, [new CreateOrderLineInput(product.Id, 3)]),
            CancellationToken.None);

        Assert.Equal("Confirmed", result.Status);
        Assert.Equal(7, product.Stock);
        Assert.Single(seed.Movements.Items);
        Assert.Equal(StockMovementType.Reserve, seed.Movements.Items[0].Type);
        Assert.Equal(-3, seed.Movements.Items[0].QuantityDelta);
    }

    [Fact]
    public async Task Create_ThrowsForbidden_WhenMonthlyOrderLimitReached()
    {
        var seed = SeedShop("Starter");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        for (var i = 0; i < 300; i++)
        {
            seed.Orders.Add(Order.CreateManual(
                seed.User.ShopId!,
                "Customer",
                null,
                null,
                [new OrderLineDraft(product.Id, product.Name, product.Sku, 1, product.Price)],
                seed.User.UserId));
        }

        var handler = CreateHandler(seed);

        var ex = await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            handler.Handle(
                new CreateOrderCommand("Ama", null, null, false, [new CreateOrderLineInput(product.Id, 1)]),
                CancellationToken.None));

        Assert.Contains("300 orders", ex.Message);
    }

    [Fact]
    public async Task Create_ThrowsConflict_WhenProductIsInactive()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        product.UpdateDetails(product.Name, product.Sku, product.Category, product.Price, product.LowStockThreshold, false);
        seed.Products.Add(product);
        var handler = CreateHandler(seed);

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            handler.Handle(
                new CreateOrderCommand("Ama", null, null, false, [new CreateOrderLineInput(product.Id, 1)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Confirm_ThrowsConflict_WhenStockIsInsufficient()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 1, 0);
        seed.Products.Add(product);
        var create = CreateHandler(seed);
        var created = await create.Handle(
            new CreateOrderCommand("Ama", null, null, false, [new CreateOrderLineInput(product.Id, 5)]),
            CancellationToken.None);

        var change = new ChangeOrderStatusCommandHandler(
            seed.User, seed.Orders, seed.Products, seed.Movements, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<ConflictAppException>(() =>
            change.Handle(
                new ChangeOrderStatusCommand(created.Id, "Confirmed", created.Version),
                CancellationToken.None));

        Assert.Contains("Not enough stock", ex.Message);
        Assert.Equal(1, product.Stock);
    }

    [Fact]
    public async Task Pay_WritesDeductAuditWithoutChangingStock_ThenCancelReleases()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        var create = CreateHandler(seed);
        var created = await create.Handle(
            new CreateOrderCommand("Ama", null, null, true, [new CreateOrderLineInput(product.Id, 3)]),
            CancellationToken.None);

        Assert.Equal(7, product.Stock);

        var change = new ChangeOrderStatusCommandHandler(
            seed.User, seed.Orders, seed.Products, seed.Movements, new FakeUnitOfWork());

        var paid = await change.Handle(
            new ChangeOrderStatusCommand(created.Id, "Paid", created.Version),
            CancellationToken.None);

        Assert.Equal("Paid", paid.Status);
        Assert.Equal(7, product.Stock);
        Assert.Contains(seed.Movements.Items, m => m.Type == StockMovementType.Deduct && m.QuantityDelta == -3);

        var cancelled = await change.Handle(
            new ChangeOrderStatusCommand(paid.Id, "Cancelled", paid.Version),
            CancellationToken.None);

        Assert.Equal("Cancelled", cancelled.Status);
        Assert.Equal(10, product.Stock);
        Assert.Contains(seed.Movements.Items, m => m.Type == StockMovementType.Release && m.QuantityDelta == 3);
    }

    [Fact]
    public async Task ChangeStatus_ThrowsConcurrency_WhenVersionIsStale()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        var create = CreateHandler(seed);
        var created = await create.Handle(
            new CreateOrderCommand("Ama", null, null, false, [new CreateOrderLineInput(product.Id, 1)]),
            CancellationToken.None);

        var change = new ChangeOrderStatusCommandHandler(
            seed.User, seed.Orders, seed.Products, seed.Movements, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConcurrencyAppException>(() =>
            change.Handle(new ChangeOrderStatusCommand(created.Id, "Confirmed", 99), CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_ThrowsConflict_WhenPendingJumpsToPaid()
    {
        var seed = SeedShop("Growth");
        var product = Product.Create(seed.User.ShopId!, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        seed.Products.Add(product);
        var create = CreateHandler(seed);
        var created = await create.Handle(
            new CreateOrderCommand("Ama", null, null, false, [new CreateOrderLineInput(product.Id, 1)]),
            CancellationToken.None);

        var change = new ChangeOrderStatusCommandHandler(
            seed.User, seed.Orders, seed.Products, seed.Movements, new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            change.Handle(new ChangeOrderStatusCommand(created.Id, "Paid", created.Version), CancellationToken.None));
    }

    private static CreateOrderCommandHandler CreateHandler(Seed seed)
        => new(seed.User, seed.Shops, seed.Products, seed.Orders, seed.Movements, new FakeUnitOfWork());

    private static Seed SeedShop(string planName)
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

        return new Seed(shops, new FakeProductRepository(), new FakeStockMovementRepository(), new FakeOrderRepository(), user);
    }

    private sealed record Seed(
        FakeShopRepository Shops,
        FakeProductRepository Products,
        FakeStockMovementRepository Movements,
        FakeOrderRepository Orders,
        FakeCurrentUser User);
}

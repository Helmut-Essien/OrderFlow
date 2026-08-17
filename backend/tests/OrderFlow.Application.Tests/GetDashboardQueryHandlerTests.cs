using OrderFlow.Application.Features.Dashboard.GetDashboard;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Tests;

public class GetDashboardQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsLowStockAndZeroSalesUntilOrdersExist()
    {
        var shop = Shop.Create(
            "Makola Mart",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:key",
            "Growth",
            null,
            false);
        var products = new FakeProductRepository();
        products.Add(Product.Create(shop.Id, "Indomie", "IND-70", "Snacks", 8m, 3, 5));
        products.Add(Product.Create(shop.Id, "Peak Milk", "PEAK-400", "Dairy", 18m, 22, 4));

        var handler = new GetDashboardQueryHandler(
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                ShopId = shop.Id,
                UserId = "user_1"
            },
            products,
            new FakeOrderRepository());

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Equal(0m, result.TodaysSales);
        Assert.Equal(0, result.OrderCount);
        Assert.Equal(0, result.PendingWhatsAppCount);
        Assert.Empty(result.RecentOrders);
        Assert.Equal(1, result.LowStockCount);
        Assert.Equal("IND-70", result.LowStock[0].Sku);
        Assert.Equal(3, result.LowStock[0].Stock);
    }

    [Fact]
    public async Task Handle_CountsTodaysPaidSalesAndRecentOrders()
    {
        var shop = Shop.Create(
            "Makola Mart",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:key",
            "Growth",
            null,
            false);
        var product = Product.Create(shop.Id, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        var orders = new FakeOrderRepository();
        var order = Order.CreateManual(
            shop.Id,
            "Ama",
            null,
            null,
            [new OrderLineDraft(product.Id, product.Name, product.Sku, 2, product.Price)],
            "user_1");
        order.TransitionTo(OrderStatus.Confirmed, DateTime.UtcNow);
        order.TransitionTo(OrderStatus.Paid, DateTime.UtcNow);
        orders.Add(order);

        var handler = new GetDashboardQueryHandler(
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                ShopId = shop.Id,
                UserId = "user_1"
            },
            new FakeProductRepository(),
            orders);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Equal(7.00m, result.TodaysSales);
        Assert.Equal(1, result.OrderCount);
        Assert.Single(result.RecentOrders);
        Assert.Equal("Ama", result.RecentOrders[0].CustomerName);
        Assert.Equal("Paid", result.RecentOrders[0].Status);
    }

    [Fact]
    public async Task Handle_ExcludesCancelledPaidOrdersFromTodaysSales()
    {
        var shop = Shop.Create(
            "Makola Mart",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:key",
            "Growth",
            null,
            false);
        var product = Product.Create(shop.Id, "Voltic", "VOLT-500", null, 3.5m, 10, 2);
        var orders = new FakeOrderRepository();
        var order = Order.CreateManual(
            shop.Id,
            "Ama",
            null,
            null,
            [new OrderLineDraft(product.Id, product.Name, product.Sku, 2, product.Price)],
            "user_1");
        var now = DateTime.UtcNow;
        order.TransitionTo(OrderStatus.Confirmed, now);
        order.TransitionTo(OrderStatus.Paid, now);
        order.TransitionTo(OrderStatus.Cancelled, now);
        orders.Add(order);

        var handler = new GetDashboardQueryHandler(
            new FakeCurrentUser
            {
                IsAuthenticated = true,
                ShopId = shop.Id,
                UserId = "user_1"
            },
            new FakeProductRepository(),
            orders);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Equal(0m, result.TodaysSales);
        Assert.Equal(0, result.OrderCount);
        Assert.Single(result.RecentOrders);
        Assert.Equal("Cancelled", result.RecentOrders[0].Status);
    }
}

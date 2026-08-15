using OrderFlow.Application.Features.Dashboard.GetDashboard;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

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
            products);

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.Equal(0m, result.TodaysSales);
        Assert.Equal(0, result.OrderCount);
        Assert.Equal(0, result.PendingWhatsAppCount);
        Assert.Equal(1, result.LowStockCount);
        Assert.Equal("IND-70", result.LowStock[0].Sku);
        Assert.Equal(3, result.LowStock[0].Stock);
    }
}

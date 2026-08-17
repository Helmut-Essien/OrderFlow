using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Tests;

public class OrderDomainTests
{
    [Fact]
    public void CreateManual_SnapshotsPriceAndStartsPending()
    {
        var order = Order.CreateManual(
            "shop_1",
            "  Ama Boateng  ",
            " 0244000000 ",
            "  Counter sale ",
            [new OrderLineDraft("prod_1", "Voltic Water", "volt-500", 2, 3.555m)],
            "user_1");

        Assert.Equal("Ama Boateng", order.CustomerName);
        Assert.Equal("0244000000", order.CustomerPhone);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(OrderSource.Manual, order.Source);
        Assert.Equal(1, order.Version);
        Assert.Equal(7.12m, order.TotalAmount);
        Assert.Single(order.Lines);
        Assert.Equal("VOLT-500", order.Lines.First().Sku);
        Assert.Equal(3.56m, order.Lines.First().UnitPrice);
    }

    [Fact]
    public void CreateManual_RejectsDuplicateProductIds()
    {
        var lines = new[]
        {
            new OrderLineDraft("prod_1", "Voltic", "VOLT-1", 1, 3m),
            new OrderLineDraft("prod_1", "Voltic", "VOLT-1", 2, 3m)
        };

        Assert.Throws<ArgumentException>(() =>
            Order.CreateManual("shop_1", "Ama", null, null, lines, null));
    }

    [Fact]
    public void CanTransition_AllowsConfirmedThenPaidThenFulfilled_AndCancelFromOpenStates()
    {
        Assert.True(Order.CanTransition(OrderStatus.Pending, OrderStatus.Confirmed));
        Assert.True(Order.CanTransition(OrderStatus.Confirmed, OrderStatus.Paid));
        Assert.True(Order.CanTransition(OrderStatus.Paid, OrderStatus.Fulfilled));
        Assert.True(Order.CanTransition(OrderStatus.Pending, OrderStatus.Cancelled));
        Assert.True(Order.CanTransition(OrderStatus.Confirmed, OrderStatus.Cancelled));
        Assert.True(Order.CanTransition(OrderStatus.Paid, OrderStatus.Cancelled));
        Assert.False(Order.CanTransition(OrderStatus.Pending, OrderStatus.Paid));
        Assert.False(Order.CanTransition(OrderStatus.Fulfilled, OrderStatus.Cancelled));
        Assert.False(Order.CanTransition(OrderStatus.Cancelled, OrderStatus.Confirmed));
    }

    [Fact]
    public void CountsTowardTodaysSales_IsTrueForPaidAndFulfilled_NotCancelled()
    {
        Assert.True(Order.CountsTowardTodaysSales(OrderStatus.Paid));
        Assert.True(Order.CountsTowardTodaysSales(OrderStatus.Fulfilled));
        Assert.False(Order.CountsTowardTodaysSales(OrderStatus.Cancelled));
        Assert.False(Order.CountsTowardTodaysSales(OrderStatus.Confirmed));
        Assert.False(Order.CountsTowardTodaysSales(OrderStatus.Pending));
    }

    [Fact]
    public void TransitionTo_RejectsIllegalJump()
    {
        var order = Order.CreateManual(
            "shop_1",
            "Ama",
            null,
            null,
            [new OrderLineDraft("prod_1", "Voltic", "VOLT-1", 1, 3m)],
            null);

        Assert.Throws<InvalidOperationException>(() =>
            order.TransitionTo(OrderStatus.Paid, DateTime.UtcNow));
    }

    [Fact]
    public void CreateManual_RejectsTooManyLines()
    {
        var lines = Enumerable.Range(0, OrderConstraints.MaxLinesPerOrder + 1)
            .Select(i => new OrderLineDraft($"prod_{i}", "Item", $"SKU-{i}", 1, 1m))
            .ToList();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Order.CreateManual("shop_1", "Ama", null, null, lines, null));
    }
}

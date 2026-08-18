using OrderFlow.Application.Features.Orders.ChangeOrderStatus;
using OrderFlow.Application.Features.Orders.CreateOrder;
using OrderFlow.Application.Features.Orders.ListOrders;

namespace OrderFlow.Application.Tests;

public class OrderValidatorTests
{
    [Fact]
    public async Task Create_RejectsEmptyCustomerAndEmptyLines()
    {
        var validator = new CreateOrderCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateOrderCommand(" ", null, null, false, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.CustomerName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.Lines));
    }

    [Fact]
    public async Task Create_RejectsDuplicateProductIdsAndZeroQuantity()
    {
        var validator = new CreateOrderCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateOrderCommand(
                "Ama",
                null,
                null,
                false,
                [
                    new CreateOrderLineInput("prod_1", 0),
                    new CreateOrderLineInput("prod_1", 1)
                ]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("same product", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Quantity", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChangeStatus_RejectsNumericStatusAndNonPositiveVersion()
    {
        var validator = new ChangeOrderStatusCommandValidator();
        var result = await validator.ValidateAsync(
            new ChangeOrderStatusCommand("01ARZ3NDEKTSV4RRFFQ69G5FAV", "1", 0));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("Status must be Confirmed", StringComparison.OrdinalIgnoreCase));
        // ValidationFailure.PropertyName formatting can differ (e.g., "Expected Version").
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage.Contains("greater than or equal to", StringComparison.OrdinalIgnoreCase) && e.ErrorMessage.Contains("1"));
    }

    [Fact]
    public async Task List_RejectsPageSizeAboveCap()
    {
        var validator = new ListOrdersQueryValidator();
        var result = await validator.ValidateAsync(new ListOrdersQuery(null, null, 1, 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListOrdersQuery.PageSize));
    }
}

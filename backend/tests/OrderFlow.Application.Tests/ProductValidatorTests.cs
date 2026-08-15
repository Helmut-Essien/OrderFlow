using OrderFlow.Application.Features.Products.AdjustStock;
using OrderFlow.Application.Features.Products.CreateProduct;

namespace OrderFlow.Application.Tests;

public class ProductValidatorTests
{
    [Fact]
    public async Task Create_RejectsEmptyNameAndNegativePrice()
    {
        var validator = new CreateProductCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateProductCommand(" ", "SKU-1", null, -1m, -2, -1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Price));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.Stock));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductCommand.LowStockThreshold));
    }

    [Fact]
    public async Task Adjust_RejectsZeroDelta()
    {
        var validator = new AdjustStockCommandValidator();
        var result = await validator.ValidateAsync(
            new AdjustStockCommand("01ARZ3NDEKTSV4RRFFQ69G5FAV", 0, 1, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AdjustStockCommand.QuantityDelta));
    }
}

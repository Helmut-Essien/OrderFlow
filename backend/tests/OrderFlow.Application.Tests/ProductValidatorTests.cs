using OrderFlow.Application.Features.Products.AdjustStock;
using OrderFlow.Application.Features.Products.CreateProduct;
using OrderFlow.Application.Features.Products.ListProducts;
using OrderFlow.Application.Features.Products.UpdateProduct;

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

    [Fact]
    public async Task Update_RejectsEmptyNameAndNonPositiveVersion()
    {
        var validator = new UpdateProductCommandValidator();
        var result = await validator.ValidateAsync(
            new UpdateProductCommand("id", " ", "SKU-1", null, 1m, 0, true, 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProductCommand.ExpectedVersion));
    }

    [Fact]
    public async Task List_RejectsPageSizeAboveCap()
    {
        var validator = new ListProductsQueryValidator();
        var result = await validator.ValidateAsync(new ListProductsQuery(null, null, 1, 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ListProductsQuery.PageSize));
    }
}

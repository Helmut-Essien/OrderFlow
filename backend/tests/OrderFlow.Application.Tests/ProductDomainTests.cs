using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Tests;

public class ProductDomainTests
{
    [Fact]
    public void Create_NormalizesSkuAndRoundsPrice()
    {
        var product = Product.Create("shop_1", "  Voltic Water  ", " volt-500 ", " Beverages ", 3.555m, 12, 4);

        Assert.Equal("Voltic Water", product.Name);
        Assert.Equal("VOLT-500", product.Sku);
        Assert.Equal("Beverages", product.Category);
        Assert.Equal(3.56m, product.Price);
        Assert.Equal(1, product.Version);
        Assert.False(product.IsLowStock);
    }

    [Fact]
    public void Create_MarksLowStockWhenAtThreshold()
    {
        var product = Product.Create("shop_1", "Indomie", "IND-70", null, 8m, 5, 5);
        Assert.True(product.IsLowStock);
    }

    [Fact]
    public void NormalizeSku_RejectsTooLongValue()
    {
        var sku = new string('A', ProductConstraints.SkuMaxLength + 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.NormalizeSku(sku));
    }
}

using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products;

/// <summary>Maps domain products to public <see cref="ProductDto"/> inside the handler, never in the controller.</summary>
internal static class ProductMapping
{
    public static ProductDto ToDto(Product product)
    {
        return new ProductDto
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
}

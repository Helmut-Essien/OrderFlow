using FluentValidation;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Products.CreateProduct;

/// <summary>Enforces <see cref="ProductConstraints"/> on create. FluentValidation is the authoritative API 400 source.</summary>
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    /// <summary>Binds create field limits from <see cref="ProductConstraints"/>.</summary>
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ProductConstraints.NameMaxLength);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(ProductConstraints.SkuMaxLength);
        RuleFor(x => x.Category)
            .MaximumLength(ProductConstraints.CategoryMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Category));
        RuleFor(x => x.Price).InclusiveBetween(0, ProductConstraints.MaxPrice);
        RuleFor(x => x.Stock).InclusiveBetween(0, ProductConstraints.MaxStock);
        RuleFor(x => x.LowStockThreshold).InclusiveBetween(0, ProductConstraints.MaxStock);
    }
}

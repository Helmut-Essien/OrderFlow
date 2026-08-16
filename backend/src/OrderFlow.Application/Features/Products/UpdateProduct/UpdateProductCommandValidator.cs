using FluentValidation;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Products.UpdateProduct;

/// <summary>Enforces <see cref="ProductConstraints"/> on update. Stock is not part of this command.</summary>
public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    /// <summary>Binds update field limits from <see cref="ProductConstraints"/> (stock is not on this command).</summary>
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(26);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(ProductConstraints.NameMaxLength);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(ProductConstraints.SkuMaxLength);
        RuleFor(x => x.Category)
            .MaximumLength(ProductConstraints.CategoryMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Category));
        RuleFor(x => x.Price).InclusiveBetween(0, ProductConstraints.MaxPrice);
        RuleFor(x => x.LowStockThreshold).InclusiveBetween(0, ProductConstraints.MaxStock);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(1);
    }
}

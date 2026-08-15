using FluentValidation;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Products.ListProducts;

/// <summary>Bounds search/category length and pagination (page ≥ 1, pageSize 1–100).</summary>
public sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(ProductConstraints.NameMaxLength);
        RuleFor(x => x.Category)
            .MaximumLength(ProductConstraints.CategoryMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Category));
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

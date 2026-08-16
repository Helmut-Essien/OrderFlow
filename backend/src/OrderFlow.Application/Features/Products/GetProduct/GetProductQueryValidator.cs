using FluentValidation;

namespace OrderFlow.Application.Features.Products.GetProduct;

/// <summary>Requires a non-empty ULID product id (max 26).</summary>
public sealed class GetProductQueryValidator : AbstractValidator<GetProductQuery>
{
    /// <summary>Requires a non-empty ULID product id.</summary>
    public GetProductQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(26);
    }
}

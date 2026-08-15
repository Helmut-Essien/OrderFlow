using FluentValidation;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Products.AdjustStock;

/// <summary>Rejects a zero delta and bounds notes/quantity to <see cref="ProductConstraints"/>.</summary>
public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(26);
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithMessage("Quantity delta must not be zero.")
            .InclusiveBetween(-ProductConstraints.MaxStock, ProductConstraints.MaxStock);
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Notes)
            .MaximumLength(ProductConstraints.NotesMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}

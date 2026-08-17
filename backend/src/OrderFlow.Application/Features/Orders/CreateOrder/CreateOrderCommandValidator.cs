using FluentValidation;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Orders.CreateOrder;

/// <summary>Enforces <see cref="OrderConstraints"/> on create. Duplicate product ids are rejected.</summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    /// <summary>Binds create field limits from <see cref="OrderConstraints"/>.</summary>
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(OrderConstraints.CustomerNameMaxLength);
        RuleFor(x => x.CustomerPhone)
            .MaximumLength(OrderConstraints.CustomerPhoneMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhone));
        RuleFor(x => x.Notes)
            .MaximumLength(OrderConstraints.NotesMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
        RuleFor(x => x.Lines)
            .NotEmpty()
            .Must(lines => lines.Count <= OrderConstraints.MaxLinesPerOrder)
            .WithMessage($"An order cannot exceed {OrderConstraints.MaxLinesPerOrder} lines.")
            .Must(HaveUniqueProductIds)
            .WithMessage("An order cannot contain the same product twice. Combine quantities on one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty().MaximumLength(26);
            line.RuleFor(l => l.Quantity)
                .InclusiveBetween(OrderConstraints.MinLineQuantity, OrderConstraints.MaxLineQuantity);
        });
    }

    private static bool HaveUniqueProductIds(IReadOnlyList<CreateOrderLineInput> lines)
    {
        var ids = lines.Select(l => l.ProductId.Trim()).ToList();
        return ids.Distinct(StringComparer.Ordinal).Count() == ids.Count;
    }
}

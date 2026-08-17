using FluentValidation;
using OrderFlow.Domain;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Features.Orders.ListOrders;

/// <summary>Bounds search length, optional status name, and pagination (page ≥ 1, pageSize 1–100).</summary>
public sealed class ListOrdersQueryValidator : AbstractValidator<ListOrdersQuery>
{
    /// <summary>Binds search length, defined <see cref="OrderStatus"/> names, and pageSize 1–100.</summary>
    public ListOrdersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(OrderConstraints.CustomerNameMaxLength);
        RuleFor(x => x.Status)
            .Must(BeDefinedStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Status must be Pending, Confirmed, Paid, Fulfilled, or Cancelled.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }

    private static bool BeDefinedStatus(string? status)
    {
        return Enum.GetNames<OrderStatus>().Any(n => n.Equals(status, StringComparison.OrdinalIgnoreCase));
    }
}

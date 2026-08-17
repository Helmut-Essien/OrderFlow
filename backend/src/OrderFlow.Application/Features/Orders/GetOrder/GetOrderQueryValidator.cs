using FluentValidation;

namespace OrderFlow.Application.Features.Orders.GetOrder;

/// <summary>Requires a non-empty ULID order id (max 26).</summary>
public sealed class GetOrderQueryValidator : AbstractValidator<GetOrderQuery>
{
    /// <summary>Requires a non-empty ULID order id.</summary>
    public GetOrderQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(26);
    }
}

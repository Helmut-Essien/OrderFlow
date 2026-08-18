using FluentValidation;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Features.Orders.ChangeOrderStatus;

/// <summary>Requires a ULID, a defined <see cref="OrderStatus"/> name, and a positive expected version.</summary>
public sealed class ChangeOrderStatusCommandValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    /// <summary>Binds id length, defined status names, and expectedVersion ≥ 1.</summary>
    public ChangeOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(26);
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(BeDefinedStatus)
            .WithMessage("Status must be Confirmed, Paid, Fulfilled, or Cancelled.");
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(1);
    }

    private static bool BeDefinedStatus(string status)
    {
        // Pending is a valid current state, but this endpoint is only for transitions to non-Pending targets.
        // Validation must accept enum *names* (e.g. "Paid"), not numeric strings ("1").
        var matchedName = Enum.GetNames<OrderStatus>()
            .FirstOrDefault(n => n.Equals(status, StringComparison.OrdinalIgnoreCase));

        if (matchedName is null)
            return false;

        return !matchedName.Equals(OrderStatus.Pending.ToString(), StringComparison.Ordinal);
    }
}

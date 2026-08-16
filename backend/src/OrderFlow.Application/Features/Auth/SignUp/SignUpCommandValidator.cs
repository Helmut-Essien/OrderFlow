using FluentValidation;

namespace OrderFlow.Application.Features.Auth.SignUp;

/// <summary>Signup field limits matching Shared DTOs. Password min 8 / max 128.</summary>
public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    /// <summary>Binds signup field limits to match Shared DTOs and Angular <c>AUTH_FIELD_LIMITS</c>.</summary>
    public SignUpCommandValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ShopName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName));
        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}

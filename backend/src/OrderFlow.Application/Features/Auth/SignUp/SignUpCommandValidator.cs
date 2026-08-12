using FluentValidation;

namespace OrderFlow.Application.Features.Auth.SignUp;

public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ShopName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DisplayName).MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}

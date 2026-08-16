using FluentValidation;

namespace OrderFlow.Application.Features.Auth.Login;

/// <summary>
/// Password max length is required on login too (DoS / payload bound), not only signup. Min length is not enforced at login.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Binds login payload bounds (email 320, password max 128 for DoS).</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

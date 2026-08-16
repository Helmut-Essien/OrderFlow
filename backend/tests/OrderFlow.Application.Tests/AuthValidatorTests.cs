using OrderFlow.Application.Features.Auth.Login;
using OrderFlow.Application.Features.Auth.SignUp;

namespace OrderFlow.Application.Tests;

public class AuthValidatorTests
{
    [Fact]
    public async Task Login_RejectsEmptyEmailAndOverlongPassword()
    {
        var validator = new LoginCommandValidator();
        var result = await validator.ValidateAsync(new LoginCommand("", new string('x', 129)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Email));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
    }

    [Fact]
    public async Task SignUp_RejectsShortPasswordAndMissingLicense()
    {
        var validator = new SignUpCommandValidator();
        var result = await validator.ValidateAsync(
            new SignUpCommand(" ", "owner@shop.example", "short", "Makola Mart", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpCommand.LicenseKey));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpCommand.Password));
    }
}

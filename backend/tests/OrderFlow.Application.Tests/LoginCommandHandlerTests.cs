using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Features.Auth.Login;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Tests;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsToken_WhenCredentialsAreValid_WithoutCheckingLicense()
    {
        var (handler, platform, shop) = CreateHandler();

        var result = await handler.Handle(
            new LoginCommand("owner@shop.example", "ChangeMe123"),
            CancellationToken.None);

        Assert.Equal("test-token", result.Token);
        Assert.Equal("Growth", result.Plan.Name);
        Assert.Null(platform.LastLicenseKey);
        Assert.Equal("Growth", shop.PlanName);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenPasswordIsWrong()
    {
        var (handler, _, _) = CreateHandler();

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            handler.Handle(new LoginCommand("owner@shop.example", "wrong-password"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DoesNotCallPlatform_EvenIfLicenseWouldBeInvalid()
    {
        var (handler, platform, _) = CreateHandler();
        platform.Result = new(false, null, null, "License is not valid.");

        var result = await handler.Handle(
            new LoginCommand("owner@shop.example", "ChangeMe123"),
            CancellationToken.None);

        Assert.Equal("test-token", result.Token);
        Assert.Null(platform.LastLicenseKey);
    }

    private static (LoginCommandHandler Handler, FakePlatformLicenseClient Platform, Shop Shop) CreateHandler()
    {
        var shop = Shop.Create(
            "Tema Provisions",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:ORDERFLOW-DEVK-TEST",
            "Growth",
            DateTime.UtcNow.AddYears(1),
            false);
        var user = User.CreateOwner(shop.Id, "owner@shop.example", "Ama", "hash:ChangeMe123");

        var shops = new FakeShopRepository();
        shops.Add(shop);
        var users = new FakeUserRepository();
        users.Add(user);
        var platform = new FakePlatformLicenseClient();

        var handler = new LoginCommandHandler(
            shops,
            users,
            new FakePasswordHasher(),
            new FakeJwtTokenService());

        return (handler, platform, shop);
    }
}

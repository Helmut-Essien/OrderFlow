using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Features.Auth.SignUp;
using OrderFlow.Application.Tests.Fakes;
using OrderFlow.Domain;

namespace OrderFlow.Application.Tests;

public class SignUpCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesShopAndOwner_WhenLicenseIsValid()
    {
        var platform = new FakePlatformLicenseClient();
        var shops = new FakeShopRepository();
        var users = new FakeUserRepository();
        var uow = new FakeUnitOfWork();
        var handler = new SignUpCommandHandler(
            platform,
            shops,
            users,
            uow,
            new FakePasswordHasher(),
            new FakeLicenseKeyProtector(),
            new FakeJwtTokenService());

        var result = await handler.Handle(
            new SignUpCommand(
                "ORDERFLOW-DEVK-TEST",
                "Owner@Shop.Example",
                "ChangeMe123",
                "Tema Provisions",
                "Ama",
                "+233200000000"),
            CancellationToken.None);

        Assert.Equal("test-token", result.Token);
        Assert.Equal("Tema Provisions", result.ShopName);
        Assert.Equal("owner@shop.example", result.Email);
        Assert.Equal("Owner", result.Role);
        Assert.Equal("Growth", result.Plan.Name);
        Assert.False(result.Plan.IsUnrecognized);
        Assert.Single(shops.Items);
        Assert.Single(users.Items);
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"), shops.Items[0].LicenseLookupHash);
        Assert.Equal("p:ORDERFLOW-DEVK-TEST", shops.Items[0].ProtectedLicenseKey);
        Assert.Equal("ORDERFLOW-DEVK-TEST", platform.LastLicenseKey);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenLicenseIsInvalid()
    {
        var platform = new FakePlatformLicenseClient
        {
            Result = new(false, null, null, "Invalid license key.")
        };
        var handler = CreateHandler(platform, new FakeShopRepository(), new FakeUserRepository());

        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Equal("Invalid license key.", ex.Message);
    }

    [Fact]
    public async Task Handle_ThrowsConflict_WhenShopAlreadyRegistered()
    {
        var shops = new FakeShopRepository();
        var existing = OrderFlow.Domain.Entities.Shop.Create(
            "Existing",
            null,
            LicenseLookupHasher.Compute("ORDERFLOW-DEVK-TEST"),
            "p:key",
            "Growth",
            null,
            false);
        shops.Add(existing);

        var handler = CreateHandler(new FakePlatformLicenseClient(), shops, new FakeUserRepository());

        var ex = await Assert.ThrowsAsync<ConflictAppException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public async Task Handle_MapsUnknownPlanToStarterWithWarning()
    {
        var platform = new FakePlatformLicenseClient
        {
            Result = new(true, "Pro Annual", DateTime.UtcNow.AddYears(1), null)
        };
        var shops = new FakeShopRepository();
        var handler = CreateHandler(platform, shops, new FakeUserRepository());

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("Starter", result.Plan.Name);
        Assert.True(result.Plan.IsUnrecognized);
        Assert.Equal("Pro Annual", result.Plan.OriginalName);
        Assert.True(shops.Items[0].PlanUnrecognized);
    }

    private static SignUpCommand ValidCommand() =>
        new("ORDERFLOW-DEVK-TEST", "owner@shop.example", "ChangeMe123", "Tema Provisions", "Ama", null);

    private static SignUpCommandHandler CreateHandler(
        FakePlatformLicenseClient platform,
        FakeShopRepository shops,
        FakeUserRepository users)
    {
        return new SignUpCommandHandler(
            platform,
            shops,
            users,
            new FakeUnitOfWork(),
            new FakePasswordHasher(),
            new FakeLicenseKeyProtector(),
            new FakeJwtTokenService());
    }
}

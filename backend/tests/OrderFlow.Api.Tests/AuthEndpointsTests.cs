using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Api.Tests;

[Collection("OrderFlowApi")]
public class AuthEndpointsTests
{
    private readonly OrderFlowApiFactory _factory;

    public AuthEndpointsTests(OrderFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SignUp_Then_Login_Then_Me_ReturnsAuthenticatedShop()
    {
        var client = _factory.CreateClient();
        var email = $"owner-{Guid.NewGuid():N}@shop.example";
        var signUp = await client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
        {
            LicenseKey = $"ORDERFLOW-DEVK-{Guid.NewGuid():N}",
            Email = email,
            Password = "ChangeMe123",
            ShopName = "Tema Provisions",
            DisplayName = "Ama"
        });

        signUp.EnsureSuccessStatusCode();
        var auth = await signUp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.Equal("Growth", auth.Plan.Name);

        var loginClient = _factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "ChangeMe123"
        });
        login.EnsureSuccessStatusCode();
        var loggedIn = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Tema Provisions", loggedIn!.ShopName);

        loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn.Token);
        var me = await loginClient.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();
        var profile = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal("Tema Provisions", profile!.ShopName);
        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public async Task SignUp_RejectsInvalidLicense()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
        {
            LicenseKey = "BAD-KEY",
            Email = "other@shop.example",
            Password = "ChangeMe123",
            ShopName = "Other Shop"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginFailure_IncludesCorsAllowOrigin_ForDevSpa()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:4200");

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nobody@shop.example",
            Password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://localhost:4200", origins.Single());
    }
}

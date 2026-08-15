using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OrderFlow.Shared.DTOs.Auth;
using OrderFlow.Shared.DTOs.Common;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Api.Tests;

[Collection("OrderFlowApi")]
public class ProductEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OrderFlowApiFactory _factory;

    public ProductEndpointsTests(OrderFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_List_Get_Update_And_AdjustStock()
    {
        var client = await AuthenticatedClientAsync($"owner-{Guid.NewGuid():N}@shop.example");

        var created = await client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Voltic Water 500ml",
            Sku = "volt-500",
            Category = "Beverages",
            Price = 3.50m,
            Stock = 48,
            LowStockThreshold = 6
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = await created.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(product);
        Assert.Equal("VOLT-500", product.Sku);
        Assert.Equal(48, product.Stock);
        Assert.Equal(1, product.Version);

        var list = await client.GetFromJsonAsync<PagedResult<ProductDto>>(
            "/api/products?search=voltic",
            JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal("VOLT-500", list.Items[0].Sku);

        var fetched = await client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", JsonOptions);
        Assert.Equal(product.Id, fetched!.Id);

        var updatedResponse = await client.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest
        {
            Name = "Voltic Water 500ml",
            Sku = "VOLT-500",
            Category = "Drinks",
            Price = 4.00m,
            LowStockThreshold = 8,
            IsActive = true,
            ExpectedVersion = product.Version
        });
        updatedResponse.EnsureSuccessStatusCode();
        var updated = await updatedResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.Equal(4.00m, updated!.Price);
        Assert.Equal(2, updated.Version);

        var stockResponse = await client.PostAsJsonAsync($"/api/products/{product.Id}/stock", new AdjustStockRequest
        {
            QuantityDelta = -10,
            ExpectedVersion = updated.Version,
            Notes = "Sold a crate"
        });
        stockResponse.EnsureSuccessStatusCode();
        var adjusted = await stockResponse.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.Equal(38, adjusted!.Stock);
        Assert.Equal(3, adjusted.Version);
    }

    [Fact]
    public async Task Create_RejectsDuplicateSku()
    {
        var client = await AuthenticatedClientAsync($"dup-{Guid.NewGuid():N}@shop.example");
        var first = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-DUP"));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/products", ValidProduct("sku-dup"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AdjustStock_ReturnsConcurrencyCode_WhenVersionIsStale()
    {
        var client = await AuthenticatedClientAsync($"conc-{Guid.NewGuid():N}@shop.example");
        var created = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-CON"));
        var product = await created.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);

        var response = await client.PostAsJsonAsync($"/api/products/{product!.Id}/stock", new AdjustStockRequest
        {
            QuantityDelta = 1,
            ExpectedVersion = 99
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("concurrency", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Dashboard_ReturnsLowStockAndZeroSales()
    {
        var client = await AuthenticatedClientAsync($"dash-{Guid.NewGuid():N}@shop.example");
        var created = await client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Indomie Chicken 70g",
            Sku = "IND-70",
            Category = "Snacks",
            Price = 8m,
            Stock = 3,
            LowStockThreshold = 5
        });
        created.EnsureSuccessStatusCode();

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal(0m, dashboard.TodaysSales);
        Assert.Equal(0, dashboard.OrderCount);
        Assert.Equal(1, dashboard.LowStockCount);
        Assert.Equal("IND-70", dashboard.LowStock[0].Sku);
    }

    [Fact]
    public async Task Products_RequireAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_EnforcesStarterProductLimit()
    {
        var client = _factory.CreateClient();
        var signUp = await client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
        {
            LicenseKey = $"ORDERFLOW-STARTER-{Guid.NewGuid():N}",
            Email = $"starter-{Guid.NewGuid():N}@shop.example",
            Password = "ChangeMe123",
            ShopName = "Starter Shop"
        });
        signUp.EnsureSuccessStatusCode();
        var auth = await signUp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        for (var i = 0; i < 50; i++)
        {
            var created = await client.PostAsJsonAsync("/api/products", ValidProduct($"SKU-{i:000}"));
            created.EnsureSuccessStatusCode();
        }

        var blocked = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-050"));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var signUp = await client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
        {
            LicenseKey = $"ORDERFLOW-DEVK-{Guid.NewGuid():N}",
            Email = email,
            Password = "ChangeMe123",
            ShopName = "Makola Mart"
        });
        signUp.EnsureSuccessStatusCode();
        var auth = await signUp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static CreateProductRequest ValidProduct(string sku) => new()
    {
        Name = "Sample product",
        Sku = sku,
        Category = "Snacks",
        Price = 8m,
        Stock = 10,
        LowStockThreshold = 2
    };
}

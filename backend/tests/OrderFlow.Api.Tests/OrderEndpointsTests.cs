using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Shared.DTOs.Auth;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Orders;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Api.Tests;

[Collection("OrderFlowApi")]
public class OrderEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OrderFlowApiFactory _factory;

    public OrderEndpointsTests(OrderFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_Confirm_Pay_Fulfill_And_List()
    {
        var client = await AuthenticatedClientAsync($"ord-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(client, "VOLT-500", 20);

        var createdResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Ama Boateng",
            CustomerPhone = "0244000000",
            ConfirmImmediately = true,
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 2 }]
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Confirmed", created.Status);
        Assert.Equal(7.00m, created.TotalAmount);
        Assert.Equal("VOLT-500", created.Lines[0].Sku);

        var stock = await client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", JsonOptions);
        Assert.Equal(18, stock!.Stock);

        var paidResponse = await client.PostAsJsonAsync($"/api/orders/{created.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Paid",
            ExpectedVersion = created.Version
        });
        paidResponse.EnsureSuccessStatusCode();
        var paid = await paidResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        Assert.Equal("Paid", paid!.Status);
        Assert.NotNull(paid.PaidAt);

        var stillHeld = await client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", JsonOptions);
        Assert.Equal(18, stillHeld!.Stock);

        var fulfilledResponse = await client.PostAsJsonAsync($"/api/orders/{paid.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Fulfilled",
            ExpectedVersion = paid.Version
        });
        fulfilledResponse.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<OrderListResponse>("/api/orders?status=Fulfilled", JsonOptions);
        Assert.NotNull(list);
        Assert.Equal(1, list.TotalCount);
        Assert.Equal("Fulfilled", list.Items[0].Status);
        Assert.Equal(1, list.Items[0].LineCount);

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", JsonOptions);
        Assert.Equal(7.00m, dashboard!.TodaysSales);
        Assert.Equal(1, dashboard.OrderCount);
        Assert.Single(dashboard.RecentOrders);
    }

    [Fact]
    public async Task CancelConfirmed_ReleasesStock()
    {
        var client = await AuthenticatedClientAsync($"rel-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(client, "REL-1", 10);

        var createdResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Kojo",
            ConfirmImmediately = true,
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 4 }]
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var cancelledResponse = await client.PostAsJsonAsync($"/api/orders/{created!.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Cancelled",
            ExpectedVersion = created.Version
        });
        cancelledResponse.EnsureSuccessStatusCode();

        var stock = await client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", JsonOptions);
        Assert.Equal(10, stock!.Stock);
    }

    [Fact]
    public async Task Dashboard_ExcludesCancelledPaidOrderFromTodaysSales()
    {
        var client = await AuthenticatedClientAsync($"cxlsale-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(client, "CXL-1", 10);

        var createdResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Ama",
            ConfirmImmediately = true,
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 2 }]
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var paidResponse = await client.PostAsJsonAsync($"/api/orders/{created!.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Paid",
            ExpectedVersion = created.Version
        });
        var paid = await paidResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var cancelledResponse = await client.PostAsJsonAsync($"/api/orders/{paid!.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Cancelled",
            ExpectedVersion = paid.Version
        });
        cancelledResponse.EnsureSuccessStatusCode();

        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", JsonOptions);
        Assert.Equal(0m, dashboard!.TodaysSales);
        Assert.Equal(0, dashboard.OrderCount);
        Assert.Equal("Cancelled", dashboard.RecentOrders[0].Status);
    }

    [Fact]
    public async Task Confirm_ReturnsConflict_WhenStockIsInsufficient()
    {
        var client = await AuthenticatedClientAsync($"low-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(client, "LOW-1", 1);

        var pendingResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Ama",
            ConfirmImmediately = false,
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 5 }]
        });
        var pending = await pendingResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var response = await client.PostAsJsonAsync($"/api/orders/{pending!.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Confirmed",
            ExpectedVersion = pending.Version
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("stock", doc.RootElement.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangeStatus_ReturnsConcurrencyCode_WhenVersionIsStale()
    {
        var client = await AuthenticatedClientAsync($"stale-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(client, "STALE-1", 5);
        var createdResponse = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Ama",
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 1 }]
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var response = await client.PostAsJsonAsync($"/api/orders/{created!.Id}/status", new ChangeOrderStatusRequest
        {
            Status = "Confirmed",
            ExpectedVersion = 99
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("concurrency", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task List_RejectsPageSizeAbove100()
    {
        var client = await AuthenticatedClientAsync($"page-{Guid.NewGuid():N}@shop.example");
        var response = await client.GetAsync("/api/orders?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShopCannotReadAnotherShopsOrder()
    {
        var shopA = await AuthenticatedClientAsync($"oa-{Guid.NewGuid():N}@shop.example");
        var shopB = await AuthenticatedClientAsync($"ob-{Guid.NewGuid():N}@shop.example");
        var product = await CreateProductAsync(shopA, "SKU-A", 5);

        var created = await shopA.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Ama",
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 1 }]
        });
        var order = await created.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);

        var get = await shopB.GetAsync($"/api/orders/{order!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var list = await shopB.GetFromJsonAsync<OrderListResponse>("/api/orders", JsonOptions);
        Assert.NotNull(list);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task Create_EnforcesStarterMonthlyOrderLimit()
    {
        var client = _factory.CreateClient();
        var signUp = await client.PostAsJsonAsync("/api/auth/signup", new SignUpRequest
        {
            LicenseKey = $"ORDERFLOW-STARTER-{Guid.NewGuid():N}",
            Email = $"ord-cap-{Guid.NewGuid():N}@shop.example",
            Password = "ChangeMe123",
            ShopName = "Starter Shop"
        });
        signUp.EnsureSuccessStatusCode();
        var auth = await signUp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var product = await CreateProductAsync(client, "CAP-1", 500);
        await SeedOrdersAsync(auth.ShopId, product, 299);

        var firstTask = client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "One",
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 1 }]
        });
        var secondTask = client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerName = "Two",
            Lines = [new CreateOrderLineRequest { ProductId = product.Id, Quantity = 1 }]
        });
        var raced = await Task.WhenAll(firstTask, secondTask);

        var statuses = raced.Select(r => r.StatusCode).ToArray();
        Assert.Contains(HttpStatusCode.Created, statuses);
        Assert.Contains(HttpStatusCode.Forbidden, statuses);
    }

    [Fact]
    public async Task Orders_RequireAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task SeedOrdersAsync(string shopId, ProductDto product, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        for (var i = 0; i < count; i++)
        {
            db.Orders.Add(Order.CreateManual(
                shopId,
                $"Customer {i}",
                null,
                null,
                [new OrderLineDraft(product.Id, product.Name, product.Sku, 1, product.Price)],
                null));
        }

        await db.SaveChangesAsync();
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

    private static async Task<ProductDto> CreateProductAsync(HttpClient client, string sku, int stock)
    {
        var created = await client.PostAsJsonAsync("/api/products", new CreateProductRequest
        {
            Name = "Sample product",
            Sku = sku,
            Category = "Snacks",
            Price = 3.50m,
            Stock = stock,
            LowStockThreshold = 2
        });
        created.EnsureSuccessStatusCode();
        var product = await created.Content.ReadFromJsonAsync<ProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product;
    }
}

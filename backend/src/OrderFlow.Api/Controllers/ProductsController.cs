using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Products.AdjustStock;
using OrderFlow.Application.Features.Products.CreateProduct;
using OrderFlow.Application.Features.Products.GetProduct;
using OrderFlow.Application.Features.Products.ListProducts;
using OrderFlow.Application.Features.Products.UpdateProduct;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Shop-scoped product catalog. All actions require a JWT; EF global filters restrict rows to the claim <c>shopId</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController(IMediator mediator) : ControllerBase
{
    /// <summary>Paged list. <paramref name="pageSize"/> is 1–100 (default 20). Search matches name or SKU. Categories are shop-wide.</summary>
    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListProductsQuery(search, category, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Loads one product. Other shops' ids return 404 because of tenant filters.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> Get(string id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a product. Opening stock writes an Adjustment movement when non-zero.</summary>
    /// <response code="403">Shop is at its plan product cap.</response>
    /// <response code="409">SKU already exists in the shop.</response>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateProductCommand(
                request.Name,
                request.Sku,
                request.Category,
                request.Price,
                request.Stock,
                request.LowStockThreshold),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Updates catalog fields. Does not change stock. Requires a current <c>expectedVersion</c>.</summary>
    /// <response code="409">Stale version (<c>code: concurrency</c>) or duplicate SKU.</response>
    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(
        string id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProductCommand(
                id,
                request.Name,
                request.Sku,
                request.Category,
                request.Price,
                request.LowStockThreshold,
                request.IsActive,
                request.ExpectedVersion),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Manual stock adjustment via an atomic SQL update in the same transaction as the movement row.</summary>
    /// <response code="409">Stale version (<c>code: concurrency</c>), stock would go negative, or stock would exceed the maximum.</response>
    [HttpPost("{id}/stock")]
    public async Task<ActionResult<ProductDto>> AdjustStock(
        string id,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AdjustStockCommand(
                id,
                request.QuantityDelta,
                request.ExpectedVersion,
                request.Notes),
            cancellationToken);

        return Ok(result);
    }
}

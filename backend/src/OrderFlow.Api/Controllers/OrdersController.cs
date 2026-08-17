using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Orders.ChangeOrderStatus;
using OrderFlow.Application.Features.Orders.CreateOrder;
using OrderFlow.Application.Features.Orders.GetOrder;
using OrderFlow.Application.Features.Orders.ListOrders;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Shop-scoped manual orders. All actions require a JWT; EF global filters restrict rows to the claim <c>shopId</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    /// <summary>Paged list. <paramref name="pageSize"/> is 1–100 (default 20). Search matches customer name or phone.</summary>
    [HttpGet]
    public async Task<ActionResult<OrderListResponse>> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListOrdersQuery(search, status, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Loads one order with line snapshots. Other shops' ids return 404 because of tenant filters.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> Get(string id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrderQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a manual order. Prices are snapshotted from the catalog.
    /// Set <c>confirmImmediately</c> to reserve stock in the same transaction.
    /// </summary>
    /// <response code="403">Shop is at its plan orders-per-month cap.</response>
    /// <response code="404">A line product is missing.</response>
    /// <response code="409">Inactive product, insufficient stock, or stock concurrency.</response>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateOrderCommand(
                request.CustomerName,
                request.CustomerPhone,
                request.Notes,
                request.ConfirmImmediately,
                request.Lines.Select(l => new CreateOrderLineInput(l.ProductId, l.Quantity)).ToList()),
            cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// Moves status along Pending → Confirmed → Paid → Fulfilled, or Cancelled from Pending/Confirmed/Paid.
    /// Confirmed reserves stock; Paid writes a Deduct audit (stock already held); Cancelled from Confirmed/Paid releases stock.
    /// </summary>
    /// <response code="409">Illegal transition, stale <c>expectedVersion</c>, or stock concurrency.</response>
    [HttpPost("{id}/status")]
    public async Task<ActionResult<OrderDto>> ChangeStatus(
        string id,
        [FromBody] ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ChangeOrderStatusCommand(id, request.Status, request.ExpectedVersion),
            cancellationToken);

        return Ok(result);
    }
}

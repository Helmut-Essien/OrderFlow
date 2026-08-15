using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Features.Dashboard.GetDashboard;
using OrderFlow.Shared.DTOs.Dashboard;

namespace OrderFlow.Api.Controllers;

/// <summary>Shop home KPIs. Low-stock is live; sales/orders/WhatsApp stay 0 until those slices exist.</summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns dashboard numbers for the JWT shop.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDashboardQuery(), cancellationToken);
        return Ok(result);
    }
}

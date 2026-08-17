using MediatR;
using OrderFlow.Shared.DTOs.Dashboard;

namespace OrderFlow.Application.Features.Dashboard.GetDashboard;

/// <summary>Shop home KPIs. Sales and paid-order counts use the UTC date of <c>PaidAt</c> for orders still Paid or Fulfilled (Ghana is UTC).</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

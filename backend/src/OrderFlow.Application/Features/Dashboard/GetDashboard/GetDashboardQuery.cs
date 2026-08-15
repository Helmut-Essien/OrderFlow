using MediatR;
using OrderFlow.Shared.DTOs.Dashboard;

namespace OrderFlow.Application.Features.Dashboard.GetDashboard;

/// <summary>Shop home KPIs. Order/sales/WhatsApp counts stay 0 until those slices exist.</summary>
public sealed record GetDashboardQuery : IRequest<DashboardDto>;

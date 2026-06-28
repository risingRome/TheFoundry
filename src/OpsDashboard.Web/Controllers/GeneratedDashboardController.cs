using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;

namespace OpsDashboard.Web.Controllers;

[AllowAnonymous]
public sealed class GeneratedDashboardController(IGeneratedDashboardService dashboards) : Controller
{
    [HttpGet("/GeneratedDashboard/{datasetId:int}")]
    public async Task<IActionResult> Index(
        int datasetId,
        DateOnly? from,
        DateOnly? to,
        string? department,
        string? region,
        string? employee,
        string? product,
        string? category,
        CancellationToken cancellationToken)
    {
        var dashboard = await dashboards.GenerateAsync(
            datasetId,
            new AnalyticsFilterDto(from, to, department, region, employee, product, category),
            cancellationToken);
        return dashboard is null ? NotFound() : View(dashboard);
    }
}

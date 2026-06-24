using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Web.Controllers;

[AllowAnonymous]
public sealed class GeneratedDashboardController(IGeneratedDashboardService dashboards) : Controller
{
    [HttpGet("/GeneratedDashboard/{datasetId:int}")]
    public async Task<IActionResult> Index(int datasetId, CancellationToken cancellationToken)
    {
        var dashboard = await dashboards.GenerateAsync(datasetId, cancellationToken);
        return dashboard is null ? NotFound() : View(dashboard);
    }
}

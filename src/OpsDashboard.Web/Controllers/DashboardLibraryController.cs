using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.DashboardCatalog;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.DashboardLibraryRead)]
public sealed class DashboardLibraryController(IDashboardCatalogService dashboards) : Controller
{
    [HttpGet("/DashboardLibrary")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await dashboards.GetAllAsync(cancellationToken));
    }

    [HttpGet("/DashboardLibrary/Open/{id:int}")]
    public async Task<IActionResult> Open(int id, CancellationToken cancellationToken)
    {
        var dashboard = await dashboards.GetByIdAsync(id, cancellationToken);
        if (dashboard is null)
        {
            return NotFound();
        }

        return RedirectToAction("Index", "GeneratedDashboard", BuildRouteValues(dashboard.DatasetId, dashboard.Filters));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrAnalyst)]
    public async Task<IActionResult> Save(
        int datasetId,
        string name,
        DateOnly? from,
        DateOnly? to,
        string? department,
        string? region,
        string? employee,
        string? product,
        string? category,
        CancellationToken cancellationToken)
    {
        var filters = new AnalyticsFilterDto(from, to, department, region, employee, product, category);
        var id = await dashboards.SaveAsync(new SaveDashboardRequest(datasetId, name, filters), cancellationToken);
        if (id == 0)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Dashboard saved to the library.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrAnalyst)]
    public async Task<IActionResult> Rename(int id, string name, CancellationToken cancellationToken)
    {
        if (!await dashboards.RenameAsync(id, name, cancellationToken))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Dashboard renamed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrAnalyst)]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken)
    {
        if (!await dashboards.ArchiveAsync(id, cancellationToken))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Dashboard archived.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrAnalyst)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (!await dashboards.RestoreAsync(id, cancellationToken))
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Dashboard restored.";
        return RedirectToAction(nameof(Index));
    }

    private static object BuildRouteValues(int datasetId, AnalyticsFilterDto filters)
    {
        return new
        {
            datasetId,
            from = filters.From,
            to = filters.To,
            department = filters.Department,
            region = filters.Region,
            employee = filters.Employee,
            product = filters.Product,
            category = filters.Category
        };
    }
}

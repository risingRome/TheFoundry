using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.AdminAnalystExecutive)]
public sealed class RefreshCenterController(IDataRefreshService refresh) : Controller
{
    [HttpGet("/RefreshCenter")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await refresh.GetRefreshCenterAsync(cancellationToken));
    }

    [HttpGet("/RefreshCenter/History/{datasetId:int}")]
    public async Task<IActionResult> History(int datasetId, CancellationToken cancellationToken)
    {
        var model = await refresh.GetDatasetRefreshHistoryAsync(datasetId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("/RefreshCenter/Refresh/{datasetId:int}")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminOrAnalyst)]
    public async Task<IActionResult> Refresh(int datasetId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        var job = await refresh.RefreshDatasetAsync(datasetId, userId, userEmail, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = job.Status == OpsDashboard.Domain.Enums.DataRefreshStatus.Success
            ? "Dataset refreshed successfully."
            : $"Dataset refresh finished with status {job.Status}.";
        return RedirectToAction(nameof(History), new { datasetId });
    }
}

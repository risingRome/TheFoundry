using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Web.Controllers;

[AllowAnonymous]
public sealed class InsightsController(IBusinessIntelligenceService intelligence) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await intelligence.GetInsightsAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(int datasetId, CancellationToken cancellationToken)
    {
        var insight = await intelligence.RefreshInsightAsync(datasetId, cancellationToken);
        if (insight is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "Insight recommendations refreshed.";
        return RedirectToAction(nameof(Index));
    }
}

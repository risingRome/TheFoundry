using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.Assistant;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Web.Controllers;

[Authorize(Roles = AppRoles.AdminAnalystExecutive)]
public sealed class AssistantController(IExecutiveAssistantService assistant) : Controller
{
    [HttpGet("/Assistant/{datasetId:int}")]
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
        var filters = new AnalyticsFilterDto(from, to, department, region, employee, product, category);
        var model = await assistant.GetAssistantAsync(datasetId, filters, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("/Assistant/{datasetId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(
        int datasetId,
        string userMessage,
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var exchange = await assistant.AskAsync(new AssistantQuestionRequest(datasetId, userMessage, userId, filters), cancellationToken);
        if (exchange is null)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), BuildRouteValues(datasetId, filters));
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

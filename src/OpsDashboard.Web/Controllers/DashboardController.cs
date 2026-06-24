using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Web.Controllers;

public sealed class DashboardController(IDashboardService dashboards, UserManager<ApplicationUser> users) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Executive(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var range = ResolveRange(from, to);
        return View(await dashboards.GetExecutiveDashboardAsync(range.From, range.To, cancellationToken));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Team(int? id, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var current = await users.GetUserAsync(User);
        var isAnonymous = User.Identity?.IsAuthenticated != true;
        var teamId = isAnonymous
            ? id ?? 1
            : User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Analyst)
            ? id ?? current?.TeamId ?? 1
            : current?.TeamId;

        if (teamId is null) return Forbid();

        var range = ResolveRange(from, to);
        return View(await dashboards.GetTeamDashboardAsync(teamId.Value, range.From, range.To, cancellationToken));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Agent(int? id, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var current = await users.GetUserAsync(User);
        var isAnonymous = User.Identity?.IsAuthenticated != true;
        var agentId = isAnonymous
            ? id ?? 1
            : User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Analyst) || User.IsInRole(AppRoles.TeamLead)
            ? id ?? current?.AgentId ?? 1
            : current?.AgentId;

        if (agentId is null) return Forbid();

        var range = ResolveRange(from, to);
        return View(await dashboards.GetAgentDashboardAsync(agentId.Value, range.From, range.To, cancellationToken));
    }

    private static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-29);
        return (start, end);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Dashboards;
using OpsDashboard.Infrastructure.Identity;

namespace OpsDashboard.Web.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardApiController(IDashboardService dashboards) : ControllerBase
{
    [HttpGet("executive")]
    [Authorize(Roles = AppRoles.AdminAnalystExecutive)]
    public Task<ExecutiveDashboardDto> Executive(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        dashboards.GetExecutiveDashboardAsync(from, to, cancellationToken);

    [HttpGet("teams/{teamId:int}")]
    [Authorize(Roles = AppRoles.AdminAnalystExecutive)]
    public Task<TeamDashboardDto> Team(int teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        dashboards.GetTeamDashboardAsync(teamId, from, to, cancellationToken);

    [HttpGet("agents/{agentId:int}")]
    [Authorize(Roles = AppRoles.AdminAnalystExecutive)]
    public Task<AgentDashboardDto> Agent(int agentId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        dashboards.GetAgentDashboardAsync(agentId, from, to, cancellationToken);
}

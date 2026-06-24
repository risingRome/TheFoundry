using OpsDashboard.Application.Dashboards;

namespace OpsDashboard.Application.Abstractions;

public interface IDashboardService
{
    Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<TeamDashboardDto> GetTeamDashboardAsync(int teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task<AgentDashboardDto> GetAgentDashboardAsync(int agentId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}

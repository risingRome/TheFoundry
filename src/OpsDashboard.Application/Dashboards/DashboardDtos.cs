using OpsDashboard.Application.Kpis;

namespace OpsDashboard.Application.Dashboards;

public sealed record TrendPointDto(string Label, decimal Value);
public sealed record TeamBreakdownDto(int TeamId, string TeamName, decimal AvgTatHours, decimal SlaBreachRate, decimal Productivity, int Backlog);
public sealed record AgentBreakdownDto(int AgentId, string AgentName, decimal AvgTatHours, decimal SlaBreachRate, decimal Productivity, int ClosedCases);
public sealed record AgingBucketDto(string Bucket, int Count);

public sealed class ExecutiveDashboardDto
{
    public IReadOnlyList<KpiCardDto> Kpis { get; init; } = Array.Empty<KpiCardDto>();
    public IReadOnlyList<TeamBreakdownDto> Teams { get; init; } = Array.Empty<TeamBreakdownDto>();
    public IReadOnlyList<TrendPointDto> TatTrend { get; init; } = Array.Empty<TrendPointDto>();
    public IReadOnlyList<TrendPointDto> BacklogTrend { get; init; } = Array.Empty<TrendPointDto>();
    public DateTime? LastUpdatedUtc { get; init; }
}

public sealed class TeamDashboardDto
{
    public string TeamName { get; init; } = string.Empty;
    public IReadOnlyList<KpiCardDto> Kpis { get; init; } = Array.Empty<KpiCardDto>();
    public IReadOnlyList<AgentBreakdownDto> Agents { get; init; } = Array.Empty<AgentBreakdownDto>();
    public IReadOnlyList<AgingBucketDto> AgingBuckets { get; init; } = Array.Empty<AgingBucketDto>();
}

public sealed class AgentDashboardDto
{
    public string AgentName { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public IReadOnlyList<KpiCardDto> Kpis { get; init; } = Array.Empty<KpiCardDto>();
    public IReadOnlyList<TrendPointDto> ProductivityTrend { get; init; } = Array.Empty<TrendPointDto>();
}

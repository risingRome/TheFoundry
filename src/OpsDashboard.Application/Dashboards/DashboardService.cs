using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Kpis;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.Dashboards;

public sealed class DashboardService(IOpsDashboardDbContext db) : IDashboardService
{
    public async Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var cases = await db.OperationsCases
            .AsNoTracking()
            .Include(c => c.Team)
            .Include(c => c.CaseType)
            .Where(c => c.CreatedAtUtc <= toUtc && (c.ResolvedAtUtc == null || c.ResolvedAtUtc >= fromUtc))
            .ToListAsync(cancellationToken);

        var closed = cases.Where(IsResolvedInRange(fromUtc, toUtc)).ToList();
        var workingDays = CountWorkingDays(from, to);
        var avgTat = AverageTat(closed);
        var breachRate = SlaBreachRate(closed);
        var productivity = SafeDivide(closed.Count, workingDays);
        var backlog = cases.Count(c => c.Status == CaseStatus.Open || c.ResolvedAtUtc > toUtc);
        var fcr = FcrRate(closed);

        var teamRows = cases.GroupBy(c => new { c.TeamId, c.Team.Name })
            .Select(g =>
            {
                var teamClosed = g.Where(IsResolvedInRange(fromUtc, toUtc)).ToList();
                return new TeamBreakdownDto(
                    g.Key.TeamId,
                    g.Key.Name,
                    AverageTat(teamClosed),
                    SlaBreachRate(teamClosed),
                    SafeDivide(teamClosed.Count, workingDays),
                    g.Count(c => c.Status == CaseStatus.Open || c.ResolvedAtUtc > toUtc));
            })
            .OrderByDescending(t => t.SlaBreachRate)
            .ToList();

        return new ExecutiveDashboardDto
        {
            Kpis = BuildKpis(avgTat, breachRate, productivity, backlog, fcr),
            Teams = teamRows,
            TatTrend = closed.GroupBy(c => c.ResolvedAtUtc!.Value.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new TrendPointDto(g.Key, AverageTat(g.ToList())))
                .ToList(),
            BacklogTrend = BuildBacklogTrend(cases, from, to),
            LastUpdatedUtc = await db.DataFreshness.AsNoTracking().MaxAsync(x => (DateTime?)x.RefreshedAtUtc, cancellationToken)
        };
    }

    public async Task<TeamDashboardDto> GetTeamDashboardAsync(int teamId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var cases = await db.OperationsCases
            .AsNoTracking()
            .Include(c => c.Team)
            .Include(c => c.Agent)
            .Include(c => c.CaseType)
            .Where(c => c.TeamId == teamId && c.CreatedAtUtc <= toUtc && (c.ResolvedAtUtc == null || c.ResolvedAtUtc >= fromUtc))
            .ToListAsync(cancellationToken);

        var closed = cases.Where(IsResolvedInRange(fromUtc, toUtc)).ToList();
        var workingDays = CountWorkingDays(from, to);

        return new TeamDashboardDto
        {
            TeamName = cases.FirstOrDefault()?.Team.Name ?? "Team",
            Kpis = BuildKpis(AverageTat(closed), SlaBreachRate(closed), SafeDivide(closed.Count, workingDays), cases.Count(c => c.Status == CaseStatus.Open), FcrRate(closed)),
            Agents = cases.GroupBy(c => new { c.AgentId, c.Agent.DisplayName })
                .Select(g =>
                {
                    var agentClosed = g.Where(IsResolvedInRange(fromUtc, toUtc)).ToList();
                    return new AgentBreakdownDto(g.Key.AgentId, g.Key.DisplayName, AverageTat(agentClosed), SlaBreachRate(agentClosed), SafeDivide(agentClosed.Count, workingDays), agentClosed.Count);
                })
                .OrderByDescending(a => a.ClosedCases)
                .ToList(),
            AgingBuckets = BuildAgingBuckets(cases.Where(c => c.Status == CaseStatus.Open), toUtc)
        };
    }

    public async Task<AgentDashboardDto> GetAgentDashboardAsync(int agentId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var cases = await db.OperationsCases
            .AsNoTracking()
            .Include(c => c.Agent)
            .Include(c => c.Team)
            .Include(c => c.CaseType)
            .Where(c => c.AgentId == agentId && c.CreatedAtUtc <= toUtc && (c.ResolvedAtUtc == null || c.ResolvedAtUtc >= fromUtc))
            .ToListAsync(cancellationToken);

        var closed = cases.Where(IsResolvedInRange(fromUtc, toUtc)).ToList();
        var workingDays = CountWorkingDays(from, to);

        return new AgentDashboardDto
        {
            AgentName = cases.FirstOrDefault()?.Agent.DisplayName ?? "Agent",
            TeamName = cases.FirstOrDefault()?.Team.Name ?? "Team",
            Kpis = BuildKpis(AverageTat(closed), SlaBreachRate(closed), SafeDivide(closed.Count, workingDays), cases.Count(c => c.Status == CaseStatus.Open), FcrRate(closed)),
            ProductivityTrend = closed.GroupBy(c => DateOnly.FromDateTime(c.ResolvedAtUtc!.Value).ToString("yyyy-MM-dd"))
                .OrderBy(g => g.Key)
                .Select(g => new TrendPointDto(g.Key, g.Count()))
                .ToList()
        };
    }

    private static Func<Domain.Entities.OperationsCase, bool> IsResolvedInRange(DateTime fromUtc, DateTime toUtc) =>
        c => c.ResolvedAtUtc >= fromUtc && c.ResolvedAtUtc <= toUtc && c.Status is CaseStatus.Resolved or CaseStatus.Reopened;

    private static IReadOnlyList<KpiCardDto> BuildKpis(decimal avgTat, decimal breachRate, decimal productivity, int backlog, decimal fcr) =>
    [
        new("Average TAT", avgTat, "hours", 24m, KpiThresholds.AverageTat(avgTat)),
        new("SLA Breach Rate", breachRate, "%", 5m, KpiThresholds.SlaBreachRate(breachRate)),
        new("Productivity", productivity, "cases/day", 45m, KpiThresholds.Productivity(productivity)),
        new("Backlog Volume", backlog, "cases", 250m, backlog <= 250 ? KpiStatus.Green : KpiStatus.Red),
        new("FCR", fcr, "%", 80m, KpiThresholds.Fcr(fcr))
    ];

    private static decimal AverageTat(IReadOnlyCollection<Domain.Entities.OperationsCase> cases)
    {
        var closed = cases.Where(c => c.ResolvedAtUtc.HasValue).ToList();
        return closed.Count == 0 ? 0m : Math.Round(closed.Average(c => (decimal)(c.ResolvedAtUtc!.Value - c.CreatedAtUtc).TotalHours), 2);
    }

    private static decimal SlaBreachRate(IReadOnlyCollection<Domain.Entities.OperationsCase> cases)
    {
        var closed = cases.Where(c => c.ResolvedAtUtc.HasValue).ToList();
        if (closed.Count == 0) return 0m;
        var breached = closed.Count(c => (decimal)(c.ResolvedAtUtc!.Value - c.CreatedAtUtc).TotalHours > c.CaseType.SlaThresholdHours);
        return Math.Round(SafeDivide(breached * 100m, closed.Count), 2);
    }

    private static decimal FcrRate(IReadOnlyCollection<Domain.Entities.OperationsCase> cases)
    {
        if (cases.Count == 0) return 0m;
        var fcr = cases.Count(c => !c.Escalated && !c.Reopened);
        return Math.Round(SafeDivide(fcr * 100m, cases.Count), 2);
    }

    private static IReadOnlyList<AgingBucketDto> BuildAgingBuckets(IEnumerable<Domain.Entities.OperationsCase> openCases, DateTime asOfUtc)
    {
        var buckets = new Dictionary<string, int> { ["0-4h"] = 0, ["4-12h"] = 0, ["12-24h"] = 0, ["24h+"] = 0 };
        foreach (var c in openCases)
        {
            var age = (asOfUtc - c.CreatedAtUtc).TotalHours;
            buckets[age < 4 ? "0-4h" : age < 12 ? "4-12h" : age < 24 ? "12-24h" : "24h+"]++;
        }
        return buckets.Select(kv => new AgingBucketDto(kv.Key, kv.Value)).ToList();
    }

    private static IReadOnlyList<TrendPointDto> BuildBacklogTrend(IReadOnlyCollection<Domain.Entities.OperationsCase> cases, DateOnly from, DateOnly to)
    {
        var points = new List<TrendPointDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var end = day.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            var backlog = cases.Count(c => c.CreatedAtUtc <= end && (c.ResolvedAtUtc == null || c.ResolvedAtUtc > end));
            points.Add(new TrendPointDto(day.ToString("yyyy-MM-dd"), backlog));
        }
        return points;
    }

    private static int CountWorkingDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count++;
        return Math.Max(count, 1);
    }

    private static decimal SafeDivide(decimal numerator, decimal denominator) => denominator == 0 ? 0 : Math.Round(numerator / denominator, 2);
}

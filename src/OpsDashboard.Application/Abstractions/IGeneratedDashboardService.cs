using OpsDashboard.Application.GeneratedDashboards;
using OpsDashboard.Application.Analytics;

namespace OpsDashboard.Application.Abstractions;

public interface IGeneratedDashboardService
{
    Task<GeneratedDashboardDto?> GenerateAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default);
}

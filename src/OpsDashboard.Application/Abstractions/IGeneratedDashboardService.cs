using OpsDashboard.Application.GeneratedDashboards;

namespace OpsDashboard.Application.Abstractions;

public interface IGeneratedDashboardService
{
    Task<GeneratedDashboardDto?> GenerateAsync(int datasetId, CancellationToken cancellationToken = default);
}

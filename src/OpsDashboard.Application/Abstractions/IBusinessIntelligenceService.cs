using OpsDashboard.Application.Insights;

namespace OpsDashboard.Application.Abstractions;

public interface IBusinessIntelligenceService
{
    Task<IReadOnlyList<DatasetInsightDto>> GetInsightsAsync(CancellationToken cancellationToken = default);
    Task<DatasetInsightDto?> GetInsightAsync(int datasetId, CancellationToken cancellationToken = default);
    Task<DatasetInsightDto?> RefreshInsightAsync(int datasetId, CancellationToken cancellationToken = default);
}

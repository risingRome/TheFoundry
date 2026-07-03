using OpsDashboard.Application.DataRefresh;

namespace OpsDashboard.Application.Abstractions;

public interface IDataRefreshService
{
    Task<DataRefreshJobDto?> RefreshDatasetAsync(int datasetId, string? userId, string? userEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshCenterDatasetDto>> GetRefreshCenterAsync(CancellationToken cancellationToken = default);
    Task<DatasetRefreshHistoryDto?> GetDatasetRefreshHistoryAsync(int datasetId, CancellationToken cancellationToken = default);
}

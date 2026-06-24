using OpsDashboard.Application.Datasets;

namespace OpsDashboard.Application.Abstractions;

public interface IDatasetExplorerService
{
    Task<DatasetPreviewDto?> GetPreviewAsync(int id, int rowLimit = 100, CancellationToken cancellationToken = default);
}

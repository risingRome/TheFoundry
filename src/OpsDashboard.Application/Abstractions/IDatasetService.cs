using OpsDashboard.Application.Datasets;

namespace OpsDashboard.Application.Abstractions;

public interface IDatasetService
{
    Task<IReadOnlyList<DatasetListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DatasetDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DatasetNameExistsAsync(string datasetName, CancellationToken cancellationToken = default);
    Task<int> RegisterUploadAsync(DatasetUploadRequest request, CancellationToken cancellationToken = default);
}

using OpsDashboard.Application.Datasets;

namespace OpsDashboard.Application.Abstractions;

public interface IDatasetProfileReader
{
    bool CanRead(string sourceType);
    Task<IReadOnlyList<IReadOnlyList<string>>> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}

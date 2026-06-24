using OpsDashboard.Application.Datasets;

namespace OpsDashboard.Application.Abstractions;

public interface IDatasetFileProfiler
{
    Task<DatasetProfileResult> ProfileAsync(string filePath, string sourceType, CancellationToken cancellationToken = default);
}

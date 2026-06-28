using OpsDashboard.Application.Reports;
using OpsDashboard.Application.Analytics;

namespace OpsDashboard.Application.Abstractions;

public interface IExecutiveReportService
{
    Task<IReadOnlyList<ReportDatasetListItemDto>> GetReportDatasetsAsync(CancellationToken cancellationToken = default);
    Task<ExecutiveReportDto?> GenerateReportAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default);
}

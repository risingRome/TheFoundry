using OpsDashboard.Application.Analytics;
using OpsDashboard.Domain.Enums;

namespace OpsDashboard.Application.DashboardCatalog;

public sealed record DashboardCatalogItemDto(
    int Id,
    int DatasetId,
    string Name,
    string DatasetName,
    string DomainName,
    DashboardStatus Status,
    DateTime CreatedDate,
    DateTime UpdatedDate,
    AnalyticsFilterDto Filters);

public sealed record SaveDashboardRequest(int DatasetId, string Name, AnalyticsFilterDto Filters);

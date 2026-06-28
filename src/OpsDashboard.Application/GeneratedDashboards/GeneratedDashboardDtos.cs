namespace OpsDashboard.Application.GeneratedDashboards;

using OpsDashboard.Application.Analytics;

public sealed record GeneratedDashboardDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    string BusinessDomain,
    AnalyticsFilterStateDto Filters,
    int FilteredRecordCount,
    IReadOnlyList<GeneratedKpiCardDto> Kpis,
    IReadOnlyList<GeneratedChartDto> TrendCharts,
    IReadOnlyList<GeneratedChartDto> ComparisonCharts,
    IReadOnlyList<string> SummaryColumns,
    IReadOnlyList<IReadOnlyList<string>> SummaryRows);

public sealed record GeneratedKpiCardDto(
    string Title,
    string Value,
    string Description);

public sealed record GeneratedChartDto(
    string Id,
    string Title,
    string Subtitle,
    string ChartType,
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> Values);

namespace OpsDashboard.Application.Insights;

public sealed record DatasetInsightDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    string BusinessDomain,
    DateTime GeneratedAtUtc,
    IReadOnlyList<DetectedBusinessEntityDto> DetectedMeasures,
    IReadOnlyList<DetectedBusinessEntityDto> DetectedDimensions,
    IReadOnlyList<string> RecommendedKpis,
    IReadOnlyList<string> RecommendedCharts,
    IReadOnlyList<string> RecommendedDashboardLayout);

public sealed record DetectedBusinessEntityDto(
    string Entity,
    string ColumnName,
    string DetectedType);

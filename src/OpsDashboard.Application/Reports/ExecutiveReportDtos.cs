namespace OpsDashboard.Application.Reports;

using OpsDashboard.Application.Analytics;

public sealed record ReportDatasetListItemDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    string SourceType,
    DateTime UploadDate,
    decimal DatasetQualityScore);

public sealed record ExecutiveReportDto(
    int DatasetId,
    string DatasetName,
    string DomainName,
    string SourceType,
    DateTime UploadDate,
    DateTime GeneratedAtUtc,
    AnalyticsFilterStateDto Filters,
    ExecutiveSummaryDto Summary,
    IReadOnlyList<ReportKpiDto> Kpis,
    IReadOnlyList<ReportChartDto> Charts,
    IReadOnlyList<string> TrendAnalysis,
    IReadOnlyList<DimensionRankingDto> DimensionRankings,
    IReadOnlyList<string> KeyFindings,
    IReadOnlyList<string> Recommendations);

public sealed record ExecutiveSummaryDto(
    int TotalRecords,
    int TotalColumns,
    decimal DataQualityScore,
    int MissingValues,
    int DuplicateRows);

public sealed record ReportKpiDto(
    string Title,
    string Value,
    string Description);

public sealed record ReportChartDto(
    string Id,
    string Title,
    string Rule,
    string ChartType,
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> Values);

public sealed record DimensionRankingDto(
    string DimensionName,
    string MeasureName,
    IReadOnlyList<DimensionRankingRowDto> Rows);

public sealed record DimensionRankingRowDto(
    string Label,
    decimal Value,
    int Rank);

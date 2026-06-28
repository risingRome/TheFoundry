using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.Datasets;
using OpsDashboard.Application.GeneratedDashboards;
using OpsDashboard.Application.Insights;

namespace OpsDashboard.Application.Reports;

public sealed class ExecutiveReportService(
    IDatasetService datasets,
    IDatasetExplorerService explorer,
    IBusinessIntelligenceService intelligence,
    IGeneratedDashboardService dashboards) : IExecutiveReportService
{
    private static readonly string[] MeasurePriority = ["Revenue", "Sales", "Profit", "Quantity", "Cost", "Margin"];
    private static readonly string[] RankingDimensions = ["Region", "Product", "Department"];

    public async Task<IReadOnlyList<ReportDatasetListItemDto>> GetReportDatasetsAsync(CancellationToken cancellationToken = default)
    {
        var allDatasets = await datasets.GetAllAsync(cancellationToken);
        return allDatasets
            .Select(x => new ReportDatasetListItemDto(
                x.Id,
                x.DatasetName,
                x.DomainName,
                x.SourceType,
                x.UploadDate,
                x.DatasetQualityScore))
            .ToList();
    }

    public async Task<ExecutiveReportDto?> GenerateReportAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default)
    {
        filters ??= AnalyticsFilterDto.Empty;
        var details = await datasets.GetByIdAsync(datasetId, cancellationToken);
        var preview = await explorer.GetPreviewAsync(datasetId, 100, cancellationToken);
        var insight = await intelligence.GetInsightAsync(datasetId, cancellationToken)
            ?? await intelligence.RefreshInsightAsync(datasetId, cancellationToken);
        var dashboard = await dashboards.GenerateAsync(datasetId, filters, cancellationToken);

        if (details is null || preview is null || insight is null || dashboard is null)
        {
            return null;
        }

        var columnIndex = preview.Columns
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
        var filteredPreviewRows = ApplyFilters(preview, filters).ToList();
        var summary = BuildSummary(details.ColumnCount, filteredPreviewRows, dashboard.FilteredRecordCount);

        var filteredPreview = preview with { Rows = filteredPreviewRows };
        var kpis = BuildKpis(filteredPreview, insight, dashboard, columnIndex);
        var charts = BuildCharts(dashboard);
        var trendAnalysis = BuildTrendAnalysis(dashboard);
        var rankings = BuildDimensionRankings(filteredPreview, insight, columnIndex);
        var findings = BuildFindings(summary, rankings, dashboard);
        var recommendations = BuildRecommendations(summary, trendAnalysis, rankings);

        return new ExecutiveReportDto(
            datasetId,
            details.DatasetName,
            details.DomainName,
            details.SourceType,
            details.UploadDate,
            DateTime.UtcNow,
            dashboard.Filters,
            summary,
            kpis,
            charts,
            trendAnalysis,
            rankings,
            findings,
            recommendations);
    }

    private static ExecutiveSummaryDto BuildSummary(
        int totalColumns,
        IReadOnlyList<IReadOnlyList<string>> filteredRows,
        int filteredRecordCount)
    {
        var missingValues = filteredRows.Sum(row => row.Count(string.IsNullOrWhiteSpace));
        var duplicateRows = filteredRows
            .GroupBy(row => string.Join('\u001f', row), StringComparer.OrdinalIgnoreCase)
            .Sum(group => Math.Max(0, group.Count() - 1));
        var totalCells = Math.Max(1, filteredRows.Count * Math.Max(1, totalColumns));
        var qualityScore = Math.Clamp(100m - ((decimal)missingValues / totalCells * 55m) - (filteredRows.Count == 0 ? 0m : (decimal)duplicateRows / filteredRows.Count * 35m), 0m, 100m);

        return new ExecutiveSummaryDto(
            filteredRecordCount,
            totalColumns,
            Math.Round(qualityScore, 2),
            missingValues,
            duplicateRows);
    }

    private static IEnumerable<IReadOnlyList<string>> ApplyFilters(DatasetPreviewDto preview, AnalyticsFilterDto filters)
    {
        var columnIndex = preview.Columns
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);
        var filterColumns = ResolveFilterColumns(preview);

        return preview.Rows.Where(row =>
        {
            if (filterColumns.TryGetValue("date", out var dateColumn) && columnIndex.TryGetValue(dateColumn, out var dateIndex))
            {
                var value = ReadDate(row, dateIndex);
                if (filters.From.HasValue && (!value.HasValue || DateOnly.FromDateTime(value.Value) < filters.From.Value))
                {
                    return false;
                }

                if (filters.To.HasValue && (!value.HasValue || DateOnly.FromDateTime(value.Value) > filters.To.Value))
                {
                    return false;
                }
            }

            return MatchesTextFilter(row, columnIndex, filterColumns, "department", filters.Department)
                && MatchesTextFilter(row, columnIndex, filterColumns, "region", filters.Region)
                && MatchesTextFilter(row, columnIndex, filterColumns, "employee", filters.Employee)
                && MatchesTextFilter(row, columnIndex, filterColumns, "product", filters.Product)
                && MatchesTextFilter(row, columnIndex, filterColumns, "category", filters.Category);
        });
    }

    private static IReadOnlyDictionary<string, string> ResolveFilterColumns(DatasetPreviewDto preview)
    {
        var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddFilter(columns, "date", preview.Schema.FirstOrDefault(x => x.DetectedType == "Date")?.Name ?? FindColumnByName(preview.Columns, "date", "month", "period"));
        AddFilter(columns, "department", FindColumnByName(preview.Columns, "department", "dept"));
        AddFilter(columns, "region", FindColumnByName(preview.Columns, "region", "territory", "market", "country", "state"));
        AddFilter(columns, "employee", FindColumnByName(preview.Columns, "employee", "agent", "staff", "rep"));
        AddFilter(columns, "product", FindColumnByName(preview.Columns, "product", "sku", "item"));
        AddFilter(columns, "category", FindColumnByName(preview.Columns, "category", "segment", "class"));
        return columns;
    }

    private static void AddFilter(IDictionary<string, string> columns, string key, string? column)
    {
        if (!string.IsNullOrWhiteSpace(column))
        {
            columns[key] = column;
        }
    }

    private static string? FindColumnByName(IEnumerable<string> columns, params string[] keywords)
    {
        return columns.FirstOrDefault(column =>
        {
            var normalized = column.Replace("_", " ", StringComparison.Ordinal).Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant();
            return keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static bool MatchesTextFilter(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> columnIndex,
        IReadOnlyDictionary<string, string> filterColumns,
        string key,
        string? selectedValue)
    {
        if (string.IsNullOrWhiteSpace(selectedValue) || !filterColumns.TryGetValue(key, out var column) || !columnIndex.TryGetValue(column, out var position))
        {
            return true;
        }

        return string.Equals(ReadText(row, position), selectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ReportKpiDto> BuildKpis(
        DatasetPreviewDto preview,
        DatasetInsightDto insight,
        GeneratedDashboardDto dashboard,
        IReadOnlyDictionary<string, int> columnIndex)
    {
        var kpis = dashboard.Kpis
            .Select(x => new ReportKpiDto(x.Title, x.Value, x.Description))
            .ToList();

        foreach (var entity in MeasurePriority)
        {
            var column = FindColumn(insight.DetectedMeasures, entity);
            if (column is null || !columnIndex.TryGetValue(column, out var index))
            {
                continue;
            }

            var values = preview.Rows
                .Select(row => ReadDecimal(row, index))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            if (values.Count == 0)
            {
                continue;
            }

            kpis.Add(new ReportKpiDto($"Average {entity}", FormatNumber(values.Average()), $"Average value from {column}"));
            kpis.Add(new ReportKpiDto($"Highest {entity}", FormatNumber(values.Max()), $"Highest observed value from {column}"));
            kpis.Add(new ReportKpiDto($"Lowest {entity}", FormatNumber(values.Min()), $"Lowest observed value from {column}"));
        }

        return kpis
            .DistinctBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private static IReadOnlyList<ReportChartDto> BuildCharts(GeneratedDashboardDto dashboard)
    {
        return dashboard.TrendCharts
            .Concat(dashboard.ComparisonCharts)
            .Select(x => new ReportChartDto(x.Id, x.Title, x.Subtitle, x.ChartType, x.Labels, x.Values))
            .ToList();
    }

    private static IReadOnlyList<string> BuildTrendAnalysis(GeneratedDashboardDto dashboard)
    {
        var statements = new List<string>();
        foreach (var chart in dashboard.TrendCharts)
        {
            if (chart.Values.Count < 2)
            {
                continue;
            }

            var first = chart.Values.First();
            var last = chart.Values.Last();
            var measure = chart.Title.Replace(" Trend", string.Empty, StringComparison.OrdinalIgnoreCase);
            var threshold = Math.Abs(first) * 0.05m;

            if (last > first + threshold)
            {
                statements.Add($"{measure} increased over time.");
            }
            else if (last < first - threshold)
            {
                statements.Add($"{measure} decreased over time.");
            }
            else
            {
                statements.Add($"Stable {measure.ToLowerInvariant()} trend detected.");
            }
        }

        return statements.Count == 0 ? ["No date-based trend analysis was generated for this dataset."] : statements;
    }

    private static IReadOnlyList<DimensionRankingDto> BuildDimensionRankings(
        DatasetPreviewDto preview,
        DatasetInsightDto insight,
        IReadOnlyDictionary<string, int> columnIndex)
    {
        var measureColumn = MeasurePriority
            .Select(entity => new { Entity = entity, Column = FindColumn(insight.DetectedMeasures, entity) })
            .FirstOrDefault(x => x.Column is not null && columnIndex.ContainsKey(x.Column));

        if (measureColumn is null || measureColumn.Column is null)
        {
            return Array.Empty<DimensionRankingDto>();
        }

        var rankings = new List<DimensionRankingDto>();
        foreach (var dimension in RankingDimensions)
        {
            var dimensionColumn = FindColumn(insight.DetectedDimensions, dimension);
            if (dimensionColumn is null || !columnIndex.TryGetValue(dimensionColumn, out var dimensionIndex) || !columnIndex.TryGetValue(measureColumn.Column, out var measureIndex))
            {
                continue;
            }

            var rows = preview.Rows
                .Select(row => new { Label = ReadText(row, dimensionIndex), Value = ReadDecimal(row, measureIndex) ?? 0m })
                .Where(x => x.Label.Length > 0)
                .GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
                .Select(x => new { Label = x.Key, Value = x.Sum(y => y.Value) })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .Select((x, index) => new DimensionRankingRowDto(x.Label, x.Value, index + 1))
                .ToList();

            if (rows.Count > 0)
            {
                rankings.Add(new DimensionRankingDto(dimension, measureColumn.Entity, rows));
            }
        }

        return rankings;
    }

    private static IReadOnlyList<string> BuildFindings(
        ExecutiveSummaryDto summary,
        IReadOnlyList<DimensionRankingDto> rankings,
        GeneratedDashboardDto dashboard)
    {
        var findings = new List<string>();
        foreach (var ranking in rankings)
        {
            var highest = ranking.Rows.FirstOrDefault();
            var lowest = ranking.Rows.LastOrDefault();
            if (highest is not null)
            {
                findings.Add($"Highest {ranking.MeasureName.ToLowerInvariant()} {ranking.DimensionName.ToLowerInvariant()}: {highest.Label}.");
                findings.Add($"Largest contributor: {highest.Label} represents {FormatNumber(highest.Value)} in {ranking.MeasureName.ToLowerInvariant()}.");
            }

            if (lowest is not null && lowest.Label != highest?.Label)
            {
                findings.Add($"Lowest {ranking.MeasureName.ToLowerInvariant()} {ranking.DimensionName.ToLowerInvariant()}: {lowest.Label}.");
            }
        }

        if (summary.MissingValues > 0)
        {
            findings.Add($"{summary.MissingValues:N0} missing values detected.");
        }

        if (summary.DataQualityScore >= 95m)
        {
            findings.Add("Excellent data quality detected.");
        }

        if (dashboard.TrendCharts.Count == 0 && rankings.Count == 0)
        {
            findings.Add("Dataset does not contain enough recognized business dimensions for automated performance breakdowns.");
        }

        return findings.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }

    private static IReadOnlyList<string> BuildRecommendations(
        ExecutiveSummaryDto summary,
        IReadOnlyList<string> trendAnalysis,
        IReadOnlyList<DimensionRankingDto> rankings)
    {
        var recommendations = new List<string>();
        foreach (var ranking in rankings)
        {
            var top = ranking.Rows.FirstOrDefault();
            var second = ranking.Rows.Skip(1).FirstOrDefault();
            if (top is not null && second is not null && second.Value > 0 && top.Value / second.Value >= 1.5m)
            {
                recommendations.Add($"Consider increasing investment in the highest-performing {ranking.DimensionName.ToLowerInvariant()}: {top.Label}.");
            }
        }

        if (summary.DataQualityScore < 90m)
        {
            recommendations.Add("Improve data quality before making strategic decisions.");
        }

        if (trendAnalysis.Any(x => x.Contains("decreased", StringComparison.OrdinalIgnoreCase)))
        {
            recommendations.Add("Investigate declining performance and identify drivers behind the negative trend.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Continue monitoring this dataset as additional records are uploaded.");
        }

        return recommendations.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string? FindColumn(IEnumerable<DetectedBusinessEntityDto> entities, string entity)
    {
        return entities.FirstOrDefault(x => string.Equals(x.Entity, entity, StringComparison.OrdinalIgnoreCase))?.ColumnName;
    }

    private static decimal? ReadDecimal(IReadOnlyList<string> row, int index)
    {
        return index < row.Count && decimal.TryParse(row[index], out var value) ? value : null;
    }

    private static DateTime? ReadDate(IReadOnlyList<string> row, int index)
    {
        return index < row.Count && DateTime.TryParse(row[index], out var value) ? value : null;
    }

    private static string ReadText(IReadOnlyList<string> row, int index)
    {
        return index < row.Count ? row[index].Trim() : string.Empty;
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString(value % 1 == 0 ? "N0" : "N2");
    }
}

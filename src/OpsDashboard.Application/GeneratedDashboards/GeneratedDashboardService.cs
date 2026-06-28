using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.Insights;

namespace OpsDashboard.Application.GeneratedDashboards;

public sealed class GeneratedDashboardService(
    IOpsDashboardDbContext db,
    IEnumerable<IDatasetProfileReader> readers,
    IBusinessIntelligenceService intelligence) : IGeneratedDashboardService
{
    public async Task<GeneratedDashboardDto?> GenerateAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default)
    {
        filters ??= AnalyticsFilterDto.Empty;
        var dataset = await db.Datasets
            .AsNoTracking()
            .Where(x => x.Id == datasetId)
            .Select(x => new
            {
                x.Id,
                x.DatasetName,
                x.SourceType,
                x.FilePath,
                DomainName = x.Domain!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dataset is null || !File.Exists(dataset.FilePath))
        {
            return null;
        }

        var insight = await intelligence.GetInsightAsync(datasetId, cancellationToken)
            ?? await intelligence.RefreshInsightAsync(datasetId, cancellationToken);
        if (insight is null)
        {
            return null;
        }

        var reader = readers.FirstOrDefault(x => x.CanRead(dataset.SourceType));
        if (reader is null)
        {
            throw new NotSupportedException($"Source type '{dataset.SourceType}' is not supported.");
        }

        var rawRows = await reader.ReadAsync(dataset.FilePath, cancellationToken);
        if (rawRows.Count == 0)
        {
            return null;
        }

        var columnCount = rawRows.Max(x => x.Count);
        var columns = BuildColumns(rawRows[0], columnCount);
        var rows = rawRows
            .Skip(1)
            .Select(row => NormalizeRow(row, columnCount))
            .Where(row => row.Any(value => value.Length > 0))
            .ToList();
        var index = columns
            .Select((name, position) => new { name, position })
            .ToDictionary(x => x.name, x => x.position, StringComparer.OrdinalIgnoreCase);
        var filterColumns = ResolveFilterColumns(insight, columns);
        var filteredRows = ApplyFilters(rows, index, filterColumns, filters).ToList();
        var filterState = BuildFilterState(dataset.DatasetName, insight.BusinessDomain, rows, index, filterColumns, filters);

        var kpis = BuildKpis(insight, filteredRows, index);
        var trendCharts = BuildTrendCharts(insight, filteredRows, index);
        var comparisonCharts = BuildComparisonCharts(insight, filteredRows, index);

        return new GeneratedDashboardDto(
            dataset.Id,
            dataset.DatasetName,
            dataset.DomainName,
            insight.BusinessDomain,
            filterState,
            filteredRows.Count,
            kpis,
            trendCharts,
            comparisonCharts,
            columns,
            filteredRows.Take(25).ToList());
    }

    private static IReadOnlyDictionary<string, string> ResolveFilterColumns(DatasetInsightDto insight, IReadOnlyList<string> columns)
    {
        var filterColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddFilterColumn(filterColumns, "date", FindColumn(insight.DetectedDimensions, "Date"));
        AddFilterColumn(filterColumns, "department", FindColumn(insight.DetectedDimensions, "Department") ?? FindColumnByName(columns, "department", "dept"));
        AddFilterColumn(filterColumns, "region", FindColumn(insight.DetectedDimensions, "Region") ?? FindColumnByName(columns, "region", "territory", "market", "country", "state"));
        AddFilterColumn(filterColumns, "employee", FindColumn(insight.DetectedDimensions, "Employee") ?? FindColumnByName(columns, "employee", "agent", "staff", "rep"));
        AddFilterColumn(filterColumns, "product", FindColumn(insight.DetectedDimensions, "Product") ?? FindColumnByName(columns, "product", "sku", "item"));
        AddFilterColumn(filterColumns, "category", FindColumnByName(columns, "category", "segment", "class"));
        return filterColumns;
    }

    private static void AddFilterColumn(IDictionary<string, string> filterColumns, string key, string? column)
    {
        if (!string.IsNullOrWhiteSpace(column))
        {
            filterColumns[key] = column;
        }
    }

    private static string? FindColumnByName(IEnumerable<string> columns, params string[] keywords)
    {
        return columns.FirstOrDefault(column =>
        {
            var normalized = Normalize(column);
            return keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        });
    }

    private static IEnumerable<IReadOnlyList<string>> ApplyFilters(
        IEnumerable<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, string> filterColumns,
        AnalyticsFilterDto filters)
    {
        return rows.Where(row =>
        {
            if (filterColumns.TryGetValue("date", out var dateColumn) && index.TryGetValue(dateColumn, out var dateIndex))
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

            return MatchesTextFilter(row, index, filterColumns, "department", filters.Department)
                && MatchesTextFilter(row, index, filterColumns, "region", filters.Region)
                && MatchesTextFilter(row, index, filterColumns, "employee", filters.Employee)
                && MatchesTextFilter(row, index, filterColumns, "product", filters.Product)
                && MatchesTextFilter(row, index, filterColumns, "category", filters.Category);
        });
    }

    private static bool MatchesTextFilter(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, string> filterColumns,
        string key,
        string? selectedValue)
    {
        if (string.IsNullOrWhiteSpace(selectedValue) || !filterColumns.TryGetValue(key, out var column) || !index.TryGetValue(column, out var position))
        {
            return true;
        }

        return string.Equals(ReadText(row, position), selectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static AnalyticsFilterStateDto BuildFilterState(
        string datasetName,
        string businessDomain,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, string> filterColumns,
        AnalyticsFilterDto filters)
    {
        var options = new List<AnalyticsFilterOptionDto>();
        if (filterColumns.TryGetValue("date", out var dateColumn))
        {
            options.Add(new AnalyticsFilterOptionDto("date", "Date", dateColumn, Array.Empty<string>(), null));
        }
        AddOption(options, rows, index, filterColumns, "department", "Department", filters.Department);
        AddOption(options, rows, index, filterColumns, "region", "Region", filters.Region);
        AddOption(options, rows, index, filterColumns, "employee", "Employee", filters.Employee);
        AddOption(options, rows, index, filterColumns, "product", "Product", filters.Product);
        AddOption(options, rows, index, filterColumns, "category", "Category", filters.Category);

        var activeParts = new List<string>();
        AddActivePart(activeParts, "Department", filters.Department);
        AddActivePart(activeParts, "Region", filters.Region);
        AddActivePart(activeParts, "Employee", filters.Employee);
        AddActivePart(activeParts, "Product", filters.Product);
        AddActivePart(activeParts, "Category", filters.Category);
        if (filters.From.HasValue || filters.To.HasValue)
        {
            activeParts.Add($"between {filters.From?.ToString("MMM yyyy") ?? "start"} and {filters.To?.ToString("MMM yyyy") ?? "today"}");
        }

        var summary = activeParts.Count == 0
            ? $"Showing all {businessDomain} data from {datasetName}"
            : $"Showing {businessDomain} data for {string.Join(", ", activeParts)}";

        return new AnalyticsFilterStateDto(
            filters.From,
            filters.To,
            options,
            summary,
            activeParts.Count > 0);
    }

    private static void AddOption(
        ICollection<AnalyticsFilterOptionDto> options,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        IReadOnlyDictionary<string, string> filterColumns,
        string key,
        string label,
        string? selectedValue)
    {
        if (!filterColumns.TryGetValue(key, out var column) || !index.TryGetValue(column, out var position))
        {
            return;
        }

        var values = rows
            .Select(row => ReadText(row, position))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value)
            .Take(100)
            .ToList();

        options.Add(new AnalyticsFilterOptionDto(key, label, column, values, selectedValue));
    }

    private static void AddActivePart(ICollection<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{value} {label}");
        }
    }

    private static IReadOnlyList<GeneratedKpiCardDto> BuildKpis(
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index)
    {
        var kpis = new List<GeneratedKpiCardDto>
        {
            new("Records", rows.Count.ToString("N0"), "Rows included in the uploaded dataset")
        };

        AddMeasureKpi(kpis, insight, rows, index, "Revenue", "Total Revenue");
        AddMeasureKpi(kpis, insight, rows, index, "Sales", "Total Sales");
        AddMeasureKpi(kpis, insight, rows, index, "Profit", "Total Profit");
        AddMeasureKpi(kpis, insight, rows, index, "Cost", "Total Cost");
        AddMeasureKpi(kpis, insight, rows, index, "Quantity", "Total Quantity");

        return kpis.Take(5).ToList();
    }

    private static IReadOnlyList<GeneratedChartDto> BuildTrendCharts(
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index)
    {
        var charts = new List<GeneratedChartDto>();
        AddTrendChart(charts, insight, rows, index, "Revenue", "Revenue Trend", "Revenue + Date");
        AddTrendChart(charts, insight, rows, index, "Profit", "Profit Trend", "Profit + Date");
        return charts;
    }

    private static IReadOnlyList<GeneratedChartDto> BuildComparisonCharts(
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index)
    {
        var charts = new List<GeneratedChartDto>();
        AddComparisonChart(charts, insight, rows, index, "Revenue", "Region", "Revenue by Region", "Revenue + Region");
        AddComparisonChart(charts, insight, rows, index, "Sales", "Product", "Sales by Product", "Sales + Product");
        return charts;
    }

    private static void AddMeasureKpi(
        ICollection<GeneratedKpiCardDto> kpis,
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        string entity,
        string title)
    {
        var column = FindColumn(insight.DetectedMeasures, entity);
        if (column is null || !index.TryGetValue(column, out var position))
        {
            return;
        }

        var total = rows.Sum(row => ReadDecimal(row, position));
        kpis.Add(new GeneratedKpiCardDto(title, FormatNumber(total), $"Calculated from {column}"));
    }

    private static void AddTrendChart(
        ICollection<GeneratedChartDto> charts,
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        string measureEntity,
        string title,
        string rule)
    {
        var measureColumn = FindColumn(insight.DetectedMeasures, measureEntity);
        var dateColumn = FindColumn(insight.DetectedDimensions, "Date");
        if (measureColumn is null || dateColumn is null || !index.TryGetValue(measureColumn, out var measureIndex) || !index.TryGetValue(dateColumn, out var dateIndex))
        {
            return;
        }

        var points = rows
            .Select(row => new { Date = ReadDate(row, dateIndex), Value = ReadDecimal(row, measureIndex) })
            .Where(x => x.Date.HasValue)
            .GroupBy(x => new DateTime(x.Date!.Value.Year, x.Date.Value.Month, 1))
            .OrderBy(x => x.Key)
            .Select(x => new { Label = x.Key.ToString("MMM yyyy"), Value = x.Sum(y => y.Value) })
            .ToList();

        if (points.Count == 0)
        {
            return;
        }

        charts.Add(new GeneratedChartDto(
            $"trend-{measureEntity.ToLowerInvariant()}",
            title,
            rule,
            "line",
            points.Select(x => x.Label).ToList(),
            points.Select(x => x.Value).ToList()));
    }

    private static void AddComparisonChart(
        ICollection<GeneratedChartDto> charts,
        DatasetInsightDto insight,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, int> index,
        string measureEntity,
        string dimensionEntity,
        string title,
        string rule)
    {
        var measureColumn = FindColumn(insight.DetectedMeasures, measureEntity);
        var dimensionColumn = FindColumn(insight.DetectedDimensions, dimensionEntity);
        if (measureColumn is null || dimensionColumn is null || !index.TryGetValue(measureColumn, out var measureIndex) || !index.TryGetValue(dimensionColumn, out var dimensionIndex))
        {
            return;
        }

        var points = rows
            .Select(row => new { Dimension = ReadText(row, dimensionIndex), Value = ReadDecimal(row, measureIndex) })
            .Where(x => x.Dimension.Length > 0)
            .GroupBy(x => x.Dimension, StringComparer.OrdinalIgnoreCase)
            .Select(x => new { Label = x.Key, Value = x.Sum(y => y.Value) })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        if (points.Count == 0)
        {
            return;
        }

        charts.Add(new GeneratedChartDto(
            $"comparison-{measureEntity.ToLowerInvariant()}-{dimensionEntity.ToLowerInvariant()}",
            title,
            rule,
            "bar",
            points.Select(x => x.Label).ToList(),
            points.Select(x => x.Value).ToList()));
    }

    private static IReadOnlyList<string> BuildColumns(IReadOnlyList<string> headerRow, int columnCount)
    {
        return Enumerable.Range(0, columnCount)
            .Select(index =>
            {
                var header = index < headerRow.Count ? headerRow[index].Trim() : string.Empty;
                return string.IsNullOrWhiteSpace(header) ? $"Column {index + 1}" : header;
            })
            .ToList();
    }

    private static IReadOnlyList<string> NormalizeRow(IReadOnlyList<string> row, int columnCount)
    {
        return Enumerable.Range(0, columnCount)
            .Select(index => index < row.Count ? row[index].Trim() : string.Empty)
            .ToList();
    }

    private static string? FindColumn(IEnumerable<DetectedBusinessEntityDto> entities, string entity)
    {
        return entities.FirstOrDefault(x => string.Equals(x.Entity, entity, StringComparison.OrdinalIgnoreCase))?.ColumnName;
    }

    private static decimal ReadDecimal(IReadOnlyList<string> row, int index)
    {
        return index < row.Count && decimal.TryParse(row[index], out var value) ? value : 0m;
    }

    private static DateTime? ReadDate(IReadOnlyList<string> row, int index)
    {
        return index < row.Count && DateTime.TryParse(row[index], out var value) ? value : null;
    }

    private static string ReadText(IReadOnlyList<string> row, int index)
    {
        return index < row.Count ? row[index].Trim() : string.Empty;
    }

    private static string Normalize(string value)
    {
        return value.Replace("_", " ", StringComparison.Ordinal).Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString(value % 1 == 0 ? "N0" : "N2");
    }
}

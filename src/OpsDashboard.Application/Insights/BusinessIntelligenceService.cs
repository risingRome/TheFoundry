using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;

namespace OpsDashboard.Application.Insights;

public sealed class BusinessIntelligenceService(
    IOpsDashboardDbContext db,
    IDatasetExplorerService explorer) : IBusinessIntelligenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, string[]> EntityKeywords = new Dictionary<string, string[]>
    {
        ["Revenue"] = ["revenue", "net revenue", "gross revenue", "turnover"],
        ["Sales"] = ["sales", "sale amount", "booking", "order value"],
        ["Profit"] = ["profit", "net profit", "gross profit", "earnings"],
        ["Cost"] = ["cost", "expense", "spend", "cogs"],
        ["Margin"] = ["margin", "profit margin"],
        ["Quantity"] = ["quantity", "qty", "units", "volume"],
        ["Customer"] = ["customer", "client", "account"],
        ["Product"] = ["product", "sku", "item"],
        ["Region"] = ["region", "territory", "country", "state", "city", "market"],
        ["Employee"] = ["employee", "agent", "staff", "rep", "associate"],
        ["Department"] = ["department", "dept", "function", "team"],
        ["Date"] = ["date", "day", "month", "quarter", "year", "period"]
    };

    public async Task<IReadOnlyList<DatasetInsightDto>> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        var datasetIds = await db.Datasets
            .AsNoTracking()
            .OrderByDescending(x => x.UploadDate)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var insights = new List<DatasetInsightDto>();
        foreach (var datasetId in datasetIds)
        {
            var insight = await GetInsightAsync(datasetId, cancellationToken)
                ?? await RefreshInsightAsync(datasetId, cancellationToken);
            if (insight is not null)
            {
                insights.Add(insight);
            }
        }

        return insights;
    }

    public async Task<DatasetInsightDto?> GetInsightAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        var insight = await db.DatasetInsights
            .AsNoTracking()
            .Where(x => x.DatasetId == datasetId)
            .Select(x => new
            {
                x.DatasetId,
                x.BusinessDomain,
                x.DetectedMeasuresJson,
                x.DetectedDimensionsJson,
                x.RecommendedKpisJson,
                x.RecommendedChartsJson,
                x.RecommendedDashboardLayoutJson,
                x.GeneratedAtUtc,
                DatasetName = x.Dataset!.DatasetName,
                DomainName = x.Dataset.Domain!.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        return insight is null
            ? null
            : new DatasetInsightDto(
                insight.DatasetId,
                insight.DatasetName,
                insight.DomainName,
                insight.BusinessDomain,
                insight.GeneratedAtUtc,
                DeserializeEntities(insight.DetectedMeasuresJson),
                DeserializeEntities(insight.DetectedDimensionsJson),
                DeserializeStrings(insight.RecommendedKpisJson),
                DeserializeStrings(insight.RecommendedChartsJson),
                DeserializeStrings(insight.RecommendedDashboardLayoutJson));
    }

    public async Task<DatasetInsightDto?> RefreshInsightAsync(int datasetId, CancellationToken cancellationToken = default)
    {
        var preview = await explorer.GetPreviewAsync(datasetId, 100, cancellationToken);
        if (preview is null)
        {
            return null;
        }

        var detected = DetectEntities(preview.Schema);
        var measures = detected
            .Where(x => IsMeasure(x.Entity))
            .DistinctBy(x => $"{x.Entity}:{x.ColumnName}", StringComparer.OrdinalIgnoreCase)
            .ToList();
        var dimensions = detected
            .Where(x => !IsMeasure(x.Entity))
            .DistinctBy(x => $"{x.Entity}:{x.ColumnName}", StringComparer.OrdinalIgnoreCase)
            .ToList();
        var businessDomain = DetectBusinessDomain(measures, dimensions, preview.DatasetName, preview.DomainName);
        var kpis = RecommendKpis(measures, dimensions).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var charts = RecommendCharts(measures, dimensions).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var layout = RecommendLayout(kpis, charts);

        var insight = await db.DatasetInsights.FirstOrDefaultAsync(x => x.DatasetId == datasetId, cancellationToken);
        if (insight is null)
        {
            insight = new OpsDashboard.Domain.Entities.DatasetInsight { DatasetId = datasetId };
            db.DatasetInsights.Add(insight);
        }

        insight.BusinessDomain = businessDomain;
        insight.DetectedMeasuresJson = JsonSerializer.Serialize(measures, JsonOptions);
        insight.DetectedDimensionsJson = JsonSerializer.Serialize(dimensions, JsonOptions);
        insight.RecommendedKpisJson = JsonSerializer.Serialize(kpis, JsonOptions);
        insight.RecommendedChartsJson = JsonSerializer.Serialize(charts, JsonOptions);
        insight.RecommendedDashboardLayoutJson = JsonSerializer.Serialize(layout, JsonOptions);
        insight.GeneratedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await GetInsightAsync(datasetId, cancellationToken);
    }

    private static IReadOnlyList<DetectedBusinessEntityDto> DetectEntities(IReadOnlyList<Datasets.DatasetColumnSchemaDto> schema)
    {
        var entities = new List<DetectedBusinessEntityDto>();
        foreach (var column in schema)
        {
            var normalizedColumn = Normalize(column.Name);
            foreach (var entity in EntityKeywords)
            {
                if (entity.Value.Any(keyword => normalizedColumn.Contains(Normalize(keyword), StringComparison.OrdinalIgnoreCase)))
                {
                    entities.Add(new DetectedBusinessEntityDto(entity.Key, column.Name, column.DetectedType));
                }
            }

            if (column.DetectedType == "Date" && entities.All(x => x.ColumnName != column.Name || x.Entity != "Date"))
            {
                entities.Add(new DetectedBusinessEntityDto("Date", column.Name, column.DetectedType));
            }
        }

        return entities;
    }

    private static string DetectBusinessDomain(
        IReadOnlyList<DetectedBusinessEntityDto> measures,
        IReadOnlyList<DetectedBusinessEntityDto> dimensions,
        string datasetName,
        string domainName)
    {
        var text = Normalize($"{datasetName} {domainName} {string.Join(' ', measures.Select(x => x.Entity))} {string.Join(' ', dimensions.Select(x => x.Entity))}");
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sales"] = Score(text, "sales", "revenue", "customer", "product", "region", "quantity"),
            ["Finance"] = Score(text, "finance", "profit", "cost", "margin", "revenue", "expense"),
            ["HR"] = Score(text, "hr", "employee", "department", "staff"),
            ["Operations"] = Score(text, "operations", "quantity", "cost", "region", "department", "date"),
            ["Marketing"] = Score(text, "marketing", "campaign", "channel", "lead", "customer"),
            ["Customer Support"] = Score(text, "support", "ticket", "case", "agent", "customer")
        };

        return scores.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First().Value == 0
            ? "Operations"
            : scores.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First().Key;
    }

    private static IReadOnlyList<string> RecommendKpis(
        IReadOnlyList<DetectedBusinessEntityDto> measures,
        IReadOnlyList<DetectedBusinessEntityDto> dimensions)
    {
        var kpis = new List<string>();
        foreach (var measure in measures)
        {
            kpis.AddRange(measure.Entity switch
            {
                "Revenue" => ["Total Revenue", "Revenue Trend"],
                "Sales" => ["Total Sales", "Sales Growth"],
                "Profit" => ["Profit Margin", "Profit Trend"],
                "Cost" => ["Total Cost", "Cost Trend"],
                "Margin" => ["Average Margin", "Margin Trend"],
                "Quantity" => ["Total Quantity", "Quantity Trend"],
                _ => []
            });
        }

        if (HasEntity(measures, "Revenue") && HasEntity(dimensions, "Region"))
        {
            kpis.Add("Revenue by Region");
        }

        if (HasEntity(measures, "Sales") && HasEntity(dimensions, "Product"))
        {
            kpis.Add("Sales by Product");
        }

        if (HasEntity(measures, "Profit") && HasEntity(measures, "Revenue"))
        {
            kpis.Add("Profit as % of Revenue");
        }

        return kpis.Count == 0 ? ["Record Count", "Dataset Quality Score"] : kpis;
    }

    private static IReadOnlyList<string> RecommendCharts(
        IReadOnlyList<DetectedBusinessEntityDto> measures,
        IReadOnlyList<DetectedBusinessEntityDto> dimensions)
    {
        var charts = new List<string>();
        if (HasEntity(dimensions, "Date"))
        {
            charts.Add("Line chart for trends over time");
        }

        if (dimensions.Any(x => x.Entity is "Region" or "Product" or "Department" or "Customer"))
        {
            charts.Add("Bar chart for measure by dimension");
        }

        if (HasEntity(measures, "Margin") || HasEntity(measures, "Profit"))
        {
            charts.Add("KPI cards for profitability indicators");
        }

        if (HasEntity(dimensions, "Region"))
        {
            charts.Add("Regional comparison table");
        }

        return charts.Count == 0 ? ["Summary table", "KPI card row"] : charts;
    }

    private static IReadOnlyList<string> RecommendLayout(IReadOnlyList<string> kpis, IReadOnlyList<string> charts)
    {
        return
        [
            $"Top row: {Math.Min(4, kpis.Count)} executive KPI cards",
            "Middle row: primary trend or comparison chart",
            "Bottom row: dimensional breakdown table",
            $"Recommended visual count: {Math.Min(5, kpis.Count + charts.Count)}"
        ];
    }

    private static bool IsMeasure(string entity)
    {
        return entity is "Revenue" or "Sales" or "Profit" or "Cost" or "Margin" or "Quantity";
    }

    private static bool HasEntity(IEnumerable<DetectedBusinessEntityDto> entities, string entity)
    {
        return entities.Any(x => string.Equals(x.Entity, entity, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        return value.Replace("_", " ", StringComparison.Ordinal).Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant();
    }

    private static int Score(string text, params string[] keywords)
    {
        return keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<DetectedBusinessEntityDto> DeserializeEntities(string json)
    {
        return JsonSerializer.Deserialize<IReadOnlyList<DetectedBusinessEntityDto>>(json, JsonOptions) ?? Array.Empty<DetectedBusinessEntityDto>();
    }

    private static IReadOnlyList<string> DeserializeStrings(string json)
    {
        return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions) ?? Array.Empty<string>();
    }
}

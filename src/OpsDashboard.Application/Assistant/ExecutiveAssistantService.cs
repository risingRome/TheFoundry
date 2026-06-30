using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpsDashboard.Application.Abstractions;
using OpsDashboard.Application.Analytics;
using OpsDashboard.Application.GeneratedDashboards;
using OpsDashboard.Application.Reports;
using OpsDashboard.Domain.Entities;

namespace OpsDashboard.Application.Assistant;

public sealed class ExecutiveAssistantService(
    IOpsDashboardDbContext db,
    IBusinessIntelligenceService intelligence,
    IGeneratedDashboardService dashboards,
    IExecutiveReportService reports) : IExecutiveAssistantService
{
    private static readonly IReadOnlyList<string> SuggestedQuestions =
    [
        "Summarize this dataset",
        "What are the key insights?",
        "What are the top performers?",
        "What are the lowest performers?",
        "Explain this dashboard",
        "What recommendations do you have?",
        "What is the data quality?"
    ];

    public async Task<AssistantPageDto?> GetAssistantAsync(int datasetId, AnalyticsFilterDto? filters = null, CancellationToken cancellationToken = default)
    {
        var dashboard = await dashboards.GenerateAsync(datasetId, filters, cancellationToken);
        if (dashboard is null)
        {
            return null;
        }

        var conversations = await db.AssistantConversations
            .AsNoTracking()
            .Where(x => x.DatasetId == datasetId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new AssistantExchangeDto(x.Id, x.UserMessage, x.AssistantResponse, x.CreatedAt, x.UserId))
            .ToListAsync(cancellationToken);

        return new AssistantPageDto(
            dashboard.DatasetId,
            dashboard.DatasetName,
            dashboard.DomainName,
            dashboard.Filters,
            conversations,
            SuggestedQuestions);
    }

    public async Task<AssistantExchangeDto?> AskAsync(AssistantQuestionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserMessage))
        {
            return null;
        }

        var response = await BuildResponseAsync(request.DatasetId, request.UserMessage.Trim(), request.Filters, cancellationToken);
        if (response is null)
        {
            return null;
        }

        var conversation = new AssistantConversation
        {
            DatasetId = request.DatasetId,
            UserMessage = request.UserMessage.Trim(),
            AssistantResponse = response,
            CreatedAt = DateTime.UtcNow,
            UserId = request.UserId
        };

        db.AssistantConversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return new AssistantExchangeDto(conversation.Id, conversation.UserMessage, conversation.AssistantResponse, conversation.CreatedAt, conversation.UserId);
    }

    private async Task<string?> BuildResponseAsync(int datasetId, string message, AnalyticsFilterDto filters, CancellationToken cancellationToken)
    {
        var dataset = await db.Datasets
            .AsNoTracking()
            .Include(x => x.Domain)
            .FirstOrDefaultAsync(x => x.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return null;
        }

        var insight = await intelligence.GetInsightAsync(datasetId, cancellationToken)
            ?? await intelligence.RefreshInsightAsync(datasetId, cancellationToken);
        var dashboard = await dashboards.GenerateAsync(datasetId, filters, cancellationToken);
        var report = await reports.GenerateReportAsync(datasetId, filters, cancellationToken);
        if (dashboard is null || report is null)
        {
            return null;
        }

        var normalized = message.ToLowerInvariant();
        if (ContainsAny(normalized, "summarize", "summary", "overview"))
        {
            return SummarizeDataset(dataset, dashboard, report);
        }

        if (ContainsAny(normalized, "key insight", "insight", "finding"))
        {
            return KeyInsights(insight, report);
        }

        if (ContainsAny(normalized, "top performer", "best performer", "highest", "largest contributor"))
        {
            return Performers(report, highest: true);
        }

        if (ContainsAny(normalized, "lowest performer", "lowest", "worst", "underperform"))
        {
            return Performers(report, highest: false);
        }

        if (ContainsAny(normalized, "explain", "dashboard"))
        {
            return ExplainDashboard(dashboard);
        }

        if (ContainsAny(normalized, "recommendation", "recommend", "next step"))
        {
            return Recommendations(report);
        }

        if (ContainsAny(normalized, "quality", "missing", "duplicate"))
        {
            return DataQuality(report);
        }

        return "I can answer supported rule-based questions for this dataset: summarize this dataset, key insights, top performers, lowest performers, explain this dashboard, recommendations, and data quality.";
    }

    private static string SummarizeDataset(Dataset dataset, GeneratedDashboardDto dashboard, ExecutiveReportDto report)
    {
        return string.Join(Environment.NewLine,
            $"Dataset summary for {dataset.DatasetName}:",
            $"- Domain: {dataset.Domain?.Name ?? dashboard.DomainName}; source type: {dataset.SourceType}.",
            $"- Records: {report.Summary.TotalRecords:N0}; columns: {report.Summary.TotalColumns:N0}; filtered records shown in dashboard: {dashboard.FilteredRecordCount:N0}.",
            $"- Data quality score: {report.Summary.DataQualityScore:0.##}%; missing values: {report.Summary.MissingValues:N0}; duplicate rows: {report.Summary.DuplicateRows:N0}.",
            $"- Business domain detected: {dashboard.BusinessDomain}.",
            $"- Active filter context: {dashboard.Filters.Summary}");
    }

    private static string KeyInsights(Insights.DatasetInsightDto? insight, ExecutiveReportDto report)
    {
        var lines = new List<string> { "Key insights:" };
        if (insight is not null)
        {
            lines.Add($"- Detected business domain: {insight.BusinessDomain}.");
            lines.Add($"- Measures: {JoinOrNone(insight.DetectedMeasures.Select(x => $"{x.Entity} ({x.ColumnName})"))}.");
            lines.Add($"- Dimensions: {JoinOrNone(insight.DetectedDimensions.Select(x => $"{x.Entity} ({x.ColumnName})"))}.");
        }

        lines.AddRange(report.KeyFindings.Take(5).Select(x => $"- {x}"));
        return string.Join(Environment.NewLine, lines);
    }

    private static string Performers(ExecutiveReportDto report, bool highest)
    {
        var lines = new List<string> { highest ? "Top performers:" : "Lowest performers from available ranking tables:" };
        foreach (var ranking in report.DimensionRankings.Take(3))
        {
            var rows = highest
                ? ranking.Rows.OrderBy(x => x.Rank).Take(3)
                : ranking.Rows.OrderByDescending(x => x.Rank).Take(3);
            lines.Add($"- {ranking.DimensionName} by {ranking.MeasureName}: {string.Join("; ", rows.Select(x => $"{x.Label} ({FormatDecimal(x.Value)})"))}");
        }

        if (lines.Count == 1)
        {
            lines.Add("- No dimension ranking was available for this dataset.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string ExplainDashboard(GeneratedDashboardDto dashboard)
    {
        return string.Join(Environment.NewLine,
            $"Dashboard explanation for {dashboard.DatasetName}:",
            $"- The dashboard is generated from detected {dashboard.BusinessDomain} measures and dimensions.",
            $"- KPI cards: {JoinOrNone(dashboard.Kpis.Select(x => $"{x.Title}: {x.Value}"))}.",
            $"- Trend charts: {JoinOrNone(dashboard.TrendCharts.Select(x => x.Title))}.",
            $"- Comparison charts: {JoinOrNone(dashboard.ComparisonCharts.Select(x => x.Title))}.",
            $"- Current filter context: {dashboard.Filters.Summary}.");
    }

    private static string Recommendations(ExecutiveReportDto report)
    {
        return report.Recommendations.Count == 0
            ? "No rule-based recommendations were generated for this dataset."
            : $"Recommendations:{Environment.NewLine}{string.Join(Environment.NewLine, report.Recommendations.Take(6).Select(x => $"- {x}"))}";
    }

    private static string DataQuality(ExecutiveReportDto report)
    {
        var qualityLabel = report.Summary.DataQualityScore >= 90m ? "excellent" : report.Summary.DataQualityScore >= 75m ? "usable with review" : "needs attention";
        return string.Join(Environment.NewLine,
            "Data quality:",
            $"- Quality score is {report.Summary.DataQualityScore:0.##}%, which is {qualityLabel}.",
            $"- Missing values: {report.Summary.MissingValues:N0}.",
            $"- Duplicate rows: {report.Summary.DuplicateRows:N0}.",
            report.Summary.DataQualityScore < 90m
                ? "- Improve missing or duplicate records before using this dataset for strategic decisions."
                : "- The dataset is suitable for executive review based on the current quality score.");
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(value.Contains);
    }

    private static string JoinOrNone(IEnumerable<string> values)
    {
        var list = values.Where(x => !string.IsNullOrWhiteSpace(x)).Take(6).ToList();
        return list.Count == 0 ? "none detected" : string.Join(", ", list);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("N2", CultureInfo.InvariantCulture);
    }
}

namespace OpsDashboard.Domain.Entities;

public sealed class DatasetInsight
{
    public int Id { get; set; }
    public int DatasetId { get; set; }
    public string BusinessDomain { get; set; } = string.Empty;
    public string DetectedMeasuresJson { get; set; } = "[]";
    public string DetectedDimensionsJson { get; set; } = "[]";
    public string RecommendedKpisJson { get; set; } = "[]";
    public string RecommendedChartsJson { get; set; } = "[]";
    public string RecommendedDashboardLayoutJson { get; set; } = "[]";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public Dataset? Dataset { get; set; }
}
